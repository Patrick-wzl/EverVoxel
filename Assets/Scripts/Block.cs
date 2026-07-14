using UnityEngine;

public class Block : MonoBehaviour
{
    [Header("Block Data")]
    [SerializeField] private BlockDefinition definition;

    public BlockDefinition Definition => definition;

    // 初始化方块
    public void Initialize(BlockDefinition blockDefinition)
    {
        definition = blockDefinition;
        ApplyDefinition();
    }

    // 根据方块资料，应用材质与碰撞体设置
    private void ApplyDefinition()
    {
        if (definition == null)
        {
            return;
        }

        // 设置方块材质
        Renderer blockRenderer = GetComponent<Renderer>();

        if (blockRenderer != null && definition.material != null)
        {
            blockRenderer.material = definition.material;
        }

        // 根据方块资料决定是否启用碰撞体
        Collider blockCollider = GetComponent<Collider>();

        if (blockCollider != null)
        {
            blockCollider.enabled = definition.isSolid;
        }

        // 场景中显示的物体名称更清楚，例如：GrassBlock (草方块)
        gameObject.name = $"{definition.name} ({definition.displayName})";
    }
}