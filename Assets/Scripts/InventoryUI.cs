using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private class SlotView
    {
        public Image icon;
        public Text amount;
        public GameObject selected;
        public bool isHotbar;
        public int index;
    }

    public static bool IsAnyInventoryOpen { get; private set; }

    [Header("References")]
    public PlayerInventory playerInventory;
    public CameraModeController cameraModeController;

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.B;

    private readonly List<SlotView> hotbarSlots = new();
    private readonly List<SlotView> backpackSlots = new();

    private GameObject inventoryPanel;
    private Font defaultFont;

    private void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }

        if (cameraModeController == null && Camera.main != null)
        {
            cameraModeController =
                Camera.main.GetComponent<CameraModeController>();
        }

        defaultFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

        CreateHotbar();
        CreateBackpack();

        if (playerInventory != null)
        {
            playerInventory.InventoryChanged += RefreshAll;
        }

        SetInventoryOpen(false);
        RefreshAll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetInventoryOpen(!IsAnyInventoryOpen);
        }

        if (IsAnyInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetInventoryOpen(false);
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged -= RefreshAll;
        }

        Time.timeScale = 1f;
        IsAnyInventoryOpen = false;
    }

    private void CreateHotbar()
    {
        GameObject root = CreateUIObject("Hotbar", transform);
        RectTransform rect = root.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 45f);
        rect.sizeDelta = new Vector2(860f, 88f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);

        HorizontalLayoutGroup layout =
            root.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
        {
            int slotIndex = i;
            SlotView view = CreateSlot(root.transform, true, slotIndex);

            view.selected.GetComponent<Image>().color =
                new Color(1f, 0.8f, 0f, 0.35f);

            root.transform.GetChild(i).GetComponent<Button>().onClick
                .AddListener(() => playerInventory.SelectHotbarSlot(slotIndex));

            hotbarSlots.Add(view);
        }

        GameObject moreButton = CreateButton(root.transform, "…");
        moreButton.GetComponent<Button>().onClick.AddListener(
            () => SetInventoryOpen(true)
        );
    }

    private void CreateBackpack()
    {
        inventoryPanel = CreateUIObject("InventoryPanel", transform);

        RectTransform panelRect =
            inventoryPanel.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(820f, 470f);

        Image panelImage = inventoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        GameObject title = CreateTextObject(
            "Title",
            inventoryPanel.transform,
            "背包"
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(0f, 40f);

        title.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        title.GetComponent<Text>().fontSize = 28;

        GameObject closeButton = CreateButton(
            inventoryPanel.transform,
            "X"
        );

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-15f, -15f);

        closeButton.GetComponent<Button>().onClick.AddListener(
            () => SetInventoryOpen(false)
        );

        GameObject grid = CreateUIObject(
            "BackpackSlots",
            inventoryPanel.transform
        );

        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(744f, 300f);
        gridRect.anchoredPosition = new Vector2(0f, -25f);

        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(76f, 76f);
        gridLayout.spacing = new Vector2(6f, 6f);
        gridLayout.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 9;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < PlayerInventory.BackpackSlotCount; i++)
        {
            int slotIndex = i;
            SlotView view = CreateSlot(grid.transform, false, slotIndex);

            grid.transform.GetChild(i).GetComponent<Button>().onClick
                .AddListener(() =>
                    playerInventory.SwapSelectedHotbarWithBackpack(slotIndex)
                );

            backpackSlots.Add(view);
        }
    }

    private SlotView CreateSlot(
        Transform parent,
        bool isHotbar,
        int index)
    {
        GameObject slot = CreateButton(parent, "");

        Image background = slot.GetComponent<Image>();
        background.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        GameObject iconObject = CreateUIObject("Icon", slot.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.raycastTarget = false;
        Stretch(iconObject.GetComponent<RectTransform>(), 8f);

        GameObject amountObject = CreateTextObject(
            "Amount",
            slot.transform,
            ""
        );

        RectTransform amountRect =
            amountObject.GetComponent<RectTransform>();

        amountRect.anchorMin = Vector2.zero;
        amountRect.anchorMax = new Vector2(1f, 0f);
        amountRect.pivot = new Vector2(1f, 0f);
        amountRect.anchoredPosition = new Vector2(-5f, 4f);
        amountRect.sizeDelta = new Vector2(-10f, 28f);

        Text amountText = amountObject.GetComponent<Text>();
        amountText.alignment = TextAnchor.LowerRight;
        amountText.fontSize = 20;
        amountText.color = Color.white;
        amountText.raycastTarget = false;

        GameObject selected = CreateUIObject("Selected", slot.transform);
        Image selectedImage = selected.AddComponent<Image>();
        selectedImage.raycastTarget = false;
        Stretch(selected.GetComponent<RectTransform>(), 0f);

        return new SlotView
        {
            icon = icon,
            amount = amountText,
            selected = selected,
            isHotbar = isHotbar,
            index = index
        };
    }

    private GameObject CreateButton(Transform parent, string text)
    {
        GameObject buttonObject = CreateUIObject("Button", parent);

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 76f;
        layout.preferredHeight = 76f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        if (!string.IsNullOrEmpty(text))
        {
            GameObject textObject = CreateTextObject(
                "Text",
                buttonObject.transform,
                text
            );

            Stretch(textObject.GetComponent<RectTransform>(), 0f);

            Text uiText = textObject.GetComponent<Text>();
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.fontSize = 36;
            uiText.raycastTarget = false;
        }

        return buttonObject;
    }

    private GameObject CreateTextObject(
        string name,
        Transform parent,
        string content)
    {
        GameObject textObject = CreateUIObject(name, parent);
        Text text = textObject.AddComponent<Text>();

        text.font = defaultFont;
        text.text = content;
        text.color = Color.white;

        return textObject;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(
            name,
            typeof(RectTransform)
        );

        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private void RefreshAll()
    {
        RefreshList(hotbarSlots);
        RefreshList(backpackSlots);
    }

    private void RefreshList(List<SlotView> slots)
    {
        if (playerInventory == null)
        {
            return;
        }

        foreach (SlotView view in slots)
        {
            InventorySlotData data = view.isHotbar
                ? playerInventory.GetHotbarSlot(view.index)
                : playerInventory.GetBackpackSlot(view.index);

            bool hasItem = data != null && !data.IsEmpty;

            // 正式代码
            //view.icon.enabled =
            //    hasItem &&
            //    data.block != null &&
            //    data.block.itemIcon != null;

            //if (view.icon.enabled)
            //{
            //    view.icon.sprite = data.block.itemIcon;
            //}

            //view.amount.text = hasItem && data.amount > 1
            //    ? data.amount.ToString()
            //    : "";

            // 测试用，物品栏显示文字
            // ===================
            bool hasIcon = hasItem && data.block != null && data.block.itemIcon != null;

            view.icon.enabled = hasIcon;

            if (hasIcon)
            {
                view.icon.sprite = data.block.itemIcon;
                view.amount.text = data.amount > 1
                    ? data.amount.ToString()
                    : "";
            }
            else if (hasItem)
            {
                // 暂时没有图标时，直接显示方块名称和数量。
                view.amount.text = $"{data.amount}-{data.block.displayName}";
            }
            else
            {
                view.amount.text = "";
            }

            // ===================

            view.selected.SetActive(
                view.isHotbar &&
                playerInventory.SelectedHotbarIndex == view.index
            );
        }
    }

    private void SetInventoryOpen(bool isOpen)
    {
        IsAnyInventoryOpen = isOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
        }

        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool firstPerson =
            cameraModeController != null &&
            cameraModeController.IsFirstPerson;

        Cursor.lockState = firstPerson
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !firstPerson;
    }
}