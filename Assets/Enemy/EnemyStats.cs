using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(EnemyAI))]
public class EnemyStats : NetworkBehaviour, ICharacterStats
{
    [Header("Base Stats")]
    [SyncVar] private int _health;
    [SyncVar] private int _maxHealth;
    [SyncVar] private int _mana;
    [SyncVar] private int _maxMana;
    [SyncVar] private int _stamina;
    [SyncVar] private int _maxStamina;

    [SyncVar] private int _strength;
    [SyncVar] private int _agility;
    [SyncVar] private int _constitution;
    [SyncVar] private int _spirit;
    [SyncVar] private int _accuracy;
    [SyncVar] private int _luck;

    [SyncVar] private int _attack;
    [SyncVar] private int _defense;
    [SyncVar] private int _magicAttack;
    [SyncVar] private int _magicDefense;
    [SyncVar] private float _attackSpeed = 1f;
    [SyncVar] private float _moveSpeed = 3.5f;

    [SyncVar] private int _level = 1;
    [SyncVar] private int _experienceReward = 10;
    [SyncVar] private int _goldReward = 5;

    [Header("Config")]
    [SerializeField] private string enemyName = "Mob";
    [SerializeField] private bool showDebugLogs = true;

    // Properties
    public int Health => _health;
    public int MaxHealth => _maxHealth;
    public int Mana => _mana;
    public int MaxMana => _maxMana;
    public int Stamina => _stamina;
    public int MaxStamina => _maxStamina;
    public int Strength => _strength;
    public int Agility => _agility;
    public int Constitution => _constitution;
    public int Spirit => _spirit;
    public int Accuracy => _accuracy;
    public int Luck => _luck;
    public int Attack => _attack;
    public int Defense => _defense;
    public int MagicAttack => _magicAttack;
    public int MagicDefense => _magicDefense;
    public float AttackSpeed => _attackSpeed;
    public float MoveSpeed => _moveSpeed;
    public int Level => _level;
    public bool IsDead => _health <= 0;
    public int ExperienceReward => _experienceReward;
    public int GoldReward => _goldReward;
    public string EnemyName => enemyName;

    // Events
    public event Action OnHealthUpdated;
    public event Action OnManaUpdated;
    public event Action OnStaminaUpdated;
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action OnStatsChanged;

    // Modifiers
    private List<StatModifier> activeModifiers = new List<StatModifier>();

    void Start()
    {
        if (isServer)
        {
            InitializeStats();
        }
    }

    [Server]
    public void InitializeStats()
    {
        _maxHealth = 100 + (_constitution * 10) + (_level * 5);
        _maxMana = 50 + (_spirit * 8);
        _maxStamina = 100 + (_constitution * 5);

        _attack = (_strength * 2) + _agility;
        _defense = _constitution + (_agility / 2);
        _magicAttack = (_spirit * 2);
        _magicDefense = _spirit + (_constitution / 2);

        _health = _maxHealth;
        _mana = _maxMana;
        _stamina = _maxStamina;

        if (showDebugLogs)
            Debug.Log("[EnemyStats] " + enemyName + " (netId=" + netId + ") inicializado | HP=" + _health + "/" + _maxHealth + " | ATK=" + _attack + " | DEF=" + _defense);
    }

    [Server]
    public void SetBaseStats(int str, int agi, int con, int spr, int acc, int luck, int lvl)
    {
        _strength = str;
        _agility = agi;
        _constitution = con;
        _spirit = spr;
        _accuracy = acc;
        _luck = luck;
        _level = lvl;
        InitializeStats();
    }

    #region ICharacterStats Implementation

    [Server]
    public void TakeDamage(int damage, uint attackerId, DamageType damageType = DamageType.Physical)
    {
        if (showDebugLogs)
        {
            Debug.Log("[EnemyStats] ===============================================");
            Debug.Log("[EnemyStats] TakeDamage CHAMADO em " + enemyName + " (netId=" + netId + ")");
            Debug.Log("[EnemyStats]   -> dmg=" + damage + " | attackerId=" + attackerId + " | damageType=" + damageType);
            Debug.Log("[EnemyStats]   -> isDead=" + IsDead + " | HP antes=" + _health + "/" + _maxHealth);
        }

        if (IsDead)
        {
            if (showDebugLogs) Debug.Log("[EnemyStats] TakeDamage IGNORADO: " + enemyName + " ja esta morto");
            return;
        }

        int finalDamage = damage;
        if (damageType == DamageType.Physical)
            finalDamage = Mathf.Max(1, damage - Defense);
        else if (damageType == DamageType.Magical)
            finalDamage = Mathf.Max(1, damage - MagicDefense);

        _health = Mathf.Max(0, _health - finalDamage);

        if (showDebugLogs)
            Debug.Log("[EnemyStats] " + enemyName + " HP depois=" + _health + " | danoFinal=" + finalDamage);

        RpcOnDamageTaken(finalDamage, attackerId);
        OnHealthUpdated?.Invoke();

        if (_health <= 0)
        {
            if (showDebugLogs)
                Debug.Log("[EnemyStats] " + enemyName + " MORREU! killerId=" + attackerId);
            Die(attackerId);
        }
    }

    [Server]
    public void TakeTrueDamage(int damage, uint attackerId)
    {
        if (showDebugLogs)
            Debug.Log("[EnemyStats] TakeTrueDamage em " + enemyName + " | dmg=" + damage);
        if (IsDead) return;
        _health = Mathf.Max(0, _health - damage);
        RpcOnDamageTaken(damage, attackerId);
        OnHealthUpdated?.Invoke();
        if (_health <= 0) Die(attackerId);
    }

    [Server]
    public void Heal(int amount)
    {
        if (IsDead) return;
        _health = Mathf.Min(_maxHealth, _health + amount);
        OnHealthUpdated?.Invoke();
    }

    [Server]
    public void RestoreMana(int amount)
    {
        _mana = Mathf.Clamp(_mana + amount, 0, _maxMana);
        OnManaUpdated?.Invoke();
    }

    [Server]
    public void RestoreStamina(int amount)
    {
        _stamina = Mathf.Clamp(_stamina + amount, 0, _maxStamina);
        OnStaminaUpdated?.Invoke();
    }

    [Server]
    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        RecalculateStats();
    }

    [Server]
    public void RemoveModifier(StatModifier modifier)
    {
        activeModifiers.Remove(modifier);
        RecalculateStats();
    }

    [Server]
    public void ClearModifiers()
    {
        activeModifiers.Clear();
        RecalculateStats();
    }

    #endregion

    #region Attack

    /// <summary>
    /// Chamado pelo EnemyAI quando o mob chega na range de ataque.
    /// Aplica dano REAL no alvo (player) via ICharacterStats.TakeDamage.
    /// </summary>
    [Server]
    public void PerformAttack(GameObject target)
    {
        if (showDebugLogs)
        {
            Debug.Log("[EnemyStats] ===============================================");
            Debug.Log("[EnemyStats] PerformAttack() chamado");
            Debug.Log("[EnemyStats]   Atacante: " + gameObject.name);
            Debug.Log("[EnemyStats]   Alvo: " + (target != null ? target.name : "NULL"));
            Debug.Log("[EnemyStats] ===============================================");
        }

        if (IsDead)
        {
            Debug.LogWarning("[EnemyStats] PerformAttack CANCELADO: " + enemyName + " esta morto");
            return;
        }

        if (target == null)
        {
            Debug.LogWarning("[EnemyStats] PerformAttack CANCELADO: target eh NULL");
            return;
        }

        // Tenta pegar ICharacterStats no alvo (PlayerStats ou outro mob)
        ICharacterStats targetStats = target.GetComponent<ICharacterStats>();
        if (targetStats == null)
        {
            Debug.LogError("[EnemyStats] PerformAttack FALHOU: " + target.name + " nao tem ICharacterStats!");
            return;
        }

        if (targetStats.IsDead)
        {
            Debug.Log("[EnemyStats] PerformAttack CANCELADO: alvo " + target.name + " ja esta morto");
            return;
        }

        // Calcula dano do mob
        int damage = CalculateDamage();
        uint attackerNetId = netId;

        if (showDebugLogs)
            Debug.Log("[EnemyStats] " + enemyName + " causando " + damage + " de dano em " + target.name + " (netId=" + target.GetComponent<NetworkIdentity>()?.netId + ")");

        // APLICA DANO REAL NO ALVO
        targetStats.TakeDamage(damage, attackerNetId, DamageType.Physical);

        // Notifica clientes para tocar animacao de hit no alvo
        NetworkIdentity targetNetId = target.GetComponent<NetworkIdentity>();
        if (targetNetId != null)
        {
            RpcOnAttackLanded(targetNetId.netId, damage);
        }
    }

    [Server]
    private int CalculateDamage()
    {
        int baseDamage = Attack;
        float variance = UnityEngine.Random.Range(0.9f, 1.1f);
        int finalDamage = Mathf.RoundToInt(baseDamage * variance);
        return Mathf.Max(1, finalDamage);
    }

    [ClientRpc]
    private void RpcOnAttackLanded(uint targetNetId, int damage)
    {
        // Client-side: pode tocar som, particula, etc.
        if (showDebugLogs)
            Debug.Log("[EnemyStats] RpcOnAttackLanded: dano " + damage + " no netId=" + targetNetId);
    }

    #endregion

    #region Death

    [Server]
    private void Die(uint killerId)
    {
        Debug.Log("[EnemyStats] Die() chamado em " + enemyName + " | killerId=" + killerId);

        OnDeath?.Invoke();
        RpcOnDeath();

        // Recompensa XP/Gold para o killer
        if (killerId != 0 && NetworkServer.spawned.TryGetValue(killerId, out NetworkIdentity killerIdentity))
        {
            PlayerStats killerStats = killerIdentity.GetComponent<PlayerStats>();
            if (killerStats != null)
            {
                killerStats.AddExperience(_experienceReward);
                killerStats.AddGold(_goldReward);
                Debug.Log("[EnemyStats] Recompensa: " + _experienceReward + " XP, " + _goldReward + " Gold para " + killerStats.CharacterName);
            }
        }

        // Despawn ou destruir apos delay
        StartCoroutine(DespawnAfterDelay(5f));
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        Debug.Log("[EnemyStats] RpcOnDeath em " + enemyName);
        GetComponent<Animator>()?.SetTrigger("Die");
        GetComponent<EnemyAI>()?.SetEnabled(false);
    }

    [Server]
    private System.Collections.IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (gameObject != null)
        {
            Debug.Log("[EnemyStats] Destruindo " + enemyName + " apos " + delay + "s");
            NetworkServer.Destroy(gameObject);
        }
    }

    #endregion

    #region Helpers

    [Server]
    private void RecalculateStats()
    {
        // Recalcula bonus de modifiers se necessario
        OnStatsChanged?.Invoke();
    }

    [ClientRpc]
    private void RpcOnDamageTaken(int damage, uint attackerId)
    {
        DamagePopupManager.Instance?.ShowDamage(transform.position + Vector3.up * 2f, damage, false);
    }

    #endregion
}
