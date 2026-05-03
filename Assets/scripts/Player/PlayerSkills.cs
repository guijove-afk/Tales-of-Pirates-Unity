using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

public class PlayerSkills : NetworkBehaviour
{
    [Header("Skill Settings")]
    [SerializeField] private List<PlayerSkillData> skills = new List<PlayerSkillData>();
    [SerializeField] private float globalCooldown = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showSkillDebugLogs = true;

    // Estado
    private float lastSkillTime;
    private Dictionary<int, float> skillCooldowns = new Dictionary<int, float>();
    private PlayerStats playerStats;

    public event Action<SkillData, float> OnSkillCastStarted;
    public event Action OnSkillCastFinished;
    public event Action<SkillData> OnSkillExecuted;

    void Awake()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Hotkeys de skills (1-4)
        for (int i = 0; i < skills.Count && i < 4; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                TryUseSkill(i);
            }
        }
    }

    #region Skill Usage

    [Client]
    public void TryUseSkill(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: indice " + skillIndex + " invalido");
            return;
        }

        PlayerSkillData skill = skills[skillIndex];
        if (skill == null)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: skill no indice " + skillIndex + " eh null");
            return;
        }

        if (playerStats.IsDead)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: player esta morto");
            return;
        }

        // Verifica cooldown global
        if (Time.time < lastSkillTime + globalCooldown)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: global cooldown");
            return;
        }

        // Verifica cooldown da skill
        if (skillCooldowns.TryGetValue(skillIndex, out float cooldownEnd) && Time.time < cooldownEnd)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: skill em cooldown (" + (cooldownEnd - Time.time).ToString("F1") + "s restantes)");
            return;
        }

        // Verifica mana
        if (playerStats.Mana < skill.manaCost)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: mana insuficiente (" + playerStats.Mana + "/" + skill.manaCost + ")");
            return;
        }

        // Verifica stamina
        if (playerStats.Stamina < skill.staminaCost)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: stamina insuficiente (" + playerStats.Stamina + "/" + skill.staminaCost + ")");
            return;
        }

        // Verifica level
        if (playerStats.Level < skill.requiredLevel)
        {
            if (showSkillDebugLogs) Debug.Log("[PlayerSkills] TryUseSkill CANCELADO: level insuficiente (" + playerStats.Level + "/" + skill.requiredLevel + ")");
            return;
        }

        if (showSkillDebugLogs)
        {
            Debug.Log("[PlayerSkills] ===============================================");
            Debug.Log("[PlayerSkills] TryUseSkill() -> " + skill.skillName);
            Debug.Log("[PlayerSkills]   Indice: " + skillIndex);
            Debug.Log("[PlayerSkills]   Mana: " + skill.manaCost);
            Debug.Log("[PlayerSkills] ===============================================");
        }

        lastSkillTime = Time.time;
        skillCooldowns[skillIndex] = Time.time + skill.cooldown;

        // Envia comando para servidor
        CmdUseSkill(skillIndex, transform.position, transform.forward);
    }

    [Client]
    public void TryUseSkill(SkillData skill)
    {
        if (skill == null) return;
        OnSkillCastStarted?.Invoke(skill, skill.castTime);
        OnSkillCastFinished?.Invoke();
        OnSkillExecuted?.Invoke(skill);
    }

    [Command]
    private void CmdUseSkill(int skillIndex, Vector3 castPos, Vector3 castDir)
    {
        if (showSkillDebugLogs)
            Debug.Log("[PlayerSkills] CmdUseSkill recebido | skillIndex=" + skillIndex + " | isDead=" + playerStats.IsDead);

        if (playerStats.IsDead)
        {
            Debug.LogWarning("[PlayerSkills] CmdUseSkill CANCELADO: player morto");
            return;
        }

        if (skillIndex < 0 || skillIndex >= skills.Count)
        {
            Debug.LogWarning("[PlayerSkills] CmdUseSkill CANCELADO: indice invalido " + skillIndex);
            return;
        }

        PlayerSkillData skill = skills[skillIndex];
        if (skill == null)
        {
            Debug.LogWarning("[PlayerSkills] CmdUseSkill CANCELADO: skill null no indice " + skillIndex);
            return;
        }

        // Consome recursos
        playerStats.RestoreMana(-skill.manaCost);
        playerStats.RestoreStamina(-skill.staminaCost);

        // Executa efeito da skill
        ExecuteSkillEffect(skill, castPos, castDir);

        // Notifica clientes
        RpcOnSkillUsed(skillIndex, castPos, castDir);
    }

    [Server]
    private void ExecuteSkillEffect(PlayerSkillData skill, Vector3 castPos, Vector3 castDir)
    {
        if (showSkillDebugLogs)
            Debug.Log("[PlayerSkills] ExecuteSkillEffect: " + skill.skillName + " | type=" + skill.skillType);

        switch (skill.skillType)
        {
            case PlayerSkillType.SingleTarget:
                ExecuteSingleTargetSkill(skill, castPos, castDir);
                break;
            case PlayerSkillType.AreaOfEffect:
                ExecuteAOESkill(skill, castPos, castDir);
                break;
            case PlayerSkillType.SelfBuff:
                ExecuteSelfBuffSkill(skill);
                break;
            case PlayerSkillType.Projectile:
                ExecuteProjectileSkill(skill, castPos, castDir);
                break;
            default:
                Debug.LogWarning("[PlayerSkills] Tipo de skill nao implementado: " + skill.skillType);
                break;
        }
    }

    [Server]
    private void ExecuteSingleTargetSkill(PlayerSkillData skill, Vector3 castPos, Vector3 castDir)
    {
        // Raycast para encontrar alvo
        if (Physics.Raycast(castPos + Vector3.up, castDir, out RaycastHit hit, skill.range, LayerMask.GetMask("Enemy")))
        {
            ICharacterStats targetStats = hit.collider.GetComponent<ICharacterStats>();
            if (targetStats != null && !targetStats.IsDead)
            {
                int damage = CalculateSkillDamage(skill);
                targetStats.TakeDamage(damage, netId, skill.damageType);
                Debug.Log("[PlayerSkills] SingleTarget: " + skill.skillName + " causou " + damage + " em " + hit.collider.name);
            }
            else
            {
                Debug.Log("[PlayerSkills] SingleTarget: alvo " + hit.collider.name + " nao tem ICharacterStats ou esta morto");
            }
        }
        else
        {
            Debug.Log("[PlayerSkills] SingleTarget: nenhum alvo encontrado no raycast");
        }
    }

    [Server]
    private void ExecuteAOESkill(PlayerSkillData skill, Vector3 castPos, Vector3 castDir)
    {
        Vector3 center = castPos + castDir * skill.range * 0.5f;
        Collider[] hits = Physics.OverlapSphere(center, skill.aoeRadius, LayerMask.GetMask("Enemy"));

        Debug.Log("[PlayerSkills] AOE: " + skill.skillName + " | center=" + center + " | radius=" + skill.aoeRadius + " | hits=" + hits.Length);

        foreach (var hit in hits)
        {
            ICharacterStats targetStats = hit.GetComponent<ICharacterStats>();
            if (targetStats != null && !targetStats.IsDead)
            {
                int damage = CalculateSkillDamage(skill);
                targetStats.TakeDamage(damage, netId, skill.damageType);
                Debug.Log("[PlayerSkills] AOE hit: " + hit.name + " recebeu " + damage + " dmg");
            }
        }
    }

    [Server]
    private void ExecuteSelfBuffSkill(PlayerSkillData skill)
    {
        Debug.Log("[PlayerSkills] SelfBuff: " + skill.skillName + " aplicado em " + gameObject.name);

        foreach (var modifier in skill.statModifiers)
        {
            playerStats.AddModifier(modifier);
        }

        // Remove buffs apos duracao
        if (skill.buffDuration > 0)
        {
            StartCoroutine(RemoveBuffAfterDelay(skill));
        }
    }

    [Server]
    private System.Collections.IEnumerator RemoveBuffAfterDelay(PlayerSkillData skill)
    {
        yield return new WaitForSeconds(skill.buffDuration);
        foreach (var modifier in skill.statModifiers)
        {
            playerStats.RemoveModifier(modifier);
        }
        Debug.Log("[PlayerSkills] Buff " + skill.skillName + " expirou");
    }

    [Server]
    private void ExecuteProjectileSkill(PlayerSkillData skill, Vector3 castPos, Vector3 castDir)
    {
        // Instancia projetil no servidor
        if (skill.projectilePrefab != null)
        {
            GameObject proj = Instantiate(skill.projectilePrefab, castPos + Vector3.up, Quaternion.LookRotation(castDir));
            NetworkServer.Spawn(proj);
            Debug.Log("[PlayerSkills] Projectile: " + skill.skillName + " spawnado");
        }
        else
        {
            Debug.LogWarning("[PlayerSkills] Projectile: " + skill.skillName + " nao tem projectilePrefab!");
        }
    }

    [Server]
    private int CalculateSkillDamage(PlayerSkillData skill)
    {
        int baseDamage = skill.baseDamage;

        // Adiciona scaling de stats
        switch (skill.scalingStat)
        {
            case StatType.STR:
                baseDamage += Mathf.RoundToInt(playerStats.Strength * skill.scalingFactor);
                break;
            case StatType.AGI:
                baseDamage += Mathf.RoundToInt(playerStats.Agility * skill.scalingFactor);
                break;
            case StatType.SPR:
                baseDamage += Mathf.RoundToInt(playerStats.Spirit * skill.scalingFactor);
                break;
            case StatType.Attack:
                baseDamage += Mathf.RoundToInt(playerStats.Attack * skill.scalingFactor);
                break;
            case StatType.MagicAttack:
                baseDamage += Mathf.RoundToInt(playerStats.MagicAttack * skill.scalingFactor);
                break;
        }

        float variance = UnityEngine.Random.Range(0.9f, 1.1f);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * variance));
    }

    [ClientRpc]
    private void RpcOnSkillUsed(int skillIndex, Vector3 castPos, Vector3 castDir)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count) return;
        PlayerSkillData skill = skills[skillIndex];
        if (skill == null) return;

        if (showSkillDebugLogs)
            Debug.Log("[PlayerSkills] RpcOnSkillUsed: " + skill.skillName);

        // Efeitos visuais client-side
        if (skill.castEffectPrefab != null)
        {
            Instantiate(skill.castEffectPrefab, castPos, Quaternion.identity);
        }

        // Animacao
        GetComponent<PlayerAnimation>()?.SetTrigger("Skill" + skillIndex);
    }

    #endregion

    #region Public API

    public float GetSkillCooldownRemaining(int skillIndex)
    {
        if (skillCooldowns.TryGetValue(skillIndex, out float endTime))
            return Mathf.Max(0, endTime - Time.time);
        return 0;
    }

    public bool IsSkillReady(int skillIndex)
    {
        if (skillIndex < 0 || skillIndex >= skills.Count) return false;
        if (playerStats.IsDead) return false;

        PlayerSkillData skill = skills[skillIndex];
        if (skill == null) return false;
        if (playerStats.Mana < skill.manaCost) return false;
        if (playerStats.Stamina < skill.staminaCost) return false;
        if (playerStats.Level < skill.requiredLevel) return false;
        if (GetSkillCooldownRemaining(skillIndex) > 0) return false;
        if (Time.time < lastSkillTime + globalCooldown) return false;

        return true;
    }

    public List<PlayerSkillData> GetSkills() => skills;

    #endregion
}

// Enums e Data
public enum PlayerSkillType
{
    SingleTarget,
    AreaOfEffect,
    SelfBuff,
    Projectile
}

[CreateAssetMenu(fileName = "NewPlayerSkill", menuName = "Tales of Pirates/Player Skill Data")]
public class PlayerSkillData : ScriptableObject
{
    public string skillName = "Skill";
    public string description = "";
    public Sprite icon;
    public PlayerSkillType skillType = PlayerSkillType.SingleTarget;
    public DamageType damageType = DamageType.Physical;

    [Header("Costs")]
    public int manaCost = 10;
    public int staminaCost = 5;
    public int requiredLevel = 1;
    public float cooldown = 5f;

    [Header("Damage")]
    public int baseDamage = 20;
    public StatType scalingStat = StatType.STR;
    public float scalingFactor = 1.0f;

    [Header("AOE")]
    public float range = 10f;
    public float aoeRadius = 3f;

    [Header("Buff")]
    public float buffDuration = 10f;
    public List<StatModifier> statModifiers = new List<StatModifier>();

    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Header("Effects")]
    public GameObject castEffectPrefab;
    public GameObject hitEffectPrefab;
}
