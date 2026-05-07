using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    [Header("Player Reference")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerEquipment playerEquipment;

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

    [Header("Drag Canvas")]
    [SerializeField] private Canvas dragCanvas;
    public Canvas DragCanvas => dragCanvas;

    [Header("Drag Handle")]
    [SerializeField] private Transform dragHandle;
    private bool isPanelDragging = false;
    private Vector2 dragOffset;
    private RectTransform panelRectTransform;

    private ItemSlotUI[] inventorySlots;
    private Dictionary<EquipmentSlot, EquipmentSlotUI> equipmentSlots = new Dictionary<EquipmentSlot, EquipmentSlotUI>();
    private int selectedSlot = -1;
    private bool isSetup = false;

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
        Invoke(nameof(Setup), 0.5f);
    }

    void Setup()
    {
        if (isSetup) return;

        Debug.Log("[InventoryUI] ====== SETUP INICIADO ======");

        PlayerInventory[] allInventories = FindObjectsByType<PlayerInventory>(FindObjectsInactive.Include);
        Debug.Log($"[InventoryUI] Encontrados {allInventories.Length} PlayerInventory na cena");
        
        foreach (var inv in allInventories)
        {
            Debug.Log($"[InventoryUI] PlayerInventory: {inv.name}, isLocalPlayer: {inv.isLocalPlayer}");
            if (inv.isLocalPlayer)
            {
                playerInventory = inv;
                break;
            }
        }

        if (playerInventory == null && allInventories.Length > 0)
        {
            playerInventory = allInventories[0];
            Debug.LogWarning("[InventoryUI] Nenhum player local. Usando primeiro disponível.");
        }

        if (playerInventory != null)
        {
            playerEquipment = playerInventory.GetComponent<PlayerEquipment>();
            Debug.Log($"[InventoryUI] ✅ Conectado: {playerInventory.name}");
        }
        else
        {
            Debug.LogError("[InventoryUI] ❌ Nenhum PlayerInventory encontrado!");
            return;
        }

        FindExistingSlots();

        playerInventory.OnSlotChanged += OnInventorySlotChanged;
        playerInventory.OnItemAdded += OnItemAdded;
        playerInventory.OnItemRemoved += OnItemRemoved;

        if (playerEquipment != null)
        {
            playerEquipment.OnItemEquipped += OnItemEquipped;
            playerEquipment.OnItemUnequipped += OnItemUnequipped;
            Debug.Log("[InventoryUI] ✅ PlayerEquipment conectado");
        }

        if (inventoryPanel != null)
        {
            panelRectTransform = inventoryPanel.GetComponent<RectTransform>();
            if (dragHandle == null)
            {
                var title = inventoryPanel.transform.Find("Title");
                if (title != null) dragHandle = title;
            }
        }

        if (dragCanvas == null)
        {
            var existing = GameObject.Find("DragCanvas");
            if (existing != null)
            {
                dragCanvas = existing.GetComponent<Canvas>();
                Debug.Log("[InventoryUI] ✅ DragCanvas encontrado");
            }
            else
            {
                Debug.LogWarning("[InventoryUI] ❌ DragCanvas NÃO encontrado!");
            }
        }

        inventoryPanel?.SetActive(false);
        tooltipPanel?.SetActive(false);
        isSetup = true;

        RefreshInventory();
        RefreshEquipment();
        
        Debug.Log("[InventoryUI] ====== SETUP COMPLETO ======");
    }

    void FindExistingSlots()
    {
        if (inventoryGrid != null)
        {
            var slots = inventoryGrid.GetComponentsInChildren<ItemSlotUI>(true);
            inventorySlots = slots;
            Debug.Log($"[InventoryUI] {slots.Length} ItemSlotUI encontrados");
            
            for (int i = 0; i < slots.Length; i++)
                slots[i].Initialize(i, this);
        }
        else
        {
            Debug.LogError("[InventoryUI] ❌ inventoryGrid é NULL!");
        }

        if (equipmentPanel != null)
        {
            var equipSlots = equipmentPanel.GetComponentsInChildren<EquipmentSlotUI>(true);
            Debug.Log($"[InventoryUI] {equipSlots.Length} EquipmentSlotUI encontrados");
            
            foreach (var slot in equipSlots)
            {
                slot.Initialize(this);
                if (!equipmentSlots.ContainsKey(slot.SlotType))
                    equipmentSlots.Add(slot.SlotType, slot);
            }
        }
        else
        {
            Debug.LogError("[InventoryUI] ❌ equipmentPanel é NULL!");
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (playerInventory != null)
        {
            playerInventory.OnSlotChanged -= OnInventorySlotChanged;
            playerInventory.OnItemAdded -= OnItemAdded;
            playerInventory.OnItemRemoved -= OnItemRemoved;
        }
        if (playerEquipment != null)
        {
            playerEquipment.OnItemEquipped -= OnItemEquipped;
            playerEquipment.OnItemUnequipped -= OnItemUnequipped;
        }
    }

    void Update()
    {
        if (!isSetup) return;
        if (Input.GetKeyDown(toggleKey))
            ToggleInventory();
        if (Input.GetKeyDown(KeyCode.Escape) && inventoryPanel.activeSelf)
            CloseInventory();
        HandlePanelDrag();
    }

    void OnInventorySlotChanged(int index, InventoryItem newSlot)
    {
        Debug.Log($"[InventoryUI] OnInventorySlotChanged index:{index}, IsEmpty:{newSlot.IsEmpty}");
        
        if (inventorySlots == null || index < 0 || index >= inventorySlots.Length) return;
        if (newSlot.IsEmpty)
            inventorySlots[index].Clear();
        else
        {
            var itemData = ItemDatabase.Instance?.GetItem(newSlot.itemId);
            Debug.Log($"[InventoryUI] Slot {index}: ItemData={(itemData?.itemName ?? "NULL")}");
            
            if (itemData is EquipmentData eq)
                inventorySlots[index].SetItem(eq, newSlot.quantity, newSlot.durability);
        }
    }

    void OnItemAdded(ItemData item, int quantity) 
    {
        Debug.Log($"[InventoryUI] OnItemAdded: {item?.itemName} x{quantity}");
    }
    
    void OnItemRemoved(ItemData item, int quantity) 
    {
        Debug.Log($"[InventoryUI] OnItemRemoved: {item?.itemName} x{quantity}");
    }

    void OnItemEquipped(EquipmentSlot slot, EquipmentData item)
    {
        Debug.Log($"[InventoryUI] OnItemEquipped: {slot} - {item?.itemName}");
        RefreshEquipment();
        RefreshInventory();
    }

    void OnItemUnequipped(EquipmentSlot slot)
    {
        Debug.Log($"[InventoryUI] OnItemUnequipped: {slot}");
        RefreshEquipment();
        RefreshInventory();
    }

    void HandlePanelDrag()
    {
        if (dragHandle == null || panelRectTransform == null) return;
        if (!inventoryPanel.activeSelf) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            panelRectTransform.parent.GetComponent<RectTransform>(),
            Input.mousePosition, null, out Vector2 localMousePos);

        if (Input.GetMouseButtonDown(0) && IsMouseOverDragHandle())
        {
            isPanelDragging = true;
            dragOffset = panelRectTransform.anchoredPosition - localMousePos;
        }

        if (isPanelDragging && Input.GetMouseButton(0))
            panelRectTransform.anchoredPosition = localMousePos + dragOffset;

        if (Input.GetMouseButtonUp(0))
            isPanelDragging = false;
    }

    bool IsMouseOverDragHandle()
    {
        if (dragHandle == null) return false;
        PointerEventData pointerData = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);
        foreach (var result in results)
        {
            if (result.gameObject.transform == dragHandle || result.gameObject.transform.IsChildOf(dragHandle))
                return true;
        }
        return false;
    }

    public void ToggleInventory()
    {
        if (inventoryPanel.activeSelf) CloseInventory();
        else OpenInventory();
    }

    // ✅ CORREÇÃO: Proteção contra chamada duplicada + ordem correta do cursor
    public void CloseInventory()
    {
        if (!inventoryPanel.activeSelf) return; // Já está fechado, ignora

        inventoryPanel.SetActive(false);
        tooltipPanel?.SetActive(false);
        isPanelDragging = false;

        // ✅ ORDEM CORRETA: primeiro esconde, DEPOIS trava
        // Se inverter, o cursor some mas fica preso em estado estranho
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        Debug.Log("[InventoryUI] Inventário fechado. Cursor locked.");
    }

    void OpenInventory()
    {
        if (inventoryPanel.activeSelf) return; // Já está aberto, ignora

        inventoryPanel.SetActive(true);
        RefreshInventory();
        RefreshEquipment();

        // ✅ ORDEM CORRETA: primeiro libera, DEPOIS mostra
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[InventoryUI] Inventário aberto. Cursor liberado.");
    }

    void RefreshInventory()
    {
        if (playerInventory == null || inventorySlots == null) return;
        
        Debug.Log($"[InventoryUI] RefreshInventory - Slots no inventário: {playerInventory.inventorySlots.Count}");
        
        for (int i = 0; i < inventorySlots.Length; i++)
        {
            if (i >= playerInventory.inventorySlots.Count)
            {
                inventorySlots[i].Clear();
                continue;
            }
            var slot = playerInventory.inventorySlots[i];
            if (slot.IsEmpty)
                inventorySlots[i].Clear();
            else
            {
                var itemData = ItemDatabase.Instance?.GetItem(slot.itemId);
                if (itemData is EquipmentData eq)
                {
                    Debug.Log($"[InventoryUI] Refresh slot {i}: {eq.itemName}");
                    inventorySlots[i].SetItem(eq, slot.quantity, slot.durability);
                }
                else
                {
                    inventorySlots[i].Clear();
                }
            }
        }
    }

    void RefreshEquipment()
    {
        if (playerEquipment == null) return;
        foreach (var kvp in equipmentSlots)
        {
            var item = playerEquipment.GetEquippedItem(kvp.Key);
            if (item != null)
                kvp.Value.SetItem(item);
            else
                kvp.Value.Clear();
        }
    }

    #region Interações

    public void OnSlotDoubleClick(int slotIndex)
    {
        Debug.Log($"[InventoryUI] OnSlotDoubleClick({slotIndex})");
        
        if (playerInventory == null) 
        {
            Debug.LogError("[InventoryUI] playerInventory é NULL!");
            return;
        }

        var slot = playerInventory.GetSlot(slotIndex);
        Debug.Log($"[InventoryUI] Slot {slotIndex}: IsEmpty={slot.IsEmpty}, itemId={slot.itemId}");
        
        if (slot.IsEmpty) return;

        var itemData = ItemDatabase.Instance?.GetItem(slot.itemId);
        Debug.Log($"[InventoryUI] Usando item: {itemData?.itemName ?? "NULL"}");

        if (itemData is EquipmentData equipData)
        {
            Debug.Log($"[InventoryUI] Item é equipamento ({equipData.slot}) → equipando via CmdUseItem");
        }
        
        playerInventory.CmdUseItem(slotIndex);
    }

    public void OnSlotSelected(int slotIndex)
    {
        Debug.Log($"[InventoryUI] OnSlotSelected({slotIndex})");
        selectedSlot = slotIndex;
        for (int i = 0; i < inventorySlots.Length; i++)
            inventorySlots[i].SetSelected(i == slotIndex);
    }

    public void OnSlotDropped(int fromIndex, int toIndex)
    {
        Debug.Log($"[InventoryUI] OnSlotDropped({fromIndex}, {toIndex})");
        playerInventory?.CmdMoveItem(fromIndex, toIndex);
    }

    public void OnSlotDraggedToEquipment(int inventoryIndex, EquipmentSlot targetSlot)
    {
        Debug.Log($"[InventoryUI] OnSlotDraggedToEquipment({inventoryIndex}, {targetSlot})");
        
        if (playerInventory == null) return;
        var slot = playerInventory.GetSlot(inventoryIndex);
        if (slot.IsEmpty) return;
        
        var equipData = ItemDatabase.Instance?.GetEquipment(slot.itemId);
        Debug.Log($"[InventoryUI] EquipData: {equipData?.itemName ?? "NULL"}, slot: {equipData?.slot}");
        
        if (equipData == null || equipData.slot != targetSlot)
        {
            Debug.LogWarning($"[InventoryUI] Item não pode ser equipado em {targetSlot}");
            return;
        }
        playerInventory.CmdUseItem(inventoryIndex);
    }

    public void OnEquipmentDoubleClick(EquipmentSlot slot)
    {
        Debug.Log($"[InventoryUI] OnEquipmentDoubleClick({slot})");
        playerInventory?.CmdUnequipItem(slot);
    }

    public void OnEquipmentDraggedToInventory(EquipmentSlot slot, int targetInventoryIndex)
    {
        Debug.Log($"[InventoryUI] OnEquipmentDraggedToInventory({slot}, {targetInventoryIndex})");
        playerInventory?.CmdUnequipItem(slot);
    }

    #endregion

    #region Tooltip

    public void ShowTooltip(EquipmentData item, Vector3 position)
    {
        if (item == null || tooltipPanel == null) return;
        tooltipName.text = item.itemName;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        if (item.bonusAttack > 0) sb.AppendLine($"ATAQUE +{item.bonusAttack}");
        if (item.bonusDefense > 0) sb.AppendLine($"DEFESA +{item.bonusDefense}");
        if (item.bonusSTR > 0) sb.AppendLine($"FOR +{item.bonusSTR}");
        if (item.bonusAGI > 0) sb.AppendLine($"AGI +{item.bonusAGI}");
        if (item.bonusHP > 0) sb.AppendLine($"HP +{item.bonusHP}");
        if (item.maxDurability > 0) sb.AppendLine($"Durabilidade: {item.durability}/{item.maxDurability}");
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

    #region Mouse Over

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
}