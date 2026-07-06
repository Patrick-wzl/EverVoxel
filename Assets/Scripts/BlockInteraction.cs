using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    public Material placeMaterial;
    public float interactRange = 5f;

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

        if (cameraModeController != null && cameraModeController.IsFirstPerson)
        {
            Vector3 screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
            ray = playerCamera.ScreenPointToRay(screenCenter);
        }
        else
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

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