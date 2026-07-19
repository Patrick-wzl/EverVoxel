using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    // 放置的方块
    public BlockDefinition placeBlock;
    // 放置的范围
    public float interactRange = 5f;

    [Header("Breaking")]
    [Tooltip("Hardness 为 1 的方块，挖掘需要的基础秒数")]
    public float baseBreakTime = 0.75f;

    private VoxelWorld voxelWorld;

    // 当前正在被玩家按住左键挖掘的方块
    private Block breakingBlock;

    // 已经挖掘了多久
    private float currentBreakTime;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cameraModeController == null && playerCamera != null)
        {
            cameraModeController = playerCamera.GetComponent<CameraModeController>();
        }

        // 从 World 物体获取 VoxelWorld，用它统一创建方块和掉落物
        if (worldRoot != null)
        {
            voxelWorld = worldRoot.GetComponent<VoxelWorld>();
        }
    }

    private void Update()
    {
        UpdateBreakingInput();

        // 鼠标右键放置方块
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    private void UpdateBreakingInput()
    {
        // 左键刚按下：开始尝试挖掘
        if (Input.GetMouseButtonDown(0))
        {
            BeginBreakingBlock();
        }

        // 左键持续按住：累计挖掘时间
        if (Input.GetMouseButton(0))
        {
            ContinueBreakingBlock();
        }

        // 松开左键：取消本次挖掘
        if (Input.GetMouseButtonUp(0))
        {
            CancelBreakingBlock();
        }
    }

    private bool TryGetTargetBlock(out RaycastHit hit)
    {
        hit = default;

        Ray ray;

        // 第一人称：从屏幕中心，也就是准星位置发射射线
        if (cameraModeController != null && cameraModeController.IsFirstPerson)
        {
            Vector3 screenCenter = new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f,
                0f
            );

            ray = playerCamera.ScreenPointToRay(screenCenter);
        }
        else
        {
            // 第三人称：从鼠标所在位置发射射线
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        // 必须击中真正带 Block 组件的物体
        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null)
        {
            return false;
        }

        // 只允许操作 World 下的普通方块
        if (worldRoot != null && hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        // 检查玩家与目标方块的距离
        float distanceToPlayer = Vector3.Distance(
            transform.position,
            hit.collider.transform.position
        );

        if (distanceToPlayer > interactRange)
        {
            return false;
        }

        return true;
    }

    // 左键按下时，记录最开始挖掘的方块
    private void BeginBreakingBlock()
    {
        CancelBreakingBlock();

        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null || targetBlock.Definition == null)
        {
            return;
        }

        // 不可破坏方块，例如未来的基岩，不能开始挖掘
        if (!targetBlock.Definition.isBreakable)
        {
            return;
        }

        breakingBlock = targetBlock;
        currentBreakTime = 0f;
    }

    // 左键持续按住时执行
    private void ContinueBreakingBlock()
    {
        if (breakingBlock == null)
        {
            return;
        }

        // 玩家必须始终看着同一块方块。
        // 视线移开、距离过远、改挖别的方块，都会取消本次挖掘。
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            CancelBreakingBlock();
            return;
        }

        Block currentTarget = hit.collider.GetComponent<Block>();

        if (currentTarget != breakingBlock)
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

        // Hardness 越大，所需挖掘时间越长。
        // Mathf.Max 防止有人把硬度设置成 0 或负数。
        float requiredBreakTime = baseBreakTime * Mathf.Max(0.05f, definition.hardness);

        currentBreakTime += Time.deltaTime;

        if (currentBreakTime >= requiredBreakTime)
        {
            BreakCurrentBlock();
        }
    }

    // 真正摧毁方块，并生成对应类型的掉落物
    private void BreakCurrentBlock()
    {
        if (breakingBlock == null)
        {
            return;
        }

        BlockDefinition definition = breakingBlock.Definition;
        Vector3 blockPosition = breakingBlock.transform.position;

        // 先生成掉落物，再销毁原方块。
        // 掉落物会从原方块附近出现，并自动落到下方方块上。
        if (voxelWorld != null && definition != null)
        {
            voxelWorld.SpawnBlockDrop(blockPosition, definition);
        }

        Destroy(breakingBlock.gameObject);

        CancelBreakingBlock();
    }

    // 松开鼠标、视线移开或目标变化时，重置挖掘进度
    private void CancelBreakingBlock()
    {
        breakingBlock = null;
        currentBreakTime = 0f;
    }

    // 放置当前选择的方块
    private void TryPlaceBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        // 没有指定要放什么方块时，不执行放置
        if (placeBlock == null)
        {
            return;
        }

        // 根据点击面的法线，获得相邻格子的位置
        Vector3 placePosition = hit.collider.transform.position + hit.normal;

        // 转为整数格子坐标，确保方块不偏移
        Vector3Int gridPosition = new Vector3Int(
            Mathf.RoundToInt(placePosition.x),
            Mathf.RoundToInt(placePosition.y),
            Mathf.RoundToInt(placePosition.z)
        );

        // 新位置已有碰撞体时，不允许重叠放置
        if (Physics.CheckBox(gridPosition, Vector3.one * 0.45f))
        {
            return;
        }

        // 通过 VoxelWorld 创建正常方块
        if (voxelWorld != null)
        {
            voxelWorld.CreateBlock(gridPosition, placeBlock);
        }
    }
}