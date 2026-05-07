using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Gameplay;

namespace TOP.Player
{
    public class PlayerStats : NetworkBehaviour, ICharacterStats
    {
        [Server]
public void RestoreMp(int amount) {
    // Sua lógica de restaurar MP (ex: currentMp += amount)
}

[Server]
public void RestoreSp(int amount) {
    // Sua lógica de restaurar SP
}
        public event System.Action OnHealthUpdated;
        public event System.Action OnManaUpdated;
        public event System.Action OnStaminaUpdated;
        public event System.Action OnDeath;
        public event System.Action OnRevive;
        public event System.Action OnStatsChanged;

        // Implementação obrigatória de ICharacterStats
        public int Health      => CurrentHp;
        public int Mana        => CurrentMp;
        public int Stamina     => CurrentSp;
        public int MaxHealth   => MaxHp;
        public int MaxMana     => MaxMp;
        public int MaxStamina  => MaxSp;
        public int Accuracy    => 0;
        public int Luck        => 0;
        public int Attack      => PhysicalAttack;
        public int Defense     => PhysicalDefense;
        public int MagicAttack   => CalculateMagicAttack();
        public int MagicDefense  => CalculateMagicDefense();
        public int Level       => 1;
        public bool IsDead     => CurrentHp <= 0;

        // Estatísticas derivadas
        public float MoveSpeed     => 5f + (Agility * 0.01f);
        public float AttackSpeed   => 1f + (Agility * 0.005f);
        public float CriticalRate  => 0.05f + (Agility * 0.002f);
        public float CriticalDamage => 1.5f;

        public float HpRegen => 1f + (Constitution * 0.1f);
        public float MpRegen => 1f + (Spirit * 0.1f);
        public float SpRegen => 2f + (Constitution * 0.05f);

        [Header("Base Stats")]
        [SyncVar] public int BaseStrength;
        [SyncVar] public int BaseAgility;
        [SyncVar] public int BaseConstitution;
        [SyncVar] public int BaseSpirit;

        [Header("Current Values")]
        [SyncVar] public int CurrentHp;
        [SyncVar] public int CurrentMp;
        [SyncVar] public int CurrentSp;

        [Header("Bonus Stats")]
        [SyncVar] private int _bonusStr;
        [SyncVar] private int _bonusAgi;
        [SyncVar] private int _bonusCon;
        [SyncVar] private int _bonusSpr;
        [SyncVar] private int _bonusHp;
        [SyncVar] private int _bonusMp;
        [SyncVar] private int _bonusSp;
        [SyncVar] private int _bonusAtk;
        [SyncVar] private int _bonusDef;

        // Calculados a partir de base + bonus
        public int Strength      => BaseStrength      + _bonusStr;
        public int Agility       => BaseAgility       + _bonusAgi;
        public int Constitution  => BaseConstitution  + _bonusCon;
        public int Spirit        => BaseSpirit        + _bonusSpr;

        public int MaxHp   => CalculateMaxHp() + _bonusHp;
        public int MaxMp   => CalculateMaxMp() + _bonusMp;
        public int MaxSp   => CalculateMaxSp() + _bonusSp;

        public int PhysicalAttack   => CalculatePhysicalAttack() + _bonusAtk;
   
        public int PhysicalDefense  => CalculatePhysicalDefense() + _bonusDef;
      

        // Regeneração
        private float _lastHpRegen;
        private float _lastMpRegen;
        private float _lastSpRegen;

        #region Modificadores (buffs / debuffs)
        private readonly List<StatModifier> _modifiers = new List<StatModifier>();

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            OnStatsChanged?.Invoke();
        }

        public void RemoveModifier(StatModifier modifier)
        {
            if (_modifiers.Contains(modifier))
            {
                _modifiers.Remove(modifier);
                OnStatsChanged?.Invoke();
            }
        }

        public void ClearModifiers()
        {
            _modifiers.Clear();
            OnStatsChanged?.Invoke();
        }
        #endregion

        #region ICharacterStats - Métodos de dano / cura
        [Server]
        public void TakeDamage(int damage, uint attackerId, DamageType damageType = DamageType.Physical)
        {
            TakeDamage(damage);
        }

        [Server]
        public void TakeTrueDamage(int damage, uint attackerId)
        {
            TakeDamage(damage);
        }

        [Server]
        public void Heal(int amount)
        {
            if (IsDead) return;

            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
            OnHealthUpdated?.Invoke();
        }

        [Server]
        public void RestoreMana(int amount)
        {
            CurrentMp = Mathf.Min(CurrentMp + amount, MaxMp);
            OnManaUpdated?.Invoke();
        }

        [Server]
        public void RestoreStamina(int amount)
        {
            CurrentSp = Mathf.Min(CurrentSp + amount, MaxSp);
            OnStaminaUpdated?.Invoke();
        }
        #endregion

        #region XP / Gold
        [Server]
        public void AddExperience(int amount)
        {
            // TODO implementar sistema de level
        }

        [Server]
        public void AddGold(int amount)
        {
            // TODO implementar sistema de gold
        }
        #endregion

        #region Regeneração automática (HP/MP/SP)
        void Update()
        {
            if (!isServer) return;

            if (Time.time > _lastHpRegen + 1f)
            {
                _lastHpRegen = Time.time;
                Heal(Mathf.RoundToInt(HpRegen));
            }

            if (Time.time > _lastMpRegen + 1f)
            {
                _lastMpRegen = Time.time;
                RestoreMana(Mathf.RoundToInt(MpRegen));
            }

            if (Time.time > _lastSpRegen + 1f)
            {
                _lastSpRegen = Time.time;
                RestoreStamina(Mathf.RoundToInt(SpRegen));
            }
        }
        #endregion

        #region Inicialização e valores atuais
        [Server]
        public void Initialize(int str, int agi, int con, int spr, int maxHp, int maxMp, int maxSp)
        {
            BaseStrength = str;
            BaseAgility = agi;
            BaseConstitution = con;
            BaseSpirit = spr;

            CurrentHp = maxHp;
            CurrentMp = maxMp;
            CurrentSp = maxSp;
        }

        [Server]
        public void SetCurrentHpMpSp(int hp, int mp, int sp)
        {
            CurrentHp = Mathf.Clamp(hp, 0, MaxHp);
            CurrentMp = Mathf.Clamp(mp, 0, MaxMp);
            CurrentSp = Mathf.Clamp(sp, 0, MaxSp);
        }
        #endregion

        #region Dano e cura servidor
        [Server]
        public void TakeDamage(int damage)
        {
            int finalDamage = Mathf.Max(1, damage - PhysicalDefense);
            CurrentHp -= finalDamage;

            OnHealthUpdated?.Invoke();

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                PlayerController controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Die(); // ou PlayerDead() se for o nome na sua classe
                }
            }
        }

        [Server]
        public void ConsumeMp(int amount)
        {
            CurrentMp = Mathf.Max(0, CurrentMp - amount);
        }

        [Server]
        public void ConsumeSp(int amount)
        {
            CurrentSp = Mathf.Max(0, CurrentSp - amount);
        }
        #endregion

        // ✅ public bool IsMale para resolver o erro em PlayerClass.cs
        public bool IsMale => true; // ou torne editável no Inspector

        #region Bonuses (equipamentos / buffs)
        public void AddBonusStrength(int value)       => _bonusStr += value;
        public void AddBonusAgility(int value)        => _bonusAgi += value;
        public void AddBonusConstitution(int value)   => _bonusCon += value;
        public void AddBonusSpirit(int value)         => _bonusSpr += value;
        public void AddBonusHp(int value)             => _bonusHp += value;
        public void AddBonusMp(int value)             => _bonusMp += value;
        public void AddBonusSp(int value)             => _bonusSp += value;
        public void AddBonusAttack(int value)         => _bonusAtk += value;
        public void AddBonusDefense(int value)        => _bonusDef += value;
        #endregion

        #region Cálculos de base (máximos e ataque/defesa)
        int CalculateMaxHp()   => 100 + (Constitution * 10) + (BaseStrength * 2);
        int CalculateMaxMp()   => 50 + (Spirit * 8) + (BaseConstitution * 2);
        int CalculateMaxSp()   => 30 + (Constitution * 5) + (BaseAgility * 2);

        int CalculatePhysicalAttack() => 10 + (Strength * 2) + (Agility * 1);
        int CalculateMagicAttack()    => 5 + (Spirit * 3);

        int CalculatePhysicalDefense() => 5 + (Constitution * 1) + (Strength * 1);
        int CalculateMagicDefense()    => 3 + (Spirit * 2) + (Constitution * 1);
        #endregion
    }
}