using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;
using TOP.Core;
using TOP.Player;
using TOP.Inventory;  // ✅ seu ItemDatabase    


namespace TOP.Inventory
{
    public class InventoryUI : MonoBehaviour
    {
        public static InventoryUI Instance { get; private set; }

        [Header("Player Reference")]
        [SerializeField] public PlayerInventory playerInventory;
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

            // ✅ Eventos CORRETOS do PlayerInventory corrigido
            playerInventory.OnInventoryChanged += RefreshInventory;
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
            }

            inventoryPanel?.SetActive(false);
            tooltipPanel?.SetActive(false);
            isSetup = true;

            RefreshInventory();
            RefreshEquipment();

            Debug.Log("[InventoryUI] ====== SETUP COMPLETO ======");
        }
        public void CloseInventory() {
            gameObject.SetActive(false);
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
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= RefreshInventory;
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

        // resto do código igual até OnInventorySlotChanged...

        // ✅ OnInventorySlotChanged CORRIGIDO
        void OnInventorySlotChanged(int index, InventoryItem newSlot)
        {
            Debug.Log($"[InventoryUI] OnInventorySlotChanged index:{index}, ItemId:{newSlot?.ItemId ?? 0}");

            if (inventorySlots == null || index < 0 || index >= inventorySlots.Length) return;

            if (newSlot == null)
            {
                inventorySlots[index].Clear();
                return;
            }

            var itemData = ItemDatabase.Instance?.GetItem(newSlot.ItemId);  // ✅ ItemId maiúsculo
            Debug.Log($"[InventoryUI] Slot {index}: ItemData={(itemData?.itemName ?? "NULL")}");

            inventorySlots[index].SetItem(itemData, newSlot.Quantity, 100);  // ✅ Quantity maiúsculo, durability fake
        }

        // ✅ OnItemAdded CORRIGIDO (recebe InventoryItem, não ItemData)
        void OnItemAdded(InventoryItem item, int slotIndex)
        {
            Debug.Log($"[InventoryUI] OnItemAdded: ItemId={item.ItemId} x{item.Quantity} no slot {slotIndex}");
            RefreshInventory();
        }

        // ✅ OnItemRemoved CORRIGIDO
        void OnItemRemoved(InventoryItem item, int slotIndex)
        {
            Debug.Log($"[InventoryUI] OnItemRemoved: ItemId={item.ItemId} x{item.Quantity} do slot {slotIndex}");
            RefreshInventory();
        }

        // ✅ OnItemEquipped CORRIGIDO (recebe InventoryItem, não EquipmentData)
        void OnItemEquipped(InventoryItem item, EquipmentSlot slot)
        {
            Debug.Log($"[InventoryUI] OnItemEquipped: {slot} - ItemId={item.ItemId}");
            RefreshEquipment();
            RefreshInventory();
        }

        void OnItemUnequipped(InventoryItem item, EquipmentSlot slot)
        {
            Debug.Log($"[InventoryUI] OnItemUnequipped: {slot} - ItemId={item.ItemId}");
            RefreshEquipment();
            RefreshInventory();
        }

        // ✅ RefreshInventory CORRIGIDO
        void RefreshInventory()
        {
            if (playerInventory == null || inventorySlots == null) return;

            Debug.Log($"[InventoryUI] RefreshInventory - Total slots: {inventorySlots.Length}");

            for (int i = 0; i < inventorySlots.Length; i++)
            {
                InventoryItem slot = playerInventory.GetSlot((ushort)i);  // ✅ GetSlot resolvido
                if (slot == null)
                {
                    inventorySlots[i].Clear();
                    continue;
                }

                var itemData = ItemDatabase.Instance?.GetItem(slot.ItemId);
                if (itemData != null)
                {
                    Debug.Log($"[InventoryUI] Refresh slot {i}: {itemData.itemName}");
                    inventorySlots[i].SetItem(itemData, slot.Quantity, 100);  // ✅ Quantity correto
                }
                else
                {
                    inventorySlots[i].Clear();
                }
            }
        }

void RefreshEquipment()
{
    if (playerEquipment == null) return;

    foreach (var kvp in equipmentSlots)
    {
        // 1. Pega o item equipado (que contém apenas ID e durabilidade)
        var equippedItem = playerEquipment.GetEquippedItem(kvp.Key); 

        if (equippedItem != null)
        {
            // 2. BUSCA os dados reais do equipamento no ItemDatabase usando o ID
            var data = ItemDatabase.Instance.GetEquipment(equippedItem.ItemId);

            if (data != null)
            {
                // 3. Agora passamos o 'data' (EquipmentData) que a UI espera
                kvp.Value.SetItem(data); 
            }
            else
            {
                kvp.Value.Clear();
            }
        }
        else
        {
            kvp.Value.Clear();
        }
    }
}

        // ✅ OnSlotDoubleClick CORRIGIDO
        public void OnSlotDoubleClick(int slotIndex)
        {
            Debug.Log($"[InventoryUI] OnSlotDoubleClick({slotIndex})");

            if (playerInventory == null) return;

            var slot = playerInventory.GetSlot((ushort)slotIndex);
            if (slot == null) return;

            var itemData = ItemDatabase.Instance?.GetItem(slot.ItemId);
            if (itemData == null) return;

            Debug.Log($"[InventoryUI] Double-click: {itemData.itemName}");

            if (itemData is EquipmentData equipData)
            {
                Debug.Log($"[InventoryUI] Equipando {equipData.slot}");
                playerInventory.CmdEquipItem((ushort)slotIndex, equipData.slot);  // ✅ CmdEquipItem correto
            }
            else
            {
                playerInventory.CmdUseItem( (ushort)slotIndex);  // Poções, etc.
            }
        }

        // resto dos métodos igual...
        public void OnSlotSelected(int slotIndex)
        {
            selectedSlot = slotIndex;
            for (int i = 0; i < inventorySlots.Length; i++)
                inventorySlots[i].SetSelected(i == slotIndex);
        }

        public void OnSlotDropped(int fromIndex, int toIndex)
        {
            playerInventory?.CmdMoveItem((ushort)fromIndex, (ushort)toIndex);
        }

        public void OnSlotDraggedToEquipment(int inventoryIndex, EquipmentSlot targetSlot)
        {
            var slot = playerInventory.GetSlot((ushort)inventoryIndex);
            if (slot == null) return;

            var equipData = ItemDatabase.Instance?.GetEquipment(slot.ItemId);
            if (equipData != null && equipData.slot == targetSlot)
            {
                playerInventory.CmdEquipItem((ushort)inventoryIndex, targetSlot);
            }
        }

   public void OnEquipmentDoubleClick(EquipmentSlot slot)
{
    // Mude de playerEquipment para playerInventory
    playerInventory?.CmdUnequipItem(slot);
}

public void OnEquipmentDraggedToInventory(EquipmentSlot slot, int targetInventoryIndex)
{
    // Mude de playerEquipment para playerInventory
    playerInventory?.CmdUnequipItem(slot);
}

        // resto dos métodos tooltip, drag, etc. permanecem iguais...
        public void ShowTooltip(ItemData item, Vector3 position)  // ✅ ItemData, não EquipmentData
        {
            if (item == null || tooltipPanel == null) return;
            tooltipName.text = item.itemName;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (item is EquipmentData equip)
            {
                if (equip.bonusAttack > 0) sb.AppendLine($"ATAQUE +{equip.bonusAttack}");
                if (equip.bonusDefense > 0) sb.AppendLine($"DEFESA +{equip.bonusDefense}");
                if (equip.bonusSTR > 0) sb.AppendLine($"FOR +{equip.bonusSTR}");
                if (equip.bonusAGI > 0) sb.AppendLine($"AGI +{equip.bonusAGI}");
                if (equip.bonusHP > 0) sb.AppendLine($"HP +{equip.bonusHP}");
            }
            tooltipStats.text = sb.ToString();

            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, position);
            tooltipPanel.transform.position = screenPos + new Vector2(80, 0);
            tooltipPanel.SetActive(true);
        }

        public void HideTooltip()
        {
            tooltipPanel?.SetActive(false);
        }

        // HandlePanelDrag, IsMouseOverInventory, ToggleInventory, etc. permanecem iguais...
        // [código igual ao seu original para drag, toggle, etc.]
    }
}