using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Slot individual do grid de inventário.
/// </summary>
public class ItemSlotUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private Image highlightImage;

    [Header("Cores")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);

    private int slotIndex;
    private InventoryUI inventoryUI;
    private EquipmentData currentItem;  // Seu EquipmentData
    private int currentQuantity;

    public int SlotIndex => slotIndex;
    public EquipmentData CurrentItem => currentItem;
    public bool HasItem => currentItem != null;

    public void Initialize(int index, InventoryUI ui)
    {
        slotIndex = index;
        inventoryUI = ui;
        Clear();
    }

    public void SetItem(EquipmentData item, int quantity, int durability = -1)
    {
        currentItem = item;
        currentQuantity = quantity;

        if (item == null)
        {
            Clear();
            return;
        }

        // Ícone - seu EquipmentData não tem icon ainda, pode adicionar depois
        // Por enquanto usa um sprite default ou deixa vazio
        iconImage.enabled = true;
        iconImage.color = Color.white;

        // Quantidade
        quantityText.gameObject.SetActive(false); // Equipamentos não stackam

        // Nome do item como texto (temporário até ter ícone)
        // Você pode adicionar um TextMeshPro para nome aqui
    }

    public void Clear()
    {
        currentItem = null;
        currentQuantity = 0;

        iconImage.sprite = null;
        iconImage.enabled = false;
        quantityText.gameObject.SetActive(false);
        highlightImage.enabled = false;

        backgroundImage.color = normalColor;
    }

    public void SetHighlight(bool highlight)
    {
        highlightImage.enabled = highlight;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentItem == null) return;

        // Double click = equipar
        if (eventData.clickCount == 2)
        {
            inventoryUI?.OnSlotDoubleClick(slotIndex);
        }
        else if (eventData.clickCount == 1)
        {
            inventoryUI?.OnSlotSelected(slotIndex);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        backgroundImage.color = hoverColor;

        if (currentItem != null && inventoryUI != null)
        {
            inventoryUI.ShowTooltip(currentItem, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        backgroundImage.color = highlightImage.enabled ? hoverColor : normalColor;
        inventoryUI?.HideTooltip();
    }
}