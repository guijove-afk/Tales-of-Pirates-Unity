using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using TOP.Inventory;
using TOP.Systems;
using TOP.Core;
using TOP.Gameplay;

namespace TOP.Player
{
    public class PlayerInventory : NetworkBehaviour
    {
        [SyncVar(hook = nameof(OnInventoryDataChanged))] 
        private string _inventoryData = "";

        private readonly InventoryItem[] _slots = new InventoryItem[40];

        public event Action OnInventoryChanged;
        public event Action<int, InventoryItem> OnSlotChanged;
        public event Action<InventoryItem, int> OnItemAdded;
        public event Action<InventoryItem, int> OnItemRemoved;

        public int totalSlots => _slots.Length;

        void Awake()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = null;
        }

        // ✅ NOVO: Quando o player spawna, se registra no InventoryUI
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // Aguarda 1 frame para garantir que o InventoryUI Instance já existe
            Invoke(nameof(RegisterInInventoryUI), 0.1f);
        }

        void RegisterInInventoryUI()
        {
            if (InventoryUI.Instance != null)
            {
                InventoryUI.Instance.SetPlayerInventory(this);
                Debug.Log($"[PlayerInventory] ✅ Registrado no InventoryUI: {name}");
            }
            else
            {
                Debug.LogWarning("[PlayerInventory] InventoryUI.Instance é NULL! Tentando novamente...");
                Invoke(nameof(RegisterInInventoryUI), 0.5f);
            }
        }

        #region Getters & Helpers

        public int FindEmptySlot()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null) return i;
            }
            return -1;
        }

        public int FindItemSlot(int itemId)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null && _slots[i].ItemId == itemId) return i;
            }
            return -1;
        }

        public InventoryItem GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Length) return null;
            return _slots[index];
        }

        public InventoryItem GetItem(ushort slotIndex)
        {
            return GetSlot(slotIndex);
        }

        #endregion

        #region Server Logic

        [Server]
        public bool AddItem(int itemId, int quantity, ushort slotIndex = 0)
        {
            if (slotIndex == 0 && _slots[0] != null)
            {
                int empty = FindEmptySlot();
                if (empty == -1) return false;
                slotIndex = (ushort)empty;
            }
            else if (slotIndex >= _slots.Length || _slots[slotIndex] != null)
            {
                int empty = FindEmptySlot();
                if (empty == -1) return false;
                slotIndex = (ushort)empty;
            }

            _slots[slotIndex] = new InventoryItem
            {
                ItemId = itemId,
                Quantity = quantity,
                SlotIndex = slotIndex
            };

            SerializeInventory();
            return true;
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

        #endregion

        #region Commands (Network)

        [Command]
        public void CmdAddItem(int itemId, int quantity)
        {
            int slot = FindEmptySlot();
            if (slot != -1)
            {
                AddItem(itemId, quantity, (ushort)slot);
            }
        }

        [Command]
        public void CmdAddItemDebug(int itemId, int quantity)
        {
            int slot = FindEmptySlot();
            if (slot != -1)
            {
                AddItem(itemId, quantity, (ushort)slot);
            }
        }

        [Command]
        public void CmdRemoveItemDebug(ushort slotIndex, int quantity)
        {
            RemoveItem(slotIndex, quantity);
        }

        [Command]
        public void CmdMoveItem(ushort fromSlot, ushort toSlot)
        {
            if (fromSlot >= _slots.Length || toSlot >= _slots.Length) return;

            InventoryItem temp = _slots[toSlot];
            _slots[toSlot] = _slots[fromSlot];
            if (_slots[toSlot] != null) _slots[toSlot].SlotIndex = toSlot;

            _slots[fromSlot] = temp;
            if (_slots[fromSlot] != null) _slots[fromSlot].SlotIndex = fromSlot;

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

            WorldItemManager worldItemManager = GameObject.FindAnyObjectByType<WorldItemManager>();   
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

        [Command]
        public void CmdUnequipItem(EquipmentSlot slot)
        {
            PlayerEquipment equipment = GetComponent<PlayerEquipment>();
            if (equipment != null)
            {
                equipment.UnequipItem(slot);
            }
        }

        [Command]
        public void CmdPickupWorldItem(NetworkIdentity worldItemIdentity)
        {
            WorldItem worldItem = worldItemIdentity?.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                worldItem.CmdPickup();
            }
        }

        #endregion

        #region Utility

        public void DebugGiveItem(int itemId, int quantity)
        {
            if (isLocalPlayer)
                CmdAddItemDebug(itemId, quantity);
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

        #endregion
    }
}