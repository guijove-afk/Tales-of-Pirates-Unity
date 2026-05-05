using UnityEngine;
using Mirror;

[RequireComponent(typeof(PlayerEquipment))]
[RequireComponent(typeof(PlayerInventory))]
public class InventorySystem : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int inventorySlots = 40;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    public readonly SyncList<InventoryItem> inventory = new SyncList<InventoryItem>();

    public event System.Action OnInventoryChanged;

    private PlayerEquipment playerEquipment;
    private PlayerInventory playerInventory;

    void Awake()
    {
        playerEquipment = GetComponent<PlayerEquipment>();
        playerInventory = GetComponent<PlayerInventory>();
    }

    public override void OnStartServer()
    {
        for (int i = 0; i < inventorySlots; i++)
            inventory.Add(InventoryItem.Empty);
    }

    public override void OnStartClient()
    {
        inventory.Callback += OnInventoryUpdated;
    }

    void OnInventoryUpdated(SyncList<InventoryItem>.Operation op, int index, InventoryItem oldItem, InventoryItem newItem)
    {
        OnInventoryChanged?.Invoke();
    }

    #region Comandos

    [Command]
    public void CmdEquipFromInventory(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= inventory.Count) return;

        var invItem = inventory[inventoryIndex];
        if (invItem.IsEmpty) return;

        var equipData = ItemDatabase.Instance?.GetEquipment(invItem.itemId);
        if (equipData == null) return;

        if (playerEquipment != null)
        {
            bool equipped = playerEquipment.EquipItem(equipData.itemId);
            if (equipped)
            {
                inventory[inventoryIndex] = InventoryItem.Empty;
            }
        }
    }

    [Command]
    public void CmdUnequipToInventory(EquipmentSlot slot)
    {
        if (playerEquipment == null) return;

        var equipped = playerEquipment.GetEquippedItem(slot);
        if (equipped == null) return;

        int emptySlot = FindEmptySlot();
        if (emptySlot == -1) return;

        inventory[emptySlot] = new InventoryItem
        {
            itemId = equipped.itemId,
            quantity = 1,
            durability = equipped.durability,
            refineLevel = 0
        };

        playerEquipment.UnequipItem(slot);
    }

    [Command]
    public void CmdMoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= inventory.Count) return;
        if (toIndex < 0 || toIndex >= inventory.Count) return;

        var fromItem = inventory[fromIndex];
        var toItem = inventory[toIndex];

        inventory[fromIndex] = toItem;
        inventory[toIndex] = fromItem;
    }

    [Command]
    public void CmdDragEquipToSlot(int inventoryIndex, EquipmentSlot targetSlot)
    {
        var equipped = playerEquipment.GetEquippedItem(targetSlot);
        if (equipped != null) return;

        CmdEquipFromInventory(inventoryIndex);
    }

    [Command]
    public void CmdDragUnequipToSlot(EquipmentSlot slot, int targetInventoryIndex)
    {
        if (targetInventoryIndex < 0 || targetInventoryIndex >= inventory.Count) return;
        if (!inventory[targetInventoryIndex].IsEmpty) return;

        CmdUnequipToInventory(slot);
    }

    #endregion

    #region Helpers

    [Server]
    public bool AddItem(int itemId, int quantity = 1, int durability = -1, int refineLevel = 0)
    {
        var equipData = ItemDatabase.Instance?.GetEquipment(itemId);
        if (equipData == null) return false;

        int emptySlot = FindEmptySlot();
        if (emptySlot == -1) return false;

        inventory[emptySlot] = new InventoryItem
        {
            itemId = itemId,
            quantity = 1,
            durability = durability == -1 ? equipData.maxDurability : durability,
            refineLevel = refineLevel
        };

        return true;
    }

    [Server]
    public int FindEmptySlot()
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].IsEmpty)
                return i;
        }
        return -1;
    }

    public EquipmentData GetEquipmentData(int itemId)
    {
        return ItemDatabase.Instance?.GetEquipment(itemId);
    }

    #endregion
}