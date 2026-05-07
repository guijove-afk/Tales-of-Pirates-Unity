using Mirror;
using UnityEngine;
using System;
using TOP.Core;

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

    public struct DeleteCharacterRequest : NetworkMessage
    {
        public long CharacterId;
        public string Password;
    }

    public struct DeleteCharacterResponse : NetworkMessage
    {
        public bool Success;
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
        public ServerMessageType Type;
        public string Text;
    }

    public enum ServerMessageType
    {
        Info, Warning, Error, Kicked, Banned, Maintenance
    }

    public struct MoveItemRequest : NetworkMessage
    {
        public ushort FromSlot;
        public ushort ToSlot;
    }

    public struct EquipItemRequest : NetworkMessage
    {
        public ushort InventorySlot;
        public EquipmentSlot TargetSlot;
    }

    public struct DropItemRequest : NetworkMessage
    {
        public ushort SlotIndex;
        public int Quantity;
        public Vector3 DropPosition;
    }

    public struct UseItemRequest : NetworkMessage
    {
        public ushort SlotIndex;
    }

    public struct ChatMessage : NetworkMessage
    {
        public ChatChannel Channel;
        public string Text;
        public string TargetName;
    }

    public enum ChatChannel
    {
        World, Party, Guild, Whisper, System, Trade, Shout
    }

    [Serializable]
    public class CharacterPreviewData
    {
        public long Id;
        public byte SlotIndex;
        public string Name;
        public byte Gender;
        public byte Job;
        public int Level;
        public string MapName;
        public float PosX;
        public float PosY;
        public float PosZ;
        public float RotationY;
        public byte HairStyle;
        public byte HairColor;
        public DateTime? LastOnline;
    }
}
