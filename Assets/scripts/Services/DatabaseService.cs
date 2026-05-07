using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using TOP.Data;
using TOP.Network;

namespace TOP.Services
{
    public class DatabaseService : MonoBehaviour
    {
        public static DatabaseService Instance { get; private set; }

        [Header("Configuracao")]
        [SerializeField] private string server = "localhost";
        [SerializeField] private string database = "top_unity";
        [SerializeField] private string uid = "root";
        [SerializeField] private string password = "sua_senha";
        [SerializeField] private int maxPoolSize = 100;
        [SerializeField] private int connectionTimeout = 30;

        private string _connectionString;

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
            await Task.Yield();
            return (true, 1, null);
        }

        public async Task<bool> CreateAccountAsync(string username, string password, string email)
        {
            await Task.Yield();
            return true;
        }

        public async Task<List<CharacterPreviewData>> GetCharacterListAsync(long accountId)
        {
            await Task.Yield();
            return new List<CharacterPreviewData>
            {
                new CharacterPreviewData
                {
                    Id = 1,
                    SlotIndex = 0,
                    Name = "Hero",
                    Gender = 0,
                    Job = 0,
                    Level = 1,
                    MapName = "Shaitan",
                    PosX = 0f,
                    PosY = 0f,
                    PosZ = 0f,
                    RotationY = 0,
                    HairStyle = 0,
                    HairColor = 0,
                    LastOnline = DateTime.Now
                }
            };
        }

        public async Task<(bool success, long charId, string error)> CreateCharacterAsync(
            long accountId, byte slot, string name, byte gender, byte job,
            byte hairStyle, byte hairColor)
        {
            await Task.Yield();
            return (true, DateTime.Now.Ticks, null);
        }

        public async Task<CharacterData> LoadCharacterAsync(long charId, long accountId)
        {
            await Task.Yield();
            return new CharacterData
            {
                Id = charId,
                AccountId = accountId,
                Name = "Hero",
                Job = 0,
                Gender = 0,
                Level = 1,
                CurrentHp = 100,
                CurrentMp = 50,
                CurrentSp = 30,
                PosX = 0,
                PosY = 1,
                PosZ = 0,
                RotationY = 0,
                MapName = "Shaitan",
                BaseStr = 10,
                BaseAgi = 10,
                BaseCon = 10,
                BaseSpr = 10,
                MaxHp = 100,
                MaxMp = 50,
                MaxSp = 30,
                Gold = 0,
                Inventory = new List<InventoryItemData>(),
                Skills = new List<CharacterSkillData>()
            };
        }

        public async Task SaveCharacterAsync(CharacterData data)
        {
            await Task.Yield();
            Debug.Log($"[DB] Saved character {data.Name}");
        }

        public async Task<bool> DeleteCharacterAsync(long charId, long accountId, string password)
        {
            await Task.Yield();
            return true;
        }

        public long CreateItem(long characterId, int itemId, int quantity, ushort slotIndex)
        {
            return DateTime.Now.Ticks;
        }

        public async Task SaveInventoryAsync(long characterId, List<InventoryItemData> items)
        {
            await Task.Yield();
        }

        public async Task LogAuditAsync(long? accountId, long? charId, string action,
            object detail, string ip)
        {
            await Task.Yield();
        }
    }
}
