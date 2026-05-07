using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TOP.Core;
using TOP.Database;

namespace TOP.Player
{
    public class PlayerSkills : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnSkillsDataChanged))] 
        private string _skillsData = "";

        private readonly Dictionary<int, int> _skillLevels = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _skillCooldowns = new Dictionary<int, float>();
        private PlayerStats _stats;
        private PlayerAnimation _animation;
        private PlayerCombat _combat;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _animation = GetComponent<PlayerAnimation>();
            _combat = GetComponent<PlayerCombat>();
        }

        void Update()
        {
            List<int> keys = new List<int>(_skillCooldowns.Keys);
            foreach (int key in keys)
            {
                if (_skillCooldowns[key] > 0)
                    _skillCooldowns[key] -= Time.deltaTime;
            }
        }

        [Server]
        public void LearnSkill(int skillId, int level)
        {
            _skillLevels[skillId] = level;
            SerializeSkills();
        }

        [Server]
        public void UseSkill(int skillId, Vector3 targetPosition, NetworkIdentity target)
        {
            if (!_skillLevels.ContainsKey(skillId)) return;

            SkillData skillData = SkillDatabase.Instance?.GetSkill(skillId);
            if (skillData == null) return;

            if (_skillCooldowns.ContainsKey(skillId) && _skillCooldowns[skillId] > 0)
                return;

            if (_stats.CurrentMp < skillData.MpCost || _stats.CurrentSp < skillData.SpCost)
                return;

            _stats.ConsumeMp(skillData.MpCost);
            _stats.ConsumeSp(skillData.SpCost);

            _skillCooldowns[skillId] = skillData.Cooldown;

            ExecuteSkill(skillData, targetPosition, target);

            _animation.RpcTriggerSkill(skillId);
        }

        [Server]
        void ExecuteSkill(SkillData data, Vector3 targetPosition, NetworkIdentity target)
        {
            switch (data.TargetType)
            {
                case TargetType.Self:
                    ApplySkillEffect(data, netId);
                    break;

                case TargetType.Single:
                    if (target != null)
                        ApplySkillEffect(data, target.netId);
                    break;

                case TargetType.AoE:
                    // TODO: Area of effect
                    break;

                case TargetType.Line:
                    // TODO: Line projectile
                    break;
            }

            if (data.ProjectilePrefab != null)
            {
                GameObject projectile = Instantiate(data.ProjectilePrefab, transform.position + Vector3.up, Quaternion.identity);
                SkillProjectile proj = projectile.GetComponent<SkillProjectile>();
                if (proj != null)
                {
                    proj.Initialize(data, targetPosition, target);
                }
                NetworkServer.Spawn(projectile);
            }
        }

        [Server]
        void ApplySkillEffect(SkillData data, uint targetNetId)
        {
            NetworkIdentity targetObj = NetworkServer.spawned[targetNetId];
            if (targetObj == null) return;

            PlayerStats targetStats = targetObj.GetComponent<PlayerStats>();
            if (targetStats == null) return;

            int damage = CalculateSkillDamage(data);

            if (data.DamageType == DamageType.Heal)
            {
                targetStats.Heal(damage);
            }
            else
            {
                targetStats.TakeDamage(damage);
            }
        }

        [Server]
        int CalculateSkillDamage(SkillData data)
        {
            int baseDamage = data.BaseDamage;
            int level = _skillLevels.ContainsKey(data.SkillId) ? _skillLevels[data.SkillId] : 1;

            float multiplier = data.DamageMultiplier * level;
            int finalDamage = Mathf.RoundToInt(baseDamage * multiplier);

            if (data.DamageType == DamageType.Physical)
                finalDamage += _stats.PhysicalAttack;
            else if (data.DamageType == DamageType.Magic)
                finalDamage += _stats.MagicAttack;

            return Mathf.Max(1, finalDamage);
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

        void OnSkillsDataChanged(string oldValue, string newValue)
        {
            // Atualizar UI de skills
        }

        public bool HasSkill(int skillId) => _skillLevels.ContainsKey(skillId);
        public int GetSkillLevel(int skillId) => _skillLevels.ContainsKey(skillId) ? _skillLevels[skillId] : 0;
        public float GetCooldown(int skillId) => _skillCooldowns.ContainsKey(skillId) ? _skillCooldowns[skillId] : 0;
    }
}
