using UnityEngine;

public class BlockDrop : MonoBehaviour
{
    [Header("Drop Data")]
    [SerializeField] private BlockDefinition definition;

    [Header("Visual")]
    public float dropScale = 0.35f;
    public float floatingHeight = 0.08f;
    public float floatingSpeed = 2.5f;
    public float rotationSpeed = 70f;

    [Header("Fall")]
    public float fallSpeed = 5f;
    public float groundCheckPadding = 0.03f;

    [Header("Pickup")]
    public float pickupRange = 1.5f;
    public float pickupDelay = 0.35f;

    private bool hasLanded;
    private Vector3 restingPosition;
    private float floatingOffset;
    private float aliveTime;
    private PlayerInventory playerInventory;

    public BlockDefinition Definition => definition;

    public void Initialize(BlockDefinition blockDefinition)
    {
        definition = blockDefinition;

        transform.localScale = Vector3.one * dropScale;
        transform.rotation = Random.rotation;
        floatingOffset = Random.Range(0f, Mathf.PI * 2f);

        Renderer dropRenderer = GetComponent<Renderer>();

        if (dropRenderer != null &&
            definition != null &&
            definition.material != null)
        {
            dropRenderer.sharedMaterial = definition.material;
        }

        // 掉落物不挡住人物，也不会妨碍放置方块。
        Collider dropCollider = GetComponent<Collider>();

        if (dropCollider != null)
        {
            dropCollider.enabled = false;
        }

        string blockName = definition != null
            ? definition.displayName
            : "未知方块";

        gameObject.name = $"掉落物 - {blockName}";
    }

    private void Start()
    {
        playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (!hasLanded)
        {
            FallToGround();
            return;
        }

        FloatAndRotate();
        TryPickup();
    }

    private void FallToGround()
    {
        float halfDropHeight = transform.localScale.y * 0.5f;
        float fallDistance =
            fallSpeed * Time.deltaTime +
            halfDropHeight +
            groundCheckPadding;

        if (TryGetGroundBelow(fallDistance, out RaycastHit groundHit))
        {
            restingPosition = new Vector3(
                transform.position.x,
                groundHit.point.y + halfDropHeight,
                transform.position.z
            );

            transform.position = restingPosition;
            hasLanded = true;
            return;
        }

        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }

    private bool TryGetGroundBelow(
        float checkDistance,
        out RaycastHit closestGround)
    {
        closestGround = default;
        float closestDistance = float.MaxValue;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            Vector3.down,
            checkDistance
        );

        foreach (RaycastHit hit in hits)
        {
            Block block = hit.collider.GetComponent<Block>();

            if (block == null)
            {
                continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestGround = hit;
            }
        }

        return closestDistance != float.MaxValue;
    }

    private void FloatAndRotate()
    {
        float floatingY = Mathf.Sin(
            Time.time * floatingSpeed + floatingOffset
        ) * floatingHeight;

        transform.position = restingPosition + Vector3.up * floatingY;

        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.World
        );
    }

    private void TryPickup()
    {
        if (aliveTime < pickupDelay || definition == null)
        {
            return;
        }

        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();

            if (playerInventory == null)
            {
                return;
            }
        }

        float distance = Vector3.Distance(
            transform.position,
            playerInventory.transform.position
        );

        if (distance > pickupRange)
        {
            return;
        }

        // 此掉落物代表一个方块。
        // 拾取失败说明全部 36 格背包都无法再放入该物品。
        if (playerInventory.TryAddItem(definition, 1))
        {
            Destroy(gameObject);
        }
    }
}