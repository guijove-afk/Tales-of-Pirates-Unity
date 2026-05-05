using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Config")]
    [SerializeField] private EquipmentSlot slotType;
    [SerializeField] private string slotLabel = "Mão Direita";

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private TextMeshProUGUI itemNameText;

    [Header("Cores")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.4f, 0.6f, 0.9f);

    private InventoryUI inventoryUI;
    private EquipmentData currentItem;
    private bool isDragging = false;

    public EquipmentSlot SlotType => slotType;

    public void Initialize(InventoryUI ui)
    {
        inventoryUI = ui;
        labelText.text = slotLabel;
        Clear();
    }

    public void SetItem(EquipmentData item)
    {
        currentItem = item;

        if (item == null)
        {
            Clear();
            return;
        }

        iconImage.enabled = true;
        iconImage.sprite = item.icon;
        iconImage.color = Color.white;
        itemNameText.text = item.itemName;
        itemNameText.gameObject.SetActive(true);
        emptyIndicator.SetActive(false);
    }

    public void Clear()
    {
        currentItem = null;
        iconImage.enabled = false;
        iconImage.sprite = null;
        itemNameText.gameObject.SetActive(false);
        emptyIndicator.SetActive(true);
        backgroundImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && currentItem != null)
        {
            inventoryUI?.OnEquipmentDoubleClick(slotType);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;

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

        var slot = GetInventorySlotUnderMouse(eventData);
        if (slot != null)
        {
            inventoryUI?.OnEquipmentDraggedToInventory(slotType, slot.SlotIndex);
        }
    }

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundImage.color = hoverColor;
        if (currentItem != null)
        {
            inventoryUI?.ShowTooltip(currentItem, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundImage.color = normalColor;
        inventoryUI?.HideTooltip();
    }
}