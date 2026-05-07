using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteInEditMode]
public class InventoryUICreator : MonoBehaviour
{
    [Header("Configuracoes do Inventario")]
    [SerializeField] private int inventorySlots = 48;
    [SerializeField] private int columns = 8;
    [SerializeField] private Vector2 slotSize = new Vector2(64, 64);
    [SerializeField] private Vector2 slotSpacing = new Vector2(5, 5);

    [Header("Sprites (Opcionais)")]
    [SerializeField] private Sprite slotBackgroundSprite;
    [SerializeField] private Sprite slotHighlightSprite;
    [SerializeField] private Sprite emptySlotSprite;

    [Header("Cores")]
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private Color slotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
    [SerializeField] private Color equipmentSlotColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);

    [Header("Fonte (Opcional)")]
    [SerializeField] private TMP_FontAsset fontAsset;

    [ContextMenu("Create Inventory UI")]
    public void CreateInventoryUI()
    {
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        CreateDragCanvas();

        var mainPanel = CreatePanel("InventoryPanel", transform, Vector2.zero, new Vector2(650, 520), panelColor);
        mainPanel.SetActive(false);

        CreateTitle(mainPanel.transform);
        CreateInventoryGridPanel(mainPanel.transform);
        CreateEquipmentPanel(mainPanel.transform);
        CreateTooltip(mainPanel.transform);
        CreateCloseButton(mainPanel.transform);

        Debug.Log("[InventoryUICreator] UI criada com sucesso!");
    }

    void CreateDragCanvas()
    {
        var existing = GameObject.Find("DragCanvas");
        if (existing != null) return;

        var go = new GameObject("DragCanvas");
        go.transform.SetParent(null);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        go.AddComponent<CanvasScaler>();
        go.AddComponent<GraphicRaycaster>();
        if (Application.isPlaying)
            DontDestroyOnLoad(go);
    }

    GameObject CreatePanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        if (color.a > 0)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
        }
        return go;
    }

    void CreateTitle(Transform parent)
    {
        var go = new GameObject("Title");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(620, 40);

        var dragArea = go.AddComponent<Image>();
        dragArea.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        dragArea.raycastTarget = true;

        var textGo = new GameObject("TitleText");
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        AddText(textGo, "INVENTARIO", 24, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
    }

    void CreateInventoryGridPanel(Transform parent)
    {
        var gridPanel = CreatePanel("InventoryGrid", parent, new Vector2(-140, -30), new Vector2(420, 440), Color.clear);
        var grid = gridPanel.AddComponent<GridLayoutGroup>();
        grid.cellSize = slotSize;
        grid.spacing = slotSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;

        var fitter = gridPanel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int i = 0; i < inventorySlots; i++)
            CreateInventorySlot(gridPanel.transform, i);
    }

    GameObject CreateInventorySlot(Transform parent, int index)
    {
        var go = new GameObject("ItemSlot_" + index);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = slotSize;

        var bg = go.AddComponent<Image>();
        if (slotBackgroundSprite != null)
        {
            bg.sprite = slotBackgroundSprite;
            bg.color = Color.white;
        }
        else bg.color = slotColor;

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(4, 4);
        iconRt.offsetMax = new Vector2(-4, -4);
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.clear;
        iconImg.raycastTarget = false;

        var qtyGo = new GameObject("Quantity");
        qtyGo.transform.SetParent(go.transform, false);
        var qtyRt = qtyGo.AddComponent<RectTransform>();
        qtyRt.anchorMin = new Vector2(1, 0);
        qtyRt.anchorMax = new Vector2(1, 0);
        qtyRt.pivot = new Vector2(1, 0);
        qtyRt.anchoredPosition = new Vector2(-2, 2);
        qtyRt.sizeDelta = new Vector2(30, 20);
        AddText(qtyGo, "", 14, FontStyles.Normal, TextAlignmentOptions.Right, Color.white);
        qtyGo.SetActive(false);

        var hlGo = new GameObject("Highlight");
        hlGo.transform.SetParent(go.transform, false);
        var hlRt = hlGo.AddComponent<RectTransform>();
        hlRt.anchorMin = Vector2.zero;
        hlRt.anchorMax = Vector2.one;
        hlRt.offsetMin = Vector2.zero;
        hlRt.offsetMax = Vector2.zero;
        var hlImg = hlGo.AddComponent<Image>();
        if (slotHighlightSprite != null)
        {
            hlImg.sprite = slotHighlightSprite;
            hlImg.color = new Color(1, 1, 1, 0);
        }
        else hlImg.color = highlightColor;
        hlImg.raycastTarget = false;
        hlGo.SetActive(false);

        go.AddComponent<ItemSlotUI>();
        return go;
    }

    void CreateEquipmentPanel(Transform parent)
    {
        var equipPanel = CreatePanel("EquipmentPanel", parent, new Vector2(210, -30), new Vector2(180, 440), Color.clear);

        var titleGo = new GameObject("EquipTitle");
        titleGo.transform.SetParent(equipPanel.transform, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleRt.sizeDelta = new Vector2(160, 25);
        AddText(titleGo, "EQUIPAMENTO", 14, FontStyles.Bold, TextAlignmentOptions.Center, new Color(0.8f, 0.8f, 0.8f));

        float startY = -45;
        float spacing = 58;
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Helmet, "Capacete", startY);
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Armor, "Armadura", startY - spacing);
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Weapon, "Arma", startY - spacing * 2);
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Shield, "Escudo", startY - spacing * 3);
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Gloves, "Luvas", startY - spacing * 4);
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Boots, "Botas", startY - spacing * 5);
        CreateEquipmentSlot(equipPanel.transform, EquipmentSlot.Cape, "Capa", startY - spacing * 6);
    }

    GameObject CreateEquipmentSlot(Transform parent, EquipmentSlot slotType, string label, float posY)
    {
        var go = new GameObject("EquipmentSlot_" + slotType);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, posY);
        rt.sizeDelta = new Vector2(160, 55);

        var bg = go.AddComponent<Image>();
        if (slotBackgroundSprite != null)
        {
            bg.sprite = slotBackgroundSprite;
            bg.color = Color.white;
        }
        else bg.color = equipmentSlotColor;

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 1);
        labelRt.anchorMax = new Vector2(1, 1);
        labelRt.pivot = new Vector2(0.5f, 1);
        labelRt.anchoredPosition = new Vector2(0, -2);
        labelRt.sizeDelta = new Vector2(0, 16);
        AddText(labelGo, label, 11, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.6f, 0.6f, 0.6f));

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = new Vector2(0, -2);
        iconRt.sizeDelta = new Vector2(32, 32);
        var iconImg = iconGo.AddComponent<Image>();
        if (emptySlotSprite != null)
        {
            iconImg.sprite = emptySlotSprite;
            iconImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        else iconImg.color = Color.clear;

        var nameGo = new GameObject("ItemName");
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0);
        nameRt.anchorMax = new Vector2(1, 0);
        nameRt.pivot = new Vector2(0.5f, 0);
        nameRt.anchoredPosition = new Vector2(0, 2);
        nameRt.sizeDelta = new Vector2(0, 14);
        AddText(nameGo, "", 10, FontStyles.Normal, TextAlignmentOptions.Center, new Color(0.4f, 0.4f, 0.4f));

        var emptyGo = new GameObject("EmptyIndicator");
        emptyGo.transform.SetParent(go.transform, false);
        var emptyRt = emptyGo.AddComponent<RectTransform>();
        emptyRt.anchorMin = new Vector2(0.5f, 0.5f);
        emptyRt.anchorMax = new Vector2(0.5f, 0.5f);
        emptyRt.pivot = new Vector2(0.5f, 0.5f);
        emptyRt.anchoredPosition = new Vector2(0, -2);
        emptyRt.sizeDelta = new Vector2(32, 32);
        var emptyImg = emptyGo.AddComponent<Image>();
        if (emptySlotSprite != null)
        {
            emptyImg.sprite = emptySlotSprite;
            emptyImg.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        }
        else emptyImg.color = new Color(0.2f, 0.2f, 0.2f, 0.3f);

        var slotUI = go.AddComponent<EquipmentSlotUI>();
        return go;
    }

    void CreateTooltip(Transform parent)
    {
        var go = new GameObject("TooltipPanel");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(20, -20);
        rt.sizeDelta = new Vector2(250, 150);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.98f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0.4f, 0.3f, 0.1f);
        outline.effectDistance = new Vector2(2, 2);

        var nameGo = new GameObject("TooltipName");
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, -8);
        nameRt.sizeDelta = new Vector2(-16, 22);
        AddText(nameGo, "Nome do Item", 16, FontStyles.Bold, TextAlignmentOptions.Left, new Color(1f, 0.8f, 0.2f));

        var statsGo = new GameObject("TooltipStats");
        statsGo.transform.SetParent(go.transform, false);
        var statsRt = statsGo.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0, 0);
        statsRt.anchorMax = new Vector2(1, 1);
        statsRt.offsetMin = new Vector2(10, 10);
        statsRt.offsetMax = new Vector2(-10, -35);
        AddText(statsGo, "ATAQUE +10\nFOR +5\nAGI +3", 12, FontStyles.Normal, TextAlignmentOptions.TopLeft, Color.white);

        go.SetActive(false);
    }

    void CreateCloseButton(Transform parent)
    {
        var go = new GameObject("CloseButton");
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-10, -10);
        rt.sizeDelta = new Vector2(30, 30);

        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.6f, 0.1f, 0.1f, 0.9f);

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;
        AddText(txtGo, "X", 18, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            var ui = FindAnyObjectByType<InventoryUI>();
            if (ui != null) ui.CloseInventory();
        });
    }

    void AddText(GameObject go, string text, int fontSize, FontStyles fontStyle, TextAlignmentOptions alignment, Color color)
    {
        var txt = go.AddComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            txt.text = text;
            txt.fontSize = fontSize;
            txt.fontStyle = fontStyle;
            txt.alignment = alignment;
            txt.color = color;
            if (fontAsset != null) txt.font = fontAsset;
            return;
        }
        var fallback = go.AddComponent<Text>();
        fallback.text = text;
        fallback.fontSize = fontSize;
        fallback.fontStyle = (fontStyle == FontStyles.Bold) ? FontStyle.Bold : FontStyle.Normal;
        fallback.alignment = alignment switch
        {
            TextAlignmentOptions.Center => TextAnchor.MiddleCenter,
            TextAlignmentOptions.Left => TextAnchor.MiddleLeft,
            TextAlignmentOptions.Right => TextAnchor.MiddleRight,
            TextAlignmentOptions.TopLeft => TextAnchor.UpperLeft,
            TextAlignmentOptions.TopRight => TextAnchor.UpperRight,
            _ => TextAnchor.MiddleCenter
        };
        fallback.color = color;
    }
}