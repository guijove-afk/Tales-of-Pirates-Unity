using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Slot fixo de equipamento (arma, armadura, elmo, etc.).
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Config")]
    [SerializeField] private EquipmentSlot slotType;
    [SerializeField] private string slotLabel = "Mão Direita";

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private GameObject emptyIndicator;
    [SerializeField] private TextMeshProUGUI itemNameText; // Nome do item equipado

    [Header("Cores")]
    [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.4f, 0.6f, 0.9f);

    private InventoryUI inventoryUI;
    private EquipmentData currentItem;

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
        itemNameText.text = item.name;
        itemNameText.gameObject.SetActive(true);
        emptyIndicator.SetActive(false);
    }

    public void Clear()
    {
        currentItem = null;
        iconImage.enabled = false;
        itemNameText.gameObject.SetActive(false);
        emptyIndicator.SetActive(true);
        backgroundImage.color = normalColor;
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