using UnityEngine;
using UnityEngine.UI;
using TMPro;

///
/// Adicione este script a um Canvas vazio e clique em "Create Inventory UI" no Inspector.
/// Gera toda a estrutura de UI do inventário automaticamente.
///
[ExecuteInEditMode]
public class InventoryUICreator : MonoBehaviour
{
    [Header("Configurações")]
    [SerializeField] private int inventorySlots = 40;
    [SerializeField] private int columns = 8;
    [SerializeField] private Vector2 slotSize = new Vector2(64, 64);
    [SerializeField] private Vector2 slotSpacing = new Vector2(5, 5);

    [Header("Sprites")]
    [SerializeField] private Sprite slotBackgroundSprite;      // slot_background.png
    [SerializeField] private Sprite slotHighlightSprite;       // slot_highlight.png

    [Header("Ícones de Equipamento Vazio")]
    [SerializeField] private Sprite emptyHelmetIcon;
    [SerializeField] private Sprite emptyArmorIcon;
    [SerializeField] private Sprite emptyWeaponIcon;
    [SerializeField] private Sprite emptyShieldIcon;
    [SerializeField] private Sprite emptyGlovesIcon;
    [SerializeField] private Sprite emptyBootsIcon;
    [SerializeField] private Sprite emptyCapeIcon;

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
        // Scroll View
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

        // Background - USA O SPRITE slot_background SE DISPONÍVEL
        var bg = go.AddComponent<Image>();
        if (slotBackgroundSprite != null)
        {
            bg.sprite = slotBackgroundSprite;
            bg.color = Color.white;
        }
        else
        {
            bg.color = slotColor;
            bg.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");
        }

        // Icon (child)
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

        // Highlight (child, overlay) - USA O SPRITE slot_highlight SE DISPONÍVEL
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
        else
        {
            hlImg.color = highlightColor;
        }
        hlImg.raycastTarget = false;
        hlGo.SetActive(false);

        // Adiciona componente ItemSlotUI
        var slotUI = go.AddComponent<ItemSlotUI>();

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

    // Slots com ícones específicos - DISTRIBUÍDOS VERTICALMENTE
    float startY = -50;      // Posição Y inicial (abaixo do título)
    float spacing = 65;      // Espaço entre cada slot

    CreateEquipmentSlot(parent, EquipmentSlot.Helmet, "Elmo", emptyHelmetIcon, startY);
    CreateEquipmentSlot(parent, EquipmentSlot.Armor, "Armadura", emptyArmorIcon, startY - spacing);
    CreateEquipmentSlot(parent, EquipmentSlot.Weapon, "Arma", emptyWeaponIcon, startY - spacing * 2);
    CreateEquipmentSlot(parent, EquipmentSlot.Shield, "Escudo", emptyShieldIcon, startY - spacing * 3);
    CreateEquipmentSlot(parent, EquipmentSlot.Gloves, "Luvas", emptyGlovesIcon, startY - spacing * 4);
    CreateEquipmentSlot(parent, EquipmentSlot.Boots, "Botas", emptyBootsIcon, startY - spacing * 5);
    CreateEquipmentSlot(parent, EquipmentSlot.Cape, "Capa", emptyCapeIcon, startY - spacing * 6);
}

GameObject CreateEquipmentSlot(Transform parent, EquipmentSlot slotType, string label, Sprite emptyIcon, float posY)
{
    var go = new GameObject($"EquipSlot_{slotType}");
    go.transform.SetParent(parent, false);

    var rt = go.AddComponent<RectTransform>();
    rt.anchorMin = new Vector2(0.5f, 1);
    rt.anchorMax = new Vector2(0.5f, 1);
    rt.pivot = new Vector2(0.5f, 1);
    rt.anchoredPosition = new Vector2(0, posY);
    rt.sizeDelta = new Vector2(180, 60);

    // Background
    var bg = go.AddComponent<Image>();
    if (slotBackgroundSprite != null)
    {
        bg.sprite = slotBackgroundSprite;
        bg.color = Color.white;
    }
    else
    {
        bg.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
        bg.sprite = Resources.GetBuiltinResource<Sprite>("Background.psd");
    }

    // Label (nome do tipo - "Elmo", "Armadura", etc.)
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

    // Icon area - CENTRALIZADO
    var iconGo = new GameObject("Icon");
    iconGo.transform.SetParent(go.transform, false);
    var iconRt = iconGo.AddComponent<RectTransform>();
    iconRt.anchorMin = new Vector2(0.5f, 0.5f);  // Centro
    iconRt.anchorMax = new Vector2(0.5f, 0.5f);  // Centro
    iconRt.pivot = new Vector2(0.5f, 0.5f);      // Pivot no centro
    iconRt.anchoredPosition = new Vector2(0, -5); // Levemente abaixo do centro
    iconRt.sizeDelta = new Vector2(40, 40);

    var iconImg = iconGo.AddComponent<Image>();
    if (emptyIcon != null)
    {
        iconImg.sprite = emptyIcon;
        iconImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }
    else
    {
        iconImg.color = Color.clear;
    }

    // Item name (nome do item equipado)
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

    // Empty indicator (texto "---")
    var emptyGo = new GameObject("EmptyIndicator");
    emptyGo.transform.SetParent(go.transform, false);
    var emptyRt = emptyGo.AddComponent<RectTransform>();
    emptyRt.anchorMin = new Vector2(0.5f, 0.5f);  // Centro
    emptyRt.anchorMax = new Vector2(0.5f, 0.5f);  // Centro
    emptyRt.pivot = new Vector2(0.5f, 0.5f);
    emptyRt.anchoredPosition = new Vector2(0, -5);
    emptyRt.sizeDelta = new Vector2(40, 40);

    var emptyTxt = emptyGo.AddComponent<TextMeshProUGUI>();
    emptyTxt.text = "---";
    emptyTxt.fontSize = 14;
    emptyTxt.alignment = TextAlignmentOptions.Center;
    emptyTxt.color = new Color(0.3f, 0.3f, 0.3f);
    if (fontAsset != null) emptyTxt.font = fontAsset;

    // Adiciona componente EquipmentSlotUI
    var slotUI = go.AddComponent<EquipmentSlotUI>();
    ConfigureEquipmentSlotUI(slotUI, slotType, label, bg, iconImg, labelTxt, emptyGo, nameTxt);

    return go;
}

    void ConfigureEquipmentSlotUI(EquipmentSlotUI slotUI, EquipmentSlot slotType, string label, 
        Image bg, Image icon, TextMeshProUGUI labelTxt, GameObject emptyInd, TextMeshProUGUI itemName)
    {
        // Usa reflection para setar os campos serialized privados
        var typeField = typeof(EquipmentSlotUI).GetField("slotType", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (typeField != null) typeField.SetValue(slotUI, slotType);

        var labelField = typeof(EquipmentSlotUI).GetField("slotLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (labelField != null) labelField.SetValue(slotUI, label);

        var bgField = typeof(EquipmentSlotUI).GetField("backgroundImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (bgField != null) bgField.SetValue(slotUI, bg);

        var iconField = typeof(EquipmentSlotUI).GetField("iconImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (iconField != null) iconField.SetValue(slotUI, icon);

        var labelTextField = typeof(EquipmentSlotUI).GetField("labelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (labelTextField != null) labelTextField.SetValue(slotUI, labelTxt);

        var emptyField = typeof(EquipmentSlotUI).GetField("emptyIndicator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (emptyField != null) emptyField.SetValue(slotUI, emptyInd);

        var nameField = typeof(EquipmentSlotUI).GetField("itemNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (nameField != null) nameField.SetValue(slotUI, itemName);
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
        nameTxt.color = new Color(1f, 0.8f, 0.2f);
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
            var inventoryUI = GetComponent<InventoryUI>();
            if (inventoryUI != null) inventoryUI.ToggleInventory();
        });
    }
    #endregion
}