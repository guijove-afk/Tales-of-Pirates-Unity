using UnityEngine;
using System.Collections.Generic;

public class ItemDatabase : MonoBehaviour
{
    [Header("Configuração")]
    [SerializeField] private string itemsResourcePath = "Items";

    private static ItemDatabase _instance;
    public static ItemDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<ItemDatabase>();
                if (_instance == null)
                    Debug.LogError("[ItemDatabase] Nenhuma instância encontrada!");
            }
            return _instance;
        }
    }

    private Dictionary<int, ItemData> itemsById = new Dictionary<int, ItemData>();
    private Dictionary<int, EquipmentData> equipmentById = new Dictionary<int, EquipmentData>();
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

    public void Initialize()
    {
        if (isInitialized) return;
        LoadItemsFromResources();
        isInitialized = true;
        Debug.Log($"[ItemDatabase] Inicializado com {itemsById.Count} items ({equipmentById.Count} equipamentos)");
    }

    void LoadItemsFromResources()
    {
        ItemData[] allItems = Resources.LoadAll<ItemData>(itemsResourcePath);
        foreach (var item in allItems)
        {
            if (item == null || item.itemId <= 0)
            {
                Debug.LogWarning($"[ItemDatabase] Item inválido encontrado em Resources/{itemsResourcePath}");
                continue;
            }
            if (!itemsById.ContainsKey(item.itemId))
            {
                itemsById.Add(item.itemId, item);
                if (item is EquipmentData equip)
                    equipmentById.Add(item.itemId, equip);
            }
            else
            {
                Debug.LogWarning($"[ItemDatabase] ID duplicado: {item.itemId}");
            }
        }
    }

    public ItemData GetItem(int itemId)
    {
        if (itemId <= 0) return null;
        if (!isInitialized) Initialize();
        return itemsById.TryGetValue(itemId, out var item) ? item : null;
    }

    public EquipmentData GetEquipment(int itemId)
    {
        if (itemId <= 0) return null;
        if (!isInitialized) Initialize();
        return equipmentById.TryGetValue(itemId, out var item) ? item : null;
    }

    public Sprite GetIcon(int itemId)
    {
        return GetItem(itemId)?.icon;
    }

    public string GetItemName(int itemId)
    {
        return GetItem(itemId)?.itemName ?? "Desconhecido";
    }

    // ====== MÉTODOS ADICIONADOS PARA COMPATIBILIDADE ======

    public IReadOnlyCollection<ItemData> GetAllItems()
    {
        if (!isInitialized) Initialize();
        return itemsById.Values;
    }

    public IReadOnlyCollection<EquipmentData> GetAllEquipment()
    {
        if (!isInitialized) Initialize();
        return equipmentById.Values;
    }

    public bool TryGetItem(int itemId, out ItemData item)
    {
        if (!isInitialized) Initialize();
        return itemsById.TryGetValue(itemId, out item);
    }
}