using UnityEngine;
using UnityEngine.EventSystems;

// 玩家与方块交互系统
// 1. 挖掘方块
// 2. 放置方块
// 3. 根据方块硬度计算挖掘时间
// 4. 与背包系统同步物品数量
public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    // 用于判断当前是第一人称还是第三人称
    public CameraModeController cameraModeController;

    [Header("Placement")]
    // 放置的方块
    public BlockDefinition placeBlock;
    // 放置的范围
    public float interactRange = 5f;

    [Header("Breaking")]
    // 最终挖掘时间 = baseBreakTime * hardness
    public float baseBreakTime = 0.75f;

    private VoxelWorld voxelWorld;
    private PlayerInventory inventory;
    // 当前正在被玩家按住左键挖掘的方块
    private Block breakingBlock;
    // 当前已经挖掘的时间
    private float currentBreakTime;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cameraModeController == null && playerCamera != null)
        {
            cameraModeController =
                playerCamera.GetComponent<CameraModeController>();
        }

        if (worldRoot != null)
        {
            voxelWorld = worldRoot.GetComponent<VoxelWorld>();
        }

        inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        // 打开背包时禁止操作世界
        if (InventoryUI.IsAnyInventoryOpen)
        {
            CancelBreakingBlock();
            return;
        }

        // 鼠标位于UI上时，不响应方块操作
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            CancelBreakingBlock();
            return;
        }

        // 左键挖掘方块
        if (Input.GetMouseButtonDown(0))
        {
            BeginBreakingBlock();
        }

        // 持续挖掘
        if (Input.GetMouseButton(0))
        {
            ContinueBreakingBlock();
        }

        // 松开左键取消挖掘
        if (Input.GetMouseButtonUp(0))
        {
            CancelBreakingBlock();
        }

        // 右键放置方块
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    // 获取玩家当前瞄准的方块
    // 第一人称：从屏幕中心发射射线
    // 第三人称：从鼠标位置发射射线
    private bool TryGetTargetBlock(out RaycastHit hit)
    {
        hit = default;

        Ray ray;

        if (cameraModeController != null &&
            cameraModeController.IsFirstPerson)
        {
            ray = playerCamera.ScreenPointToRay(
                new Vector3(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f,
                    0f
                )
            );
        }
        else
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        // 只有带Block组件的对象才是真正方块
        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null)
        {
            return false;
        }

        // 只允许操作World下的方块
        if (worldRoot != null &&
            hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        // 检查玩家距离
        return Vector3.Distance(
            transform.position,
            hit.collider.transform.position
        ) <= interactRange;
    }

    // 开始挖掘方块
    // 左键第一次按下时调用
    // 保存当前目标方块
    private void BeginBreakingBlock()
    {
        CancelBreakingBlock();

        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null ||
            targetBlock.Definition == null ||
            !targetBlock.Definition.isBreakable)
        {
            return;
        }

        breakingBlock = targetBlock;
    }
    // 持续挖掘方块
    // 玩家需要持续看向同一个方块
    // 根据hardness判断是否达到破坏时间
    private void ContinueBreakingBlock()
    {
        if (breakingBlock == null)
        {
            return;
        }

        if (!TryGetTargetBlock(out RaycastHit hit) ||
            hit.collider.GetComponent<Block>() != breakingBlock)
        {
            CancelBreakingBlock();
            return;
        }

        BlockDefinition definition = breakingBlock.Definition;

        if (definition == null || !definition.isBreakable)
        {
            CancelBreakingBlock();
            return;
        }

        // hardness越高，需要挖掘时间越长
        float breakTime =
            baseBreakTime * Mathf.Max(0.05f, definition.hardness);

        currentBreakTime += Time.deltaTime;

        if (currentBreakTime >= breakTime)
        {
            Vector3 position = breakingBlock.transform.position;
            // 生成掉落物
            if (voxelWorld != null)
            {
                voxelWorld.SpawnBlockDrop(position, definition);
            }
            // 删除原方块
            Destroy(breakingBlock.gameObject);
            CancelBreakingBlock();
        }
    }

    // 取消当前挖掘状态
    // 用于：
    // 1. 松开鼠标
    // 2. 改变目标方块
    // 3. 打开背包
    private void CancelBreakingBlock()
    {
        breakingBlock = null;
        currentBreakTime = 0f;
    }

    // 放置当前选择的方块
    // 创建成功后：
    // 消耗背包中的一个物品
    private void TryPlaceBlock()
    {
        if (inventory != null && !inventory.HasSelectedItem())
        {
            return;
        }

        if (placeBlock == null || !TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        // 根据点击面的方向计算放置位置
        Vector3 placePosition = hit.collider.transform.position + hit.normal;

        // 转换为整数坐标
        Vector3Int gridPosition = new Vector3Int(
            Mathf.RoundToInt(placePosition.x),
            Mathf.RoundToInt(placePosition.y),
            Mathf.RoundToInt(placePosition.z)
        );

        // 防止方块重叠
        if (Physics.CheckBox(gridPosition, Vector3.one * 0.45f))
        {
            return;
        }

        if (voxelWorld == null)
        {
            return;
        }

        // 创建方块
        GameObject createdBlock =
            voxelWorld.CreateBlock(gridPosition, placeBlock);

        // 放置成功后消耗物品
        if (createdBlock != null && inventory != null)
        {
            inventory.ConsumeSelectedItem();
        }
    }
}