using UnityEngine;
using Mirror;
using TOP.Core;
using TOP.Inventory;
using TOP.Systems;

namespace TOP.Player
{
    public class PlayerConsumables : NetworkBehaviour
    {
        private PlayerStats _stats;
        private PlayerInventory _inventory;

        void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _inventory = GetComponent<PlayerInventory>();
        }

        [Command]
        public void CmdUseItem(ushort slotIndex)
        {
            InventoryItem item = _inventory.GetItem(slotIndex);
            if (item == null) return;

            ItemData itemData = ItemDatabase.Instance?.GetItem(item.ItemId);
            if (itemData == null || itemData.Type != ItemType.Consumable) return;

            ConsumableData consumable = itemData as ConsumableData;
            if (consumable == null) return;

            if (consumable.HpRestore > 0)
                _stats.Heal(consumable.HpRestore);

            if (consumable.MpRestore > 0)
                _stats.RestoreMp(consumable.MpRestore);

            if (consumable.SpRestore > 0)
                _stats.RestoreSp(consumable.SpRestore);

            if (consumable.BuffId > 0)
            {
                BuffManager buffManager = FindObjectOfType<BuffManager>();
                if (buffManager != null)
                {
                    buffManager.ApplyBuff(netId, consumable.BuffId, consumable.BuffLevel, consumable.Duration);
                }
            }

            _inventory.RemoveItem(slotIndex, 1);

            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.RpcShowMessage($"Usou {itemData.ItemName}", PlayerMessageType.Success);
            }
        }
    }
}
