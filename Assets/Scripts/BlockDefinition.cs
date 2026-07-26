using UnityEngine;

// CreateAssetMenu：可以在 Project 面板右键创建不同种类的方块资源
[CreateAssetMenu(
    fileName = "New Block",
    menuName = "EverVoxel/Block Definition")]
public class BlockDefinition : ScriptableObject
{
    [Header("Basic Info")]
    public string displayName = "新方块";

    [Header("Appearance")]
    // 方块在世界中使用的材质
    public Material material;

    // 方块在背包中显示的图标
    public Sprite itemIcon;

    [Header("Gameplay")]
    // 是否有实体碰撞
    public bool isSolid = true;

    // 是否允许被破坏
    public bool isBreakable = true;

    // 硬度
    public float hardness = 1f;

    [Header("Drop")]
    // 方块被破坏后实际掉落的方块类型
    // 没有设置时默认掉落自己
    public BlockDefinition dropBlock;

    // 获取方块被破坏后实际掉落的类型
    public BlockDefinition DropDefinition =>
        dropBlock != null
        ? dropBlock
        : this;
}