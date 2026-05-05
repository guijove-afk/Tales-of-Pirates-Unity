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

    private int slotIndex;
    private InventoryUI inventoryUI;
    private bool isDragging = false;

    public int SlotIndex => slotIndex;

    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
    }

    public void SetItem(EquipmentData item, int quantity, int durability)
    {
        if (item == null)
        {
            Clear();
            return;
        }

        iconImage.sprite = item.icon;
        iconImage.color = Color.white;
        iconImage.enabled = true;

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

    public void Clear()
    {
        iconImage.sprite = null;
        iconImage.color = Color.clear;
        iconImage.enabled = false;
        quantityText.gameObject.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2)
        {
            inventoryUI?.OnSlotDoubleClick(slotIndex);
        }
        else if (eventData.clickCount == 1)
        {
            inventoryUI?.OnSlotSelected(slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (iconImage.sprite == null) return;

        isDragging = true;
        iconImage.transform.SetParent(inventoryUI.transform);
        iconImage.transform.SetAsLastSibling();
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

        iconImage.transform.SetParent(transform);
        iconImage.transform.localPosition = Vector3.zero;

        var equipSlot = GetEquipmentSlotUnderMouse(eventData);
        if (equipSlot != null)
        {
            inventoryUI?.OnSlotDraggedToEquipment(slotIndex, equipSlot.SlotType);
        }
        else
        {
            var otherSlot = GetSlotUnderMouse(eventData);
            if (otherSlot != null && otherSlot != this)
            {
                inventoryUI?.OnSlotDropped(slotIndex, otherSlot.slotIndex);
            }
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        var draggedEquip = eventData.pointerDrag?.GetComponent<EquipmentSlotUI>();
        if (draggedEquip != null)
        {
            inventoryUI?.OnEquipmentDraggedToInventory(draggedEquip.SlotType, slotIndex);
        }
    }

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundImage.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundImage.color = normalColor;
    }
}