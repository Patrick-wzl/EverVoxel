using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;

    [Header("Placement")]
    public Material placeMaterial;
    public float interactRange = 2.5f;   // 修改方块距离限制

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
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

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        if (worldRoot != null && hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, hit.collider.transform.position);

        if (distanceToPlayer > interactRange)
        {
            return false;
        }

        return true;
    }

    private void TryBreakBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Destroy(hit.collider.gameObject);
    }

    private void TryPlaceBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Vector3 placePosition = hit.collider.transform.position + hit.normal;
        placePosition = new Vector3(
            Mathf.Round(placePosition.x),
            Mathf.Round(placePosition.y),
            Mathf.Round(placePosition.z)
        );

        if (Physics.CheckBox(placePosition, Vector3.one * 0.45f))
        {
            return;
        }

        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.position = placePosition;

        if (worldRoot != null)
        {
            block.transform.parent = worldRoot;
        }

        Renderer renderer = block.GetComponent<Renderer>();

        if (placeMaterial != null)
        {
            renderer.material = placeMaterial;
        }
    }
}