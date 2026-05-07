using UnityEngine;
using Mirror;
using System.Collections.Generic;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Configuração")]
    [SyncVar] public int totalSlots = 48;

    [Header("Inventário")]
    public readonly SyncList<InventoryItem> inventorySlots = new SyncList<InventoryItem>();

    public event System.Action<int, InventoryItem> OnSlotChanged;
    public event System.Action<ItemData, int> OnItemAdded;
    public event System.Action<ItemData, int> OnItemRemoved;

    public override void OnStartServer()
    {
        base.OnStartServer();
        while (inventorySlots.Count < totalSlots)
            inventorySlots.Add(InventoryItem.Empty);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        inventorySlots.Callback += OnInventoryUpdated;
    }

    void OnDestroy()
    {
        inventorySlots.Callback -= OnInventoryUpdated;
    }

    void OnInventoryUpdated(SyncList<InventoryItem>.Operation op, int index, InventoryItem oldItem, InventoryItem newItem)
    {
        OnSlotChanged?.Invoke(index, newItem);
    }

    [Command]
    public void CmdUseItem(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= inventorySlots.Count) return;
        var item = inventorySlots[inventoryIndex];
        if (item.IsEmpty) return;

        var itemData = ItemDatabase.Instance?.GetItem(item.itemId);
        if (itemData == null) return;

        if (itemData is EquipmentData equipData)
        {
            var playerEquip = GetComponent<PlayerEquipment>();
            if (playerEquip != null && playerEquip.CanEquip(equipData))
            {
                inventorySlots[inventoryIndex] = InventoryItem.Empty;
                playerEquip.EquipItem(equipData, item.durability, item.refineLevel);
                RpcNotifyItemEquipped(itemData.itemName);
            }
        }
        else if (itemData.itemType == ItemType.Consumable)
        {
            item.quantity--;
            if (item.quantity <= 0)
                inventorySlots[inventoryIndex] = InventoryItem.Empty;
            else
                inventorySlots[inventoryIndex] = item;
            RpcNotifyItemUsed(itemData.itemName);
        }
    }

    [Command]
    public void CmdMoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || fromIndex >= inventorySlots.Count) return;
        if (toIndex < 0 || toIndex >= inventorySlots.Count) return;
        if (fromIndex == toIndex) return;

        var fromItem = inventorySlots[fromIndex];
        var toItem = inventorySlots[toIndex];

        if (!fromItem.IsEmpty && !toItem.IsEmpty && fromItem.itemId == toItem.itemId)
        {
            var itemData = ItemDatabase.Instance?.GetItem(fromItem.itemId);
            if (itemData != null && itemData.IsStackable)
            {
                int total = fromItem.quantity + toItem.quantity;
                if (total <= itemData.maxStack)
                {
                    inventorySlots[toIndex] = new InventoryItem
                    {
                        itemId = toItem.itemId,
                        quantity = total,
                        durability = toItem.durability,
                        refineLevel = toItem.refineLevel
                    };
                    inventorySlots[fromIndex] = InventoryItem.Empty;
                    return;
                }
            }
        }

        inventorySlots[fromIndex] = toItem;
        inventorySlots[toIndex] = fromItem;
    }

    [Command]
    public void CmdUnequipItem(EquipmentSlot slot)
    {
        var playerEquip = GetComponent<PlayerEquipment>();
        if (playerEquip == null) return;

        var equipped = playerEquip.GetEquippedItemStruct(slot);
        if (equipped.IsEmpty) return;

        int emptyIndex = FindEmptySlot();
        if (emptyIndex == -1)
        {
            TargetShowMessage(connectionToClient, "Inventário cheio!");
            return;
        }

        var itemData = ItemDatabase.Instance?.GetItem(equipped.itemId);
        if (itemData == null) return;

        playerEquip.UnequipItem(slot);
        inventorySlots[emptyIndex] = new InventoryItem
        {
            itemId = equipped.itemId,
            quantity = 1,
            durability = equipped.durability,
            refineLevel = equipped.refineLevel
        };
    }

    [Command]
    public void CmdAddItemDebug(int itemId, int quantity)
    {
        AddItem(itemId, quantity);
    }

    [Command]
    public void CmdRemoveItemDebug(int itemId, int quantity)
    {
        RemoveItem(itemId, quantity);
    }

    public void AddItem(int itemId, int quantity)
    {
        if (!isServer) return;
        var itemData = ItemDatabase.Instance?.GetItem(itemId);
        if (itemData == null) return;

        int remaining = quantity;

        if (itemData.IsStackable)
        {
            for (int i = 0; i < inventorySlots.Count && remaining > 0; i++)
            {
                if (inventorySlots[i].itemId == itemId)
                {
                    int canAdd = itemData.maxStack - inventorySlots[i].quantity;
                    if (canAdd > 0)
                    {
                        int add = Mathf.Min(canAdd, remaining);
                        var slot = inventorySlots[i];
                        slot.quantity += add;
                        inventorySlots[i] = slot;
                        remaining -= add;
                    }
                }
            }
        }

        while (remaining > 0)
        {
            int emptyIndex = FindEmptySlot();
            if (emptyIndex == -1) break;

            int add = itemData.IsStackable ? Mathf.Min(remaining, itemData.maxStack) : 1;
            inventorySlots[emptyIndex] = new InventoryItem
            {
                itemId = itemId,
                quantity = add,
                durability = (itemData is EquipmentData eq) ? eq.maxDurability : -1,
                refineLevel = 0
            };
            remaining -= add;

            if (!itemData.IsStackable) break;
        }

        if (quantity - remaining > 0)
            OnItemAdded?.Invoke(itemData, quantity - remaining);
    }

    public void RemoveItem(int itemId, int quantity)
    {
        if (!isServer) return;
        int remaining = quantity;

        for (int i = 0; i < inventorySlots.Count && remaining > 0; i++)
        {
            if (inventorySlots[i].itemId == itemId)
            {
                int remove = Mathf.Min(inventorySlots[i].quantity, remaining);
                var slot = inventorySlots[i];
                slot.quantity -= remove;
                if (slot.quantity <= 0)
                    inventorySlots[i] = InventoryItem.Empty;
                else
                    inventorySlots[i] = slot;
                remaining -= remove;
            }
        }

        if (quantity - remaining > 0)
        {
            var itemData = ItemDatabase.Instance?.GetItem(itemId);
            OnItemRemoved?.Invoke(itemData, quantity - remaining);
        }
    }

    public int FindEmptySlot()
    {
        for (int i = 0; i < inventorySlots.Count; i++)
            if (inventorySlots[i].IsEmpty) return i;
        return -1;
    }

    // ====== MÉTODO ADICIONADO PARA COMPATIBILIDADE ======

    public int FindItemSlot(int itemId)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].itemId == itemId)
                return i;
        }
        return -1; // Não encontrado
    }

    public InventoryItem GetSlot(int index)
    {
        if (index < 0 || index >= inventorySlots.Count) return InventoryItem.Empty;
        return inventorySlots[index];
    }

    [ClientRpc]
    void RpcNotifyItemEquipped(string itemName)
    {
        Debug.Log($"[PlayerInventory] Equipou: {itemName}");
    }

    [ClientRpc]
    void RpcNotifyItemUsed(string itemName)
    {
        Debug.Log($"[PlayerInventory] Usou: {itemName}");
    }

    [TargetRpc]
    void TargetShowMessage(NetworkConnection target, string message)
    {
        Debug.Log($"[PlayerInventory] {message}");
    }
}