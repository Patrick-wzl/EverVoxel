using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    public Material placeMaterial;   // 放置的方块的材质
    public float interactRange = 5f;   // 放置的范围

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
    }

    private void Update()
    {
        // 鼠标左键破坏，右键放置
        if (Input.GetMouseButtonDown(0))
        {
            TryBreakBlock();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    private bool TryGetTargetBlock(out RaycastHit hit)
    {
        hit = default;

        Ray ray;

        // 第一人称屏幕中心发射射线
        if (cameraModeController != null && cameraModeController.IsFirstPerson)
        {
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            ray = playerCamera.ScreenPointToRay(screenCenter);
        }
        else   // 第三人称鼠标发射射线
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

        // 射线没有击中任何物体
        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        // 只允许操作World下的方块
        if (worldRoot != null && hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        // 判断目标是否在玩家可交互范围内
        float distanceToPlayer = Vector3.Distance(transform.position, hit.collider.transform.position);

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

        Destroy(hit.collider.gameObject);
    }

    // 放置目标方块
    private void TryPlaceBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        // 根据命中的面法线计算新方块的位置
        Vector3 placePosition = hit.collider.transform.position + hit.normal;

        // 四舍五入到整数坐标，保证方块对齐网格
        placePosition = new Vector3(
            Mathf.Round(placePosition.x),
            Mathf.Round(placePosition.y),
            Mathf.Round(placePosition.z)
        );

        // 如果该位置已有碰撞体，则不能放置
        if (Physics.CheckBox(placePosition, Vector3.one * 0.45f))
        {
            return;
        }

        // 创建新的Cube方块
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.position = placePosition;

        // 放到World节点下，保持Hierarchy整洁
        if (worldRoot != null)
        {
            block.transform.parent = worldRoot;
        }

        Renderer renderer = block.GetComponent<Renderer>();

        // 设置方块材质
        if (placeMaterial != null)
        {
            renderer.material = placeMaterial;
        }
    }
}