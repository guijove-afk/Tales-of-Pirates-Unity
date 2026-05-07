// Assets/Scripts/Data/CharacterData.cs
using System;
using System.Collections.Generic;

namespace TOP.Data
{
    [Serializable]
    public class CharacterData
    {
        public long Id;
        public long AccountId;
        public string Name;
        public byte Job;
        public byte Gender;
        public byte HairStyle;
        public byte HairColor;
        public byte FaceStyle;
        
        public int Level;
        public ulong Exp;
        
        public int BaseStr;
        public int BaseAgi;
        public int BaseCon;
        public int BaseSpr;
        
        public int MaxHp;
        public int MaxMp;
        public int MaxSp;
        public int CurrentHp;
        public int CurrentMp;
        public int CurrentSp;
        
        public ulong Gold;
        public string MapName;
        public float PosX, PosY, PosZ;
        public float RotationY;
        
        public DateTime CreatedAt;
        public DateTime LastOnline;
        public bool IsDeleted;
        public bool IsOnline;
        
        public List<InventoryItemData> Inventory = new List<InventoryItemData>();
        public List<CharacterSkillData> Skills = new List<CharacterSkillData>();
    }
    
    [Serializable]
    public class InventoryItemData
    {
        public long Id;
        public long CharacterId;
        public ushort SlotIndex;
        public int ItemId;
        public int Quantity;
        public ushort Durability;
        public bool IsEquipped;
    }
    
    [Serializable]
    public class CharacterSkillData
    {
        public long Id;
        public long CharacterId;
        public int SkillId;
        public byte Level;
        public ulong Exp;
    }
}