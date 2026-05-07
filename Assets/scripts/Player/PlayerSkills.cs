using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TOP.Core;
using System.Linq;  // ✅ para Resources.LoadAll
using System;

namespace TOP.Player
{
    public class PlayerSkills : NetworkBehaviour
    {
        public event Action<SkillData, float> OnSkillCastStarted;
        public event Action OnSkillCastFinished;
        public event Action<SkillData> OnSkillExecuted;

        [SyncVar(hook = nameof(OnSkillsDataChanged))] 
        private string _skillsData = "";

        private readonly Dictionary<int, int> _skillLevels = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _skillCooldowns = new Dictionary<int, float>();
        private PlayerStats _stats;
        private PlayerAnimation _animation;
        private PlayerCombat _combat;
        private SkillData[] _allSkills;  // ✅ Cache de todas skills

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animation = GetComponent<PlayerAnimation>();
            _combat = GetComponent<PlayerCombat>();
        }

        void Start()
        {
            LoadAllSkills();  // ✅ Carrega todas skills como ItemDatabase
        }

        void Update()
        {
            // Atualiza cooldowns
            List<int> keys = new List<int>(_skillCooldowns.Keys);
            foreach (int key in keys)
            {
                if (_skillCooldowns[key] > 0)
                    _skillCooldowns[key] -= Time.deltaTime;
            }
        }
// No arquivo PlayerSkills.cs
public void TryUseSkill(int skillId)
{
    // Se for um comando de rede:
    CmdUseSkill(skillId); 
}

[Command]
public void CmdUseSkill(int skillId)
{
    // Lógica para verificar cooldown, MP e executar a skill
    Debug.Log($"Executando skill ID: {skillId}");
}
        // ✅ Carrega skills automaticamente (igual ItemDatabase)
        void LoadAllSkills()
        {
            _allSkills = Resources.LoadAll<SkillData>("Skills");
            Debug.Log($"[PlayerSkills] Carregadas {_allSkills.Length} skills de Resources/Skills");
        }

        SkillData GetSkillData(int skillId)
        {
            if (_allSkills == null) LoadAllSkills();
            
            return _allSkills.FirstOrDefault(s => s.skillId == skillId);
        }

        [Command]
        public void CmdUseSkill(int skillId, Vector3 targetPosition)
        {
            UseSkill(skillId, targetPosition, null);
        }

        [Server]
        public void UseSkill(int skillId, Vector3 targetPosition, NetworkIdentity target = null)
        {
            if (!_skillLevels.ContainsKey(skillId)) 
            {
                Debug.LogWarning($"[PlayerSkills] Skill {skillId} não aprendida!");
                return;
            }

            SkillData skillData = GetSkillData(skillId);
            if (skillData == null) 
            {
                Debug.LogWarning($"[PlayerSkills] SkillData {skillId} não encontrada!");
                return;
            }

            // Verifica cooldown
            if (_skillCooldowns.ContainsKey(skillId) && _skillCooldowns[skillId] > 0)
            {
                Debug.Log($"[PlayerSkills] Skill {skillData.skillName} em cooldown!");
                return;
            }

            // Verifica custo
            if (_stats.CurrentMp < skillData.mpCost || _stats.CurrentSp < skillData.spCost)
            {
                Debug.Log($"[PlayerSkills] MP insuficiente: {_stats.CurrentMp}/{skillData.mpCost}");
                return;
            }

            // Consome recursos
            _stats.ConsumeMp(skillData.mpCost);
            _stats.ConsumeSp(skillData.spCost);

            // Aplica cooldown
            _skillCooldowns[skillId] = skillData.cooldown;

            // Executa skill
            OnSkillCastStarted?.Invoke(skillData, skillData.castTime);
            ExecuteSkill(skillData, targetPosition, target);
            OnSkillExecuted?.Invoke(skillData);

            // Animação
            _animation?.RpcTriggerSkill(skillId);
            
            Debug.Log($"[PlayerSkills] ✅ {skillData.skillName} executada!");
        }

        [Server]
        void ExecuteSkill(SkillData data, Vector3 targetPosition, NetworkIdentity target)
        {
            // Efeitos visuais
            if (data.castEffect != null)
            {
                var effect = Instantiate(data.castEffect, transform.position, Quaternion.identity);
                Destroy(effect.gameObject, 3f);
            }

            switch (data.targetType)
            {
                case SkillTargetType.Self:
                    ApplySkillEffect(data, netId);
                    break;

                case SkillTargetType.SingleEnemy:
                    if (target != null)
                        ApplySkillEffect(data, target.netId);
                    break;

                case SkillTargetType.AreaEnemy:
                    // TODO: AoE
                    break;
            }

            // Projétil
            if (data.isProjectile && data.projectilePrefab != null)
            {
                GameObject projectile = Instantiate(data.projectilePrefab, transform.position + Vector3.up, Quaternion.identity);
                // SkillProjectile proj = projectile.GetComponent<SkillProjectile>();
                // if (proj != null) proj.Initialize(data, netId, target?.netId ?? 0);
                NetworkServer.Spawn(projectile);
            }
        }

        [Server]
        void ApplySkillEffect(SkillData data, uint targetNetId)
        {
            if (!NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetObj))
                return;

            PlayerStats targetStats = targetObj.GetComponent<PlayerStats>();
            if (targetStats == null) return;

            int damage = CalculateSkillDamage(data);

            if (data.healAmount > 0)
            {
                targetStats.Heal(damage);
            }
            else
            {
                targetStats.TakeDamage(damage, netId);
            }
        }

        [Server]
        int CalculateSkillDamage(SkillData data)
        {
            int level = _skillLevels.ContainsKey(data.skillId) ? _skillLevels[data.skillId] : 1;
            float levelMultiplier = Mathf.Pow(data.damagePerLevel, level - 1);
            
            int finalDamage = Mathf.RoundToInt(data.baseDamage * data.damageMultiplier * levelMultiplier * data.elementMultiplier);

            if (data.element == SkillElement.None)
                finalDamage += _stats.PhysicalAttack;
            else
                finalDamage += _stats.MagicAttack;

            return Mathf.Max(1, finalDamage);
        }

        [Server]
        public void LearnSkill(int skillId, int level = 1)
        {
            _skillLevels[skillId] = level;
            SerializeSkills();
            Debug.Log($"[PlayerSkills] Skill {skillId} aprendida nível {level}");
        }

        [Server]
        void SerializeSkills()
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<int, int> kvp in _skillLevels)
            {
                list.Add($"{kvp.Key}:{kvp.Value}");
            }
            _skillsData = string.Join(";", list);
        }

        // Getters públicos
        public bool HasSkill(int skillId) => _skillLevels.ContainsKey(skillId);
        public int GetSkillLevel(int skillId) => _skillLevels.ContainsKey(skillId) ? _skillLevels[skillId] : 0;
        public float GetCooldown(int skillId) => _skillCooldowns.ContainsKey(skillId) ? _skillCooldowns[skillId] : 0f;

        void OnSkillsDataChanged(string oldValue, string newValue) { }
    }
}