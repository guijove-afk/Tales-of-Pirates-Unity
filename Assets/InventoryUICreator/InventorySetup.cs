using UnityEngine;
using UnityEngine.UI;
using TMPro;

///
/// Adicione este script junto com InventoryUICreator.
/// Depois de criar a UI, clique em "Setup References" para conectar tudo.
///
[ExecuteInEditMode]
public class InventorySetup : MonoBehaviour
{
    [Header("Referências do Player")]
    [SerializeField] private InventorySystem inventorySystem;
    [SerializeField] private PlayerEquipment playerEquipment;
    
    [Header("Referência do Database")]
    [SerializeField] private ItemDatabase itemDatabase;  // <-- NOVO CAMPO!

    [ContextMenu("Setup References")]
    public void SetupReferences()
    {
        // Encontra ou adiciona InventoryUI
        var inventoryUI = GetComponent<InventoryUI>();
        if (inventoryUI == null)
            inventoryUI = gameObject.AddComponent<InventoryUI>();

        // Configura referências
        inventoryUI.SetField("inventorySystem", inventorySystem);
        inventoryUI.SetField("playerEquipment", playerEquipment);

        // Encontra panels
        var panel = transform.Find("InventoryPanel");
        if (panel != null)
        {
            inventoryUI.SetField("inventoryPanel", panel.gameObject);

            var grid = panel.Find("GridPanel/ScrollView/Viewport/InventoryGrid");
            if (grid != null) inventoryUI.SetField("inventoryGrid", grid);

            var equipPanel = panel.Find("EquipmentPanel");
            if (equipPanel != null) inventoryUI.SetField("equipmentPanel", equipPanel);

            var tooltip = panel.Find("TooltipPanel");
            if (tooltip != null) inventoryUI.SetField("tooltipPanel", tooltip.gameObject);

            // Configura tooltip texts
            var tooltipName = tooltip?.Find("ItemName")?.GetComponent<TextMeshProUGUI>();
            var tooltipStats = tooltip?.Find("Stats")?.GetComponent<TextMeshProUGUI>();

            if (tooltipName != null) inventoryUI.SetField("tooltipName", tooltipName);
            if (tooltipStats != null) inventoryUI.SetField("tooltipStats", tooltipStats);
        }

        // Configura slots de inventário
        var gridTransform = transform.Find("InventoryPanel/GridPanel/ScrollView/Viewport/InventoryGrid");
        if (gridTransform != null)
        {
            var slots = gridTransform.GetComponentsInChildren<ItemSlotUI>(true);
            foreach (var slot in slots)
            {
                // Configura referências internas do slot
                var bg = slot.transform.GetComponent<Image>();
                var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
                var qty = slot.transform.Find("Quantity")?.GetComponent<TextMeshProUGUI>();
                var hl = slot.transform.Find("Highlight")?.GetComponent<Image>();

                if (bg != null) slot.SetField("backgroundImage", bg);
                if (icon != null) slot.SetField("iconImage", icon);
                if (qty != null) slot.SetField("quantityText", qty);
                if (hl != null) slot.SetField("highlightImage", hl);
            }
        }

        // Configura slots de equipamento
        var equipTransform = transform.Find("InventoryPanel/EquipmentPanel");
        if (equipTransform != null)
        {
            var equipSlots = equipTransform.GetComponentsInChildren<EquipmentSlotUI>(true);
            foreach (var slot in equipSlots)
            {
                var bg = slot.transform.GetComponent<Image>();
                var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
                var label = slot.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
                var empty = slot.transform.Find("EmptyIndicator")?.gameObject;
                var nameTxt = slot.transform.Find("ItemName")?.GetComponent<TextMeshProUGUI>();

                if (bg != null) slot.SetField("backgroundImage", bg);
                if (icon != null) slot.SetField("iconImage", icon);
                if (label != null) slot.SetField("labelText", label);
                if (empty != null) slot.SetField("emptyIndicator", empty);
                if (nameTxt != null) slot.SetField("itemNameText", nameTxt);

                // Detecta tipo pelo nome
                if (slot.name.Contains("Weapon")) slot.SetField("slotType", EquipmentSlot.Weapon);
                else if (slot.name.Contains("Armor")) slot.SetField("slotType", EquipmentSlot.Armor);
                else if (slot.name.Contains("Helmet")) slot.SetField("slotType", EquipmentSlot.Helmet);
                else if (slot.name.Contains("Shield")) slot.SetField("slotType", EquipmentSlot.Shield);
                else if (slot.name.Contains("Gloves")) slot.SetField("slotType", EquipmentSlot.Gloves);
                else if (slot.name.Contains("Boots")) slot.SetField("slotType", EquipmentSlot.Boots);
            }
        }

        Debug.Log("[InventorySetup] Referências configuradas! Verifique no Inspector.");
    }
}

// Extension method para facilitar
public static class ReflectionHelper
{
    public static void SetField(this object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public);
        if (field != null)
            field.SetValue(obj, value);
    }
}