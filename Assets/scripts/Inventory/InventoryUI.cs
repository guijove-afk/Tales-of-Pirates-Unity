using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI principal do inventário.
/// Alt+E para abrir/fechar.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Player Reference")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private ItemDatabaseAdapter itemDatabaseAdapter;

    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private Transform equipmentPanel;

    [Header("Prefabs")]
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private GameObject equipmentSlotPrefab;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipName;
    [SerializeField] private TextMeshProUGUI tooltipStats;

    [Header("Config")]
    [SerializeField] private KeyCode toggleKey1 = KeyCode.LeftAlt;
    [SerializeField] private KeyCode toggleKey2 = KeyCode.E;

    private ItemSlotUI[] inventorySlots;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotUI>();
    private int selectedSlot = -1;

    void Start()
    {
        if (inventorySystem == null)
            inventorySystem = FindObjectOfType<InventorySystem>();
        if (itemDatabaseAdapter == null)
            itemDatabaseAdapter = ItemDatabaseAdapter.Instance;

        // Só inicializa se for o player local
        if (inventorySystem != null && !inventorySystem.isLocalPlayer)
        {
            enabled = false;
            return;
        }

        CreateInventorySlots();
        CreateEquipmentSlots();

        if (inventorySystem != null)
            inventorySystem.OnInventoryChanged += RefreshInventory;

        if (playerEquipment != null)
        {
            playerEquipment.OnItemEquipped += OnItemEquipped;
            playerEquipment.OnItemUnequipped += OnItemUnequipped;
        }

        inventoryPanel.SetActive(false);
        tooltipPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (inventorySystem != null)
            inventorySystem.OnInventoryChanged -= RefreshInventory;
        if (playerEquipment != null)
        {
            playerEquipment.OnItemEquipped -= OnItemEquipped;
            playerEquipment.OnItemUnequipped -= OnItemUnequipped;
        }
    }

    void Update()
    {
        if (Input.GetKey(toggleKey1) && Input.GetKeyDown(toggleKey2))
            ToggleInventory();

        if (Input.GetKeyDown(KeyCode.Escape) && inventoryPanel.activeSelf)
            CloseInventory();
    }

    #region Criação de Slots

    void CreateInventorySlots()
    {
        if (inventorySystem == null || slotPrefab == null) return;

        int slotCount = inventorySystem.inventory.Count;
        inventorySlots = new ItemSlotUI[slotCount];

        for (int i = 0; i < slotCount; i++)
        {
            var go = Instantiate(slotPrefab, inventoryGrid);
            inventorySlots[i] = go.GetComponent<ItemSlotUI>();
            inventorySlots[i].Initialize(i, this);
        }
    }

    void CreateEquipmentSlots()
    {
        if (equipmentSlotPrefab == null) return;

        // Cria slots baseado nos EquipmentSlot do seu enum
        EquipmentSlot[] slots = { 
            EquipmentSlot.Helmet, EquipmentSlot.Armor, EquipmentSlot.Weapon, 
            EquipmentSlot.Shield, EquipmentSlot.Gloves, EquipmentSlot.Boots, 
            EquipmentSlot.Cape, EquipmentSlot.Costume 
        };

        string[] labels = { "Elmo", "Armadura", "Arma", "Escudo", "Luvas", "Botas", "Capa", "Traje" };

        for (int i = 0; i < slots.Length; i++)
        {
            var go = Instantiate(equipmentSlotPrefab, equipmentPanel);
            var slotUI = go.GetComponent<EquipmentSlotUI>();
            // Configure o slotType via Inspector no prefab, ou aqui por código se exposto
            slotUI.Initialize(this);
            equipmentSlots[slots[i]] = slotUI;
        }
    }

    #endregion

    #region Abrir/Fechar

    public void ToggleInventory()
    {
        if (inventoryPanel.activeSelf)
            CloseInventory();
        else
            OpenInventory();
    }

    void OpenInventory()
    {
        inventoryPanel.SetActive(true);
        RefreshInventory();
        RefreshEquipment();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        tooltipPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    #endregion

    #region Refresh

    void RefreshInventory()
    {
        if (inventorySystem == null || inventorySlots == null) return;

        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i >= inventorySystem.inventory.Count)
            {
                inventorySlots[i].Clear();
                continue;
            }

            var invItem = inventorySystem.inventory[i];
            if (invItem.IsEmpty)
            {
                inventorySlots[i].Clear();
            }
            else
            {
                var equipData = itemDatabaseAdapter?.GetEquipmentById(invItem.itemId);
                inventorySlots[i].SetItem(equipData, invItem.quantity, invItem.durability);
            }
        }
    }

    void RefreshEquipment()
    {
        if (playerEquipment == null) return;

        foreach (var kvp in equipmentSlots)
        {
            var item = playerEquipment.GetEquippedItem(kvp.Key);
            kvp.Value.SetItem(item);
        }
    }

    void OnItemEquipped(EquipmentSlot slot, EquipmentData item)
    {
        RefreshEquipment();
    }

    void OnItemUnequipped(EquipmentSlot slot)
    {
        RefreshEquipment();
        RefreshInventory();
    }

    #endregion

    #region Interações

    public void OnSlotDoubleClick(int slotIndex)
    {
        if (inventorySystem == null) return;

        var item = inventorySystem.inventory[slotIndex];
        if (item.IsEmpty) return;

        var equipData = itemDatabaseAdapter?.GetEquipmentById(item.itemId);
        if (equipData == null) return;

        // Equipar (seu EquipmentData sempre é equipável)
        inventorySystem.CmdEquipFromInventory(slotIndex);
    }

    public void OnSlotSelected(int slotIndex)
    {
        selectedSlot = slotIndex;

        // Pode mostrar detalhes fixos em painel lateral
        var item = inventorySystem.inventory[slotIndex];
        if (item.IsEmpty) return;

        var equipData = itemDatabaseAdapter?.GetEquipmentById(item.itemId);
        if (equipData != null)
        {
            Debug.Log($"[InventoryUI] Selecionado: {equipData.name}");
        }
    }

    public void OnSlotDropped(int fromIndex, int toIndex)
    {
        inventorySystem?.CmdMoveItem(fromIndex, toIndex);
    }

    #endregion

    #region Tooltip

    public void ShowTooltip(EquipmentData item, Vector3 position)
    {
        if (item == null || tooltipPanel == null) return;

        tooltipName.text = item.name;

        // Monta texto de stats do seu EquipmentData
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (item.bonusAttack > 0) sb.AppendLine($"ATAQUE +{item.bonusAttack}");
        if (item.bonusDefense > 0) sb.AppendLine($"DEFESA +{item.bonusDefense}");
        if (item.bonusSTR > 0) sb.AppendLine($"FOR +{item.bonusSTR}");
        if (item.bonusAGI > 0) sb.AppendLine($"AGI +{item.bonusAGI}");
        if (item.bonusHP > 0) sb.AppendLine($"HP +{item.bonusHP}");
        if (item.durability > 0) sb.AppendLine($"Durabilidade: {item.durability}/{item.maxDurability}");

        tooltipStats.text = sb.ToString();

        // Posiciona
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, position);
        tooltipPanel.transform.position = screenPos + new Vector2(80, 0);

        tooltipPanel.SetActive(true);
    }

    public void HideTooltip()
    {
        tooltipPanel?.SetActive(false);
    }

    #endregion
}