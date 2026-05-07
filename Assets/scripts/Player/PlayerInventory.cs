using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using TOP.Inventory;
using TOP.Core;

namespace TOP.Player
{
    public class PlayerInventory : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnInventoryDataChanged))] 
        private string _inventoryData = "";

        private readonly InventoryItem[] _slots = new InventoryItem[40];
        public event Action OnInventoryChanged;

        void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = null;
        }

        [Server]
        public void AddItem(int itemId, int quantity, ushort slotIndex)
        {
            if (slotIndex >= _slots.Length) return;

            _slots[slotIndex] = new InventoryItem
            {
                ItemId = itemId,
                Quantity = quantity,
                SlotIndex = slotIndex
            };

            SerializeInventory();
        }

        [Command]
        public void CmdAddItem(int itemId, int quantity)
        {
            for (ushort i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    AddItem(itemId, quantity, i);
                    return;
                }
            }
        }

        [Command]
        public void CmdMoveItem(ushort fromSlot, ushort toSlot)
        {
            if (fromSlot >= _slots.Length || toSlot >= _slots.Length) return;

            InventoryItem temp = _slots[toSlot];
            _slots[toSlot] = _slots[fromSlot];
            if (_slots[toSlot] != null)
                _slots[toSlot].SlotIndex = toSlot;

            _slots[fromSlot] = temp;
            if (_slots[fromSlot] != null)
                _slots[fromSlot].SlotIndex = fromSlot;

            SerializeInventory();
        }

        [Command]
        public void CmdEquipItem(ushort inventorySlot, EquipmentSlot targetSlot)
        {
            PlayerEquipment equipment = GetComponent<PlayerEquipment>();
            if (equipment == null) return;

            InventoryItem item = _slots[inventorySlot];
            if (item == null) return;

            equipment.CmdEquipItem(inventorySlot, targetSlot);
        }

        [Command]
        public void CmdDropItem(ushort slotIndex, int quantity, Vector3 dropPosition)
        {
            InventoryItem item = _slots[slotIndex];
            if (item == null || item.Quantity < quantity) return;

            WorldItemManager worldItemManager = FindObjectOfType<WorldItemManager>();
            if (worldItemManager != null)
            {
                worldItemManager.SpawnWorldItem(item.ItemId, quantity, dropPosition);
            }

            item.Quantity -= quantity;
            if (item.Quantity <= 0)
                _slots[slotIndex] = null;

            SerializeInventory();
        }

        [Command]
        public void CmdUseItem(ushort slotIndex)
        {
            PlayerConsumables consumables = GetComponent<PlayerConsumables>();
            if (consumables != null)
            {
                consumables.CmdUseItem(slotIndex);
            }
        }

        [Server]
        public void RemoveItem(ushort slotIndex, int quantity)
        {
            if (slotIndex >= _slots.Length || _slots[slotIndex] == null) return;

            _slots[slotIndex].Quantity -= quantity;
            if (_slots[slotIndex].Quantity <= 0)
                _slots[slotIndex] = null;

            SerializeInventory();
        }

        public InventoryItem GetItem(ushort slotIndex)
        {
            if (slotIndex >= _slots.Length) return null;
            return _slots[slotIndex];
        }

        [Server]
        public void SerializeInventory()
        {
            List<string> list = new List<string>();
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                {
                    list.Add($"{i}:{_slots[i].ItemId}:{_slots[i].Quantity}:{(_slots[i].IsEquipped ? 1 : 0)}");
                }
            }
            _inventoryData = string.Join(";", list);
        }

        void OnInventoryDataChanged(string oldValue, string newValue)
        {
            OnInventoryChanged?.Invoke();
        }
    }
}
