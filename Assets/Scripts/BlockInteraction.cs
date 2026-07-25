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
    // 第一人称放置和破坏方块的范围
    public float interactRange = 5f;

    [Header("Third Person")]
    // 第三人称向下寻找人物脚下方块的距离
    public float thirdPersonGroundCheckDistance = 2.5f;

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

        if (cameraModeController == null &&
            playerCamera != null)
        {
            cameraModeController =
                playerCamera.GetComponent<CameraModeController>();
        }

        if (worldRoot != null)
        {
            voxelWorld =
                worldRoot.GetComponent<VoxelWorld>();
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

        // 按住左键持续挖掘方块
        if (Input.GetMouseButton(0))
        {
            // 当前没有正在挖掘的方块时
            // 自动寻找新的目标方块
            if (breakingBlock == null)
            {
                BeginBreakingBlock();
            }

            // 持续挖掘当前目标方块
            ContinueBreakingBlock();
        }

        // 松开左键取消挖掘
        if (Input.GetMouseButtonUp(0))
        {
            CancelBreakingBlock();
        }

        // 右键放置方块
        // 只有右键第一次按下时才放置
        // 持续按住右键不会连续放置
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    // 获取玩家当前准备破坏的方块
    // 第一人称：从屏幕中心发射射线
    // 第三人称：寻找人物身前的方块
    private bool TryGetBreakingTarget(
        out Block targetBlock)
    {
        targetBlock = null;

        if (cameraModeController != null &&
            cameraModeController.IsFirstPerson)
        {
            if (!TryGetFirstPersonTargetBlock(
                out RaycastHit hit))
            {
                return false;
            }

            targetBlock =
                hit.collider.GetComponent<Block>();

            return targetBlock != null;
        }

        return TryGetThirdPersonTargetBlock(
            out targetBlock
        );
    }

    // 获取第一人称当前瞄准的方块
    // 从屏幕中心发射射线
    private bool TryGetFirstPersonTargetBlock(
        out RaycastHit hit)
    {
        hit = default;

        if (playerCamera == null)
        {
            return false;
        }

        Ray ray = playerCamera.ScreenPointToRay(
            new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f,
                0f
            )
        );

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        // 只有带Block组件的对象才是真正方块
        Block targetBlock =
            hit.collider.GetComponent<Block>();

        if (targetBlock == null)
        {
            return false;
        }

        // 只允许操作World下的方块
        if (!IsWorldBlock(targetBlock))
        {
            return false;
        }

        // 检查玩家距离
        return Vector3.Distance(
            transform.position,
            targetBlock.transform.position
        ) <= interactRange;
    }

    // 获取第三人称当前准备破坏的方块
    // 优先寻找身前上上层方块
    // 然后寻找身前上层方块
    // 最后寻找身前地面方块
    private bool TryGetThirdPersonTargetBlock(
        out Block targetBlock)
    {
        targetBlock = null;

        if (!TryGetThirdPersonGridPositions(
            out Vector3Int frontGroundPosition,
            out Vector3Int frontUpperPosition,
            out Vector3Int frontTopPosition))
        {
            return false;
        }

        // 优先破坏人物身前上上层的方块
        if (TryGetBlockAtGridPosition(
            frontTopPosition,
            out targetBlock))
        {
            return true;
        }

        // 身前上上层为空时
        // 尝试破坏人物身前上层方块
        if (TryGetBlockAtGridPosition(
            frontUpperPosition,
            out targetBlock))
        {
            return true;
        }

        // 身前上层为空时
        // 尝试破坏人物身前地面方块
        return TryGetBlockAtGridPosition(
            frontGroundPosition,
            out targetBlock
        );
    }

    // 获取第三人称放置方块的位置
    // 按照地面、上层、上上层的顺序寻找空位
    private bool TryGetThirdPersonPlacePosition(
        out Vector3Int placePosition)
    {
        placePosition = default;

        if (!TryGetThirdPersonGridPositions(
            out Vector3Int frontGroundPosition,
            out Vector3Int frontUpperPosition,
            out Vector3Int frontTopPosition))
        {
            return false;
        }

        // 检查人物身前地面是否存在方块
        bool hasFrontGroundBlock =
            TryGetBlockAtGridPosition(
                frontGroundPosition,
                out Block frontGroundBlock
            );

        // 身前地面为空
        // 在地面位置放置方块，用于填坑或者铺路
        if (!hasFrontGroundBlock)
        {
            placePosition = frontGroundPosition;
            return true;
        }

        // 检查人物身前上层是否存在方块
        bool hasFrontUpperBlock =
            TryGetBlockAtGridPosition(
                frontUpperPosition,
                out Block frontUpperBlock
            );

        // 身前地面存在，上层为空
        // 把方块放在身前上层
        if (!hasFrontUpperBlock)
        {
            placePosition = frontUpperPosition;
            return true;
        }

        // 检查人物身前上上层是否存在方块
        bool hasFrontTopBlock =
            TryGetBlockAtGridPosition(
                frontTopPosition,
                out Block frontTopBlock
            );

        // 身前地面和上层存在，上上层为空
        // 把方块放在身前上上层
        if (!hasFrontTopBlock)
        {
            placePosition = frontTopPosition;
            return true;
        }

        // 身前地面、上层和上上层都有方块
        // 当前没有可以放置方块的位置
        return false;
    }

    // 计算第三人称人物身前的三个方块位置
    private bool TryGetThirdPersonGridPositions(
        out Vector3Int frontGroundPosition,
        out Vector3Int frontUpperPosition,
        out Vector3Int frontTopPosition)
    {
        frontGroundPosition = default;
        frontUpperPosition = default;
        frontTopPosition = default;

        // 找到人物当前站立的方块
        if (!TryGetStandingBlockPosition(
            out Vector3Int standingBlockPosition))
        {
            return false;
        }

        // 把人物当前朝向转换成方块方向
        Vector3Int facingDirection =
            GetThirdPersonGridDirection();

        // 人物脚下方块加上人物朝向
        // 得到人物身前的地面方块位置
        frontGroundPosition =
            standingBlockPosition + facingDirection;

        // 身前地面上方一格
        frontUpperPosition =
            frontGroundPosition + Vector3Int.up;

        // 身前地面上方两格
        frontTopPosition =
            frontGroundPosition + Vector3Int.up * 2;

        return true;
    }

    // 获取人物当前站立方块的位置
    private bool TryGetStandingBlockPosition(
        out Vector3Int standingBlockPosition)
    {
        standingBlockPosition = default;

        // 从人物中心稍微上方开始
        // 向下寻找人物脚下的方块
        Vector3 rayOrigin =
            transform.position + Vector3.up * 0.1f;

        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            thirdPersonGroundCheckDistance
        );

        Block closestBlock = null;
        float closestDistance = float.MaxValue;

        foreach (RaycastHit hit in hits)
        {
            // 只有带Block组件的对象才是真正方块
            Block block =
                hit.collider.GetComponent<Block>();

            if (block == null ||
                !IsWorldBlock(block))
            {
                continue;
            }

            // 保存距离人物最近的脚下方块
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestBlock = block;
            }
        }

        if (closestBlock == null)
        {
            return false;
        }

        // 把脚下方块的位置转换为整数坐标
        standingBlockPosition = new Vector3Int(
            Mathf.RoundToInt(
                closestBlock.transform.position.x
            ),
            Mathf.RoundToInt(
                closestBlock.transform.position.y
            ),
            Mathf.RoundToInt(
                closestBlock.transform.position.z
            )
        );

        return true;
    }

    // 把人物当前朝向转换成八方向方块坐标
    private Vector3Int GetThirdPersonGridDirection()
    {
        Vector3 forward = transform.forward;

        // 第三人称只使用水平方向
        forward.y = 0f;
        forward.Normalize();

        int directionX = 0;
        int directionZ = 0;

        // 0.382683约等于22.5度的正弦值
        // 用它把人物朝向平均划分为八个方向
        const float directionThreshold = 0.382683f;

        if (Mathf.Abs(forward.x) >=
            directionThreshold)
        {
            directionX =
                forward.x > 0f ? 1 : -1;
        }

        if (Mathf.Abs(forward.z) >=
            directionThreshold)
        {
            directionZ =
                forward.z > 0f ? 1 : -1;
        }

        // 正常情况下至少会得到一个有效方向
        // 如果没有得到方向，默认使用世界前方
        if (directionX == 0 &&
            directionZ == 0)
        {
            directionZ = 1;
        }

        return new Vector3Int(
            directionX,
            0,
            directionZ
        );
    }

    // 获取指定整数坐标上的方块
    private bool TryGetBlockAtGridPosition(
        Vector3Int gridPosition,
        out Block targetBlock)
    {
        targetBlock = null;

        // 检查这个格子范围内的所有碰撞体
        Collider[] colliders = Physics.OverlapBox(
            gridPosition,
            Vector3.one * 0.45f
        );

        foreach (Collider blockCollider in colliders)
        {
            Block block =
                blockCollider.GetComponent<Block>();

            if (block == null ||
                !IsWorldBlock(block))
            {
                continue;
            }

            targetBlock = block;
            return true;
        }

        return false;
    }

    // 判断方块是否属于当前VoxelWorld
    private bool IsWorldBlock(Block block)
    {
        if (block == null)
        {
            return false;
        }

        // 没有设置World Root时
        // 接受所有带Block组件的方块
        if (worldRoot == null)
        {
            return true;
        }

        return block.transform.parent == worldRoot;
    }

    // 开始挖掘方块
    // 当前没有正在挖掘的方块时调用
    // 保存当前目标方块
    private void BeginBreakingBlock()
    {
        CancelBreakingBlock();

        if (!TryGetBreakingTarget(
            out Block targetBlock))
        {
            return;
        }

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

        if (!TryGetBreakingTarget(
                out Block targetBlock) ||
            targetBlock != breakingBlock)
        {
            CancelBreakingBlock();
            return;
        }

        BlockDefinition definition =
            breakingBlock.Definition;

        if (definition == null ||
            !definition.isBreakable)
        {
            CancelBreakingBlock();
            return;
        }

        // hardness越高，需要挖掘时间越长
        float breakTime =
            baseBreakTime *
            Mathf.Max(
                0.05f,
                definition.hardness
            );

        currentBreakTime += Time.deltaTime;

        if (currentBreakTime >= breakTime)
        {
            Vector3 position =
                breakingBlock.transform.position;

            // 生成掉落物
            if (voxelWorld != null)
            {
                voxelWorld.SpawnBlockDrop(
                    position,
                    definition
                );
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
        if (inventory != null &&
            !inventory.HasSelectedItem())
        {
            return;
        }

        if (placeBlock == null)
        {
            return;
        }

        Vector3Int gridPosition;

        if (cameraModeController != null &&
            cameraModeController.IsFirstPerson)
        {
            if (!TryGetFirstPersonTargetBlock(
                out RaycastHit hit))
            {
                return;
            }

            // 根据点击面的方向计算放置位置
            Vector3 placePosition =
                hit.collider.transform.position +
                hit.normal;

            // 转换为整数坐标
            gridPosition = new Vector3Int(
                Mathf.RoundToInt(placePosition.x),
                Mathf.RoundToInt(placePosition.y),
                Mathf.RoundToInt(placePosition.z)
            );
        }
        else
        {
            // 第三人称不使用鼠标位置选择目标
            // 根据人物朝向计算身前放置位置
            if (!TryGetThirdPersonPlacePosition(
                out gridPosition))
            {
                return;
            }
        }

        // 防止方块重叠
        if (Physics.CheckBox(
            gridPosition,
            Vector3.one * 0.45f))
        {
            return;
        }

        if (voxelWorld == null)
        {
            return;
        }

        // 创建方块
        GameObject createdBlock =
            voxelWorld.CreateBlock(
                gridPosition,
                placeBlock
            );

        // 放置成功后消耗物品
        if (createdBlock != null &&
            inventory != null)
        {
            inventory.ConsumeSelectedItem();
        }
    }
}