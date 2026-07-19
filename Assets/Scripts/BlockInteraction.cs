using UnityEngine;
using UnityEngine.EventSystems;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    public BlockDefinition placeBlock;
    public float interactRange = 5f;

    [Header("Breaking")]
    public float baseBreakTime = 0.75f;

    private VoxelWorld voxelWorld;
    private PlayerInventory inventory;
    private Block breakingBlock;
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
        if (InventoryUI.IsAnyInventoryOpen)
        {
            CancelBreakingBlock();
            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            CancelBreakingBlock();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginBreakingBlock();
        }

        if (Input.GetMouseButton(0))
        {
            ContinueBreakingBlock();
        }

        if (Input.GetMouseButtonUp(0))
        {
            CancelBreakingBlock();
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

        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null)
        {
            return false;
        }

        if (worldRoot != null &&
            hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        return Vector3.Distance(
            transform.position,
            hit.collider.transform.position
        ) <= interactRange;
    }

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

        float breakTime =
            baseBreakTime * Mathf.Max(0.05f, definition.hardness);

        currentBreakTime += Time.deltaTime;

        if (currentBreakTime >= breakTime)
        {
            Vector3 position = breakingBlock.transform.position;

            if (voxelWorld != null)
            {
                voxelWorld.SpawnBlockDrop(position, definition);
            }

            Destroy(breakingBlock.gameObject);
            CancelBreakingBlock();
        }
    }

    private void CancelBreakingBlock()
    {
        breakingBlock = null;
        currentBreakTime = 0f;
    }

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

        Vector3 placePosition = hit.collider.transform.position + hit.normal;

        Vector3Int gridPosition = new Vector3Int(
            Mathf.RoundToInt(placePosition.x),
            Mathf.RoundToInt(placePosition.y),
            Mathf.RoundToInt(placePosition.z)
        );

        if (Physics.CheckBox(gridPosition, Vector3.one * 0.45f))
        {
            return;
        }

        if (voxelWorld == null)
        {
            return;
        }

        GameObject createdBlock =
            voxelWorld.CreateBlock(gridPosition, placeBlock);

        if (createdBlock != null && inventory != null)
        {
            inventory.ConsumeSelectedItem();
        }
    }
}