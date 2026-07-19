using UnityEngine;

// 方块掉落
// 待实现：以后制作背包时，在这里继续添加“靠近玩家自动拾取”的功能。
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

    private bool hasLanded;
    private Vector3 restingPosition;
    private float floatingOffset;

    // 掉落物代表的原始方块类型。
    // 以后背包拾取时，可以通过它知道拾取的是草、泥土还是石头。
    public BlockDefinition Definition => definition;

    public void Initialize(BlockDefinition blockDefinition)
    {
        definition = blockDefinition;

        // 掉落物缩小为普通方块的 35%
        transform.localScale = Vector3.one * dropScale;

        // 让每个掉落物的初始角度略有不同，看起来更自然
        transform.rotation = Random.rotation;

        floatingOffset = Random.Range(0f, Mathf.PI * 2f);

        Renderer dropRenderer = GetComponent<Renderer>();

        if (dropRenderer != null && definition != null && definition.material != null)
        {
            // 使用原方块的材质，因此草会掉草方块，石头会掉石头
            dropRenderer.sharedMaterial = definition.material;
        }

        // 掉落物暂时不需要实体碰撞。
        // 这样它不会挡住玩家，也不会阻止后续放置方块。
        Collider dropCollider = GetComponent<Collider>();

        if (dropCollider != null)
        {
            dropCollider.enabled = false;
        }

        string blockName = definition != null ? definition.displayName : "未知方块";
        gameObject.name = $"掉落物 - {blockName}";
    }

    private void Update()
    {
        if (!hasLanded)
        {
            FallToGround();
            return;
        }

        FloatAndRotate();
    }

    private void FallToGround()
    {
        float halfDropHeight = transform.localScale.y * 0.5f;
        float fallDistance = fallSpeed * Time.deltaTime + halfDropHeight + groundCheckPadding;

        if (TryGetGroundBelow(fallDistance, out RaycastHit groundHit))
        {
            // 掉落物中心停在地面上方，避免模型嵌进方块。
            restingPosition = new Vector3(
                transform.position.x,
                groundHit.point.y + halfDropHeight,
                transform.position.z
            );

            transform.position = restingPosition;
            hasLanded = true;
            return;
        }

        // 下方暂时没有方块，继续下落
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
    }

    private bool TryGetGroundBelow(float checkDistance, out RaycastHit closestGround)
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
            // 只把真正带 Block 组件的物体视为地面。
            // 玩家、相机、未来的怪物等不会让掉落物停住。
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
}