using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Reflection;

[ExecuteInEditMode]
public class InventorySetup : MonoBehaviour
{
    [Header("Referências do Player (deixe vazio para auto-detectar)")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerEquipment playerEquipment;

    [ContextMenu("Setup References")]
    public void SetupReferences()
    {
        var inventoryUI = GetComponent<InventoryUI>();
        if (inventoryUI == null)
        {
            inventoryUI = gameObject.AddComponent<InventoryUI>();
            Debug.Log("[InventorySetup] InventoryUI adicionado.");
        }

        var panel = transform.Find("InventoryPanel");
        if (panel != null)
        {
            SetField(inventoryUI, "inventoryPanel", panel.gameObject);
            Debug.Log("[InventorySetup] InventoryPanel configurado.");

            var grid = panel.Find("InventoryGrid");
            if (grid != null)
            {
                SetField(inventoryUI, "inventoryGrid", grid);
                Debug.Log("[InventorySetup] InventoryGrid configurado.");
            }

            var equipPanel = panel.Find("EquipmentPanel");
            if (equipPanel != null)
            {
                SetField(inventoryUI, "equipmentPanel", equipPanel);
                Debug.Log("[InventorySetup] EquipmentPanel configurado.");
            }

            var tooltip = panel.Find("TooltipPanel");
            if (tooltip != null)
            {
                SetField(inventoryUI, "tooltipPanel", tooltip.gameObject);
                Debug.Log("[InventorySetup] TooltipPanel configurado.");

                var tooltipName = FindTextComponent(tooltip, "TooltipName");
                var tooltipStats = FindTextComponent(tooltip, "TooltipStats");
                if (tooltipName != null) SetField(inventoryUI, "tooltipName", tooltipName);
                if (tooltipStats != null) SetField(inventoryUI, "tooltipStats", tooltipStats);
            }

            var title = panel.Find("Title");
            if (title != null) SetField(inventoryUI, "dragHandle", title);
        }

        var dragCanvas = GameObject.Find("DragCanvas")?.GetComponent<Canvas>();
        if (dragCanvas != null)
        {
            SetField(inventoryUI, "dragCanvas", dragCanvas);
            Debug.Log("[InventorySetup] DragCanvas configurado.");
        }
        else
        {
            Debug.LogWarning("[InventorySetup] DragCanvas não encontrado!");
        }

        var gridTransform = transform.Find("InventoryPanel/InventoryGrid");
        if (gridTransform != null)
        {
            var slots = gridTransform.GetComponentsInChildren<ItemSlotUI>(true);
            Debug.Log($"[InventorySetup] {slots.Length} ItemSlotUI encontrados.");

            foreach (var slot in slots)
            {
                var bg = slot.GetComponent<Image>();
                var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
                var qty = FindTextComponent(slot.transform, "Quantity");
                var hl = slot.transform.Find("Highlight")?.GetComponent<Image>();

                if (bg != null) SetField(slot, "backgroundImage", bg);
                if (icon != null) SetField(slot, "iconImage", icon);
                if (qty != null) SetField(slot, "quantityText", qty);
                if (hl != null) SetField(slot, "highlightImage", hl);
            }
        }

        var equipTransform = transform.Find("InventoryPanel/EquipmentPanel");
        if (equipTransform != null)
        {
            var equipSlots = equipTransform.GetComponentsInChildren<EquipmentSlotUI>(true);
            Debug.Log($"[InventorySetup] {equipSlots.Length} EquipmentSlotUI encontrados.");

            foreach (var slot in equipSlots)
            {
                var bg = slot.GetComponent<Image>();
                var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
                var label = FindTextComponent(slot.transform, "Label");
                var empty = slot.transform.Find("EmptyIndicator")?.gameObject;
                var nameTxt = FindTextComponent(slot.transform, "ItemName");

                if (bg != null) SetField(slot, "backgroundImage", bg);
                if (icon != null) SetField(slot, "iconImage", icon);
                if (label != null) SetField(slot, "labelText", label);
                if (empty != null) SetField(slot, "emptyIndicator", empty);
                if (nameTxt != null) SetField(slot, "itemNameText", nameTxt);

                EquipmentSlot slotType = DetectSlotType(slot.name);
                SetField(slot, "slotType", slotType);
                SetField(slot, "slotLabel", GetSlotLabel(slotType));
            }
        }

        Debug.Log("[InventorySetup] ✅ Referências configuradas com sucesso!");
    }

    void SetField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
        if (field != null)
            field.SetValue(obj, value);
        else
            Debug.LogWarning($"[InventorySetup] Campo '{fieldName}' não encontrado em {obj.GetType().Name}");
    }

    TextMeshProUGUI FindTextComponent(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child == null) return null;
        return child.GetComponent<TextMeshProUGUI>();
    }

    EquipmentSlot DetectSlotType(string gameObjectName)
    {
        if (gameObjectName.Contains("Helmet")) return EquipmentSlot.Helmet;
        if (gameObjectName.Contains("Armor")) return EquipmentSlot.Armor;
        if (gameObjectName.Contains("Weapon")) return EquipmentSlot.Weapon;
        if (gameObjectName.Contains("Shield")) return EquipmentSlot.Shield;
        if (gameObjectName.Contains("Gloves")) return EquipmentSlot.Gloves;
        if (gameObjectName.Contains("Boots")) return EquipmentSlot.Boots;
        if (gameObjectName.Contains("Cape")) return EquipmentSlot.Cape;
        if (gameObjectName.Contains("Belt")) return EquipmentSlot.Belt;
        if (gameObjectName.Contains("Earring")) return EquipmentSlot.Earring;
        if (gameObjectName.Contains("Necklace")) return EquipmentSlot.Necklace;
        if (gameObjectName.Contains("Ring1")) return EquipmentSlot.Ring1;
        if (gameObjectName.Contains("Ring2")) return EquipmentSlot.Ring2;
        if (gameObjectName.Contains("Tattoo")) return EquipmentSlot.Tattoo;
        if (gameObjectName.Contains("Costume")) return EquipmentSlot.Costume;
        if (gameObjectName.Contains("Pet")) return EquipmentSlot.Pet;
        if (gameObjectName.Contains("Mount")) return EquipmentSlot.Mount;
        return EquipmentSlot.Weapon;
    }

    string GetSlotLabel(EquipmentSlot slot)
    {
        return slot switch
        {
            EquipmentSlot.Helmet => "Capacete",
            EquipmentSlot.Armor => "Armadura",
            EquipmentSlot.Weapon => "Arma",
            EquipmentSlot.Shield => "Escudo",
            EquipmentSlot.Gloves => "Luvas",
            EquipmentSlot.Boots => "Botas",
            EquipmentSlot.Cape => "Capa",
            EquipmentSlot.Belt => "Cinto",
            EquipmentSlot.Earring => "Brinco",
            EquipmentSlot.Necklace => "Colar",
            EquipmentSlot.Ring1 => "Anel 1",
            EquipmentSlot.Ring2 => "Anel 2",
            EquipmentSlot.Tattoo => "Tatuagem",
            EquipmentSlot.Costume => "Traje",
            EquipmentSlot.Pet => "Mascote",
            EquipmentSlot.Mount => "Montaria",
            _ => "Slot"
        };
    }
}