// Assets/Scripts/Data/SyncData.cs
using System;
using Mirror;

namespace TOP.Data
{
    [Serializable]
    public struct ItemSyncData : IEquatable<ItemSyncData>
    {
        public long DbId;
        public ushort SlotIndex;
        public int ItemId;
        public int Quantity;
        public ushort Durability;
        public bool IsEquipped;
        
        public bool Equals(ItemSyncData other)
        {
            return DbId == other.DbId && SlotIndex == other.SlotIndex;
        }
    }
    
    [Serializable]
    public struct SkillSyncData : IEquatable<SkillSyncData>
    {
        public int SkillId;
        public byte Level;
        public ulong Exp;
        
        public bool Equals(SkillSyncData other)
        {
            return SkillId == other.SkillId;
        }
    }
    
    [Serializable]
    public struct EquipmentSyncData : IEquatable<EquipmentSyncData>
    {
        public byte SlotType;
        public long ItemDbId;
        
        public bool Equals(EquipmentSyncData other)
        {
            return SlotType == other.SlotType;
        }
    }
}