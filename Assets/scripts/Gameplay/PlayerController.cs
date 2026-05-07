// Assets/Scripts/Gameplay/PlayerController.cs
using System;
using Mirror;
using UnityEngine;
using UnityEngine.AI;
using TOP.Services;
using TOP.Data;

namespace TOP.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("SyncVars")]
        [SyncVar(hook = nameof(OnNameChanged))] public string CharacterName;
        [SyncVar] public byte Job;
        [SyncVar] public int Level;
        [SyncVar] public int CurrentHp;
        [SyncVar] public int CurrentMp;
        [SyncVar] public int CurrentSp;

        [Header("References")]
        [SerializeField] private NavMeshAgent agent;
        [SerializeField] private Animator animator;

        [System.NonSerialized] public long CharacterId;
        [System.NonSerialized] public long AccountId;
        [System.NonSerialized] public ulong Exp;
        [System.NonSerialized] public int BaseStr, BaseAgi, BaseCon, BaseSpr;
        [System.NonSerialized] public int MaxHp, MaxMp, MaxSp;
        [System.NonSerialized] public ulong Gold;
        [System.NonSerialized] public string MapName;
        [System.NonSerialized] public float RotationY;
        [System.NonSerialized] public byte Gender;
        [System.NonSerialized] public byte HairStyle, HairColor, FaceStyle;

        private readonly SyncList<ItemSyncData> _inventory = new SyncList<ItemSyncData>();
        private readonly SyncList<SkillSyncData> _skills = new SyncList<SkillSyncData>();
        private readonly SyncList<EquipmentSyncData> _equipment = new SyncList<EquipmentSyncData>();

        private float _nextSaveTime;
        private const float SAVE_INTERVAL = 60f;
        private bool _initialized;
        private bool _isSaving;

        public event System.Action OnInventoryChanged;
        public event System.Action OnSkillsChanged;
        public event System.Action OnEquipmentChanged;
        public event System.Action<string> OnNameChangedEvent;

        void Awake()
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();
            if (animator == null) animator = GetComponent<Animator>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            _inventory.Callback += OnInventoryUpdated;
            _skills.Callback += OnSkillsUpdated;
            _equipment.Callback += OnEquipmentUpdated;
        }

        void Update()
        {
            if (!isServer) return;

            if (Time.time >= _nextSaveTime && !_isSaving)
            {
                SaveToDatabase();
                _nextSaveTime = Time.time + SAVE_INTERVAL;
            }

            if (animator != null && agent != null)
            {
                float speed = agent.velocity.magnitude / agent.speed;
                animator.SetFloat("Speed", speed);
            }
        }

        [Server]
        public void InitializeFromDatabase(CharacterData data)
        {
            CharacterId = data.Id;
            AccountId = data.AccountId;
            CharacterName = data.Name;
            Job = data.Job;
            Gender = data.Gender;
            HairStyle = data.HairStyle;
            HairColor = data.HairColor;
            FaceStyle = data.FaceStyle;

            Level = data.Level;
            Exp = data.Exp;

            BaseStr = data.BaseStr;
            BaseAgi = data.BaseAgi;
            BaseCon = data.BaseCon;
            BaseSpr = data.BaseSpr;

            MaxHp = data.MaxHp;
            MaxMp = data.MaxMp;
            MaxSp = data.MaxSp;
            CurrentHp = data.CurrentHp;
            CurrentMp = data.CurrentMp;
            CurrentSp = data.CurrentSp;

            Gold = data.Gold;
            MapName = data.MapName;
            RotationY = data.RotationY;

            transform.position = new Vector3(data.PosX, data.PosY, data.PosZ);
            transform.rotation = Quaternion.Euler(0, data.RotationY, 0);

            _inventory.Clear();
            foreach (var item in data.Inventory)
            {
                _inventory.Add(new ItemSyncData
                {
                    DbId = item.Id,
                    SlotIndex = item.SlotIndex,
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    Durability = item.Durability,
                    IsEquipped = item.IsEquipped
                });
            }

            _skills.Clear();
            foreach (var skill in data.Skills)
            {
                _skills.Add(new SkillSyncData
                {
                    SkillId = skill.SkillId,
                    Level = skill.Level,
                    Exp = skill.Exp
                });
            }

            _equipment.Clear();

            _initialized = true;
            _nextSaveTime = Time.time + SAVE_INTERVAL;

            Debug.Log($"[PlayerController] {CharacterName} inicializado. HP:{CurrentHp}/{MaxHp} Pos:{transform.position} Map:{MapName}");
        }

        [Command]
        public void CmdMoveTo(Vector3 destination)
        {
            if (!_initialized || CurrentHp <= 0) return;

            float distance = Vector3.Distance(transform.position, destination);
            if (distance > 50f)
            {
                LogSecurityEvent($"Teleport hack detectado: {distance:F1}m");
                return;
            }

            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.SetDestination(destination);
            }
        }

        [Command]
        public void CmdStopMovement()
        {
            if (agent != null && agent.isActiveAndEnabled)
            {
                agent.ResetPath();
            }
        }

        [Command]
        public void CmdMoveItem(ushort fromSlot, ushort toSlot)
        {
            if (!_initialized) return;
            if (fromSlot == toSlot) return;
            if (fromSlot > 47 || toSlot > 47) return;

            var fromItem = FindItemInSlot(fromSlot);
            if (!fromItem.HasValue) return;

            if (fromItem.Value.IsEquipped)
            {
                LogSecurityEvent($"Tentou mover item equipado slot {fromSlot}");
                return;
            }

            var toItem = FindItemInSlot(toSlot);

            if (!toItem.HasValue)
            {
                UpdateItemSlot(fromItem.Value.DbId, toSlot);
            }
            else if (fromItem.Value.ItemId == toItem.Value.ItemId && ItemDatabase.Instance.IsStackable(fromItem.Value.ItemId))
            {
                int maxStack = ItemDatabase.Instance.GetMaxStack(fromItem.Value.ItemId);
                int total = fromItem.Value.Quantity + toItem.Value.Quantity;

                if (total <= maxStack)
                {
                    UpdateItemQuantity(toItem.Value.DbId, total);
                    RemoveItem(fromItem.Value.DbId);
                }
                else
                {
                    UpdateItemQuantity(toItem.Value.DbId, maxStack);
                    UpdateItemQuantity(fromItem.Value.DbId, total - maxStack);
                }
            }
            else
            {
                UpdateItemSlot(fromItem.Value.DbId, toSlot);
                UpdateItemSlot(toItem.Value.DbId, fromSlot);
            }
        }

        [Command]
        public void CmdDropItem(ushort slotIndex, int quantity)
        {
            if (!_initialized || CurrentHp <= 0) return;

            var item = FindItemInSlot(slotIndex);
            if (!item.HasValue) return;
            if (item.Value.IsEquipped) return;
            if (quantity <= 0 || quantity > item.Value.Quantity) return;

            if (quantity >= item.Value.Quantity)
            {
                RemoveItem(item.Value.DbId);
            }
            else
            {
                UpdateItemQuantity(item.Value.DbId, item.Value.Quantity - quantity);
            }

            SaveToDatabase();
        }

        [Command]
        public void CmdEquipItem(ushort slotIndex)
        {
            if (!_initialized || CurrentHp <= 0) return;

            var item = FindItemInSlot(slotIndex);
            if (!item.HasValue || item.Value.IsEquipped) return;

            byte equipSlot = ItemDatabase.Instance.GetEquipSlot(item.Value.ItemId);
            if (equipSlot == 255) return;

            var equipped = FindEquippedItem(equipSlot);
            if (equipped.HasValue)
            {
                SetItemEquipped(equipped.Value.DbId, false);
            }

            SetItemEquipped(item.Value.DbId, true);
            RecalculateStats();
            SaveToDatabase();
        }

        [Command]
        public void CmdUnequipItem(byte equipSlot)
        {
            if (!_initialized) return;

            var item = FindEquippedItem(equipSlot);
            if (!item.HasValue) return;

            ushort emptySlot = FindEmptySlot();
            if (emptySlot == 65535)
            {
                TargetRpcShowMessage("Inventário cheio!");
                return;
            }

            SetItemEquipped(item.Value.DbId, false);
            UpdateItemSlot(item.Value.DbId, emptySlot);

            RecalculateStats();
            SaveToDatabase();
        }

        [Server]
        public void SaveToDatabase()
        {
            if (_isSaving || !_initialized) return;
            _isSaving = true;

            var data = new CharacterData
            {
                Id = CharacterId,
                AccountId = AccountId,
                Name = CharacterName,
                Job = Job,
                Gender = Gender,
                HairStyle = HairStyle,
                HairColor = HairColor,
                FaceStyle = FaceStyle,
                Level = Level,
                Exp = Exp,
                BaseStr = BaseStr,
                BaseAgi = BaseAgi,
                BaseCon = BaseCon,
                BaseSpr = BaseSpr,
                MaxHp = MaxHp,
                MaxMp = MaxMp,
                MaxSp = MaxSp,
                CurrentHp = CurrentHp,
                CurrentMp = CurrentMp,
                CurrentSp = CurrentSp,
                Gold = Gold,
                MapName = MapName,
                PosX = transform.position.x,
                PosY = transform.position.y,
                PosZ = transform.position.z,
                RotationY = transform.rotation.eulerAngles.y,
                LastOnline = DateTime.Now
            };

            data.Inventory.Clear();
            foreach (var item in _inventory)
            {
                data.Inventory.Add(new InventoryItemData
                {
                    Id = item.DbId,
                    CharacterId = CharacterId,
                    SlotIndex = item.SlotIndex,
                    ItemId = item.ItemId,
                    Quantity = item.Quantity,
                    Durability = item.Durability,
                    IsEquipped = item.IsEquipped
                });
            }

            data.Skills.Clear();
            foreach (var skill in _skills)
            {
                data.Skills.Add(new CharacterSkillData
                {
                    CharacterId = CharacterId,
                    SkillId = skill.SkillId,
                    Level = skill.Level,
                    Exp = skill.Exp
                });
            }

            DatabaseService.Instance.SaveCharacterAsync(data).ContinueWith(_ =>
            {
                _isSaving = false;
                Debug.Log($"[Save] {CharacterName} salvo em {transform.position}");
            });
        }

        ItemSyncData? FindItemInSlot(ushort slot)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].SlotIndex == slot && !_inventory[i].IsEquipped)
                    return _inventory[i];
            }
            return null;
        }

        ItemSyncData? FindEquippedItem(byte equipSlot)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].IsEquipped && ItemDatabase.Instance.GetEquipSlot(_inventory[i].ItemId) == equipSlot)
                    return _inventory[i];
            }
            return null;
        }

        ushort FindEmptySlot()
        {
            bool[] used = new bool[48];
            foreach (var item in _inventory)
            {
                if (!item.IsEquipped && item.SlotIndex < 48)
                    used[item.SlotIndex] = true;
            }
            for (ushort i = 0; i < 48; i++)
            {
                if (!used[i]) return i;
            }
            return 65535;
        }

        void UpdateItemSlot(long dbId, ushort newSlot)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].DbId == dbId)
                {
                    var updated = _inventory[i];
                    updated.SlotIndex = newSlot;
                    _inventory[i] = updated;
                    return;
                }
            }
        }

        void UpdateItemQuantity(long dbId, int newQty)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].DbId == dbId)
                {
                    if (newQty <= 0)
                    {
                        _inventory.RemoveAt(i);
                    }
                    else
                    {
                        var updated = _inventory[i];
                        updated.Quantity = newQty;
                        _inventory[i] = updated;
                    }
                    return;
                }
            }
        }

        void RemoveItem(long dbId)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].DbId == dbId)
                {
                    _inventory.RemoveAt(i);
                    return;
                }
            }
        }

        void SetItemEquipped(long dbId, bool equipped)
        {
            for (int i = 0; i < _inventory.Count; i++)
            {
                if (_inventory[i].DbId == dbId)
                {
                    var updated = _inventory[i];
                    updated.IsEquipped = equipped;
                    _inventory[i] = updated;
                    return;
                }
            }
        }

        void RecalculateStats()
        {
            int bonusStr = 0, bonusAgi = 0, bonusCon = 0, bonusSpr = 0;

            foreach (var item in _inventory)
            {
                if (item.IsEquipped)
                {
                    var stats = ItemDatabase.Instance.GetItemStats(item.ItemId);
                    bonusStr += stats.Str;
                    bonusAgi += stats.Agi;
                    bonusCon += stats.Con;
                    bonusSpr += stats.Spr;
                }
            }

            MaxHp = CalculateMaxHp(BaseCon + bonusCon, Level);
            MaxMp = CalculateMaxMp(BaseSpr + bonusSpr, Level);
            MaxSp = CalculateMaxSp(BaseStr + bonusStr, Level);

            CurrentHp = Mathf.Min(CurrentHp, MaxHp);
            CurrentMp = Mathf.Min(CurrentMp, MaxMp);
            CurrentSp = Mathf.Min(CurrentSp, MaxSp);
        }

        int CalculateMaxHp(int totalCon, int level) => 100 + (totalCon * 10) + (level * 5);
        int CalculateMaxMp(int totalSpr, int level) => 50 + (totalSpr * 8) + (level * 3);
        int CalculateMaxSp(int totalStr, int level) => 100 + (totalStr * 5) + (level * 2);

        void LogSecurityEvent(string message)
        {
            Debug.LogWarning($"[SECURITY] {CharacterName}: {message}");
        }

        void OnInventoryUpdated(SyncList<ItemSyncData>.Operation op, int itemIndex,
            ItemSyncData oldItem, ItemSyncData newItem)
        {
            OnInventoryChanged?.Invoke();
        }

        void OnSkillsUpdated(SyncList<SkillSyncData>.Operation op, int itemIndex,
            SkillSyncData oldItem, SkillSyncData newItem)
        {
            OnSkillsChanged?.Invoke();
        }

        void OnEquipmentUpdated(SyncList<EquipmentSyncData>.Operation op, int itemIndex,
            EquipmentSyncData oldItem, EquipmentSyncData newItem)
        {
            OnEquipmentChanged?.Invoke();
        }

        void OnNameChanged(string oldName, string newName)
        {
            OnNameChangedEvent?.Invoke(newName);
        }

        [TargetRpc]
        void TargetRpcShowMessage(string message)
        {
            Debug.Log($"[Server Message] {message}");
        }

        void OnDestroy()
        {
            if (isServer && _initialized)
            {
                SaveToDatabase();
                Debug.Log($"[PlayerController] {CharacterName} destruído. Save final executado.");
            }
        }
    }
}