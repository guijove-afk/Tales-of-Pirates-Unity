using System;
using Mirror;

namespace TOP.Inventory
{
    [Serializable]
    public class InventoryItem
    {
        // Propriedades PascalCase (usadas pelo sistema namespaced)
        public int ItemId;
        public int Quantity;
        public ushort SlotIndex;
        public int Durability;
        public int RefineLevel;
        public bool IsEquipped;

        // Aliases camelCase para compatibilidade com codigo legado
        public int itemId { get => ItemId; set => ItemId = value; }
        public int quantity { get => Quantity; set => Quantity = value; }
        public int durability { get => Durability; set => Durability = value; }
        public int refineLevel { get => RefineLevel; set => RefineLevel = value; }

        public bool IsEmpty => ItemId == 0;

        public static InventoryItem Empty => new InventoryItem { ItemId = 0, Quantity = 0, Durability = -1, RefineLevel = 0 };
    }
}
