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
    public BlockDefinition grassBlock;
    public BlockDefinition dirtBlock;
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
                float noise = Mathf.PerlinNoise(x / noiseScale, z / noiseScale);
                int height = Mathf.FloorToInt(noise * maxHeight) + 1;

                for (int y = 0; y < height; y++)
                {
                    CreateTerrainBlock(x, y, z, height);
                }
            }
        }
    }

    // 根据方块所在高度，决定它应该是什么种类
    private void CreateTerrainBlock(int x, int y, int z, int columnHeight)
    {
        BlockDefinition blockToCreate;

        // 最顶部生成草方块
        if (y == columnHeight - 1)
        {
            blockToCreate = grassBlock;
        }
        // 草方块下方两层生成泥土
        else if (y >= columnHeight - 3)
        {
            blockToCreate = dirtBlock;
        }
        // 更深处生成石头
        else
        {
            blockToCreate = stoneBlock;
        }

        CreateBlock(new Vector3Int(x, y, z), blockToCreate);
    }

    // 创建方块
    public GameObject CreateBlock(Vector3Int blockPosition, BlockDefinition blockDefinition)
    {
        if (blockDefinition == null)
        {
            return null;
        }

        // 创建 Unity Cube【Cube 自带 Mesh Renderer 和 Box Collider】
        GameObject blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blockObject.transform.position = blockPosition;

        // 所有方块都放到 World 下
        blockObject.transform.parent = transform;

        // 为该 Cube 添加 Block 组件，保存它的真实类型
        Block block = blockObject.AddComponent<Block>();

        // 把草、泥土、石头等定义写入这个方块
        block.Initialize(blockDefinition);

        return blockObject;
    }
}