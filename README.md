# 游戏背景
这款游戏参考如下两个游戏：
- 我的世界：第一人称
- 我的世界地下城：第三人称【上帝视角】

游戏最终效果如下：
- 像我的世界，是开放式的，所有地形都是方块
- 对于树木，怪物并不是方块的。是有血条的【参考泰拉瑞亚】
- 游戏有两个视角：两种视角可以自由自由切换
  1. 我的世界那种，方便玩家建造，探索
  2. 我的世界地下城那种，方便玩家探索，杀怪
- 游戏有各种怪，各种地形，各种方块，各种装备：就像我的世界地下城
- 对于建造，要能像我的世界那样，建起复杂建筑。未来要实现蓝图功能，只要建好一次，就可以用蓝图一键建造

这个宏大的游戏效果是未来的最终效果，这个项目就命名为：EverVoxel


# 开发环境

https://unity.com/download，使用google账号登录。下载安装UnityHub，然后安装：Unity 6.3 LTS (6000.3.19f1)，勾选：

- Microsoft Visual Studio Community
- Windows Build Support (IL2CPP)
- Android Build Support
  - Android SDK & NDK Tools
  - OpenJDK

新建项目：Universal 3D (URP)

Assets/Scenes/SampleScene重命名为Main。Assets下新建目录：Scripts、Materials、Prefabs

设置输入系统

```
Edit > Project Settings > Player > Other Settings > Active Input Handling
```

改成：

```
Both
```

重启 Unity  



# 开发路线

第一阶段（地基）：

- 简单地形：已实现
- 人物移动：已实现
- 视角切换：已实现
- 背包/装备栏/物品掉落：待实现
- 方块血条
- 人物血条
- 第三人称视角：待讨论，待整改

第二阶段（MC）：

- 天气
- 日夜交替
- 各种方块
- 树/草
- 简单生物
- 僵尸
- 无限地图
- 模型设计
- 美术
- 存档

第三阶段（地下城）：

- 各种地对生物
- 各种地形
- 各种事件【血月，火星人入侵】
- 少量剧情
- npc

第四阶段（大型）：

- 蓝图系统
-  地牢
- Boss
- 联机
- 手机适配

# 已实现功能

角色在一片方块地形，wsad上下左右移动，空格跳跃，按v切换视角，放置破坏方块

## 方块

Assets/Materials 创建 3 个材质：右键 -> Create > Material

```
Grass.mat   绿色
Dirt.mat   棕色
Stone.mat   灰色
```



在 `Assets/Scripts` 创建 `BlockDefinition.cs`：

```c#
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
```



在 `Assets` 下新建一个目录`Blocks`

然后在 `Blocks` 目录空白处右键：Create -> EverVoxel -> Block Definition

创建三个资源并命名：GrassBlock、DirtBlock、StoneBlock。依次选中它们，在 Inspector 设置：

| 方块资源   | Display Name | Material  | Hardness |
| ---------- | ------------ | --------- | -------- |
| GrassBlock | 草方块       | Grass.mat | 1        |
| DirtBlock  | 泥土         | Dirt.mat  | 0.8      |
| StoneBlock | 石头         | Stone.mat | 3        |

`Is Solid` 与 `Is Breakable` 都保持勾选



 `Assets/Scripts` 新建`Block.cs`：

```c#
using UnityEngine;

// 每一个实际生成到场景里的方块，都会挂上这个组件
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
```



## 地形

Assets/Scripts 创建 VoxelWorld.cs

```c#
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
    // 不再直接引用材质，而是引用真正的方块定义
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

    // 创建一个真正具有方块定义的方块
    public GameObject CreateBlock(Vector3Int blockPosition, BlockDefinition blockDefinition)
    {
        // 没有方块资料时不生成，避免产生没有类型的 Cube
        if (blockDefinition == null)
        {
            return null;
        }

        // 创建 Unity Cube，Cube 自带 Mesh Renderer 和 Box Collider
        GameObject blockObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // Vector3Int 保证方块始终对齐整数网格
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
```

在 Hierarchy 里右键 -> Create Empty 命名为 World

把 `VoxelWorld.cs` 拖到 `World` 上

把 3 种方块拖到`VoxelWorld`组件对应位置

```
GrassBlock  -> Grass Block
DirtBlock   -> Dirt Block
StoneBlock  -> Stone Block
```



## 角色

在 Hierarchy 里：右键 > 3D Object > Capsule 命名 Player

设置位置：

```
Position X: 16
Position Y: 12
Position Z: 16
```

给 Player 添加组件：

```
Add Component > Character Controller
```

设置 Character Controller：

```
Center X: 0
Center Y: 0
Center Z: 0

Height: 2
Radius: 0.5
```

Capsule 自带的 Capsule Collider 可以删掉，因为我们用 `Character Controller`

在Assets/Scripts 创建 `PlayerController.cs`

```c#
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 4f;
    public float jumpHeight = 2f;
    public float gravity = -9.8f;   // 重力加速度，负数表示向下

    [Header("View")]
    public CameraModeController cameraModeController;

    private CharacterController controller;
    private Vector3 velocity;
    private bool canJump;

    private void Awake()
    {
        // 获取CharacterController
        controller = GetComponent<CharacterController>();

        if (cameraModeController == null)
        {
            cameraModeController = Camera.main.GetComponent<CameraModeController>();
        }
    }

    private void Update()
    {
        // 在地面允许跳跃
        if (controller.isGrounded)
        {
            canJump = true;
            velocity.y = -2f;
        }

        // 获取输入按键ADWS
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move;

        // 第一人称运动逻辑
        if (cameraModeController != null && cameraModeController.IsFirstPerson)
        {
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;

            forward.y = 0f;
            right.y = 0f;

            forward.Normalize();
            right.Normalize();

            move = forward * vertical + right * horizontal;
        }
        else   // 第三人称运动逻辑
        {
            move = new Vector3(horizontal, 0f, vertical);
        }

        move = Vector3.ClampMagnitude(move, 1f);

        // 跳跃逻辑
        if (canJump && Input.GetKeyDown(KeyCode.Space))
        {
            // v2=v02​+2as
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            canJump = false;
        }

        velocity.y += gravity * Time.deltaTime;

        // 把水平移动和竖直移动合并成一个向量
        Vector3 finalMove = move * moveSpeed;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }
}
```

选中 `Player`

1. 把 `PlayerController.cs` 拖到 `Player` 上
2. 把 `Main Camera` 拖到 `PlayerController` 的 `Camera Mode Controller` 字段




## 视角

按v切换第三人称【上帝视角】视角、第一人称



选中场景里的 `Main Camera`。设置初始位置：

```
Position X: 16
Position Y: 18
Position Z: 6

Rotation X: 60
Rotation Y: 0
Rotation Z: 0
```



在 `Assets/Scripts` 创建 `CameraModeController.cs`

```c#
using UnityEngine;

public class CameraModeController : MonoBehaviour
{
    public enum ViewMode
    {
        TopDown,   // 第三人称【俯视角】
        FirstPerson   // 第一人称
    }

    [Header("References")]
    public Transform target;   // 角色

    [Header("Switch")]
    public KeyCode switchKey = KeyCode.V;   // 切换视角的按键为v
    public ViewMode currentMode = ViewMode.TopDown;   // 默认第三人称视角

    [Header("Top Down")]
    public Vector3 topDownOffset = new Vector3(0f, 10f, -10f);
    public float topDownSmoothSpeed = 8f;

    [Header("First Person")]
    public Vector3 firstPersonOffset = new Vector3(0f, 0.75f, 0f);
    public float mouseSensitivity = 2.5f;   // 鼠标灵敏度
    public float minPitch = -80f;   // 第一人称允许向上下看的最大角度
    public float maxPitch = 80f;

    // 人物负责左右旋转（yaw），相机继承人物的左右旋转，再额外负责上下旋转
    private float yaw;   // 角色左右旋转角度
    private float pitch;   // 相机上下俯仰角

    public bool IsFirstPerson => currentMode == ViewMode.FirstPerson;

    private void Start()
    {
        if (target != null)
        {
            // 初始化yaw，使相机朝向与角色初始朝向一致
            yaw = target.eulerAngles.y;
        }
        // 根据当前模式设置鼠标状态
        ApplyCursorState();
    }

    private void Update()
    {
        // v键切换视角
        if (Input.GetKeyDown(switchKey))
        {
            ToggleMode();
        }

        // 第一人称下，根据鼠标移动更新角色朝向和相机俯仰角
        if (IsFirstPerson)
        {
            UpdateFirstPersonLook();
        }
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (IsFirstPerson)
        {
            UpdateFirstPersonCamera();
        }
        else
        {
            UpdateTopDownCamera();
        }
    }

    // 切换视角
    private void ToggleMode()
    {
        currentMode = IsFirstPerson ? ViewMode.TopDown : ViewMode.FirstPerson;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }

        // 重置上下视角，避免切换回第一人称时仍保持之前的俯仰角
        pitch = 0f;
        ApplyCursorState();
    }

    // 根据当前模式设置鼠标状态
    private void ApplyCursorState()
    {
        if (IsFirstPerson)
        {
            // 锁定鼠标到屏幕中央
            Cursor.lockState = CursorLockMode.Locked;
            // 第一人称隐藏鼠标
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void UpdateFirstPersonLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        target.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    private void UpdateFirstPersonCamera()
    {
        // 将相机放到角色头部位置
        transform.position = target.position + target.TransformDirection(firstPersonOffset);
        // 应用pitch和yaw，实现第一人称视角
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    private void UpdateTopDownCamera()
    {
        Vector3 desiredPosition = target.position + topDownOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            topDownSmoothSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up);
    }
}
```

选中 `Main Camera`

1. 把 `CameraModeController.cs` 拖到 `Main Camera` 上
2. 把 Hierarchy 里的 `Player` 拖到 `Target`字段



## 放置/摧毁方块

鼠标左键摧毁，鼠标右键放置：

- 第三人称：范围是以角色为球心2个单元格半径
- 第一人称：同MC



在 `Assets/Scripts` 创建 `BlockInteraction.cs`

```c#
using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    public BlockDefinition placeBlock;   // 放置的方块
    public float interactRange = 5f;   // 放置的范围

    private VoxelWorld voxelWorld;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cameraModeController == null && playerCamera != null)
        {
            cameraModeController = playerCamera.GetComponent<CameraModeController>();
        }

        // 从 World 物体获取 VoxelWorld，用它统一创建方块
        if (worldRoot != null)
        {
            voxelWorld = worldRoot.GetComponent<VoxelWorld>();
        }
    }

    private void Update()
    {
        // 鼠标左键破坏方块
        if (Input.GetMouseButtonDown(0))
        {
            TryBreakBlock();
        }

        // 鼠标右键放置方块
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    private bool TryGetTargetBlock(out RaycastHit hit)
    {
        hit = default;

        Ray ray;

        // 第一人称：从屏幕中心，也就是准星位置发射射线
        if (cameraModeController != null && cameraModeController.IsFirstPerson)
        {
            Vector3 screenCenter = new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f,
                0f
            );

            ray = playerCamera.ScreenPointToRay(screenCenter);
        }
        else
        {
            // 第三人称：从鼠标所在位置发射射线
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

        // 射线没有击中任何物体
        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        // 必须击中真正带 Block 组件的物体
        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null)
        {
            return false;
        }

        // 只允许操作 World 下的方块
        if (worldRoot != null && hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        // 检查玩家与目标方块的距离
        float distanceToPlayer = Vector3.Distance(
            transform.position,
            hit.collider.transform.position
        );

        if (distanceToPlayer > interactRange)
        {
            return false;
        }

        return true;
    }

    // 摧毁目标方块
    private void TryBreakBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        // 未来基岩等方块可设为不可破坏
        if (targetBlock != null &&
            targetBlock.Definition != null &&
            !targetBlock.Definition.isBreakable)
        {
            return;
        }

        Destroy(hit.collider.gameObject);
    }

    // 放置当前选择的方块
    private void TryPlaceBlock()
    {
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        // 没有指定要放什么方块时，不执行放置
        if (placeBlock == null)
        {
            return;
        }

        // 根据点击面的法线，获得相邻格子的位置
        Vector3 placePosition = hit.collider.transform.position + hit.normal;

        // 转为整数格子坐标，确保方块不偏移
        Vector3Int gridPosition = new Vector3Int(
            Mathf.RoundToInt(placePosition.x),
            Mathf.RoundToInt(placePosition.y),
            Mathf.RoundToInt(placePosition.z)
        );

        // 新位置已有碰撞体时，不允许重叠放置
        if (Physics.CheckBox(gridPosition, Vector3.one * 0.45f))
        {
            return;
        }

        // 通过 VoxelWorld 创建方块，确保所有方块都有 Block 类型资料
        if (voxelWorld != null)
        {
            voxelWorld.CreateBlock(gridPosition, placeBlock);
        }
    }
}
```

把它挂到 `Player`

1. 把 `Main Camera` 拖到 `Player Camera`字段
2. 把 `World` 拖到 `World Root`字段
4. 把 `Main Camera` 拖到  `Camera Mode Controller `字段
4. 把 `Assets/Blocks/GrassBlock`拖到 `Place Block`字段



## 第一人称的准星

在Hierarchy空白处右键 -> 选择UI(Canvas) -> 选择Canvas

Hierarchy中会变成：

- Canvas
- EventSystem（自动生成的，不能删）



选中Hierarchy中创建的Canvas，Inspector里面找到Canvas组件。检查如下选项，保证为：

- Render Mode选项：选择Screen Space - Overlay【含义：UI画在整个游戏画面的最上层】

Inspector里面找到Canvas Scaler组件。检查如下选项，保证为：

- UI Scale Mode选项：选择Scale With Screen Size【含义：自动按屏幕大小缩放UI】
- Reference Resolution选项：x 1920   y 1080
- Screen Match Mode选项：选择Match Width Or Height
- Match选项：选择0.5



右键Hierarchy中创建的Canvas -> 选择UI(Canvas) -> 选择Image，重命名为Crosshair

最终Hierarchy应该变成：

- Canvas
  - Crosshair
- EventSystem

选中Hierarchy中Canvas下的Crosshair，Inspector里面找到Rect Transform组件：

- Anchor Presets选项【位于左上方的方块】：选择Middle Center
- width、height：100 100



Assets目录下新建Sprites目录，Sprites目录导进去Crosshair.png

<img src="README.assets/Crosshair.png" style="zoom: 25%;" />

选中Crosshair.png，Inspector里面，找到Texture Type选项，选择Sprite (2D and UI)

找到Sprite Mode选项，选择Single，然后点击Apply。



然后引用图片：点击Hierarchy中Canvas下的Crosshair，Inspector里面找到Image组件：

Source Image选项：把Crosshair.png拖进去

Image Type选项：Simple

Image Type选项下的Preserve Aspect：勾选



修改 CameraModeController.cs

```c#
[Header("First Person")]
public Vector3 firstPersonOffset = new Vector3(0f, 0.75f, 0f);

// ===============以下是新增代码=================
[Header("UI")]
public GameObject crosshair;
// ===============以上是新增代码=================

public float mouseSensitivity = 2.5f;
```

```c#

private void ApplyCursorState()
{
    if (IsFirstPerson)
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // ===============以下是新增代码=================
        if (crosshair != null)
        {
            crosshair.SetActive(true);
        }
        // ===============以上是新增代码=================
    }
    else
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // ===============以下是新增代码=================
        if (crosshair != null)
        {
            crosshair.SetActive(false);
        }
        // ===============以上是新增代码=================
    }
}
```

选中Hierarchy中Main Camera，Inspector里面Camera Mode Controller组件会多出来Crosshair，把Hierarchy里面Canvas下的Crosshair拖进去



# 当前需求

## 物品栏/背包/方块掉落

mc同款物品栏，屏幕下方有10个物品栏，前9个是放物品的，第10个是省略号，点击省略号进入背包

mc同款方块掉落效果，方块被破坏时，变小，悬浮在地面上



当前代码右键放置的是草方块。背包系统完成后，只要把当前选中物品对应的 `BlockDefinition` 赋给 `placeBlock`，就能放置泥土、石头、木头或其他任何方块。