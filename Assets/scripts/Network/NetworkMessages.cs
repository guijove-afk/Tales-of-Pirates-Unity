// Assets/Scripts/Network/NetworkMessages.cs
using Mirror;
using UnityEngine;
using System;
using TOP.Services;

namespace TOP.Network
{
    public struct LoginRequest : NetworkMessage
    {
        public string Username;
        public string Password;
    }
    
    public struct LoginResponse : NetworkMessage
    {
        public bool Success;
        public string SessionToken;
        public string ErrorCode;
        public string Message;
    }
    
    public struct CharacterListRequest : NetworkMessage { }
    
    public struct CharacterListResponse : NetworkMessage
    {
        public bool Success;
        public string Error;
        public CharacterPreviewData[] Characters;
    }
    
    public struct CreateCharacterRequest : NetworkMessage
    {
        public byte SlotIndex;
        public string Name;
        public byte Gender;
        public byte Job;
        public byte HairStyle;
        public byte HairColor;
    }
    
    public struct CreateCharacterResponse : NetworkMessage
    {
        public bool Success;
        public long CharacterId;
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
}