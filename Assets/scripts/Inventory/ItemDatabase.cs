using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Extensão do ItemDatabase existente para suportar busca por int ID.
/// Coloque este script em um GameObject na cena (ex: GameManager) ou adapte seu ItemDatabase existente.
/// </summary>
public class ItemDatabaseAdapter : MonoBehaviour
{
    [System.Serializable]
    public class ItemMapping
    {
        public int itemId;
        public EquipmentData equipmentData;  // Seu EquipmentData existente
    }

    [Header("Mapeamento ID -> EquipmentData")]
    [SerializeField] private List<ItemMapping> itemMappings = new List<ItemMapping>();

    private static ItemDatabaseAdapter _instance;
    public static ItemDatabaseAdapter Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<ItemDatabaseAdapter>();
            return _instance;
        }
    }

    private Dictionary<int, EquipmentData> idToEquipment;
    private Dictionary<string, int> nameToId;

    void Awake()
    {
        _instance = this;
        BuildDictionary();
    }

    void BuildDictionary()
    {
        idToEquipment = new Dictionary<int, EquipmentData>();
        nameToId = new Dictionary<string, int>();

        foreach (var mapping in itemMappings)
        {
            if (mapping.equipmentData == null) continue;

            if (!idToEquipment.ContainsKey(mapping.itemId))
                idToEquipment.Add(mapping.itemId, mapping.equipmentData);

            if (!nameToId.ContainsKey(mapping.equipmentData.name))
                nameToId.Add(mapping.equipmentData.name, mapping.itemId);
        }
    }

    /// <summary>
    /// Busca EquipmentData por ID numérico (usado pelo inventário).
    /// </summary>
    public EquipmentData GetEquipmentById(int itemId)
    {
        if (idToEquipment == null) BuildDictionary();
        return idToEquipment.TryGetValue(itemId, out var item) ? item : null;
    }

    /// <summary>
    /// Busca ID numérico pelo nome do asset (usado para converter string -> int).
    /// </summary>
    public int GetIdByName(string itemName)
    {
        if (nameToId == null) BuildDictionary();
        return nameToId.TryGetValue(itemName, out var id) ? id : 0;
    }

    /// <summary>
    /// Verifica se o item é equipável.
    /// </summary>
    public bool IsEquippable(int itemId)
    {
        var item = GetEquipmentById(itemId);
        return item != null; // Seu EquipmentData sempre é equipável
    }

    /// <summary>
    /// Retorna o slot de equipamento do item.
    /// </summary>
    public EquipmentSlot GetEquipSlot(int itemId)
    {
        var item = GetEquipmentById(itemId);
        return item != null ? item.slot : EquipmentSlot.Weapon; // default
    }

    /// <summary>
    /// Retorna o ícone do item (se tiver, senão null).
    /// </summary>
    public Sprite GetIcon(int itemId)
    {
        var item = GetEquipmentById(itemId);
        // Seu EquipmentData não tem ícone ainda - adicione um campo Sprite icon nele se quiser
        return null;
    }

    /// <summary>
    /// Retorna o nome do item.
    /// </summary>
    public string GetItemName(int itemId)
    {
        var item = GetEquipmentById(itemId);
        return item != null ? item.name : "Desconhecido";
    }
}