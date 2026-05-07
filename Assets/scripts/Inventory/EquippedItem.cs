using System;
using Mirror;

[Serializable]
public struct EquippedItem : NetworkMessage
{
    public int itemId;
    public int durability;
    public int[] gemSlots;
    public int refineLevel;

    public bool IsEmpty => itemId == 0;

    public static EquippedItem Empty => new EquippedItem { itemId = 0, durability = 0, gemSlots = null, refineLevel = 0 };
}