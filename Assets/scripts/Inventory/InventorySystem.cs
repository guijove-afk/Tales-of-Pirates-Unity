using UnityEngine;
using Mirror;

/// <summary>
/// Sistema de inventário de mochila (40 slots).
/// Integrado ao PlayerEquipment existente (usa string itemId).
/// </summary>
[RequireComponent(typeof(PlayerEquipment))]
public class InventorySystem : NetworkBehaviour
{
    [Header("Config")]
    [SerializeField] private int inventorySlots = 40;

    [Header("Referências")]
    [SerializeField] private ItemDatabaseAdapter itemDatabaseAdapter;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    // SyncList: inventário sincronizado pelo Mirror
    public readonly SyncList<InventoryItem> inventory = new SyncList<InventoryItem>();

    // Eventos para UI
    public event System.Action OnInventoryChanged;

    private PlayerEquipment playerEquipment;

    void Awake()
    {
        playerEquipment = GetComponent<PlayerEquipment>();

        if (itemDatabaseAdapter == null)
            itemDatabaseAdapter = ItemDatabaseAdapter.Instance;
    }

    public override void OnStartServer()
    {
        // Inicializa slots vazios
        for (int i = 0; i < inventorySlots; i++)
            inventory.Add(InventoryItem.Empty);

        if (showDebugLogs)
            Debug.Log($"[InventorySystem] Inventário inicializado com {inventorySlots} slots");
    }

    public override void OnStartClient()
    {
        inventory.Callback += OnInventoryUpdated;
    }

    void OnInventoryUpdated(SyncList<InventoryItem>.Operation op, int index, InventoryItem oldItem, InventoryItem newItem)
    {
        OnInventoryChanged?.Invoke();

        if (showDebugLogs)
        {
            string oldName = oldItem.IsEmpty ? "vazio" : $"ID:{oldItem.itemId}";
            string newName = newItem.IsEmpty ? "vazio" : $"ID:{newItem.itemId}";
            Debug.Log($"[InventorySystem] Slot {index}: {oldName} -> {newName}");
        }
    }

    #region Comandos do Jogador

    /// <summary>
    /// Equipa item do inventário. Converte int ID -> string nome para seu PlayerEquipment.
    /// </summary>
    [Command]
    public void CmdEquipFromInventory(int inventoryIndex)
    {
        if (inventoryIndex < 0 || inventoryIndex >= inventory.Count)
        {
            Debug.LogWarning($"[InventorySystem] Índice inválido: {inventoryIndex}");
            return;
        }

        var invItem = inventory[inventoryIndex];
        if (invItem.IsEmpty)
        {
            Debug.Log("[InventorySystem] Slot vazio, nada para equipar");
            return;
        }

        // Busca o EquipmentData pelo ID
        var equipData = itemDatabaseAdapter?.GetEquipmentById(invItem.itemId);
        if (equipData == null)
        {
            Debug.LogError($"[InventorySystem] Item ID {invItem.itemId} não encontrado no banco!");
            return;
        }

        // Usa seu PlayerEquipment existente (que recebe string)
        if (playerEquipment != null)
        {
            bool equipped = playerEquipment.EquipItem(equipData.name);

            if (equipped)
            {
                // Remove do inventário
                inventory[inventoryIndex] = InventoryItem.Empty;

                if (showDebugLogs)
                    Debug.Log($"[InventorySystem] {equipData.name} equipado com sucesso");
            }
            else
            {
                if (showDebugLogs)
                    Debug.Log($"[InventorySystem] Falha ao equipar {equipData.name} (requisitos não atendidos?)");
            }
        }
    }

    /// <summary>
    /// Desequipa item e volta para inventário.
    /// </summary>
    [Command]
    public void CmdUnequipToInventory(EquipmentSlot slot)
    {
        if (playerEquipment == null) return;

        // Pega item equipado (seu EquipmentData)
        var equipped = playerEquipment.GetEquippedItem(slot);
        if (equipped == null) return;

        // Procura slot vazio no inventário
        int emptySlot = FindEmptySlot();
        if (emptySlot == -1)
        {
            Debug.LogWarning("[InventorySystem] Inventário cheio! Não pode desequipar.");
            return;
        }

        // Converte EquipmentData -> int ID
        int itemId = itemDatabaseAdapter?.GetIdByName(equipped.name) ?? 0;
        if (itemId == 0)
        {
            Debug.LogError($"[InventorySystem] {equipped.name} não mapeado no ItemDatabaseAdapter!");
            return;
        }

        // Adiciona ao inventário
        inventory[emptySlot] = new InventoryItem 
        { 
            itemId = itemId, 
            quantity = 1,
            durability = equipped.durability,
            refineLevel = 0
        };

        // Desequipa usando seu método existente
        playerEquipment.UnequipItem(slot);

        if (showDebugLogs)
            Debug.Log($"[InventorySystem] {equipped.name} desequipado para slot {emptySlot}");
    }

    /// <summary>
    /// Move item entre slots do inventário.
    /// </summary>
    [Command]
    public void CmdMoveItem(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return;
        if (fromIndex < 0 || fromIndex >= inventory.Count) return;
        if (toIndex < 0 || toIndex >= inventory.Count) return;

        var fromItem = inventory[fromIndex];
        var toItem = inventory[toIndex];

        // Troca simples (seu jogo não tem stackable ainda, todos são únicos)
        inventory[fromIndex] = toItem;
        inventory[toIndex] = fromItem;
    }

    #endregion

    #region Admin / Debug / Loot

    /// <summary>
    /// Adiciona item ao inventário (loot, recompensa, admin).
    /// </summary>
    [Server]
    public bool AddItem(int itemId, int quantity = 1, int durability = -1, int refineLevel = 0)
    {
        var equipData = itemDatabaseAdapter?.GetEquipmentById(itemId);
        if (equipData == null)
        {
            Debug.LogError($"[InventorySystem] Item ID {itemId} não encontrado!");
            return false;
        }

        // Procura slot vazio (equipamentos não stackam)
        int emptySlot = FindEmptySlot();
        if (emptySlot == -1)
        {
            Debug.LogWarning($"[InventorySystem] Inventário cheio!");
            return false;
        }

        inventory[emptySlot] = new InventoryItem 
        { 
            itemId = itemId, 
            quantity = 1,
            durability = durability == -1 ? equipData.maxDurability : durability,
            refineLevel = refineLevel
        };

        if (showDebugLogs)
            Debug.Log($"[InventorySystem] Adicionado {equipData.name} no slot {emptySlot}");

        return true;
    }

    [Server]
    public bool RemoveItem(int itemId)
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].itemId == itemId)
            {
                inventory[i] = InventoryItem.Empty;
                return true;
            }
        }
        return false;
    }

    [Server]
    public int FindEmptySlot()
    {
        for (int i = 0; i < inventory.Count; i++)
        {
            if (inventory[i].IsEmpty)
                return i;
        }
        return -1;
    }

    [Server]
    public int CountEmptySlots()
    {
        int count = 0;
        foreach (var item in inventory)
        {
            if (item.IsEmpty) count++;
        }
        return count;
    }

    #endregion

    #region Helpers

    public EquipmentData GetEquipmentData(int itemId)
    {
        return itemDatabaseAdapter?.GetEquipmentById(itemId);
    }

    #endregion
}