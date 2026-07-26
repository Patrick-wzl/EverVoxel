using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    [Header("World Size")]
    public int width = 32;
    public int depth = 32;
    public int maxHeight = 6;

    [Header("Noise")]
    public float noiseScale = 12f;

    [Header("Block Types")]
    // 泥土的绿色世界状态
    public BlockDefinition grassBlock;

    // 玩家能够获得和放置的泥土
    public BlockDefinition dirtBlock;

    // 石头
    public BlockDefinition stoneBlock;

    private void Start()
    {
        GenerateWorld();
    }

    private void GenerateWorld()
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float noise = Mathf.PerlinNoise(
                    x / noiseScale,
                    z / noiseScale
                );

                int height =
                    Mathf.FloorToInt(
                        noise * maxHeight
                    ) + 1;

                for (int y = 0; y < height; y++)
                {
                    CreateTerrainBlock(
                        x,
                        y,
                        z,
                        height
                    );
                }
            }
        }
    }

    // 根据方块所在高度，决定它应该是什么种类
    private void CreateTerrainBlock(
        int x,
        int y,
        int z,
        int columnHeight)
    {
        BlockDefinition blockToCreate;

        // 最顶部生成绿色泥土
        if (y == columnHeight - 1)
        {
            blockToCreate = grassBlock;
        }
        // 绿色泥土下方两层生成普通泥土
        else if (y >= columnHeight - 3)
        {
            blockToCreate = dirtBlock;
        }
        // 更深处生成石头
        else
        {
            blockToCreate = stoneBlock;
        }

        CreateBlock(
            new Vector3Int(x, y, z),
            blockToCreate
        );
    }

    // 创建方块
    public GameObject CreateBlock(
        Vector3Int blockPosition,
        BlockDefinition blockDefinition)
    {
        if (blockDefinition == null)
        {
            return null;
        }

        // 创建 Unity Cube
        // Cube自带Mesh Renderer和Box Collider
        GameObject blockObject =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        blockObject.transform.position =
            blockPosition;

        // 所有方块都放到World下
        blockObject.transform.parent =
            transform;

        // 为该Cube添加Block组件
        // 保存它的真实类型
        Block block =
            blockObject.AddComponent<Block>();

        // 把方块定义写入这个方块
        block.Initialize(blockDefinition);

        // 普通泥土和绿色泥土
        // 都需要检测上方是否存在方块
        if (blockDefinition == dirtBlock ||
            blockDefinition == grassBlock)
        {
            GrassGrowth grassGrowth =
                blockObject.AddComponent<GrassGrowth>();

            grassGrowth.Initialize(
                this,
                dirtBlock,
                grassBlock
            );
        }

        return blockObject;
    }

    // 获取指定整数坐标上的方块
    public bool TryGetBlockAt(
        Vector3Int blockPosition,
        out Block targetBlock)
    {
        targetBlock = null;

        // 检查目标格子范围内的所有碰撞体
        Collider[] colliders =
            Physics.OverlapBox(
                blockPosition,
                Vector3.one * 0.45f
            );

        foreach (Collider blockCollider
            in colliders)
        {
            Block block =
                blockCollider.GetComponent<Block>();

            if (block == null)
            {
                continue;
            }

            // 只接受当前World下的方块
            if (block.transform.parent != transform)
            {
                continue;
            }

            targetBlock = block;
            return true;
        }

        return false;
    }

    // 创建方块掉落物
    public GameObject SpawnBlockDrop(
        Vector3 worldPosition,
        BlockDefinition blockDefinition)
    {
        if (blockDefinition == null)
        {
            return null;
        }

        GameObject dropObject =
            GameObject.CreatePrimitive(
                PrimitiveType.Cube
            );

        // 让掉落物从被破坏方块稍微上方出现
        // X/Z有轻微随机偏移
        // 多个方块掉落时不会完全重叠
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.18f, 0.18f),
            0.65f,
            Random.Range(-0.18f, 0.18f)
        );

        dropObject.transform.position =
            worldPosition + randomOffset;

        dropObject.transform.parent =
            transform;

        // 掉落物不添加Block组件
        // 因此不能把掉落物当成世界方块挖掉
        BlockDrop blockDrop =
            dropObject.AddComponent<BlockDrop>();

        blockDrop.Initialize(blockDefinition);

        return dropObject;
    }
}