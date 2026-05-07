using UnityEngine;
using Mirror;
using TOP.Data;
using TOP.Player;
using TOP.Core;
using TOP.UI;
using TOP.Systems;
using TOP.Network;

namespace TOP.Gameplay
{
    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(PlayerStats))]
    [RequireComponent(typeof(PlayerInventory))]
    [RequireComponent(typeof(PlayerEquipment))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerSkills))]
    [RequireComponent(typeof(PlayerAnimation))]
    public class PlayerController : NetworkBehaviour
    {
        [SyncVar] public long CharacterId;
        [SyncVar] public long AccountId;
        [SyncVar] public string CharacterName;
        [SyncVar] public byte Job;
        [SyncVar] public int Level;
        [SyncVar] public int CurrentHp;
        [SyncVar] public int CurrentMp;
        [SyncVar] public int CurrentSp;

        [Header("Components")]
        public PlayerMovement Movement;
        public PlayerStats Stats;
        public PlayerInventory Inventory;
        public PlayerEquipment Equipment;
        public PlayerCombat Combat;
        public PlayerSkills Skills;
        public PlayerAnimation Animation;
        public PlayerConsumables Consumables;

        [Header("Settings")]
        [SerializeField] private float autoSaveInterval = 60f;
        private float _lastSaveTime;

        void Awake()
        {
            Movement = GetComponent<PlayerMovement>();
            Stats = GetComponent<PlayerStats>();
            Inventory = GetComponent<PlayerInventory>();
            Equipment = GetComponent<PlayerEquipment>();
            Combat = GetComponent<PlayerCombat>();
            Skills = GetComponent<PlayerSkills>();
            Animation = GetComponent<PlayerAnimation>();
            Consumables = GetComponent<PlayerConsumables>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            CameraFollow camFollow = Camera.main?.GetComponent<CameraFollow>();
            if (camFollow != null)
                camFollow.SetTarget(transform);

            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
                uiManager.SetupLocalPlayer(this);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            SaveToDatabase();
        }

        void Update()
        {
            if (isServer && Time.time > _lastSaveTime + autoSaveInterval)
            {
                _lastSaveTime = Time.time;
                SaveToDatabase();
            }
        }

        public void InitializeFromDatabase(CharacterData data)
        {
            CharacterId = data.Id;
            AccountId = data.AccountId;
            CharacterName = data.Name;
            Job = data.Job;
            Level = data.Level;
            CurrentHp = data.CurrentHp;
            CurrentMp = data.CurrentMp;
            CurrentSp = data.CurrentSp;

            if (Stats != null)
            {
                Stats.Initialize(data.BaseStr, data.BaseAgi, data.BaseCon, data.BaseSpr,
                    data.MaxHp, data.MaxMp, data.MaxSp);
                Stats.SetCurrentHpMpSp(data.CurrentHp, data.CurrentMp, data.CurrentSp);
            }

            if (Inventory != null && data.Inventory != null)
            {
                foreach (InventoryItemData item in data.Inventory)
                {
                    Inventory.AddItem(item.ItemId, item.Quantity, item.SlotIndex);
                }
            }

            if (Skills != null && data.Skills != null)
            {
                foreach (CharacterSkillData skill in data.Skills)
                {
                    Skills.LearnSkill(skill.SkillId, skill.Level);
                }
            }
        }

        [Server]
        public void SaveToDatabase()
        {
            if (Stats == null) return;

            CharacterData data = new CharacterData
            {
                Id = CharacterId,
                AccountId = AccountId,
                Name = CharacterName,
                Job = Job,
                Level = Level,
                CurrentHp = Stats.CurrentHp,
                CurrentMp = Stats.CurrentMp,
                CurrentSp = Stats.CurrentSp,
                PosX = transform.position.x,
                PosY = transform.position.y,
                PosZ = transform.position.z,
                RotationY = transform.rotation.eulerAngles.y,
                BaseStr = Stats.BaseStrength,
                BaseAgi = Stats.BaseAgility,
                BaseCon = Stats.BaseConstitution,
                BaseSpr = Stats.BaseSpirit,
                MaxHp = Stats.MaxHp,
                MaxMp = Stats.MaxMp,
                MaxSp = Stats.MaxSp
            };

            _ = TOP.Services.DatabaseService.Instance.SaveCharacterAsync(data);
        }

        [Command]
        public void CmdMoveTo(Vector3 destination)
        {
            if (Movement != null)
                Movement.SetDestination(destination);
        }

        [Command]
        public void CmdAttackTarget(NetworkIdentity target)
        {
            if (Combat != null && target != null)
                Combat.AttackTarget(target);
        }

        [Command]
        public void CmdUseSkill(int skillId, Vector3 targetPosition, NetworkIdentity target)
        {
            if (Skills != null)
                Skills.UseSkill(skillId, targetPosition, target);
        }

        [Command]
        public void CmdSetTarget(NetworkIdentity target)
        {
            if (Combat != null)
                Combat.SetTarget(target);
        }

        [ClientRpc]
        public void RpcTakeDamage(int damage, Vector3 hitPosition)
        {
            DamagePopupManager popupManager = FindObjectOfType<DamagePopupManager>();
            if (popupManager != null)
                popupManager.ShowDamage(damage, hitPosition, false);

            HitFlashEffect hitFlash = GetComponentInChildren<HitFlashEffect>();
            if (hitFlash != null)
                hitFlash.Flash();
        }

        [ClientRpc]
        public void RpcHeal(int amount)
        {
            DamagePopupManager popupManager = FindObjectOfType<DamagePopupManager>();
            if (popupManager != null)
                popupManager.ShowHeal(amount, transform.position + Vector3.up * 2f);
        }

        [ClientRpc]
        public void RpcLevelUp()
        {
            Level++;
            LevelUpEffectManager effectManager = FindObjectOfType<LevelUpEffectManager>();
            if (effectManager != null)
                effectManager.PlayLevelUpEffect(transform.position);
        }

        [ClientRpc]
        public void RpcShowMessage(string message, PlayerMessageType type)
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager != null)
                uiManager.ShowMessage(message, type);
        }

        [Server]
        public void Die()
        {
            RpcDie();
            Invoke(nameof(RespawnPlayer), 5f);
        }

        [ClientRpc]
        void RpcDie()
        {
            Animation.PlayDeath();
            Movement.enabled = false;
            Combat.enabled = false;
        }

        [Server]
        void RespawnPlayer()
        {
            CurrentHp = Stats.MaxHp;
            CurrentMp = Stats.MaxMp;
            CurrentSp = Stats.MaxSp;

            transform.position = Vector3.zero + Vector3.up;

            RpcRespawn();
        }

        [ClientRpc]
        void RpcRespawn()
        {
            Animation.PlayRespawn();
            Movement.enabled = true;
            Combat.enabled = true;
        }

        void OnDestroy()
        {
            if (isServer)
            {
                SaveToDatabase();
            }
        }
    }

    public enum PlayerMessageType
    {
        Info, Warning, Error, Success
    }
}
