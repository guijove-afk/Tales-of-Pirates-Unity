using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TOP.Core;
using TOP.Inventory;

namespace TOP.Player
{
    public class PlayerEquipment : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnEquipmentDataChanged))] 
        private string _equipmentData = "";

        private readonly Dictionary<EquipmentSlot, EquippedItem> _equippedItems = new Dictionary<EquipmentSlot, EquippedItem>();
        private PlayerStats _stats;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
        }

        [Server]
        public void EquipItem(InventoryItem item, EquipmentSlot slot)
        {
            EquipmentData itemData = ItemDatabase.Instance?.GetItem(item.ItemId) as EquipmentData;
            if (itemData == null) return;

            // Desequipar item atual se houver
            if (_equippedItems.ContainsKey(slot))
            {
                UnequipItem(slot);
            }

            _equippedItems[slot] = new EquippedItem
            {
                Slot = slot,
                ItemId = item.Id,
                ItemDatabaseId = item.ItemId,
                Durability = item.Durability
            };

            ApplyEquipmentStats(itemData, true);
            SerializeEquipment();
        }

        [Server]
        public void UnequipItem(EquipmentSlot slot)
        {
            if (!_equippedItems.ContainsKey(slot)) return;

            EquippedItem equipped = _equippedItems[slot];
            EquipmentData itemData = ItemDatabase.Instance?.GetItem(equipped.ItemDatabaseId) as EquipmentData;

            if (itemData != null)
                ApplyEquipmentStats(itemData, false);

            _equippedItems.Remove(slot);
            SerializeEquipment();
        }

        [Server]
        void ApplyEquipmentStats(EquipmentData data, bool add)
        {
            int multiplier = add ? 1 : -1;

            _stats.AddBonusStrength(data.StrBonus * multiplier);
            _stats.AddBonusAgility(data.AgiBonus * multiplier);
            _stats.AddBonusConstitution(data.ConBonus * multiplier);
            _stats.AddBonusSpirit(data.SprBonus * multiplier);
            _stats.AddBonusHp(data.HpBonus * multiplier);
            _stats.AddBonusMp(data.MpBonus * multiplier);
            _stats.AddBonusSp(data.SpBonus * multiplier);
            _stats.AddBonusAttack(data.PhysicalAttack * multiplier);
            _stats.AddBonusDefense(data.PhysicalDefense * multiplier);
        }

        [Server]
        void SerializeEquipment()
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<EquipmentSlot, EquippedItem> kvp in _equippedItems)
            {
                list.Add($"{kvp.Key}:{kvp.Value.ItemDatabaseId}");
            }
            _equipmentData = string.Join(";", list);
        }

        void OnEquipmentDataChanged(string oldValue, string newValue)
        {
            UpdateVisuals();
        }

        void UpdateVisuals()
        {
            // TODO: Trocar modelos 3D baseado no equipamento
        }

        public int GetTotalAttackBonus()
        {
            int bonus = 0;
            foreach (EquippedItem item in _equippedItems.Values)
            {
                EquipmentData data = ItemDatabase.Instance?.GetItem(item.ItemDatabaseId) as EquipmentData;
                if (data != null)
                    bonus += data.PhysicalAttack;
            }
            return bonus;
        }

        public int GetTotalDefenseBonus()
        {
            int bonus = 0;
            foreach (EquippedItem item in _equippedItems.Values)
            {
                EquipmentData data = ItemDatabase.Instance?.GetItem(item.ItemDatabaseId) as EquipmentData;
                if (data != null)
                    bonus += data.PhysicalDefense;
            }
            return bonus;
        }

        [Command]
        public void CmdEquipItem(ushort inventorySlot, EquipmentSlot targetSlot)
        {
            PlayerInventory inv = GetComponent<PlayerInventory>();
            if (inv == null) return;

            InventoryItem item = inv.GetItem(inventorySlot);
            if (item == null) return;

            EquipItem(item, targetSlot);
            item.IsEquipped = true;
            inv.SerializeInventory(); // Forca sync
        }
    }
}
