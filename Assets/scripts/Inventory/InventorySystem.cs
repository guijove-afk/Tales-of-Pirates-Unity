using UnityEngine;
using Mirror;
using TOP.Player;
using TOP.Core;
using TOP.Inventory;

namespace TOP.Inventory
{

    [RequireComponent(typeof(PlayerInventory))]
    [RequireComponent(typeof(PlayerEquipment))]
    public class InventorySystem : NetworkBehaviour
    {
        [Header("Config")]
        [SerializeField] private int inventorySlots = 48;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;

        private PlayerInventory playerInventory;
        private PlayerEquipment playerEquipment;

        public event System.Action OnInventoryChanged;

        void Awake()
        {
            playerInventory = GetComponent<PlayerInventory>();
            playerEquipment = GetComponent<PlayerEquipment>();
        }

        void Start()
        {
            playerInventory.OnSlotChanged += (index, slot) => OnInventoryChanged?.Invoke();
            playerInventory.OnItemAdded += (item, qty) => OnInventoryChanged?.Invoke();
            playerInventory.OnItemRemoved += (item, qty) => OnInventoryChanged?.Invoke();
        }

[Command]
public void CmdEquipFromInventory(ushort inventoryIndex)
{
    playerInventory.CmdUseItem(inventoryIndex);
}
        [Command]
        public void CmdUnequipToInventory(EquipmentSlot slot)
        {
            playerInventory.CmdUnequipItem(slot);
        }

        [Command]
        public void CmdMoveItem(int fromIndex, int toIndex)
        {
            playerInventory.CmdMoveItem((ushort)fromIndex, (ushort)toIndex);
        }

        [Command]
        public void CmdDragEquipToSlot(ushort inventoryIndex, EquipmentSlot targetSlot)
        {
            playerInventory.CmdUseItem(inventoryIndex);
        }

[Command]
public void CmdDragUnequipToSlot(EquipmentSlot slot, ushort targetInventoryIndex)
{
    // Atualmente você apenas desequipa
    playerInventory.CmdUnequipItem(slot);
    
    // Se no futuro você quiser que o item vá para um slot ESPECÍFICO do inventário
    // você precisará atualizar a lógica dentro do CmdUnequipItem para aceitar esse index.
}

        public int InventorySlots => playerInventory.totalSlots;
    }
}
