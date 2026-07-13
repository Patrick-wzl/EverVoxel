using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    public BlockDefinition placeBlock;

    public float interactRange = 5f;

    private VoxelWorld voxelWorld;

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

        // 从 World 物体获取 VoxelWorld，用它统一创建方块
        if (worldRoot != null)
        {
            voxelWorld = worldRoot.GetComponent<VoxelWorld>();
        }
    }

    private void Update()
    {
        // 鼠标左键破坏方块
        if (Input.GetMouseButtonDown(0))
        {
            TryBreakBlock();
        }

        // 鼠标右键放置方块
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
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

        // 只允许操作 World 下的方块
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

    // 摧毁目标方块
    private void TryBreakBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        // 未来基岩等方块可设为不可破坏
        if (targetBlock != null &&
            targetBlock.Definition != null &&
            !targetBlock.Definition.isBreakable)
        {
            return;
        }

        Destroy(hit.collider.gameObject);
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

        // 通过 VoxelWorld 创建方块，确保所有方块都有 Block 类型资料
        if (voxelWorld != null)
        {
            voxelWorld.CreateBlock(gridPosition, placeBlock);
        }
    }
}