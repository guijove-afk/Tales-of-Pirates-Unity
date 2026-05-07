using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TOP.Core;
using TOP.Inventory;  // ✅ seu namespace
using TOP.Systems;
using System;

namespace TOP.Player
{
    [System.Serializable]
    public class EquippedItem
    {
        public EquipmentSlot Slot;
        public int ItemId;
        public int ItemDatabaseId;
        public int Durability;
    }

    public class PlayerEquipment : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnEquipmentDataChanged))] 
        private string _equipmentData = "";

        private readonly Dictionary<EquipmentSlot, EquippedItem> _equippedItems = new Dictionary<EquipmentSlot, EquippedItem>();
        private PlayerStats _stats;
        private PlayerInventory _inventory;

        public event Action<InventoryItem, EquipmentSlot> OnItemEquipped;
        public event Action<InventoryItem, EquipmentSlot> OnItemUnequipped;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _inventory = GetComponent<PlayerInventory>();
        }

        [Server]
        public void EquipItem(InventoryItem item, EquipmentSlot slot)
        {
            // ✅ seu ItemDatabase no namespace TOP.Inventory
            EquipmentData itemData = ItemDatabase.Instance?.GetEquipment(item.ItemId);
            if (itemData == null) return;

            if (_equippedItems.ContainsKey(slot))
            {
                UnequipItem(slot);
            }

         _equippedItems[slot] = new EquippedItem
{
    Slot = slot,
    ItemId = item.ItemId,
    ItemDatabaseId = item.ItemId,
    Durability = item.Durability // Removido o ?? 100
};

            ApplyEquipmentStats(itemData, true);
            OnItemEquipped?.Invoke(item, slot);
            SerializeEquipment();
        }

        [Server]
        public void UnequipItem(EquipmentSlot slot)
        {
            if (!_equippedItems.ContainsKey(slot)) return;

            EquippedItem equipped = _equippedItems[slot];
            EquipmentData itemData = ItemDatabase.Instance?.GetEquipment(equipped.ItemDatabaseId);

            if (itemData != null)
                ApplyEquipmentStats(itemData, false);

            InventoryItem unequippedItem = new InventoryItem 
            { 
                ItemId = equipped.ItemId, 
                Quantity = 1 
            };
            OnItemUnequipped?.Invoke(unequippedItem, slot);

            _equippedItems.Remove(slot);
            SerializeEquipment();
        }

        [Server]
        void ApplyEquipmentStats(EquipmentData data, bool add)
        {
            int multiplier = add ? 1 : -1;

            _stats.AddBonusStrength(data.bonusSTR * multiplier);
            _stats.AddBonusAgility(data.bonusAGI * multiplier);
            _stats.AddBonusConstitution(data.bonusINT * multiplier);
            _stats.AddBonusSpirit(data.bonusINT * multiplier);
            _stats.AddBonusHp(data.bonusHP * multiplier);
            _stats.AddBonusMp(data.bonusMP * multiplier);
            _stats.AddBonusSp(0 * multiplier);
            _stats.AddBonusAttack(data.bonusAttack * multiplier);
            _stats.AddBonusDefense(data.bonusDefense * multiplier);
        }

        // resto igual...
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

        void UpdateVisuals() { }

        public int GetTotalAttackBonus()
        {
            int bonus = 0;
            foreach (EquippedItem item in _equippedItems.Values)
            {
                EquipmentData data = ItemDatabase.Instance?.GetEquipment(item.ItemDatabaseId);
                if (data != null)
                    bonus += data.bonusAttack;
            }
            return bonus;
        }

        public int GetTotalDefenseBonus()
        {
            int bonus = 0;
            foreach (EquippedItem item in _equippedItems.Values)
            {
                EquipmentData data = ItemDatabase.Instance?.GetEquipment(item.ItemDatabaseId);
                if (data != null)
                    bonus += data.bonusDefense;
            }
            return bonus;
        }

        public InventoryItem GetEquippedItem(EquipmentSlot slot)
        {
            if (_equippedItems.TryGetValue(slot, out EquippedItem equipped))
            {
                return new InventoryItem 
                { 
                    ItemId = equipped.ItemId, 
                    Quantity = 1 
                };
            }
            return null;
        }

        [Command]
        public void CmdEquipItem(ushort inventorySlot, EquipmentSlot targetSlot)
        {
            if (_inventory == null) return;

            InventoryItem item = _inventory.GetSlot(inventorySlot);
            if (item == null) return;

            EquipItem(item, targetSlot);
            item.IsEquipped = true;
            _inventory.SerializeInventory();
        }
    }
}