// Assets/Scripts/Network/NetworkMessages.cs
using Mirror;
using UnityEngine;
using System;

namespace TOP.Network
{
    // ============================================================
    // LOGIN / AUTHENTICATION
    // ============================================================
    
    public struct LoginRequest : NetworkMessage
    {
        public string Username;
        public string Password;
    }
    
    public struct LoginResponse : NetworkMessage
    {
        public bool Success;
        public string SessionToken;
        public string ErrorCode;    // INVALID_CREDENTIALS, RATE_LIMITED, ACCOUNT_BANNED, etc.
        public string Message;
    }
    
    public struct RegisterRequest : NetworkMessage
    {
        public string Username;
        public string Password;
        public string Email;
    }
    
    public struct RegisterResponse : NetworkMessage
    {
        public bool Success;
        public string Error;
    }
    
    // ============================================================
    // CHARACTER MANAGEMENT
    // ============================================================
    
    public struct CharacterListRequest : NetworkMessage { }
    
    public struct CharacterListResponse : NetworkMessage
    {
        public bool Success;
        public string Error;
        public CharacterPreviewData[] Characters;
    }
    
    public struct CreateCharacterRequest : NetworkMessage
    {
        public byte SlotIndex;      // 0-2
        public string Name;
        public byte Gender;         // 0=Male, 1=Female
        public byte Job;            // 0=Novice, 1=Swordsman, 2=Hunter, 3=Explorer
        public byte HairStyle;
        public byte HairColor;
    }
    
    public struct CreateCharacterResponse : NetworkMessage
    {
        public bool Success;
        public long CharacterId;
        public string Error;
    }
    
    public struct DeleteCharacterRequest : NetworkMessage
    {
        public long CharacterId;
    }
    
    public struct DeleteCharacterResponse : NetworkMessage
    {
        public bool Success;
        public string Error;
    }
    
    public struct SelectCharacterRequest : NetworkMessage
    {
        public long CharacterId;
    }
    
    public struct SelectCharacterResponse : NetworkMessage
    {
        public bool Success;
        public long CharacterId;
        public string MapName;
        public Vector3 Position;
        public float RotationY;
        public string Error;
    }
    
    // ============================================================
    // GAMEPLAY
    // ============================================================
    
    public struct ClientPing : NetworkMessage
    {
        public float ClientTime;
    }
    
    public struct ServerPong : NetworkMessage
    {
        public float ClientTime;
        public float ServerTime;
    }
    
    public struct ServerMessage : NetworkMessage
    {
        public MessageType Type;
        public string Text;
    }
    
    public enum MessageType
    {
        Info,
        Warning,
        Error,
        Kicked,
        Banned,
        Maintenance
    }
    
    // ============================================================
    // DATA STRUCTURES FOR MESSAGES
    // ============================================================
    
    [Serializable]
    public struct CharacterPreviewData
    {
        public long Id;
        public byte SlotIndex;
        public string Name;
        public byte Gender;
        public byte Job;
        public int Level;
        public string MapName;
        public Vector3 Position;
        public float RotationY;
        public byte HairStyle;
        public byte HairColor;
        public long LastOnlineTicks; // DateTime.Ticks ou 0 se nunca logou
    }
}