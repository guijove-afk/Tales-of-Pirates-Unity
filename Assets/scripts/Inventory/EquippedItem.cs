using System;
using Mirror;
using TOP.Core;

namespace TOP.Inventory
{
    [Serializable]
    public class EquippedItem
    {
        // Propriedades PascalCase (usadas pelo sistema namespaced)
        public EquipmentSlot Slot;
        public int ItemId;
        public int ItemDatabaseId;
        public int Durability;
        public int RefineLevel;
        public int[] GemSlots;

        // Aliases camelCase para compatibilidade com codigo legado
        public int itemId { get => ItemId; set => ItemId = value; }
        public int durability { get => Durability; set => Durability = value; }
        public int[] gemSlots { get => GemSlots; set => GemSlots = value; }
        public int refineLevel { get => RefineLevel; set => RefineLevel = value; }

        public bool IsEmpty => ItemId == 0;
    }
}
