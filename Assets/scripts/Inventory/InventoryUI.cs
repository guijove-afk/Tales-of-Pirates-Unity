using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Panels")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform inventoryGrid;
    [SerializeField] private Transform equipmentPanel;

    [Header("Tooltip")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipName;
    [SerializeField] private TextMeshProUGUI tooltipStats;

    [Header("Config")]
    [SerializeField] private KeyCode toggleKey = KeyCode.I;

    [Header("Drag Panel")]
    [SerializeField] private Transform dragHandle;
    private bool isDragging = false;
    private Vector2 dragOffset;
    private RectTransform panelRectTransform;

    private ItemSlotUI[] inventorySlots;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotUI>();
    private int selectedSlot = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (inventorySystem == null)
            inventorySystem = FindAnyObjectByType<InventorySystem>();
        if (itemDatabase == null)
            itemDatabase = ItemDatabase.Instance;

        if (inventorySystem != null && !inventorySystem.isLocalPlayer)
        {
            enabled = false;
            return;
        }

        FindExistingSlots();

        if (inventorySystem != null)
            inventorySystem.OnInventoryChanged += RefreshInventory;

        if (playerEquipment != null)
        {
            playerEquipment.OnItemEquipped += OnItemEquipped;
            playerEquipment.OnItemUnequipped += OnItemUnequipped;
        }

        if (inventoryPanel != null)
        {
            panelRectTransform = inventoryPanel.GetComponent<RectTransform>();
            
            if (dragHandle == null)
            {
                var title = inventoryPanel.transform.Find("Title");
                if (title != null)
                    dragHandle = title;
            }
        }

        inventoryPanel.SetActive(false);
        tooltipPanel.SetActive(false);
    }

    void FindExistingSlots()
    {
        if (inventoryGrid != null)
        {
            var slots = inventoryGrid.GetComponentsInChildren<ItemSlotUI>(true);
            inventorySlots = slots;
            
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].Initialize(i, this);
            }
        }

        if (equipmentPanel != null)
        {
            var equipSlots = equipmentPanel.GetComponentsInChildren<EquipmentSlotUI>(true);
            foreach (var slot in equipSlots)
            {
                slot.Initialize(this);
                equipmentSlots[slot.SlotType] = slot;
            }
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

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
        if (Input.GetKeyDown(toggleKey))
            ToggleInventory();

        if (Input.GetKeyDown(KeyCode.Escape) && inventoryPanel.activeSelf)
            CloseInventory();

        HandlePanelDrag();
    }

    #region Bloquear Input do Player

    public bool IsMouseOverInventory()
    {
        if (!inventoryPanel.activeSelf) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.transform.IsChildOf(inventoryPanel.transform) ||
                result.gameObject == inventoryPanel)
                return true;
        }

        return false;
    }

    #endregion

    #region Arrastar Painel

    void HandlePanelDrag()
    {
        if (dragHandle == null || panelRectTransform == null) return;
        if (!inventoryPanel.activeSelf) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRectTransform.parent.GetComponent<RectTransform>(),
            Input.mousePosition,
            null,
            out Vector2 localMousePos);

        if (Input.GetMouseButtonDown(0) && IsMouseOverDragHandle())
        {
            isDragging = true;
            dragOffset = panelRectTransform.anchoredPosition - localMousePos;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            panelRectTransform.anchoredPosition = localMousePos + dragOffset;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
    }

    bool IsMouseOverDragHandle()
    {
        if (dragHandle == null) return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        foreach (var result in results)
        {
            if (result.gameObject.transform == dragHandle || 
                result.gameObject.transform.IsChildOf(dragHandle))
                return true;
        }

        return false;
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

    public void CloseInventory()
    {
        inventoryPanel.SetActive(false);
        tooltipPanel.SetActive(false);
        isDragging = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OpenInventory()
    {
        inventoryPanel.SetActive(true);
        RefreshInventory();
        RefreshEquipment();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
                var equipData = ItemDatabase.Instance?.GetEquipment(invItem.itemId);
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

        var equipData = ItemDatabase.Instance?.GetEquipment(item.itemId);
        if (equipData == null) return;

        inventorySystem.CmdEquipFromInventory(slotIndex);
    }

    public void OnSlotSelected(int slotIndex)
    {
        selectedSlot = slotIndex;

        var item = inventorySystem.inventory[slotIndex];
        if (item.IsEmpty) return;

        var equipData = ItemDatabase.Instance?.GetEquipment(item.itemId);
        if (equipData != null)
        {
            Debug.Log($"[InventoryUI] Selecionado: {equipData.name}");
        }
    }

    public void OnSlotDropped(int fromIndex, int toIndex)
    {
        inventorySystem?.CmdMoveItem(fromIndex, toIndex);
    }

    // NOVO: Drag de inventário para slot de equipamento
    public void OnSlotDraggedToEquipment(int inventoryIndex, EquipmentSlot targetSlot)
    {
        if (inventorySystem == null) return;
        
        var item = inventorySystem.inventory[inventoryIndex];
        if (item.IsEmpty) return;

        var equipData = ItemDatabase.Instance?.GetEquipment(item.itemId);
        if (equipData == null || equipData.slot != targetSlot) return;

        inventorySystem.CmdEquipFromInventory(inventoryIndex);
    }

    // NOVO: Double-click no equipamento para desequipar
    public void OnEquipmentDoubleClick(EquipmentSlot slot)
    {
        if (inventorySystem == null) return;
        inventorySystem.CmdUnequipToInventory(slot);
    }

    // NOVO: Drag de equipamento para inventário
    public void OnEquipmentDraggedToInventory(EquipmentSlot slot, int targetInventoryIndex)
    {
        if (inventorySystem == null) return;
        
        if (targetInventoryIndex < 0 || targetInventoryIndex >= inventorySystem.inventory.Count) return;
        if (!inventorySystem.inventory[targetInventoryIndex].IsEmpty) return;

        inventorySystem.CmdUnequipToInventory(slot);
    }

    #endregion

    #region Tooltip

    public void ShowTooltip(EquipmentData item, Vector3 position)
    {
        if (item == null || tooltipPanel == null) return;

        tooltipName.text = item.name;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (item.bonusAttack > 0) sb.AppendLine($"ATAQUE +{item.bonusAttack}");
        if (item.bonusDefense > 0) sb.AppendLine($"DEFESA +{item.bonusDefense}");
        if (item.bonusSTR > 0) sb.AppendLine($"FOR +{item.bonusSTR}");
        if (item.bonusAGI > 0) sb.AppendLine($"AGI +{item.bonusAGI}");
        if (item.bonusHP > 0) sb.AppendLine($"HP +{item.bonusHP}");
        if (item.durability > 0) sb.AppendLine($"Durabilidade: {item.durability}/{item.maxDurability}");

        tooltipStats.text = sb.ToString();

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