using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 背包UI系统
public class InventoryUI : MonoBehaviour
{
    // UI中的一个物品格
    private class SlotView
    {
        // 物品图片
        public Image icon;
        // 数量文字
        public Text amount;
        // 选中效果
        public GameObject selected;
        // 是否属于快捷栏
        public bool isHotbar;
        // 对应背包索引
        public int index;
    }

    public static bool IsAnyInventoryOpen { get; private set; }

    [Header("References")]
    // 玩家背包数据
    public PlayerInventory playerInventory;
    // 用于判断第一人称/第三人称状态
    public CameraModeController cameraModeController;

    [Header("Keys")]
    // 打开背包按键
    public KeyCode toggleKey = KeyCode.B;

    // 快捷栏UI列表
    private readonly List<SlotView> hotbarSlots = new();
    // 背包UI列表
    private readonly List<SlotView> backpackSlots = new();

    // 背包窗口
    private GameObject inventoryPanel;
    // 默认字体
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

        // 获取Unity默认字体
        defaultFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );
        // 创建快捷栏
        CreateHotbar();
        // 创建背包窗口
        CreateBackpack();

        // 监听背包变化
        // 物品变化时自动刷新UI
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged += RefreshAll;
        }

        // 默认关闭背包
        SetInventoryOpen(false);

        // 初始刷新
        RefreshAll();
    }

    private void Update()
    {
        // B键打开/关闭背包
        if (Input.GetKeyDown(toggleKey))
        {
            SetInventoryOpen(!IsAnyInventoryOpen);
        }
        // ESC关闭背包
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

        // 恢复游戏时间
        Time.timeScale = 1f;
        IsAnyInventoryOpen = false;
    }

    // 创建底部快捷栏
    // 显示玩家当前可以快速使用的9个物品
    private void CreateHotbar()
    {
        // 创建快捷栏根节点
        GameObject root = CreateUIObject("Hotbar", transform);
        RectTransform rect = root.GetComponent<RectTransform>();

        // 设置快捷栏位置：屏幕底部居中
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 45f);

        // 快捷栏大小
        rect.sizeDelta = new Vector2(860f, 88f);

        // 背景
        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);

        // 水平排列格子
        HorizontalLayoutGroup layout =
            root.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // 创建9个快捷栏格子
        for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
        {
            int slotIndex = i;
            // 创建格子UI
            SlotView view = CreateSlot(root.transform, true, slotIndex);

            // 设置选中颜色
            view.selected.GetComponent<Image>().color =
                new Color(1f, 0.8f, 0f, 0.35f);

            // 点击快捷栏切换物品
            root.transform.GetChild(i).GetComponent<Button>().onClick
                .AddListener(() => playerInventory.SelectHotbarSlot(slotIndex));

            hotbarSlots.Add(view);
        }

        // 创建打开背包按钮
        GameObject moreButton = CreateButton(root.transform, "…");
        moreButton.GetComponent<Button>().onClick.AddListener(
            () => SetInventoryOpen(true)
        );
    }

    // 创建背包窗口
    private void CreateBackpack()
    {
        // 创建背包面板
        inventoryPanel = CreateUIObject("InventoryPanel", transform);

        RectTransform panelRect =
            inventoryPanel.GetComponent<RectTransform>();

        // 设置窗口居中
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        // 背包窗口大小
        panelRect.sizeDelta = new Vector2(820f, 470f);

        // 背景
        Image panelImage = inventoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        // 创建标题
        GameObject title = CreateTextObject(
            "Title",
            inventoryPanel.transform,
            "背包"
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        // 标题位置
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(0f, 40f);

        title.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        title.GetComponent<Text>().fontSize = 28;

        // 创建关闭按钮
        GameObject closeButton = CreateButton(
            inventoryPanel.transform,
            "X"
        );

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        // 放在右上角
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-15f, -15f);

        // 点击关闭
        closeButton.GetComponent<Button>().onClick.AddListener(
            () => SetInventoryOpen(false)
        );

        // 创建背包格子区域
        GameObject grid = CreateUIObject(
            "BackpackSlots",
            inventoryPanel.transform
        );

        RectTransform gridRect = grid.GetComponent<RectTransform>();
        // 格子区域居中
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(744f, 300f);
        gridRect.anchoredPosition = new Vector2(0f, -25f);

        // 网格布局
        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        // 每个格子大小
        gridLayout.cellSize = new Vector2(76f, 76f);
        // 格子间距
        gridLayout.spacing = new Vector2(6f, 6f);
        // 固定9列
        gridLayout.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 9;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        // 创建27个背包格子
        for (int i = 0; i < PlayerInventory.BackpackSlotCount; i++)
        {
            int slotIndex = i;
            SlotView view = CreateSlot(grid.transform, false, slotIndex);

            // 点击背包格子交换物品
            grid.transform.GetChild(i).GetComponent<Button>().onClick
                .AddListener(() =>
                    playerInventory.SwapSelectedHotbarWithBackpack(slotIndex)
                );

            backpackSlots.Add(view);
        }
    }

    // 创建一个物品格
    private SlotView CreateSlot(
        Transform parent,
        bool isHotbar,
        int index)
    {
        // 创建按钮作为格子
        // 点击格子需要响应操作
        GameObject slot = CreateButton(parent, "");

        // 设置格子背景颜色
        Image background = slot.GetComponent<Image>();
        background.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // 创建物品图标
        GameObject iconObject = CreateUIObject("Icon", slot.transform);
        Image icon = iconObject.AddComponent<Image>();

        // 图标不接收点击
        icon.raycastTarget = false;

        // 图标铺满格子
        Stretch(iconObject.GetComponent<RectTransform>(), 8f);

        // 创建数量文字
        GameObject amountObject = CreateTextObject(
            "Amount",
            slot.transform,
            ""
        );

        RectTransform amountRect =
            amountObject.GetComponent<RectTransform>();

        // 数量显示在右下角
        amountRect.anchorMin = Vector2.zero;
        amountRect.anchorMax = new Vector2(1f, 0f);
        amountRect.pivot = new Vector2(1f, 0f);
        amountRect.anchoredPosition = new Vector2(-5f, 4f);
        amountRect.sizeDelta = new Vector2(-10f, 28f);

        Text amountText = amountObject.GetComponent<Text>();
        amountText.alignment = TextAnchor.LowerRight;
        amountText.fontSize = 20;
        amountText.color = Color.white;

        // 数量文字不阻挡点击
        amountText.raycastTarget = false;

        // 创建选中框
        // 快捷栏当前选择物品时显示
        GameObject selected = CreateUIObject("Selected", slot.transform);
        Image selectedImage = selected.AddComponent<Image>();
        // 不影响按钮点击
        selectedImage.raycastTarget = false;
        // 铺满整个格子
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

    // 创建按钮
    private GameObject CreateButton(Transform parent, string text)
    {
        // 创建按钮对象
        GameObject buttonObject = CreateUIObject("Button", parent);

        // 设置按钮默认大小
        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 76f;
        layout.preferredHeight = 76f;

        // 添加背景
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // 添加点击组件
        Button button = buttonObject.AddComponent<Button>();

        // 有文字时创建文字显示
        if (!string.IsNullOrEmpty(text))
        {
            GameObject textObject = CreateTextObject(
                "Text",
                buttonObject.transform,
                text
            );
            // 文字铺满按钮
            Stretch(textObject.GetComponent<RectTransform>(), 0f);

            Text uiText = textObject.GetComponent<Text>();
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.fontSize = 36;
            // 文字不阻挡按钮点击
            uiText.raycastTarget = false;
        }

        return buttonObject;
    }

    // 创建文字对象
    private GameObject CreateTextObject(
        string name,
        Transform parent,
        string content)
    {
        GameObject textObject = CreateUIObject(name, parent);
        Text text = textObject.AddComponent<Text>();

        // 使用默认字体
        text.font = defaultFont;
        text.text = content;
        text.color = Color.white;

        return textObject;
    }

    // 创建UI空对象
    // 所有UI元素最终都会通过它创建
    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(
            name,
            typeof(RectTransform)
        );

        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    // 设置UI拉伸范围
    // padding控制四周边距
    private void Stretch(RectTransform rect, float padding)
    {
        // 从左下角到右上角铺满
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    // 刷新全部背包UI
    // 当背包数据变化时调用
    private void RefreshAll()
    {
        RefreshList(hotbarSlots);
        RefreshList(backpackSlots);
    }

    // 刷新一组格子
    // 将背包数据同步到UI显示
    private void RefreshList(List<SlotView> slots)
    {
        // 没有背包数据，不刷新
        if (playerInventory == null)
        {
            return;
        }

        foreach (SlotView view in slots)
        {
            // 根据类型获取对应数据
            InventorySlotData data = view.isHotbar
                ? playerInventory.GetHotbarSlot(view.index)
                : playerInventory.GetBackpackSlot(view.index);

            // 判断当前格子是否有物品
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

    // 打开/关闭背包
    private void SetInventoryOpen(bool isOpen)
    {
        // 保存当前背包状态
        IsAnyInventoryOpen = isOpen;

        // 显示或隐藏背包窗口
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
        }

        // 打开背包暂停游戏
        // 防止玩家移动和操作
        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            // 打开背包释放鼠标
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // 关闭背包后恢复鼠标状态
        bool firstPerson =
            cameraModeController != null &&
            cameraModeController.IsFirstPerson;

        // 第一人称重新锁定鼠标
        Cursor.lockState = firstPerson
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        // 第一人称隐藏鼠标
        Cursor.visible = !firstPerson;
    }
}