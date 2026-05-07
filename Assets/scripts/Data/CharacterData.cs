// Assets/Scripts/Data/CharacterData.cs
using System;
using System.Collections.Generic;

namespace TOP.Data
{
    /// <summary>
    /// Dados completos de personagem - usado para carregar/salvar no banco.
    /// Separado do PlayerController para não poluir o NetworkBehaviour.
    /// </summary>
    [Serializable]
    public class CharacterData
    {
        public long Id;
        public long AccountId;
        public string Name;
        public byte Job;            // 0=Novice, 1=Swordsman, 2=Hunter, 3=Explorer
        public byte Gender;         // 0=Male, 1=Female
        public byte HairStyle;
        public byte HairColor;
        public byte FaceStyle;
        
        public int Level;
        public ulong Exp;
        
        // Stats base
        public int BaseStr;
        public int BaseAgi;
        public int BaseCon;
        public int BaseSpr;
        
        // Stats calculados (salvos para evitar recalcular)
        public int MaxHp;
        public int MaxMp;
        public int MaxSp;
        public int CurrentHp;
        public int CurrentMp;
        public int CurrentSp;
        
        public ulong Gold;
        public string MapName;      // "garner", "magicsea", "darkswamp"
        public float PosX, PosY, PosZ;
        public float RotationY;
        
        public DateTime CreatedAt;
        public DateTime LastOnline;
        public bool IsDeleted;      // Soft delete
        
        // Relacionamentos
        public List<ItemData> Inventory = new List<ItemData>();
        public List<SkillData> Skills = new List<SkillData>();
        public List<HotbarSlotData> Hotbar = new List<HotbarSlotData>();
        public List<FriendData> Friends = new List<FriendData>();
    }
    
    [Serializable]
    public class ItemData
    {
        public long Id;             // ID único global no banco
        public long CharacterId;
        public ushort SlotIndex;
        public int ItemId;          // Referência ao ItemDatabase
        public int Quantity;
        public ushort Durability;
        public bool IsEquipped;
        public DateTime CreatedAt;
        public long? DroppedBy;     // Quem dropou (null se spawn do mundo)
    }
    
    [Serializable]
    public class SkillData
    {
        public long Id;
        public long CharacterId;
        public int SkillId;
        public byte Level;
        public ulong Exp;
        public DateTime LearnedAt;
    }
    
    [Serializable]
    public class HotbarSlotData
    {
        public byte SlotIndex;      // 0-9
        public byte Type;           // 0=Skill, 1=Item
        public int Id;              // SkillId ou ItemId
    }
    
    [Serializable]
    public class FriendData
    {
        public long Id;
        public long CharacterId;    // Meu char
        public long FriendCharId;   // Amigo
        public string FriendName;   // Denormalizado para performance
        public byte Status;         // 0=Pending, 1=Accepted, 2=Blocked
        public DateTime AddedAt;
    }
}