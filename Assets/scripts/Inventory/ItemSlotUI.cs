using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image highlightImage;

    [Header("Cores")]
    [SerializeField] private Color normalColor = new Color(1, 1, 1, 0.8f);
    [SerializeField] private Color hoverColor = new Color(1, 1, 1, 1);
    [SerializeField] private Color selectedColor = new Color(0.3f, 0.7f, 1f, 1f);
    [SerializeField] private Color dragColor = new Color(1, 1, 1, 0.3f);

    private int slotIndex;
    private InventoryUI inventoryUI;
    private bool isDragging = false;
    private Vector3 originalIconPosition;
    private Transform originalIconParent;
    private CanvasGroup iconCanvasGroup;

    // ✅ NOVO: Detecção manual de double-click (mais confiável que eventData.clickCount)
    private float lastClickTime = -1f;
    private const float DOUBLE_CLICK_THRESHOLD = 0.3f;

    public int SlotIndex => slotIndex;
    public Image IconImage => iconImage;
    public bool HasItem => iconImage != null && iconImage.sprite != null && iconImage.enabled;

    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
        
        Debug.Log($"[ItemSlotUI] Slot {index} inicializado. inventoryUI: {(ui != null ? "OK" : "NULL")}");
        
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
            Debug.LogError($"[ItemSlotUI] Slot {index}: iconImage é NULL!");
        }
    }

    public void SetItem(EquipmentData item, int quantity, int durability)
    {
        if (item == null || iconImage == null)
        {
            Clear();
            return;
        }
        
        Debug.Log($"[ItemSlotUI] Slot {slotIndex}: SetItem({item.itemName}, qty:{quantity})");
        
        iconImage.sprite = item.icon;
        iconImage.color = Color.white;
        iconImage.enabled = true;
        iconImage.raycastTarget = true;

        if (quantityText != null)
        {
            if (quantity > 1)
            {
                quantityText.text = quantity.ToString();
                quantityText.gameObject.SetActive(true);
            }
            else
            {
                quantityText.gameObject.SetActive(false);
            }
        }
    }

    public void Clear()
    {
        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.color = Color.clear;
            iconImage.enabled = false;
            iconImage.raycastTarget = false;
        }
        if (quantityText != null)
            quantityText.gameObject.SetActive(false);
    }

    public void SetSelected(bool selected)
    {
        if (backgroundImage != null)
            backgroundImage.color = selected ? selectedColor : normalColor;
    }

    public void SetDragging(bool dragging)
    {
        if (backgroundImage != null)
            backgroundImage.color = dragging ? dragColor : normalColor;
        if (iconCanvasGroup != null)
            iconCanvasGroup.alpha = dragging ? 0.5f : 1f;
    }

    #region Pointer Events

    public void OnPointerClick(PointerEventData eventData)
    {
        // ✅ CORREÇÃO: Detecção manual de double-click
        // eventData.clickCount buga quando IDragHandler está no mesmo objeto
        float timeSinceLastClick = Time.time - lastClickTime;
        lastClickTime = Time.time;

        bool isDoubleClick = (timeSinceLastClick <= DOUBLE_CLICK_THRESHOLD) && HasItem;

        Debug.Log($"[ItemSlotUI] OnPointerClick slot {slotIndex}, timeSinceLastClick: {timeSinceLastClick:F3}, isDoubleClick: {isDoubleClick}, HasItem: {HasItem}");

        if (isDoubleClick)
        {
            Debug.Log($"[ItemSlotUI] ✅ DOUBLE-CLICK CONFIRMADO slot {slotIndex} → chamando OnSlotDoubleClick");
            inventoryUI?.OnSlotDoubleClick(slotIndex);
        }
        else
        {
            Debug.Log($"[ItemSlotUI] SINGLE-CLICK slot {slotIndex} → chamando OnSlotSelected");
            inventoryUI?.OnSlotSelected(slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[ItemSlotUI] OnBeginDrag slot {slotIndex}, HasItem: {HasItem}, inventoryUI: {(inventoryUI != null ? "OK" : "NULL")}");
        
        if (!HasItem || inventoryUI == null) 
        {
            Debug.LogWarning($"[ItemSlotUI] Drag BLOQUEADO! HasItem:{HasItem}, inventoryUI:{inventoryUI != null}");
            return;
        }
        
        isDragging = true;
        originalIconParent = iconImage.transform.parent;
        originalIconPosition = iconImage.transform.localPosition;

        if (inventoryUI.DragCanvas != null)
        {
            iconImage.transform.SetParent(inventoryUI.DragCanvas.transform);
            Debug.Log($"[ItemSlotUI] Ícone movido para DragCanvas");
        }
        else
        {
            iconImage.transform.SetParent(transform.root);
            Debug.LogWarning($"[ItemSlotUI] DragCanvas NULL, usando root");
        }

        iconImage.transform.SetAsLastSibling();
        iconImage.raycastTarget = false;
        SetDragging(true);
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
        
        Debug.Log($"[ItemSlotUI] OnEndDrag slot {slotIndex}");
        
        iconImage.transform.SetParent(originalIconParent);
        iconImage.transform.localPosition = originalIconPosition;
        iconImage.raycastTarget = true;
        SetDragging(false);

        var equipSlot = GetEquipmentSlotUnderMouse(eventData);
        if (equipSlot != null)
        {
            Debug.Log($"[ItemSlotUI] Drop em EquipmentSlot: {equipSlot.SlotType}");
            inventoryUI?.OnSlotDraggedToEquipment(slotIndex, equipSlot.SlotType);
            return;
        }

        var otherSlot = GetSlotUnderMouse(eventData);
        if (otherSlot != null && otherSlot != this)
        {
            Debug.Log($"[ItemSlotUI] Drop no slot {otherSlot.slotIndex}");
            inventoryUI?.OnSlotDropped(slotIndex, otherSlot.slotIndex);
            return;
        }
        
        Debug.Log($"[ItemSlotUI] Drop em lugar inválido");
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedEquip = eventData.pointerDrag?.GetComponent<EquipmentSlotUI>();
        if (draggedEquip != null)
        {
            Debug.Log($"[ItemSlotUI] OnDrop: recebendo Equipment {draggedEquip.SlotType}");
            inventoryUI?.OnEquipmentDraggedToInventory(draggedEquip.SlotType, slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isDragging && backgroundImage != null)
            backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isDragging && backgroundImage != null)
            backgroundImage.color = normalColor;
    }

    #endregion

    #region Helpers

    EquipmentSlotUI GetEquipmentSlotUnderMouse(PointerEventData eventData)
    {
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        foreach (var result in results)
        {
            var equipSlot = result.gameObject.GetComponent<EquipmentSlotUI>();
            if (equipSlot != null) return equipSlot;
        }
        return null;
    }

    ItemSlotUI GetSlotUnderMouse(PointerEventData eventData)
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