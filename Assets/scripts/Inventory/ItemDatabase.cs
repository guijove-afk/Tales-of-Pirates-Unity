using UnityEngine;
using System.Collections.Generic;
using System.Linq;

///
/// ItemDatabase singleton - compatível com PlayerInventory e PlayerEquipment existentes.
/// Também suporta busca por int ID para o novo InventorySystem.
/// Coloque este script em um GameObject na cena (ex: GameManager).
///
public class ItemDatabase : MonoBehaviour
{
    [Header("Configuração")]
    [Tooltip("Pasta dentro de Resources/ onde estão os ScriptableObjects de items")]
    [SerializeField] private string itemsResourcePath = "Items";
    
    [Header("Mapeamento Manual (opcional)")]
    [Tooltip("Mapeamento explícito ID -> ItemData. Se deixar vazio, o sistema gera IDs automaticamente.")]
    [SerializeField] private List<ItemMapping> manualMappings = new List<ItemMapping>();

    [System.Serializable]
    public class ItemMapping
    {
        public int itemId;
        public ItemData itemData;
    }

    // Singleton
    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // Unity 6: FindAnyObjectByType (FindFirstObjectByType foi deprecado)
                _instance = FindAnyObjectByType<ItemDatabase>();
                if (_instance == null)
                {
                    Debug.LogError("[ItemDatabase] Nenhuma instância encontrada na cena! Crie um GameObject com ItemDatabase.");
                }
            }
            return _instance;
        }
    }

    // Dicionários
    private Dictionary<string, ItemData> itemsByStringId = new Dictionary<string, ItemData>();
    private Dictionary<string, EquipmentData> equipmentByStringId = new Dictionary<string, EquipmentData>();
    private Dictionary<int, ItemData> itemsByIntId = new Dictionary<int, ItemData>();
    private Dictionary<int, EquipmentData> equipmentByIntId = new Dictionary<int, EquipmentData>();
    private Dictionary<string, int> stringIdToIntId = new Dictionary<string, int>();
    private Dictionary<int, string> intIdToStringId = new Dictionary<int, string>();

    private bool isInitialized = false;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        Initialize();
    }

    ///
    /// Inicializa o banco de dados carregando todos os items.
    ///
    public void Initialize()
    {
        if (isInitialized) return;
        
        LoadItemsFromResources();
        BuildDictionaries();
        
        isInitialized = true;
        
        Debug.Log($"[ItemDatabase] Inicializado com {itemsByStringId.Count} items ({equipmentByStringId.Count} equipamentos)");
    }

    ///
    /// Carrega todos ScriptableObjects da pasta Resources/Items/
    ///
    private void LoadItemsFromResources()
    {
        // Carrega todos ItemData (incluindo subclasses como EquipmentData, ConsumableData)
        ItemData[] allItems = Resources.LoadAll<ItemData>(itemsResourcePath);
        
        foreach (var item in allItems)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            
            // Adiciona ao dicionário por string ID
            if (!itemsByStringId.ContainsKey(item.itemId))
            {
                itemsByStringId.Add(item.itemId, item);
                
                // Se for equipamento, adiciona também ao dicionário de equipamentos
                if (item is EquipmentData equip)
                {
                    equipmentByStringId.Add(item.itemId, equip);
                }
            }
            else
            {
                Debug.LogWarning($"[ItemDatabase] Item duplicado com ID '{item.itemId}': {item.name}");
            }
        }
    }

    ///
    /// Constrói os dicionários de mapeamento int <-> string
    ///
    private void BuildDictionaries()
    {
        itemsByIntId.Clear();
        equipmentByIntId.Clear();
        stringIdToIntId.Clear();
        intIdToStringId.Clear();

        // Primeiro aplica mapeamentos manuais
        foreach (var mapping in manualMappings)
        {
            if (mapping.itemData == null) continue;
            
            string stringId = mapping.itemData.itemId;
            if (string.IsNullOrEmpty(stringId)) continue;
            
            stringIdToIntId[stringId] = mapping.itemId;
            intIdToStringId[mapping.itemId] = stringId;
            itemsByIntId[mapping.itemId] = mapping.itemData;
            
            if (mapping.itemData is EquipmentData equip)
            {
                equipmentByIntId[mapping.itemId] = equip;
            }
        }

        // Depois gera IDs automáticos para items sem mapeamento manual
        int autoId = 1000;
        foreach (var kvp in itemsByStringId)
        {
            string stringId = kvp.Key;
            
            // Se já tem mapeamento manual, pula
            if (stringIdToIntId.ContainsKey(stringId)) continue;
            
            // Gera ID automático (evita conflito com manuais)
            while (intIdToStringId.ContainsKey(autoId)) autoId++;
            
            stringIdToIntId[stringId] = autoId;
            intIdToStringId[autoId] = stringId;
            itemsByIntId[autoId] = kvp.Value;
            
            if (kvp.Value is EquipmentData equip)
            {
                equipmentByIntId[autoId] = equip;
            }
            
            autoId++;
        }
    }

    #region Métodos Públicos - Compatibilidade com código existente (string ID)

    ///
    /// Busca ItemData por string ID (usado por PlayerInventory, PlayerEquipment, etc.)
    ///
    public ItemData GetItem(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (!isInitialized) Initialize();
        
        return itemsByStringId.TryGetValue(itemId, out var item) ? item : null;
    }

    ///
    /// Busca EquipmentData por string ID (usado por PlayerEquipment)
    ///
    public EquipmentData GetEquipment(string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        if (!isInitialized) Initialize();
        
        return equipmentByStringId.TryGetValue(itemId, out var item) ? item : null;
    }

    #endregion

    #region Métodos Públicos - Novo sistema com int ID (usado por InventorySystem)

    ///
    /// Busca ItemData por int ID (novo sistema)
    ///
    public ItemData GetItem(int itemId)
    {
        if (itemId <= 0) return null;
        if (!isInitialized) Initialize();
        
        return itemsByIntId.TryGetValue(itemId, out var item) ? item : null;
    }

    ///
    /// Busca EquipmentData por int ID (novo sistema)
    ///
    public EquipmentData GetEquipment(int itemId)
    {
        if (itemId <= 0) return null;
        if (!isInitialized) Initialize();
        
        return equipmentByIntId.TryGetValue(itemId, out var item) ? item : null;
    }

    ///
    /// Converte string ID para int ID
    ///
    public int GetIntId(string stringId)
    {
        if (string.IsNullOrEmpty(stringId)) return 0;
        if (!isInitialized) Initialize();
        
        return stringIdToIntId.TryGetValue(stringId, out var id) ? id : 0;
    }

    ///
    /// Converte int ID para string ID
    ///
    public string GetStringId(int intId)
    {
        if (intId <= 0) return null;
        if (!isInitialized) Initialize();
        
        return intIdToStringId.TryGetValue(intId, out var id) ? id : null;
    }

    ///
    /// Verifica se o item é equipável (por int ID)
    ///
    public bool IsEquippable(int itemId)
    {
        return GetEquipment(itemId) != null;
    }

    ///
    /// Retorna o slot de equipamento do item (por int ID)
    ///
    public EquipmentSlot GetEquipSlot(int itemId)
    {
        var equip = GetEquipment(itemId);
        return equip != null ? equip.slot : EquipmentSlot.Weapon;
    }

    ///
    /// Retorna o ícone do item (por int ID)
    ///
    public Sprite GetIcon(int itemId)
    {
        var item = GetItem(itemId);
        return item != null ? item.icon : null;
    }

    ///
    /// Retorna o nome do item (por int ID)
    ///
    public string GetItemName(int itemId)
    {
        var item = GetItem(itemId);
        return item != null ? item.itemName : "Desconhecido";
    }

    #endregion

    #region Helpers

    ///
    /// Retorna todos os items carregados
    ///
    public IReadOnlyCollection<ItemData> GetAllItems()
    {
        return itemsByStringId.Values;
    }

    ///
    /// Retorna todos os equipamentos carregados
    ///
    public IReadOnlyCollection<EquipmentData> GetAllEquipment()
    {
        return equipmentByStringId.Values;
    }

    #endregion
}