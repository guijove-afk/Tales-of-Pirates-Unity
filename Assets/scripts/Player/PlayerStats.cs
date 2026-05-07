using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class PlayerStats : NetworkBehaviour, ICharacterStats
{
    #region SyncVars
    [SyncVar(hook = nameof(OnHealthChanged))] private int _health = 100;
    [SyncVar(hook = nameof(OnMaxHealthChanged))] private int _maxHealth = 100;
    [SyncVar(hook = nameof(OnManaChanged))] private int _mana = 50;
    [SyncVar(hook = nameof(OnMaxManaChanged))] private int _maxMana = 50;
    [SyncVar(hook = nameof(OnStaminaChanged))] private int _stamina = 100;
    [SyncVar(hook = nameof(OnMaxStaminaChanged))] private int _maxStamina = 100;

    [SyncVar] private int _strength = 10;
    [SyncVar] private int _agility = 10;
    [SyncVar] private int _constitution = 10;
    [SyncVar] private int _spirit = 10;
    [SyncVar] private int _accuracy = 10;
    [SyncVar] private int _luck = 5;

    [SyncVar] private int _attack = 10;
    [SyncVar] private int _defense = 5;
    [SyncVar] private int _magicAttack = 5;
    [SyncVar] private int _magicDefense = 5;
    [SyncVar] private float _attackSpeed = 1f;
    [SyncVar] private float _moveSpeed = 5f;

    [SyncVar(hook = nameof(OnLevelChanged))] private int _level = 1;
    [SyncVar] private int _experience = 0;
    [SyncVar] private int _statPoints = 0;
    [SyncVar] private int _skillPoints = 0;
    [SyncVar] private int _gold = 0;
    [SyncVar] private int _reputation = 0;
    [SyncVar] private int _fame = 0;

    [SyncVar] private CharacterClass _characterClass = CharacterClass.Swordsman;
    [SyncVar] private bool _isMale = true;
    [SyncVar] private string _characterName = "Player";
    #endregion

    #region Properties
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
    public int Experience => _experience;
    public int StatPoints => _statPoints;
    public int SkillPoints => _skillPoints;
    public int Gold => _gold;
    public int Reputation => _reputation;
    public int Fame => _fame;
    public CharacterClass CharacterClass => _characterClass;
    public bool IsMale => _isMale;
    public string CharacterName => _characterName;
    public bool IsDead => _health <= 0;

    public int CriticalRate => Mathf.Min(50, 5 + _luck / 2 + _agility / 5);
    public float CriticalDamage => 1.5f + (_luck * 0.01f);
    public int HitRate => _accuracy + _level * 2;
    public int DodgeRate => _agility / 2 + _luck / 3;
    public float HPRegen => 1f + (_constitution * 0.1f) + (_level * 0.05f);
    public float MPRegen => 1f + (_spirit * 0.1f) + (_level * 0.05f);
    public float SPRegen => 2f + (_constitution * 0.05f);
    #endregion

    #region Events
    public event Action OnHealthUpdated;
    public event Action OnManaUpdated;
    public event Action OnStaminaUpdated;
    public event Action<int> OnLevelUp;
    public event Action<int> OnExpGained;
    public event Action OnDeath;
    public event Action OnRevive;
    public event Action OnStatsChanged;
    #endregion

    #region Modifiers
    private List<StatModifier> activeModifiers = new List<StatModifier>();
    private float lastRegenTime;
    private const float REGEN_INTERVAL = 5f;
    private Animator anim;
    #endregion

    [Header("Debug")]
    [SerializeField] private bool showStatsDebugLogs = true;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    void Start()
    {
        if (isServer)
        {
            InitializeCharacter("Player", CharacterClass.Swordsman, true);
        }

        if (showStatsDebugLogs)
            Debug.Log("[PlayerStats] Start: " + gameObject.name + " | HP=" + _health + "/" + _maxHealth + " | IsDead=" + IsDead + " | isServer=" + isServer);
    }

    void Update()
    {
        if (!isServer) return;

        if (Time.time >= lastRegenTime + REGEN_INTERVAL)
        {
            lastRegenTime = Time.time;
            RegenerateStats();
        }
    }

    #region Initialization
    [Server]
    public void InitializeCharacter(string charName, CharacterClass charClass, bool isMale)
    {
        _characterName = charName;
        _characterClass = charClass;
        _isMale = isMale;

        CharacterClassData classData = GetClassData(charClass);
        if (classData == null)
        {
            Debug.LogWarning("[PlayerStats] CharacterClassData nao encontrado para " + charClass + ", usando valores padrao.");
            _strength = 10;
            _agility = 10;
            _constitution = 10;
            _spirit = 10;
            _accuracy = 10;
            _luck = 5;
        }
        else
        {
            _strength = classData.baseSTR;
            _agility = classData.baseAGI;
            _constitution = classData.baseCON;
            _spirit = classData.baseSPR;
            _accuracy = classData.baseACC;
            _luck = classData.baseLUCK;
        }

        RecalculateDerivedStats();

        _health = _maxHealth;
        _mana = _maxMana;
        _stamina = _maxStamina;
        _statPoints = 0;
        _skillPoints = 0;

        if (showStatsDebugLogs)
            Debug.Log("[PlayerStats] InitializeCharacter: " + charName + " | HP=" + _health + "/" + _maxHealth + " | IsDead=" + IsDead);
    }

    private CharacterClassData GetClassData(CharacterClass charClass)
    {
        return Resources.Load<CharacterClassData>("Classes/" + charClass);
    }
    #endregion

    #region Stat Calculation
    [Server]
    public void RecalculateDerivedStats()
    {
        int bonusHP = GetModifierValue(StatType.HP);
        int bonusMP = GetModifierValue(StatType.MP);
        int bonusSP = GetModifierValue(StatType.SP);
        int bonusAtk = GetModifierValue(StatType.Attack);
        int bonusDef = GetModifierValue(StatType.Defense);
        int bonusMatk = GetModifierValue(StatType.MagicAttack);
        int bonusMdef = GetModifierValue(StatType.MagicDefense);
        float bonusAtkSpd = GetModifierValue(StatType.AttackSpeed) * 0.01f;
        float bonusMoveSpd = GetModifierValue(StatType.MoveSpeed) * 0.01f;

        _maxHealth = 100 + (_constitution * 10) + (_level * 5) + bonusHP;
        _maxMana = 50 + (_spirit * 8) + (_level * 3) + bonusMP;
        _maxStamina = 100 + (_constitution * 5) + bonusSP;

        _attack = (_strength * 2) + _agility + bonusAtk;
        _defense = _constitution + (_agility / 2) + bonusDef;
        _magicAttack = (_spirit * 2) + bonusMatk;
        _magicDefense = _spirit + (_constitution / 2) + bonusMdef;

        _attackSpeed = 1.0f + (_agility * 0.01f) + bonusAtkSpd;
        _moveSpeed = 5.0f + (_agility * 0.02f) + bonusMoveSpd;

        OnStatsChanged?.Invoke();

        if (TryGetComponent(out PlayerMovement movement))
            movement.UpdateMoveSpeed(_moveSpeed);

        if (TryGetComponent(out PlayerCombat combat))
            combat.UpdateAttackSpeed(_attackSpeed);
    }

    private int GetModifierValue(StatType statType)
    {
        int total = 0;
        foreach (var mod in activeModifiers)
        {
            if (mod.statType == statType)
                total += mod.isPercent ? 0 : mod.value;
        }
        return total;
    }
    #endregion

    #region Damage & Healing
    [Server]
    public void TakeDamage(int damage, uint attackerId, DamageType damageType = DamageType.Physical)
    {
        if (showStatsDebugLogs)
        {
            Debug.Log("[PlayerStats] ===============================================");
            Debug.Log("[PlayerStats] TakeDamage CHAMADO em " + gameObject.name);
            Debug.Log("[PlayerStats]   -> dmg=" + damage + " | attackerId=" + attackerId + " | damageType=" + damageType);
            Debug.Log("[PlayerStats]   -> isDead=" + IsDead + " | HP antes=" + _health + "/" + _maxHealth);
            Debug.Log("[PlayerStats]   -> netId=" + netId);

            var stackTrace = new System.Diagnostics.StackTrace(1, false);
            var frames = stackTrace.GetFrames();
            if (frames != null)
            {
                for (int i = 0; i < Mathf.Min(5, frames.Length); i++)
                {
                    var method = frames[i].GetMethod();
                    if (method.DeclaringType != null)
                        Debug.Log("[PlayerStats]   -> caller[" + i + "]: " + method.DeclaringType.Name + "." + method.Name);
                }
            }
            Debug.Log("[PlayerStats] ===============================================");
        }

        if (IsDead)
        {
            if (showStatsDebugLogs) Debug.Log("[PlayerStats] TakeDamage IGNORADO: " + gameObject.name + " ja esta morto");
            return;
        }

        int finalDamage = damage;

        if (damageType == DamageType.Physical)
            finalDamage = Mathf.Max(1, damage - Defense);
        else if (damageType == DamageType.Magical)
            finalDamage = Mathf.Max(1, damage - MagicDefense);

        int dodgeRoll = UnityEngine.Random.Range(0, 100);
        if (dodgeRoll < DodgeRate)
        {
            if (showStatsDebugLogs) Debug.Log("[PlayerStats] DODGE! dodgeRoll=" + dodgeRoll + " < DodgeRate=" + DodgeRate);
            RpcShowDodge();
            return;
        }

        _health = Mathf.Max(0, _health - finalDamage);

        if (showStatsDebugLogs)
            Debug.Log("[PlayerStats] " + gameObject.name + " HP depois=" + _health + " | danoFinal=" + finalDamage);

        RpcOnDamageTaken(finalDamage, attackerId);

        if (_health <= 0)
        {
            if (showStatsDebugLogs)
                Debug.Log("[PlayerStats] " + gameObject.name + " HP <= 0! Chamando Die(attackerId=" + attackerId + ")");
            Die(attackerId);
        }
    }

    [Server]
    public void TakeTrueDamage(int damage, uint attackerId)
    {
        Debug.Log("[PlayerStats] TakeTrueDamage CHAMADO em " + gameObject.name + " | dmg=" + damage + " | isDead=" + IsDead);
        if (IsDead) return;
        _health = Mathf.Max(0, _health - damage);
        RpcOnDamageTaken(damage, attackerId);
        if (_health <= 0) Die(attackerId);
    }

    [Server]
    public void Heal(int amount)
    {
        if (IsDead) return;
        _health = Mathf.Min(_maxHealth, _health + amount);
        RpcOnHealed(amount);
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
    private void RegenerateStats()
    {
        if (IsDead) return;

        if (_health < _maxHealth)
            _health = Mathf.Min(_maxHealth, _health + Mathf.RoundToInt(HPRegen));

        if (_mana < _maxMana)
            _mana = Mathf.Min(_maxMana, _mana + Mathf.RoundToInt(MPRegen));

        if (_stamina < _maxStamina)
            _stamina = Mathf.Min(_maxStamina, _stamina + Mathf.RoundToInt(SPRegen));
    }
    #endregion

    #region Death & Respawn
    [Server]
    private void Die(uint killerId)
    {
        Debug.Log("[PlayerStats] ===============================================");
        Debug.Log("[PlayerStats] Die() chamado em " + gameObject.name);
        Debug.Log("[PlayerStats]   killerId=" + killerId);
        Debug.Log("[PlayerStats] ===============================================");

        OnDeath?.Invoke();
        RpcOnDeath();

        int expLoss = Mathf.FloorToInt(_experience * 0.05f);
        _experience = Mathf.Max(0, _experience - expLoss);

        if (_fame > 0)
            _fame = Mathf.Max(0, _fame - 10);
    }

    [Server]
    public void Revive(bool atSpawn = true)
    {
        _health = _maxHealth;
        _mana = _maxMana;
        _stamina = _maxStamina;

        RpcOnRevive(atSpawn);
        OnRevive?.Invoke();
    }

    [ClientRpc]
    private void RpcOnDeath()
    {
        Debug.Log("[PlayerStats] RpcOnDeath recebido em " + gameObject.name);
        if (anim != null) anim.SetTrigger("Die");
        GetComponent<PlayerMovement>()?.SetMovementEnabled(false);
        GetComponent<PlayerCombat>()?.SetCombatEnabled(false);
    }

    [ClientRpc]
    private void RpcOnRevive(bool atSpawn)
    {
        Debug.Log("[PlayerStats] RpcOnRevive recebido em " + gameObject.name);
        if (anim != null) anim.SetTrigger("Revive");
        GetComponent<PlayerMovement>()?.SetMovementEnabled(true);
        GetComponent<PlayerCombat>()?.SetCombatEnabled(true);

        if (atSpawn && TryGetComponent(out PlayerRespawn respawn))
            respawn.RespawnAtSpawnPoint();
    }
    #endregion

    #region Experience & Leveling
    [Server]
    public void AddExperience(int amount)
    {
        if (amount <= 0) return;

        float expBonus = 1f + (GetModifierValue(StatType.EXP) * 0.01f);
        int finalExp = Mathf.RoundToInt(amount * expBonus);

        _experience += finalExp;
        RpcOnExpGained(finalExp);
        OnExpGained?.Invoke(finalExp);

        while (_experience >= GetExpForNextLevel())
        {
            _experience -= GetExpForNextLevel();
            LevelUp();
        }
    }

    [Server]
    private void LevelUp()
    {
        _level++;
        _statPoints += 5;
        _skillPoints += 1;

        CharacterClassData classData = GetClassData(_characterClass);
        if (classData != null)
        {
            _strength += Mathf.RoundToInt(classData.strGrowth);
            _agility += Mathf.RoundToInt(classData.agiGrowth);
            _constitution += Mathf.RoundToInt(classData.conGrowth);
            _spirit += Mathf.RoundToInt(classData.sprGrowth);
            _accuracy += Mathf.RoundToInt(classData.accGrowth);
            _luck += Mathf.RoundToInt(classData.luckGrowth);
        }

        RecalculateDerivedStats();

        _health = _maxHealth;
        _mana = _maxMana;
        _stamina = _maxStamina;

        OnLevelUp?.Invoke(_level);
        RpcOnLevelUp(_level);
    }

    public int GetExpForNextLevel()
    {
        return Mathf.FloorToInt(100 * Mathf.Pow(_level, 1.5f));
    }

    public float GetExpPercentage()
    {
        return (float)_experience / GetExpForNextLevel();
    }
    #endregion

    #region Stat Points
    [Command]
    public void CmdAddStatPoint(StatType statType)
    {
        if (_statPoints <= 0) return;

        switch (statType)
        {
            case StatType.STR: _strength++; break;
            case StatType.AGI: _agility++; break;
            case StatType.CON: _constitution++; break;
            case StatType.SPR: _spirit++; break;
            case StatType.ACC: _accuracy++; break;
            case StatType.LUCK: _luck++; break;
            default: return;
        }

        _statPoints--;
        RecalculateDerivedStats();
    }
    #endregion

    #region Modifiers
    [Server]
    public void AddModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        RecalculateDerivedStats();
    }

    [Server]
    public void RemoveModifier(StatModifier modifier)
    {
        activeModifiers.Remove(modifier);
        RecalculateDerivedStats();
    }

    // 🔧 NOVO: Remove todos os modifiers de uma fonte (sourceId)
    [Server]
    public void RemoveModifiersBySource(string sourceId)
    {
        activeModifiers.RemoveAll(m => m.sourceId == sourceId);
        RecalculateDerivedStats();
    }

    [Server]
    public void ClearModifiers()
    {
        activeModifiers.Clear();
        RecalculateDerivedStats();
    }
    #endregion

    #region Economy
    [Server]
    public bool AddGold(int amount)
    {
        if (amount < 0 && _gold + amount < 0) return false;
        _gold = Mathf.Max(0, _gold + amount);
        return true;
    }

    [Server]
    public bool SpendGold(int amount)
    {
        if (_gold < amount) return false;
        _gold -= amount;
        return true;
    }

    [Server]
    public void AddReputation(int amount)
    {
        _reputation += amount;
    }

    [Server]
    public void AddFame(int amount)
    {
        _fame = Mathf.Max(0, _fame + amount);
    }
    #endregion

    #region RPCs
    [ClientRpc]
    private void RpcOnDamageTaken(int damage, uint attackerId)
    {
        OnHealthUpdated?.Invoke();
        DamagePopupManager.Instance?.ShowDamage(transform.position + Vector3.up * 2f, damage, true);
    }

    [ClientRpc]
    private void RpcOnHealed(int amount)
    {
        OnHealthUpdated?.Invoke();
        DamagePopupManager.Instance?.ShowHeal(transform.position + Vector3.up * 2f, amount);
    }

    [ClientRpc]
    private void RpcShowDodge()
    {
        DamagePopupManager.Instance?.ShowText(transform.position + Vector3.up * 2f, "MISS", Color.yellow);
    }

    [ClientRpc]
    private void RpcOnExpGained(int amount)
    {
        DamagePopupManager.Instance?.ShowText(transform.position + Vector3.up * 2.5f, "+" + amount + " EXP", Color.cyan);
    }

    [ClientRpc]
    private void RpcOnLevelUp(int newLevel)
    {
        LevelUpEffectManager.Instance?.PlayEffect(transform.position);
        OnLevelUp?.Invoke(newLevel);
    }
    #endregion

    #region Hooks
    private void OnHealthChanged(int oldVal, int newVal)
    {
        OnHealthUpdated?.Invoke();
        if (showStatsDebugLogs)
            Debug.Log("[PlayerStats] OnHealthChanged: " + oldVal + " -> " + newVal + " | IsDead=" + IsDead);
    }
    private void OnMaxHealthChanged(int oldVal, int newVal) => OnHealthUpdated?.Invoke();
    private void OnManaChanged(int oldVal, int newVal) => OnManaUpdated?.Invoke();
    private void OnMaxManaChanged(int oldVal, int newVal) => OnManaUpdated?.Invoke();
    private void OnStaminaChanged(int oldVal, int newVal) => OnStaminaUpdated?.Invoke();
    private void OnMaxStaminaChanged(int oldVal, int newVal) => OnStaminaUpdated?.Invoke();
    private void OnLevelChanged(int oldVal, int newVal) { }
    #endregion
}