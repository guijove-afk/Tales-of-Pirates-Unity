using System;
using Mirror;

[Serializable]
public struct InventoryItem : NetworkMessage
{
    public int itemId;      // 0 = slot vazio
    public int quantity;
    public int durability;
    public int refineLevel;

    public bool IsEmpty => itemId == 0;

    public static InventoryItem Empty => new InventoryItem { itemId = 0, quantity = 0, durability = -1, refineLevel = 0 };
}