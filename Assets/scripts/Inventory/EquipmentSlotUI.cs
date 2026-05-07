using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Config")]
    [SerializeField] private EquipmentSlot slotType;
    [SerializeField] private string slotLabel = "Slot";

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Header("Cores")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.4f, 0.6f, 0.9f);
    [SerializeField] private Color validDropColor = new Color(0.2f, 0.8f, 0.2f, 0.9f);

    private InventoryUI inventoryUI;
    private EquipmentData currentItem;
    private bool isDragging = false;
    private Vector3 originalIconPosition;
    private Transform originalIconParent;
    private CanvasGroup iconCanvasGroup;

    // ✅ NOVO: Detecção manual de double-click
    private float lastClickTime = -1f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

    public EquipmentSlot SlotType => slotType;
    public bool HasItem => currentItem != null;

    public void Initialize(InventoryUI ui)
    {
        inventoryUI = ui;
        
        Debug.Log($"[EquipmentSlotUI] {slotType} inicializado. inventoryUI: {(ui != null ? "OK" : "NULL")}");
        
        if (labelText != null)
            labelText.text = slotLabel;
            
        if (iconImage != null)
        {
            iconCanvasGroup = iconImage.GetComponent<CanvasGroup>();
            if (iconCanvasGroup == null)
                iconCanvasGroup = iconImage.gameObject.AddComponent<CanvasGroup>();
            originalIconParent = iconImage.transform.parent;
            originalIconPosition = iconImage.transform.localPosition;
        }
        else
        {
            Debug.LogError($"[EquipmentSlotUI] {slotType}: iconImage é NULL!");
        }
        
        Clear();
    }

    public void SetItem(EquipmentData item)
    {
        currentItem = item;
        if (item == null || iconImage == null)
        {
            Clear();
            return;
        }
        
        Debug.Log($"[EquipmentSlotUI] {slotType}: SetItem({item.itemName})");
        
        iconImage.enabled = true;
        iconImage.sprite = item.icon;
        iconImage.color = Color.white;
        iconImage.raycastTarget = true;
        
        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
            itemNameText.gameObject.SetActive(true);
        }
        if (emptyIndicator != null)
            emptyIndicator.SetActive(false);
    }

    public void Clear()
    {
        currentItem = null;
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            iconImage.color = Color.clear;
            iconImage.raycastTarget = false;
        }
        if (itemNameText != null)
            itemNameText.gameObject.SetActive(false);
        if (emptyIndicator != null)
            emptyIndicator.SetActive(true);
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    #region Pointer Events

    public void OnPointerClick(PointerEventData eventData)
    {
        // ✅ CORREÇÃO: Detecção manual de double-click
        float timeSinceLastClick = Time.time - lastClickTime;
        lastClickTime = Time.time;

        bool isDoubleClick = (timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD) && HasItem;

        Debug.Log($"[EquipmentSlotUI] OnPointerClick {slotType}, timeSinceLastClick: {timeSinceLastClick:F3}, isDoubleClick: {isDoubleClick}, HasItem: {HasItem}");
        
        if (isDoubleClick && currentItem != null)
        {
            Debug.Log($"[EquipmentSlotUI] ✅ DOUBLE-CLICK CONFIRMADO {slotType} → chamando OnEquipmentDoubleClick");
            inventoryUI?.OnEquipmentDoubleClick(slotType);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[EquipmentSlotUI] OnBeginDrag {slotType}, HasItem: {HasItem}");
        
        if (currentItem == null || iconImage == null || inventoryUI == null) 
        {
            Debug.LogWarning($"[EquipmentSlotUI] Drag BLOQUEADO! currentItem:{currentItem != null}, iconImage:{iconImage != null}, inventoryUI:{inventoryUI != null}");
            return;
        }
        
        isDragging = true;
        originalIconParent = iconImage.transform.parent;
        originalIconPosition = iconImage.transform.localPosition;

        if (inventoryUI.DragCanvas != null)
            iconImage.transform.SetParent(inventoryUI.DragCanvas.transform);
        else
            iconImage.transform.SetParent(transform.root);

        iconImage.transform.SetAsLastSibling();
        iconImage.raycastTarget = false;
        if (iconCanvasGroup != null) iconCanvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        iconImage.transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        isDragging = false;
        
        Debug.Log($"[EquipmentSlotUI] OnEndDrag {slotType}");
        
        iconImage.transform.SetParent(originalIconParent);
        iconImage.transform.localPosition = originalIconPosition;
        iconImage.raycastTarget = true;
        if (iconCanvasGroup != null) iconCanvasGroup.alpha = 1f;

        var slot = GetInventorySlotUnderMouse(eventData);
        if (slot != null)
        {
            Debug.Log($"[EquipmentSlotUI] Drop em ItemSlot {slot.SlotIndex}");
            inventoryUI?.OnEquipmentDraggedToInventory(slotType, slot.SlotIndex);
        }
        else
        {
            Debug.Log($"[EquipmentSlotUI] Drop em lugar inválido");
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedItem = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
        if (draggedItem != null && draggedItem.HasItem)
        {
            Debug.Log($"[EquipmentSlotUI] OnDrop: recebendo ItemSlot {draggedItem.SlotIndex} em {slotType}");
            inventoryUI?.OnSlotDraggedToEquipment(draggedItem.SlotIndex, slotType);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage != null)
        {
            var draggedItem = eventData.pointerDrag?.GetComponent<ItemSlotUI>();
            if (draggedItem != null && draggedItem.HasItem)
                backgroundImage.color = validDropColor;
            else
                backgroundImage.color = hoverColor;
        }
        if (currentItem != null)
            inventoryUI?.ShowTooltip(currentItem, transform.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null)
            backgroundImage.color = normalColor;
        inventoryUI?.HideTooltip();
    }

    #endregion

    #region Helpers

    ItemSlotUI GetInventorySlotUnderMouse(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            var slot = result.gameObject.GetComponent<ItemSlotUI>();
            if (slot != null) return slot;
        }
        return null;
    }

    #endregion
}