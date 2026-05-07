using UnityEngine;
using Mirror;
using TOP.Core;

namespace TOP.Player
{
    public class PlayerStats : NetworkBehaviour, ICharacterStats
    {
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

        public int Strength => BaseStrength + _bonusStr;
        public int Agility => BaseAgility + _bonusAgi;
        public int Constitution => BaseConstitution + _bonusCon;
        public int Spirit => BaseSpirit + _bonusSpr;

        public int MaxHp => CalculateMaxHp() + _bonusHp;
        public int MaxMp => CalculateMaxMp() + _bonusMp;
        public int MaxSp => CalculateMaxSp() + _bonusSp;

        public int PhysicalAttack => CalculatePhysicalAttack() + _bonusAtk;
        public int MagicAttack => CalculateMagicAttack();
        public int PhysicalDefense => CalculatePhysicalDefense() + _bonusDef;
        public int MagicDefense => CalculateMagicDefense();

        public float MoveSpeed => 5f + (Agility * 0.01f);
        public float AttackSpeed => 1f + (Agility * 0.005f);
        public float CriticalRate => 0.05f + (Agility * 0.002f);
        public float CriticalDamage => 1.5f;

        public float HpRegen => 1f + (Constitution * 0.1f);
        public float MpRegen => 1f + (Spirit * 0.1f);
        public float SpRegen => 2f + (Constitution * 0.05f);

        private float _lastHpRegen;
        private float _lastMpRegen;
        private float _lastSpRegen;

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
                RestoreMp(Mathf.RoundToInt(MpRegen));
            }

            if (Time.time > _lastSpRegen + 1f)
            {
                _lastSpRegen = Time.time;
                RestoreSp(Mathf.RoundToInt(SpRegen));
            }
        }

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

        [Server]
        public void TakeDamage(int damage)
        {
            int finalDamage = Mathf.Max(1, damage - PhysicalDefense);
            CurrentHp -= finalDamage;

            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                PlayerController controller = GetComponent<PlayerController>();
                if (controller != null)
                {
                    controller.Die();
                }
            }
        }

        [Server]
        public void Heal(int amount)
        {
            CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        }

        [Server]
        public void ConsumeMp(int amount)
        {
            CurrentMp = Mathf.Max(0, CurrentMp - amount);
        }

        [Server]
        public void RestoreMp(int amount)
        {
            CurrentMp = Mathf.Min(CurrentMp + amount, MaxMp);
        }

        [Server]
        public void ConsumeSp(int amount)
        {
            CurrentSp = Mathf.Max(0, CurrentSp - amount);
        }

        [Server]
        public void RestoreSp(int amount)
        {
            CurrentSp = Mathf.Min(CurrentSp + amount, MaxSp);
        }

        public void AddBonusStrength(int value) => _bonusStr += value;
        public void AddBonusAgility(int value) => _bonusAgi += value;
        public void AddBonusConstitution(int value) => _bonusCon += value;
        public void AddBonusSpirit(int value) => _bonusSpr += value;
        public void AddBonusHp(int value) => _bonusHp += value;
        public void AddBonusMp(int value) => _bonusMp += value;
        public void AddBonusSp(int value) => _bonusSp += value;
        public void AddBonusAttack(int value) => _bonusAtk += value;
        public void AddBonusDefense(int value) => _bonusDef += value;

        int CalculateMaxHp() => 100 + (Constitution * 10) + (BaseStrength * 2);
        int CalculateMaxMp() => 50 + (Spirit * 8) + (BaseConstitution * 2);
        int CalculateMaxSp() => 30 + (Constitution * 5) + (BaseAgility * 2);
        int CalculatePhysicalAttack() => 10 + (Strength * 2) + (Agility * 1);
        int CalculateMagicAttack() => 5 + (Spirit * 3);
        int CalculatePhysicalDefense() => 5 + (Constitution * 1) + (Strength * 1);
        int CalculateMagicDefense() => 3 + (Spirit * 2) + (Constitution * 1);
    }
}
