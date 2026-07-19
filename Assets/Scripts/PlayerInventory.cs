using System;
using UnityEngine;

[Serializable]
public class InventorySlotData
{
    public BlockDefinition block;

    [Min(0)]
    public int amount;

    public bool IsEmpty => block == null || amount <= 0;

    public void Clear()
    {
        block = null;
        amount = 0;
    }
}

[RequireComponent(typeof(BlockInteraction))]
public class PlayerInventory : MonoBehaviour
{
    public const int HotbarSlotCount = 9;
    public const int BackpackSlotCount = 27;
    public const int MaxStackSize = 999;

    [Header("References")]
    public BlockInteraction blockInteraction;

    [Header("快捷栏：屏幕下方前 9 格")]
    public InventorySlotData[] hotbar = new InventorySlotData[HotbarSlotCount];

    [Header("背包：27 格")]
    public InventorySlotData[] backpack = new InventorySlotData[BackpackSlotCount];

    [Range(0, HotbarSlotCount - 1)]
    public int selectedHotbarIndex;

    public int SelectedHotbarIndex => selectedHotbarIndex;

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
        SelectHotbarSlot(selectedHotbarIndex);
        NotifyChanged();
    }

    private void Update()
    {
        if (InventoryUI.IsAnyInventoryOpen)
        {
            return;
        }

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
            }
        }
    }

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

        for (int i = 0; i < hotbar.Length; i++)
        {
            if (hotbar[i] == null)
            {
                hotbar[i] = new InventorySlotData();
            }
        }

        for (int i = 0; i < backpack.Length; i++)
        {
            if (backpack[i] == null)
            {
                backpack[i] = new InventorySlotData();
            }
        }
    }

    public InventorySlotData GetHotbarSlot(int index)
    {
        return index >= 0 && index < hotbar.Length ? hotbar[index] : null;
    }

    public InventorySlotData GetBackpackSlot(int index)
    {
        return index >= 0 && index < backpack.Length ? backpack[index] : null;
    }

    public bool HasSelectedItem()
    {
        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);
        return slot != null && !slot.IsEmpty;
    }

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

    // 掉落物拾取时调用。
    // 同类方块优先填满已有格子；每格最多 999；之后自动寻找下一格。
    public bool TryAddItem(BlockDefinition blockDefinition, int amount)
    {
        if (blockDefinition == null || amount <= 0)
        {
            return false;
        }

        int remaining = amount;

        remaining = AddToExistingSlots(hotbar, blockDefinition, remaining);
        remaining = AddToExistingSlots(backpack, blockDefinition, remaining);

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

            if (slot.IsEmpty || slot.block != blockDefinition)
            {
                continue;
            }

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

    private void NotifyChanged()
    {
        InventoryChanged?.Invoke();
    }
}