using UnityEngine;

// CreateAssetMenu：可以在 Project 面板右键创建不同种类的方块资源
[CreateAssetMenu(fileName = "New Block", menuName = "EverVoxel/Block Definition")]
public class BlockDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string displayName = "新方块";

    [Header("Appearance")]
    // 材质
    public Material material;
    public Sprite itemIcon;

    [Header("Gameplay")]
    // 是否有实体碰撞
    public bool isSolid = true;

    // 是否允许被破坏
    public bool isBreakable = true;

    // 硬度
    public float hardness = 1f;
}