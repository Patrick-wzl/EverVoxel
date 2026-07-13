using UnityEngine;

// CreateAssetMenu：可以在 Project 面板右键创建不同种类的方块资源
[CreateAssetMenu(fileName = "New Block", menuName = "EverVoxel/Block Definition")]
public class BlockDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string displayName = "新方块";

    [Header("Appearance")]
    // 该方块使用的材质
    public Material material;

    [Header("Gameplay")]
    // 是否有实体碰撞，例如空气、水未来可以设为 false
    public bool isSolid = true;

    // 是否允许被破坏
    public bool isBreakable = true;

    // 破坏硬度。未来可配合镐子、斧头和挖掘时间使用
    public float hardness = 1f;
}