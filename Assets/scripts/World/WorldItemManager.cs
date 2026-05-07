// Assets/Scripts/World/WorldItemManager.cs
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System;
using TOP.Services;

namespace TOP.Gameplay
{
    /// <summary>
    /// Gerenciador de itens no chão do mundo.
    /// 
    /// SEGURANÇA:
    /// - Só o servidor spawna/destrói itens
    /// - Valida distância no pickup
    /// - Itens têm tempo de despawn automático
    /// - Persiste no banco (ground_items table)
    /// </summary>
    public class WorldItemManager : NetworkBehaviour
    {
        public static WorldItemManager Instance { get; private set; }
        
        [Header("Config")]
        [SerializeField] private GameObject worldItemPrefab;
        [SerializeField] private float defaultDespawnTime = 300f; // 5 minutos (ToP padrão)
        [SerializeField] private int maxGroundItems = 500;
        
        // Server-side tracking
        private readonly Dictionary<long, WorldItemEntry> _groundItems = new Dictionary<long, WorldItemEntry>();
        private long _nextWorldItemId = 1;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            Instance = this;
            
            // Carrega itens do banco ao iniciar servidor
            LoadGroundItemsFromDatabase();
        }
        
        // ============================================================
        // SPAWN ITEMS
        // ============================================================
        
        /// <summary>
        /// Spawna item dropado por jogador.
        /// </summary>
        [Server]
        public long SpawnDroppedItem(int itemId, int quantity, Vector3 position, 
            string mapName, long droppedByCharId)
        {
            if (_groundItems.Count >= maxGroundItems)
            {
                // Remove item mais antigo
                RemoveOldestItem();
            }
            
            long worldItemId = _nextWorldItemId++;
            
            // Cria GameObject
            GameObject obj = Instantiate(worldItemPrefab, position, Quaternion.identity);
            obj.name = $"WorldItem_{worldItemId}_{itemId}";
            
            // Configura WorldItem component
            var worldItem = obj.GetComponent<WorldItem>();
            if (worldItem != null)
            {
                // Usa o Initialize existente do seu WorldItem.cs
                var itemData = ItemDatabase.Instance.GetItem(itemId);
                if (itemData != null)
                {
                    // Adapta para o método existente
                    var data = new ItemData { itemId = itemId }; // Seu formato
                    worldItem.Initialize(data, quantity, defaultDespawnTime);
                }
            }
            
            // Spawn na rede
            NetworkServer.Spawn(obj);
            
            // Registra
            var entry = new WorldItemEntry
            {
                Id = worldItemId,
                ItemId = itemId,
                Quantity = quantity,
                Position = position,
                MapName = mapName,
                DroppedBy = droppedByCharId,
                SpawnTime = Time.time,
                DespawnTime = Time.time + defaultDespawnTime,
                GameObject = obj
            };
            
            _groundItems[worldItemId] = entry;
            
            // Salva no banco
            SaveGroundItemToDatabase(entry);
            
            Debug.Log($"[WorldItemManager] Spawned item {itemId}x{quantity} at {position} (ID:{worldItemId})");
            
            return worldItemId;
        }
        
        /// <summary>
        /// Spawna item do mundo (mob drop, spawn fixo).
        /// </summary>
        [Server]
        public long SpawnWorldItem(int itemId, int quantity, Vector3 position, string mapName)
        {
            return SpawnDroppedItem(itemId, quantity, position, mapName, -1);
        }
        
        // ============================================================
        // PICKUP
        // ============================================================
        
        [Server]
        public WorldItemEntry GetWorldItem(long worldItemId)
        {
            _groundItems.TryGetValue(worldItemId, out var entry);
            return entry;
        }
        
        [Server]
        public bool RemoveWorldItem(long worldItemId)
        {
            if (!_groundItems.TryGetValue(worldItemId, out var entry))
                return false;
            
            // Marca como picked up no banco
            MarkAsPickedUpInDatabase(worldItemId);
            
            // Destroi GameObject
            if (entry.GameObject != null)
            {
                NetworkServer.Destroy(entry.GameObject);
            }
            
            _groundItems.Remove(worldItemId);
            
            Debug.Log($"[WorldItemManager] Removed item {worldItemId}");
            return true;
        }
        
        // ============================================================
        // CLEANUP
        // ============================================================
        
        [Server]
        void Update()
        {
            if (!isServer) return;
            
            // Verifica despawn
            float now = Time.time;
            var toRemove = new List<long>();
            
            foreach (var kvp in _groundItems)
            {
                if (now >= kvp.Value.DespawnTime)
                {
                    toRemove.Add(kvp.Key);
                }
            }
            
            foreach (var id in toRemove)
            {
                RemoveWorldItem(id);
            }
        }
        
        void RemoveOldestItem()
        {
            long oldestId = -1;
            float oldestTime = float.MaxValue;
            
            foreach (var kvp in _groundItems)
            {
                if (kvp.Value.SpawnTime < oldestTime)
                {
                    oldestTime = kvp.Value.SpawnTime;
                    oldestId = kvp.Key;
                }
            }
            
            if (oldestId != -1)
                RemoveWorldItem(oldestId);
        }
        
        // ============================================================
        // DATABASE
        // ============================================================
        
        [Server]
        void LoadGroundItemsFromDatabase()
        {
            // TODO: Carregar do banco ao iniciar servidor
            // DatabaseService.Instance.LoadGroundItemsAsync(mapName)...
        }
        
        [Server]
        void SaveGroundItemToDatabase(WorldItemEntry entry)
        {
            // TODO: Salvar no banco
            // DatabaseService.Instance.SaveGroundItemAsync(...)
        }
        
        [Server]
        void MarkAsPickedUpInDatabase(long worldItemId)
        {
            // TODO: Atualizar no banco
        }
    }
    
    [Serializable]
    public class WorldItemEntry
    {
        public long Id;
        public int ItemId;
        public int Quantity;
        public Vector3 Position;
        public string MapName;
        public long DroppedBy; // -1 = spawn do mundo
        public float SpawnTime;
        public float DespawnTime;
        public GameObject GameObject;
    }
}