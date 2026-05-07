// Assets/Scripts/Services/DatabaseService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TOP.Data;

namespace TOP.Services
{
    public class DatabaseService : MonoBehaviour
    {
        public static DatabaseService Instance;
        
        [Header("Configuração")]
        [SerializeField] private string server = "localhost";
        [SerializeField] private string database = "top_unity";
        [SerializeField] private string uid = "root";
        [SerializeField] private string password = "sua_senha";
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private int connectionTimeout = 30;
        
        private string _connectionString;
        private bool _initialized;
        
        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _connectionString = $"Server={server};Database={database};" +
                               $"User ID={uid};Password={password};" +
                               $"Max Pool Size={maxPoolSize};" +
                               $"Connection Timeout={connectionTimeout};";
        }
        
        public async Task<(bool success, long accountId, string error)> ValidateLoginAsync(
            string username, string password)
        {
            // TODO: Implementar quando MySqlConnector estiver instalado
            await Task.Yield();
            return (true, 1, null);
        }
        
        public async Task<List<CharacterInfo>> GetCharacterListAsync(long accountId)
        {
            await Task.Yield();
            return new List<CharacterInfo>();
        }
        
        public async Task<(bool success, long charId, string error)> CreateCharacterAsync(
            long accountId, byte slot, string name, byte gender, byte job, 
            byte hairStyle, byte hairColor)
        {
            await Task.Yield();
            return (true, 1, null);
        }
        
        public async Task<CharacterData> LoadCharacterAsync(long charId, long accountId)
        {
            await Task.Yield();
            return null;
        }
        
        public async Task SaveCharacterAsync(CharacterData data)
        {
            await Task.Yield();
        }
        
        public async Task<bool> CreateAccountAsync(string username, string password, string email)
        {
            await Task.Yield();
            return true;
        }
        
        public async Task LogAuditAsync(long? accountId, long? charId, string action, 
            object detail, string ip)
        {
            await Task.Yield();
        }
        
        public long CreateItem(long characterId, int itemId, int quantity, ushort slotIndex)
        {
            return 1;
        }
    }
    
    [System.Serializable]
    public class CharacterInfo
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
        public DateTime? LastOnline;
    }
}