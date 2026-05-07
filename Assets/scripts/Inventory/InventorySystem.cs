using UnityEngine;
using Mirror;

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
    public void CmdEquipFromInventory(int inventoryIndex)
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
        playerInventory.CmdMoveItem(fromIndex, toIndex);
    }

    [Command]
    public void CmdDragEquipToSlot(int inventoryIndex, EquipmentSlot targetSlot)
    {
        playerInventory.CmdUseItem(inventoryIndex);
    }

    [Command]
    public void CmdDragUnequipToSlot(EquipmentSlot slot, int targetInventoryIndex)
    {
        playerInventory.CmdUnequipItem(slot);
    }

    public int InventorySlots => playerInventory.totalSlots;
}