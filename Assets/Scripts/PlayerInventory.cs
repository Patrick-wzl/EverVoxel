using System;
using UnityEngine;

// 背包单个格子的数据类
[Serializable]
public class InventorySlotData
{
    // 当前格子的方块类型
    public BlockDefinition block;

    // 当前数量
    [Min(0)]
    public int amount;

    // 是否为空
    // 没有物品或者数量为0，都认为空
    public bool IsEmpty => block == null || amount <= 0;

    // 清空格子
    public void Clear()
    {
        block = null;
        amount = 0;
    }
}

// 背包数据
[RequireComponent(typeof(BlockInteraction))]
public class PlayerInventory : MonoBehaviour
{
    // 快捷栏数量
    public const int HotbarSlotCount = 9;
    // 普通背包物品栏数量
    public const int BackpackSlotCount = 27;
    // 每个物品栏最大数量
    public const int MaxStackSize = 999;

    [Header("References")]
    public BlockInteraction blockInteraction;

    [Header("快捷栏：屏幕下方前 9 格")]
    // 快捷栏
    // 玩家数字键1-9选择这里的物品
    public InventorySlotData[] hotbar = new InventorySlotData[HotbarSlotCount];

    [Header("背包：27 格")]
    // 背包区域
    public InventorySlotData[] backpack = new InventorySlotData[BackpackSlotCount];

    [Range(0, HotbarSlotCount - 1)]
    // 当前选中的快捷栏索引
    public int selectedHotbarIndex;

    // 外部读取当前选择位置
    public int SelectedHotbarIndex => selectedHotbarIndex;

    // 背包变化事件
    // UI监听这个事件刷新显示
    public event Action InventoryChanged;

    private void Awake()
    {
        if (blockInteraction == null)
        {
            blockInteraction = GetComponent<BlockInteraction>();
        }

        EnsureSlots();
    }

    private void Start()
    {
        // 初始化当前选择物品
        SelectHotbarSlot(selectedHotbarIndex);
        NotifyChanged();
    }

    private void Update()
    {
        // 打开背包时禁止快捷键切换
        if (InventoryUI.IsAnyInventoryOpen)
        {
            return;
        }

        // 数字键1-9切换快捷栏
        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
            }
        }
    }

    // 初始化背包格子
    private void EnsureSlots()
    {
        if (hotbar == null || hotbar.Length != HotbarSlotCount)
        {
            hotbar = new InventorySlotData[HotbarSlotCount];
        }

        if (backpack == null || backpack.Length != BackpackSlotCount)
        {
            backpack = new InventorySlotData[BackpackSlotCount];
        }

        // 创建快捷栏格子
        for (int i = 0; i < hotbar.Length; i++)
        {
            if (hotbar[i] == null)
            {
                hotbar[i] = new InventorySlotData();
            }
        }

        // 创建背包格子
        for (int i = 0; i < backpack.Length; i++)
        {
            if (backpack[i] == null)
            {
                backpack[i] = new InventorySlotData();
            }
        }
    }

    // 获取快捷栏指定格子
    public InventorySlotData GetHotbarSlot(int index)
    {
        return index >= 0 && index < hotbar.Length ? hotbar[index] : null;
    }

    // 获取背包指定格子
    public InventorySlotData GetBackpackSlot(int index)
    {
        return index >= 0 && index < backpack.Length ? backpack[index] : null;
    }

    // 当前快捷栏是否有物品
    public bool HasSelectedItem()
    {
        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);
        return slot != null && !slot.IsEmpty;
    }

    // 选择快捷栏
    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
        {
            return;
        }

        selectedHotbarIndex = index;
        ApplySelectedItemToPlacement();
        NotifyChanged();
    }

    // 消耗当前选择物品
    // 放置方块时调用
    public bool ConsumeSelectedItem()
    {
        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);

        if (slot == null || slot.IsEmpty)
        {
            return false;
        }

        slot.amount--;

        if (slot.amount <= 0)
        {
            slot.Clear();
        }

        ApplySelectedItemToPlacement();
        NotifyChanged();
        return true;
    }

    // 添加物品
    // 掉落物拾取时调用
    public bool TryAddItem(BlockDefinition blockDefinition, int amount)
    {
        if (blockDefinition == null || amount <= 0)
        {
            return false;
        }

        int remaining = amount;

        // 优先填充已有同类物品
        remaining = AddToExistingSlots(hotbar, blockDefinition, remaining);
        remaining = AddToExistingSlots(backpack, blockDefinition, remaining);

        // 没有空余堆叠后，再找空格子
        remaining = AddToEmptySlots(hotbar, blockDefinition, remaining);
        remaining = AddToEmptySlots(backpack, blockDefinition, remaining);

        if (remaining != amount)
        {
            ApplySelectedItemToPlacement();
            NotifyChanged();
        }

        // true 代表全部加入背包；false 代表背包已经完全满了
        return remaining == 0;
    }

    // 添加到已有物品格
    private int AddToExistingSlots(
        InventorySlotData[] slots,
        BlockDefinition blockDefinition,
        int remaining)
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            // 不是同一种物品
            if (slot.IsEmpty || slot.block != blockDefinition)
            {
                continue;
            }

            // 当前格还能放多少
            int canAdd = MaxStackSize - slot.amount;

            if (canAdd <= 0)
            {
                continue;
            }

            int addAmount = Mathf.Min(canAdd, remaining);
            slot.amount += addAmount;
            remaining -= addAmount;
        }

        return remaining;
    }

    // 添加到空格子
    private int AddToEmptySlots(
        InventorySlotData[] slots,
        BlockDefinition blockDefinition,
        int remaining)
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (!slot.IsEmpty)
            {
                continue;
            }

            int addAmount = Mathf.Min(MaxStackSize, remaining);
            slot.block = blockDefinition;
            slot.amount = addAmount;
            remaining -= addAmount;
        }

        return remaining;
    }

    // 交换快捷栏和背包物品
    // 点击背包格子时调用
    public void SwapSelectedHotbarWithBackpack(int backpackIndex)
    {
        InventorySlotData hotbarSlot = GetHotbarSlot(selectedHotbarIndex);
        InventorySlotData backpackSlot = GetBackpackSlot(backpackIndex);

        if (hotbarSlot == null || backpackSlot == null)
        {
            return;
        }

        BlockDefinition temporaryBlock = hotbarSlot.block;
        int temporaryAmount = hotbarSlot.amount;

        hotbarSlot.block = backpackSlot.block;
        hotbarSlot.amount = backpackSlot.amount;

        backpackSlot.block = temporaryBlock;
        backpackSlot.amount = temporaryAmount;

        ApplySelectedItemToPlacement();
        NotifyChanged();
    }

    // 把当前快捷栏物品同步给放置系统
    private void ApplySelectedItemToPlacement()
    {
        if (blockInteraction == null)
        {
            return;
        }

        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);

        blockInteraction.placeBlock =
            slot != null && !slot.IsEmpty
            ? slot.block
            : null;
    }

    // 通知UI刷新
    private void NotifyChanged()
    {
        InventoryChanged?.Invoke();
    }
}