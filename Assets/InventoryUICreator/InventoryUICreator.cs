using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Adicione este script a um Canvas vazio e clique em "Create Inventory UI" no Inspector.
/// Gera toda a estrutura de UI do inventário automaticamente.
/// </summary>
[ExecuteInEditMode]
public class InventoryUICreator : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private int inventorySlots = 40;
    [SerializeField] private int columns = 8;
    [SerializeField] private Vector2 slotSize = new Vector2(64, 64);
    [SerializeField] private Vector2 slotSpacing = new Vector2(5, 5);

    [Header("Cores")]
    [SerializeField] private Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    [SerializeField] private Color slotColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);

    [Header("Fonte")]
    [SerializeField] private TMP_FontAsset fontAsset; // Opcional

    [ContextMenu("Create Inventory UI")]
    public void CreateInventoryUI()
    {
        // Limpa filhos existentes
        foreach (Transform child in transform)
        {
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject);
        }

        // Cria estrutura principal
        var mainPanel = CreatePanel("InventoryPanel", transform, Vector2.zero, 
            new Vector2(800, 500), panelColor);
        mainPanel.SetActive(false); // Começa desativado

        // Título
        CreateTitle(mainPanel.transform);

        // Grid de inventário (esquerda)
        var gridPanel = CreatePanel("GridPanel", mainPanel.transform, 
            new Vector2(-200, 0), new Vector2(530, 420), Color.clear);
        CreateInventoryGrid(gridPanel.transform);

        // Painel de equipamento (direita)
        var equipPanel = CreatePanel("EquipmentPanel", mainPanel.transform,
            new Vector2(280, 0), new Vector2(200, 420), Color.clear);
        CreateEquipmentSlots(equipPanel.transform);

        // Tooltip
        CreateTooltip(mainPanel.transform);

        // Botão Fechar
        CreateCloseButton(mainPanel.transform);

        Debug.Log("[InventoryUICreator] UI criada com sucesso! Configure as referências no InventoryUI.cs");
    }

    #region Helpers de Criação

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
            img.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");
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
        rt.sizeDelta = new Vector2(300, 40);

        var txt = go.AddComponent<TextMeshProUGUI>();
        txt.text = "INVENTÁRIO";
        txt.fontSize = 24;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        if (fontAsset != null) txt.font = fontAsset;
    }

    void CreateInventoryGrid(Transform parent)
    {
        // Scroll View (para grids grandes)
        var scrollGo = new GameObject("ScrollView");
        scrollGo.transform.SetParent(parent, false);
        var scrollRt = scrollGo.AddComponent<RectTransform>();
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = Vector2.zero;
        scrollRt.offsetMax = Vector2.zero;

        var scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        var viewportGo = new GameObject("Viewport");
        viewportGo.transform.SetParent(scrollGo.transform, false);
        var viewportRt = viewportGo.AddComponent<RectTransform>();
        viewportRt.anchorMin = Vector2.zero;
        viewportRt.anchorMax = Vector2.one;
        viewportRt.offsetMin = Vector2.zero;
        viewportRt.offsetMax = Vector2.zero;

        var mask = viewportGo.AddComponent<Mask>();
        var viewportImg = viewportGo.AddComponent<Image>();
        viewportImg.color = new Color(0, 0, 0, 0.1f);

        scroll.viewport = viewportRt;

        // Content (Grid)
        var contentGo = new GameObject("InventoryGrid");
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = contentGo.AddComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0, 1);
        contentRt.anchorMax = new Vector2(0, 1);
        contentRt.pivot = new Vector2(0, 1);
        contentRt.anchoredPosition = Vector2.zero;

        var grid = contentGo.AddComponent<GridLayoutGroup>();
        grid.cellSize = slotSize;
        grid.spacing = slotSpacing;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = columns;
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;

        var contentSize = contentGo.AddComponent<ContentSizeFitter>();
        contentSize.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = contentRt;

        // Cria slots
        for (int i = 0; i < inventorySlots; i++)
        {
            CreateInventorySlot(contentGo.transform, i);
        }
    }

    GameObject CreateInventorySlot(Transform parent, int index)
    {
        var go = new GameObject($"Slot_{index}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = slotSize;

        // Background
        var bg = go.AddComponent<Image>();
        bg.color = slotColor;
        bg.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");

        // Icon (child)
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = Vector2.zero;
        iconRt.anchorMax = Vector2.one;
        iconRt.offsetMin = new Vector2(4, 4);
        iconRt.offsetMax = new Vector2(-4, -4);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.clear; // Invisível até ter item
        iconImg.raycastTarget = false;

        // Quantity (child)
        var qtyGo = new GameObject("Quantity");
        qtyGo.transform.SetParent(go.transform, false);
        var qtyRt = qtyGo.AddComponent<RectTransform>();
        qtyRt.anchorMin = new Vector2(1, 0);
        qtyRt.anchorMax = new Vector2(1, 0);
        qtyRt.pivot = new Vector2(1, 0);
        qtyRt.anchoredPosition = new Vector2(-2, 2);
        qtyRt.sizeDelta = new Vector2(30, 20);

        var qtyTxt = qtyGo.AddComponent<TextMeshProUGUI>();
        qtyTxt.text = "";
        qtyTxt.fontSize = 14;
        qtyTxt.alignment = TextAlignmentOptions.Right;
        qtyTxt.color = Color.white;
        qtyTxt.gameObject.SetActive(false);
        if (fontAsset != null) qtyTxt.font = fontAsset;

        // Highlight (child, overlay)
        var hlGo = new GameObject("Highlight");
        hlGo.transform.SetParent(go.transform, false);
        var hlRt = hlGo.AddComponent<RectTransform>();
        hlRt.anchorMin = Vector2.zero;
        hlRt.anchorMax = Vector2.one;
        hlRt.offsetMin = Vector2.zero;
        hlRt.offsetMax = Vector2.zero;

        var hlImg = hlGo.AddComponent<Image>();
        hlImg.color = highlightColor;
        hlImg.raycastTarget = false;
        hlGo.SetActive(false);

        // Adiciona componente ItemSlotUI
        var slotUI = go.AddComponent<ItemSlotUI>();
        // Referências serão configuradas via script ou Inspector depois

        return go;
    }

    void CreateEquipmentSlots(Transform parent)
    {
        // Título
        var titleGo = new GameObject("EquipTitle");
        titleGo.transform.SetParent(parent, false);
        var titleRt = titleGo.AddComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0.5f, 1);
        titleRt.anchorMax = new Vector2(0.5f, 1);
        titleRt.pivot = new Vector2(0.5f, 1);
        titleRt.anchoredPosition = new Vector2(0, -10);
        titleRt.sizeDelta = new Vector2(180, 30);

        var titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "EQUIPAMENTO";
        titleTxt.fontSize = 18;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleTxt.color = new Color(0.8f, 0.8f, 0.8f);
        if (fontAsset != null) titleTxt.font = fontAsset;

        // Slots
        EquipmentSlot[] slots = { 
            EquipmentSlot.Helmet, EquipmentSlot.Armor, EquipmentSlot.Weapon, 
            EquipmentSlot.Shield, EquipmentSlot.Gloves, EquipmentSlot.Boots 
        };

        string[] labels = { "Elmo", "Armadura", "Arma", "Escudo", "Luvas", "Botas" };

        float startY = -50;
        float spacing = 65;

        for (int i = 0; i < slots.Length; i++)
        {
            CreateEquipmentSlot(parent, slots[i], labels[i], new Vector2(0, startY - i * spacing));
        }
    }

    GameObject CreateEquipmentSlot(Transform parent, EquipmentSlot slotType, string label, Vector2 pos)
    {
        var go = new GameObject($"EquipSlot_{slotType}");
        go.transform.SetParent(parent, false);

        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1);
        rt.anchorMax = new Vector2(0.5f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(180, 60);

        // Background
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");

        // Label
        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(go.transform, false);
        var labelRt = labelGo.AddComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0, 1);
        labelRt.anchorMax = new Vector2(1, 1);
        labelRt.pivot = new Vector2(0.5f, 1);
        labelRt.anchoredPosition = new Vector2(0, -2);
        labelRt.sizeDelta = new Vector2(0, 18);

        var labelTxt = labelGo.AddComponent<TextMeshProUGUI>();
        labelTxt.text = label;
        labelTxt.fontSize = 12;
        labelTxt.alignment = TextAlignmentOptions.Center;
        labelTxt.color = new Color(0.6f, 0.6f, 0.6f);
        if (fontAsset != null) labelTxt.font = fontAsset;

        // Icon area
        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(go.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0);
        iconRt.anchorMax = new Vector2(0, 1);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(5, -10);
        iconRt.sizeDelta = new Vector2(40, 40);

        var iconImg = iconGo.AddComponent<Image>();
        iconImg.color = Color.clear;

        // Item name
        var nameGo = new GameObject("ItemName");
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 0);
        nameRt.anchorMax = new Vector2(1, 0);
        nameRt.pivot = new Vector2(0.5f, 0);
        nameRt.anchoredPosition = new Vector2(0, 2);
        nameRt.sizeDelta = new Vector2(0, 16);

        var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "Vazio";
        nameTxt.fontSize = 11;
        nameTxt.alignment = TextAlignmentOptions.Center;
        nameTxt.color = new Color(0.4f, 0.4f, 0.4f);
        if (fontAsset != null) nameTxt.font = fontAsset;

        // Empty indicator
        var emptyGo = new GameObject("EmptyIndicator");
        emptyGo.transform.SetParent(go.transform, false);
        var emptyRt = emptyGo.AddComponent<RectTransform>();
        emptyRt.anchorMin = Vector2.zero;
        emptyRt.anchorMax = Vector2.one;
        emptyRt.offsetMin = new Vector2(50, 5);
        emptyRt.offsetMax = new Vector2(-5, -20);

        var emptyTxt = emptyGo.AddComponent<TextMeshProUGUI>();
        emptyTxt.text = "---";
        emptyTxt.fontSize = 14;
        emptyTxt.alignment = TextAlignmentOptions.Center;
        emptyTxt.color = new Color(0.3f, 0.3f, 0.3f);
        if (fontAsset != null) emptyTxt.font = fontAsset;

        // Adiciona componente
        var slotUI = go.AddComponent<EquipmentSlotUI>();
        // Configurações via Inspector depois

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
        rt.sizeDelta = new Vector2(220, 150);

        // Background
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.05f, 0.98f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");

        // Borda
        var border = go.AddComponent<Outline>();
        border.effectColor = new Color(0.4f, 0.3f, 0.1f);
        border.effectDistance = new Vector2(2, 2);

        // Nome do item
        var nameGo = new GameObject("ItemName");
        nameGo.transform.SetParent(go.transform, false);
        var nameRt = nameGo.AddComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0, 1);
        nameRt.anchorMax = new Vector2(1, 1);
        nameRt.pivot = new Vector2(0.5f, 1);
        nameRt.anchoredPosition = new Vector2(0, -5);
        nameRt.sizeDelta = new Vector2(-10, 25);

        var nameTxt = nameGo.AddComponent<TextMeshProUGUI>();
        nameTxt.text = "Nome do Item";
        nameTxt.fontSize = 16;
        nameTxt.fontStyle = FontStyles.Bold;
        nameTxt.alignment = TextAlignmentOptions.Left;
        nameTxt.color = new Color(1f, 0.8f, 0.2f); // Dourado
        if (fontAsset != null) nameTxt.font = fontAsset;

        // Tipo
        var typeGo = new GameObject("ItemType");
        typeGo.transform.SetParent(go.transform, false);
        var typeRt = typeGo.AddComponent<RectTransform>();
        typeRt.anchorMin = new Vector2(0, 1);
        typeRt.anchorMax = new Vector2(1, 1);
        typeRt.pivot = new Vector2(0.5f, 1);
        typeRt.anchoredPosition = new Vector2(0, -30);
        typeRt.sizeDelta = new Vector2(-10, 18);

        var typeTxt = typeGo.AddComponent<TextMeshProUGUI>();
        typeTxt.text = "Arma | Mão Direita";
        typeTxt.fontSize = 12;
        typeTxt.alignment = TextAlignmentOptions.Left;
        typeTxt.color = new Color(0.7f, 0.7f, 0.7f);
        if (fontAsset != null) typeTxt.font = fontAsset;

        // Stats
        var statsGo = new GameObject("Stats");
        statsGo.transform.SetParent(go.transform, false);
        var statsRt = statsGo.AddComponent<RectTransform>();
        statsRt.anchorMin = new Vector2(0, 0);
        statsRt.anchorMax = new Vector2(1, 1);
        statsRt.offsetMin = new Vector2(10, 50);
        statsRt.offsetMax = new Vector2(-10, -55);

        var statsTxt = statsGo.AddComponent<TextMeshProUGUI>();
        statsTxt.text = "ATAQUE +10\nFOR +5\nAGI +3";
        statsTxt.fontSize = 12;
        statsTxt.alignment = TextAlignmentOptions.TopLeft;
        statsTxt.color = Color.white;
        if (fontAsset != null) statsTxt.font = fontAsset;

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
        bg.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");

        var txtGo = new GameObject("Text");
        txtGo.transform.SetParent(go.transform, false);
        var txtRt = txtGo.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = txtGo.AddComponent<TextMeshProUGUI>();
        txt.text = "X";
        txt.fontSize = 18;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color = Color.white;
        if (fontAsset != null) txt.font = fontAsset;

        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            var ui = GetComponent<InventoryUI>();
            if (ui != null) ui.ToggleInventory();
        });
    }

    #endregion
}