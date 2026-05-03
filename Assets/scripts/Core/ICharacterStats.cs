using System;

/// <summary>
/// Modificador temporario ou permanente de atributos.
/// Usado por buffs, debuffs, equipamentos, gemas, etc.
/// </summary>
[System.Serializable]
public class StatModifier
{
    public StatType statType;
    public int value;
    public bool isPercent;
    public float duration;
    public string sourceId;

    public StatModifier() { }

    public StatModifier(StatType type, int val, bool percent = false, float dur = -1f, string source = "")
    {
        statType = type;
        value = val;
        isPercent = percent;
        duration = dur;
        sourceId = source;
    }
}

/// <summary>
/// Interface unificada para estatisticas de qualquer entidade combatente
/// (Player, Mob, Boss, NPC hostil, etc.).
/// </summary>
public interface ICharacterStats
{
    int Health { get; }
    int MaxHealth { get; }
    int Mana { get; }
    int MaxMana { get; }
    int Stamina { get; }
    int MaxStamina { get; }
    int Strength { get; }
    int Agility { get; }
    int Constitution { get; }
    int Spirit { get; }
    int Accuracy { get; }
    int Luck { get; }
    int Attack { get; }
    int Defense { get; }
    int MagicAttack { get; }
    int MagicDefense { get; }
    float AttackSpeed { get; }
    float MoveSpeed { get; }
    int Level { get; }
    bool IsDead { get; }

    event Action OnHealthUpdated;
    event Action OnManaUpdated;
    event Action OnStaminaUpdated;
    event Action OnDeath;
    event Action OnRevive;
    event Action OnStatsChanged;

    void TakeDamage(int damage, uint attackerId, DamageType damageType = DamageType.Physical);
    void TakeTrueDamage(int damage, uint attackerId);
    void Heal(int amount);
    void RestoreMana(int amount);
    void RestoreStamina(int amount);
    void AddModifier(StatModifier modifier);
    void RemoveModifier(StatModifier modifier);
    void ClearModifiers();
}
