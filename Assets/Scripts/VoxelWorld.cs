using UnityEngine;

public class VoxelWorld : MonoBehaviour
{
    [Header("World Size")]
    public int width = 32;
    public int depth = 32;
    public int maxHeight = 6;

    [Header("Noise")]
    public float noiseScale = 12f;

    [Header("Materials")]
    public Material grassMaterial;
    public Material dirtMaterial;
    public Material stoneMaterial;

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
                    CreateBlock(x, y, z, height);
                }
            }
        }
    }

    private void CreateBlock(int x, int y, int z, int columnHeight)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.transform.position = new Vector3(x, y, z);
        block.transform.parent = transform;

        Renderer renderer = block.GetComponent<Renderer>();

        if (y == columnHeight - 1)
        {
            renderer.material = grassMaterial;
        }
        else if (y >= columnHeight - 3)
        {
            renderer.material = dirtMaterial;
        }
        else
        {
            renderer.material = stoneMaterial;
        }
    }
}