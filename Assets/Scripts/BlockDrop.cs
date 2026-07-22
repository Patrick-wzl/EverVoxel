using UnityEngine;

// 方块掉落
public class BlockDrop : MonoBehaviour
{
    [Header("Drop Data")]
    // 掉落物对应的方块
    [SerializeField] private BlockDefinition definition;

    [Header("Visual")]
    // 掉落物大小
    public float dropScale = 0.35f;
    // 漂浮高度
    public float floatingHeight = 0.08f;
    // 漂浮速度
    public float floatingSpeed = 2.5f;
    // 旋转速度
    public float rotationSpeed = 70f;

    [Header("Fall")]
    // 下落速度
    public float fallSpeed = 5f;
    // 地面检测误差
    public float groundCheckPadding = 0.03f;

    [Header("Pickup")]
    // 自动拾取范围
    public float pickupRange = 1.5f;
    // 生成后多久可以拾取
    public float pickupDelay = 0.35f;
    // 是否已经落地
    private bool hasLanded;
    // 落地位置
    private Vector3 restingPosition;
    // 漂浮随机偏移
    private float floatingOffset;
    // 存活时间
    private float aliveTime;
    // 玩家背包
    private PlayerInventory playerInventory;

    public BlockDefinition Definition => definition;

    // 初始化掉落物
    public void Initialize(BlockDefinition blockDefinition)
    {
        definition = blockDefinition;

        // 缩小显示
        transform.localScale = Vector3.one * dropScale;

        // 随机旋转
        transform.rotation = Random.rotation;

        // 随机漂浮相位
        floatingOffset = Random.Range(0f, Mathf.PI * 2f);

        // 设置材质
        Renderer dropRenderer = GetComponent<Renderer>();

        if (dropRenderer != null &&
            definition != null &&
            definition.material != null)
        {
            dropRenderer.sharedMaterial = definition.material;
        }

        // 掉落物关闭碰撞
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
        // 获取玩家背包
        playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        // 未落地：下落
        if (!hasLanded)
        {
            FallToGround();
            return;
        }

        // 落地后漂浮旋转
        FloatAndRotate();

        // 检测拾取
        TryPickup();
    }

    // 掉落物下落
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

    // 检测下面的方块
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

    // 漂浮旋转
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

    // 自动拾取
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

        // 加入背包成功后删除掉落物
        if (playerInventory.TryAddItem(definition, 1))
        {
            Destroy(gameObject);
        }
    }
}