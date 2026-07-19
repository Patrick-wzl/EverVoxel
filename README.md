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
- 背包/装备栏/物品掉落：已实现
- 方块坚硬度：已实现
- 人物血条：
- 第三人称视角：待讨论，待整改
- 确定画风
- 简单美术
- 功能晚上

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

按数字键或点击物品栏切换物品，按B或点击...打开背包

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
    // 材质
    public Material material;
    // 图标
    public Sprite itemIcon;

    [Header("Gameplay")]
    // 是否有实体碰撞
    public bool isSolid = true;

    // 是否允许被破坏
    public bool isBreakable = true;

    // 硬度
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

        // 是否启用碰撞体
        Collider blockCollider = GetComponent<Collider>();

        if (blockCollider != null)
        {
            blockCollider.enabled = definition.isSolid;
        }

        // 场景中显示的物体名称更清楚【例如：GrassBlock (草方块)】
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

    // 创建方块掉落物
    public GameObject SpawnBlockDrop(Vector3 worldPosition, BlockDefinition blockDefinition)
    {
        if (blockDefinition == null)
        {
            return null;
        }

        GameObject dropObject = GameObject.CreatePrimitive(PrimitiveType.Cube);

        // 让掉落物从被破坏方块稍微上方出现。
        // X/Z 有轻微随机偏移，多个方块掉落时不会完全重叠。
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.18f, 0.18f),
            0.65f,
            Random.Range(-0.18f, 0.18f)
        );

        dropObject.transform.position = worldPosition + randomOffset;
        dropObject.transform.parent = transform;

        // 掉落物不添加 Block 组件。因此玩家不能把掉落物再次当成世界方块挖掉。
        BlockDrop blockDrop = dropObject.AddComponent<BlockDrop>();
        blockDrop.Initialize(blockDefinition);

        return dropObject;
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

    [Header("UI")]
    public GameObject crosshair;
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

            if (crosshair != null)
            {
                crosshair.SetActive(true);
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (crosshair != null)
            {
                crosshair.SetActive(false);
            }
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
3. 把 Hierarchy 里`Canvas`下的`Crosshair`拖到`Crosshair`字段



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
    // 放置的方块
    public BlockDefinition placeBlock;
    // 放置的范围
    public float interactRange = 5f;

    [Header("Breaking")]
    [Tooltip("Hardness 为 1 的方块，挖掘需要的基础秒数")]
    public float baseBreakTime = 0.75f;

    private VoxelWorld voxelWorld;

    // 当前正在被玩家按住左键挖掘的方块
    private Block breakingBlock;

    // 已经挖掘了多久
    private float currentBreakTime;

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

        // 从 World 物体获取 VoxelWorld，用它统一创建方块和掉落物
        if (worldRoot != null)
        {
            voxelWorld = worldRoot.GetComponent<VoxelWorld>();
        }
    }

    private void Update()
    {
        UpdateBreakingInput();

        // 鼠标右键放置方块
        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    private void UpdateBreakingInput()
    {
        // 左键刚按下：开始尝试挖掘
        if (Input.GetMouseButtonDown(0))
        {
            BeginBreakingBlock();
        }

        // 左键持续按住：累计挖掘时间
        if (Input.GetMouseButton(0))
        {
            ContinueBreakingBlock();
        }

        // 松开左键：取消本次挖掘
        if (Input.GetMouseButtonUp(0))
        {
            CancelBreakingBlock();
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

        // 只允许操作 World 下的普通方块
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

    // 左键按下时，记录最开始挖掘的方块
    private void BeginBreakingBlock()
    {
        CancelBreakingBlock();

        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null || targetBlock.Definition == null)
        {
            return;
        }

        // 不可破坏方块，例如未来的基岩，不能开始挖掘
        if (!targetBlock.Definition.isBreakable)
        {
            return;
        }

        breakingBlock = targetBlock;
        currentBreakTime = 0f;
    }

    // 左键持续按住时执行
    private void ContinueBreakingBlock()
    {
        if (breakingBlock == null)
        {
            return;
        }

        // 玩家必须始终看着同一块方块。
        // 视线移开、距离过远、改挖别的方块，都会取消本次挖掘。
        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            CancelBreakingBlock();
            return;
        }

        Block currentTarget = hit.collider.GetComponent<Block>();

        if (currentTarget != breakingBlock)
        {
            CancelBreakingBlock();
            return;
        }

        BlockDefinition definition = breakingBlock.Definition;

        if (definition == null || !definition.isBreakable)
        {
            CancelBreakingBlock();
            return;
        }

        // Hardness 越大，所需挖掘时间越长。
        // Mathf.Max 防止有人把硬度设置成 0 或负数。
        float requiredBreakTime = baseBreakTime * Mathf.Max(0.05f, definition.hardness);

        currentBreakTime += Time.deltaTime;

        if (currentBreakTime >= requiredBreakTime)
        {
            BreakCurrentBlock();
        }
    }

    // 真正摧毁方块，并生成对应类型的掉落物
    private void BreakCurrentBlock()
    {
        if (breakingBlock == null)
        {
            return;
        }

        BlockDefinition definition = breakingBlock.Definition;
        Vector3 blockPosition = breakingBlock.transform.position;

        // 先生成掉落物，再销毁原方块。
        // 掉落物会从原方块附近出现，并自动落到下方方块上。
        if (voxelWorld != null && definition != null)
        {
            voxelWorld.SpawnBlockDrop(blockPosition, definition);
        }

        Destroy(breakingBlock.gameObject);

        CancelBreakingBlock();
    }

    // 松开鼠标、视线移开或目标变化时，重置挖掘进度
    private void CancelBreakingBlock()
    {
        breakingBlock = null;
        currentBreakTime = 0f;
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

        // 通过 VoxelWorld 创建正常方块
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



## 方块掉落

在 `Assets/Scripts` 新建 `BlockDrop.cs`

```c#
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
```



## 物品栏/背包

### 1、在 `Assets/Scripts` 新建 `PlayerInventory.cs`

```c#
using System;
using UnityEngine;

[Serializable]
public class InventorySlotData
{
    public BlockDefinition block;

    [Min(0)]
    public int amount;

    public bool IsEmpty => block == null || amount <= 0;

    public void Clear()
    {
        block = null;
        amount = 0;
    }
}

[RequireComponent(typeof(BlockInteraction))]
public class PlayerInventory : MonoBehaviour
{
    public const int HotbarSlotCount = 9;
    public const int BackpackSlotCount = 27;
    public const int MaxStackSize = 999;

    [Header("References")]
    public BlockInteraction blockInteraction;

    [Header("快捷栏：屏幕下方前 9 格")]
    public InventorySlotData[] hotbar = new InventorySlotData[HotbarSlotCount];

    [Header("背包：27 格")]
    public InventorySlotData[] backpack = new InventorySlotData[BackpackSlotCount];

    [Range(0, HotbarSlotCount - 1)]
    public int selectedHotbarIndex;

    public int SelectedHotbarIndex => selectedHotbarIndex;

    public event Action InventoryChanged;

    private void Awake()
    {
        if (blockInteraction == null)
        {
            blockInteraction = GetComponent<BlockInteraction>();
        }

        EnsureSlots();
    }

    private void Start()
    {
        SelectHotbarSlot(selectedHotbarIndex);
        NotifyChanged();
    }

    private void Update()
    {
        if (InventoryUI.IsAnyInventoryOpen)
        {
            return;
        }

        for (int i = 0; i < HotbarSlotCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectHotbarSlot(i);
            }
        }
    }

    private void EnsureSlots()
    {
        if (hotbar == null || hotbar.Length != HotbarSlotCount)
        {
            hotbar = new InventorySlotData[HotbarSlotCount];
        }

        if (backpack == null || backpack.Length != BackpackSlotCount)
        {
            backpack = new InventorySlotData[BackpackSlotCount];
        }

        for (int i = 0; i < hotbar.Length; i++)
        {
            if (hotbar[i] == null)
            {
                hotbar[i] = new InventorySlotData();
            }
        }

        for (int i = 0; i < backpack.Length; i++)
        {
            if (backpack[i] == null)
            {
                backpack[i] = new InventorySlotData();
            }
        }
    }

    public InventorySlotData GetHotbarSlot(int index)
    {
        return index >= 0 && index < hotbar.Length ? hotbar[index] : null;
    }

    public InventorySlotData GetBackpackSlot(int index)
    {
        return index >= 0 && index < backpack.Length ? backpack[index] : null;
    }

    public bool HasSelectedItem()
    {
        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);
        return slot != null && !slot.IsEmpty;
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= HotbarSlotCount)
        {
            return;
        }

        selectedHotbarIndex = index;
        ApplySelectedItemToPlacement();
        NotifyChanged();
    }

    public bool ConsumeSelectedItem()
    {
        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);

        if (slot == null || slot.IsEmpty)
        {
            return false;
        }

        slot.amount--;

        if (slot.amount <= 0)
        {
            slot.Clear();
        }

        ApplySelectedItemToPlacement();
        NotifyChanged();
        return true;
    }

    // 掉落物拾取时调用。
    // 同类方块优先填满已有格子；每格最多 999；之后自动寻找下一格。
    public bool TryAddItem(BlockDefinition blockDefinition, int amount)
    {
        if (blockDefinition == null || amount <= 0)
        {
            return false;
        }

        int remaining = amount;

        remaining = AddToExistingSlots(hotbar, blockDefinition, remaining);
        remaining = AddToExistingSlots(backpack, blockDefinition, remaining);

        remaining = AddToEmptySlots(hotbar, blockDefinition, remaining);
        remaining = AddToEmptySlots(backpack, blockDefinition, remaining);

        if (remaining != amount)
        {
            ApplySelectedItemToPlacement();
            NotifyChanged();
        }

        // true 代表全部加入背包；false 代表背包已经完全满了
        return remaining == 0;
    }

    private int AddToExistingSlots(
        InventorySlotData[] slots,
        BlockDefinition blockDefinition,
        int remaining)
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (slot.IsEmpty || slot.block != blockDefinition)
            {
                continue;
            }

            int canAdd = MaxStackSize - slot.amount;

            if (canAdd <= 0)
            {
                continue;
            }

            int addAmount = Mathf.Min(canAdd, remaining);
            slot.amount += addAmount;
            remaining -= addAmount;
        }

        return remaining;
    }

    private int AddToEmptySlots(
        InventorySlotData[] slots,
        BlockDefinition blockDefinition,
        int remaining)
    {
        foreach (InventorySlotData slot in slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (!slot.IsEmpty)
            {
                continue;
            }

            int addAmount = Mathf.Min(MaxStackSize, remaining);
            slot.block = blockDefinition;
            slot.amount = addAmount;
            remaining -= addAmount;
        }

        return remaining;
    }

    public void SwapSelectedHotbarWithBackpack(int backpackIndex)
    {
        InventorySlotData hotbarSlot = GetHotbarSlot(selectedHotbarIndex);
        InventorySlotData backpackSlot = GetBackpackSlot(backpackIndex);

        if (hotbarSlot == null || backpackSlot == null)
        {
            return;
        }

        BlockDefinition temporaryBlock = hotbarSlot.block;
        int temporaryAmount = hotbarSlot.amount;

        hotbarSlot.block = backpackSlot.block;
        hotbarSlot.amount = backpackSlot.amount;

        backpackSlot.block = temporaryBlock;
        backpackSlot.amount = temporaryAmount;

        ApplySelectedItemToPlacement();
        NotifyChanged();
    }

    private void ApplySelectedItemToPlacement()
    {
        if (blockInteraction == null)
        {
            return;
        }

        InventorySlotData slot = GetHotbarSlot(selectedHotbarIndex);

        blockInteraction.placeBlock =
            slot != null && !slot.IsEmpty
            ? slot.block
            : null;
    }

    private void NotifyChanged()
    {
        InventoryChanged?.Invoke();
    }
}
```

### 2、替换 `BlockDrop.cs`

```c#
using UnityEngine;

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

    [Header("Pickup")]
    public float pickupRange = 1.5f;
    public float pickupDelay = 0.35f;

    private bool hasLanded;
    private Vector3 restingPosition;
    private float floatingOffset;
    private float aliveTime;
    private PlayerInventory playerInventory;

    public BlockDefinition Definition => definition;

    public void Initialize(BlockDefinition blockDefinition)
    {
        definition = blockDefinition;

        transform.localScale = Vector3.one * dropScale;
        transform.rotation = Random.rotation;
        floatingOffset = Random.Range(0f, Mathf.PI * 2f);

        Renderer dropRenderer = GetComponent<Renderer>();

        if (dropRenderer != null &&
            definition != null &&
            definition.material != null)
        {
            dropRenderer.sharedMaterial = definition.material;
        }

        // 掉落物不挡住人物，也不会妨碍放置方块。
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
        playerInventory = FindFirstObjectByType<PlayerInventory>();
    }

    private void Update()
    {
        aliveTime += Time.deltaTime;

        if (!hasLanded)
        {
            FallToGround();
            return;
        }

        FloatAndRotate();
        TryPickup();
    }

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

        // 此掉落物代表一个方块。
        // 拾取失败说明全部 36 格背包都无法再放入该物品。
        if (playerInventory.TryAddItem(definition, 1))
        {
            Destroy(gameObject);
        }
    }
}
```

### 3、替换`BlockInteraction.cs`

```c#
using UnityEngine;
using UnityEngine.EventSystems;

public class BlockInteraction : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform worldRoot;
    public CameraModeController cameraModeController;

    [Header("Placement")]
    public BlockDefinition placeBlock;
    public float interactRange = 5f;

    [Header("Breaking")]
    public float baseBreakTime = 0.75f;

    private VoxelWorld voxelWorld;
    private PlayerInventory inventory;
    private Block breakingBlock;
    private float currentBreakTime;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (cameraModeController == null && playerCamera != null)
        {
            cameraModeController =
                playerCamera.GetComponent<CameraModeController>();
        }

        if (worldRoot != null)
        {
            voxelWorld = worldRoot.GetComponent<VoxelWorld>();
        }

        inventory = GetComponent<PlayerInventory>();
    }

    private void Update()
    {
        if (InventoryUI.IsAnyInventoryOpen)
        {
            CancelBreakingBlock();
            return;
        }

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
        {
            CancelBreakingBlock();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            BeginBreakingBlock();
        }

        if (Input.GetMouseButton(0))
        {
            ContinueBreakingBlock();
        }

        if (Input.GetMouseButtonUp(0))
        {
            CancelBreakingBlock();
        }

        if (Input.GetMouseButtonDown(1))
        {
            TryPlaceBlock();
        }
    }

    private bool TryGetTargetBlock(out RaycastHit hit)
    {
        hit = default;

        Ray ray;

        if (cameraModeController != null &&
            cameraModeController.IsFirstPerson)
        {
            ray = playerCamera.ScreenPointToRay(
                new Vector3(
                    Screen.width * 0.5f,
                    Screen.height * 0.5f,
                    0f
                )
            );
        }
        else
        {
            ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        }

        if (!Physics.Raycast(ray, out hit, 100f))
        {
            return false;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null)
        {
            return false;
        }

        if (worldRoot != null &&
            hit.collider.transform.parent != worldRoot)
        {
            return false;
        }

        return Vector3.Distance(
            transform.position,
            hit.collider.transform.position
        ) <= interactRange;
    }

    private void BeginBreakingBlock()
    {
        CancelBreakingBlock();

        if (!TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Block targetBlock = hit.collider.GetComponent<Block>();

        if (targetBlock == null ||
            targetBlock.Definition == null ||
            !targetBlock.Definition.isBreakable)
        {
            return;
        }

        breakingBlock = targetBlock;
    }

    private void ContinueBreakingBlock()
    {
        if (breakingBlock == null)
        {
            return;
        }

        if (!TryGetTargetBlock(out RaycastHit hit) ||
            hit.collider.GetComponent<Block>() != breakingBlock)
        {
            CancelBreakingBlock();
            return;
        }

        BlockDefinition definition = breakingBlock.Definition;

        if (definition == null || !definition.isBreakable)
        {
            CancelBreakingBlock();
            return;
        }

        float breakTime =
            baseBreakTime * Mathf.Max(0.05f, definition.hardness);

        currentBreakTime += Time.deltaTime;

        if (currentBreakTime >= breakTime)
        {
            Vector3 position = breakingBlock.transform.position;

            if (voxelWorld != null)
            {
                voxelWorld.SpawnBlockDrop(position, definition);
            }

            Destroy(breakingBlock.gameObject);
            CancelBreakingBlock();
        }
    }

    private void CancelBreakingBlock()
    {
        breakingBlock = null;
        currentBreakTime = 0f;
    }

    private void TryPlaceBlock()
    {
        if (inventory != null && !inventory.HasSelectedItem())
        {
            return;
        }

        if (placeBlock == null || !TryGetTargetBlock(out RaycastHit hit))
        {
            return;
        }

        Vector3 placePosition = hit.collider.transform.position + hit.normal;

        Vector3Int gridPosition = new Vector3Int(
            Mathf.RoundToInt(placePosition.x),
            Mathf.RoundToInt(placePosition.y),
            Mathf.RoundToInt(placePosition.z)
        );

        if (Physics.CheckBox(gridPosition, Vector3.one * 0.45f))
        {
            return;
        }

        if (voxelWorld == null)
        {
            return;
        }

        GameObject createdBlock =
            voxelWorld.CreateBlock(gridPosition, placeBlock);

        if (createdBlock != null && inventory != null)
        {
            inventory.ConsumeSelectedItem();
        }
    }
}
```

### 4、在 `Assets/Scripts` 新建 `InventoryUI.cs`

```c#
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private class SlotView
    {
        public Image icon;
        public Text amount;
        public GameObject selected;
        public bool isHotbar;
        public int index;
    }

    public static bool IsAnyInventoryOpen { get; private set; }

    [Header("References")]
    public PlayerInventory playerInventory;
    public CameraModeController cameraModeController;

    [Header("Keys")]
    public KeyCode toggleKey = KeyCode.B;

    private readonly List<SlotView> hotbarSlots = new();
    private readonly List<SlotView> backpackSlots = new();

    private GameObject inventoryPanel;
    private Font defaultFont;

    private void Start()
    {
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<PlayerInventory>();
        }

        if (cameraModeController == null && Camera.main != null)
        {
            cameraModeController =
                Camera.main.GetComponent<CameraModeController>();
        }

        defaultFont = Resources.GetBuiltinResource<Font>(
            "LegacyRuntime.ttf"
        );

        CreateHotbar();
        CreateBackpack();

        if (playerInventory != null)
        {
            playerInventory.InventoryChanged += RefreshAll;
        }

        SetInventoryOpen(false);
        RefreshAll();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetInventoryOpen(!IsAnyInventoryOpen);
        }

        if (IsAnyInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetInventoryOpen(false);
        }
    }

    private void OnDestroy()
    {
        if (playerInventory != null)
        {
            playerInventory.InventoryChanged -= RefreshAll;
        }

        Time.timeScale = 1f;
        IsAnyInventoryOpen = false;
    }

    private void CreateHotbar()
    {
        GameObject root = CreateUIObject("Hotbar", transform);
        RectTransform rect = root.GetComponent<RectTransform>();

        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 45f);
        rect.sizeDelta = new Vector2(860f, 88f);

        Image background = root.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.65f);

        HorizontalLayoutGroup layout =
            root.AddComponent<HorizontalLayoutGroup>();

        layout.padding = new RectOffset(6, 6, 6, 6);
        layout.spacing = 5f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < PlayerInventory.HotbarSlotCount; i++)
        {
            int slotIndex = i;
            SlotView view = CreateSlot(root.transform, true, slotIndex);

            view.selected.GetComponent<Image>().color =
                new Color(1f, 0.8f, 0f, 0.35f);

            root.transform.GetChild(i).GetComponent<Button>().onClick
                .AddListener(() => playerInventory.SelectHotbarSlot(slotIndex));

            hotbarSlots.Add(view);
        }

        GameObject moreButton = CreateButton(root.transform, "…");
        moreButton.GetComponent<Button>().onClick.AddListener(
            () => SetInventoryOpen(true)
        );
    }

    private void CreateBackpack()
    {
        inventoryPanel = CreateUIObject("InventoryPanel", transform);

        RectTransform panelRect =
            inventoryPanel.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(820f, 470f);

        Image panelImage = inventoryPanel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.08f, 0.08f, 0.95f);

        GameObject title = CreateTextObject(
            "Title",
            inventoryPanel.transform,
            "背包"
        );

        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -18f);
        titleRect.sizeDelta = new Vector2(0f, 40f);

        title.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
        title.GetComponent<Text>().fontSize = 28;

        GameObject closeButton = CreateButton(
            inventoryPanel.transform,
            "X"
        );

        RectTransform closeRect =
            closeButton.GetComponent<RectTransform>();

        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-15f, -15f);

        closeButton.GetComponent<Button>().onClick.AddListener(
            () => SetInventoryOpen(false)
        );

        GameObject grid = CreateUIObject(
            "BackpackSlots",
            inventoryPanel.transform
        );

        RectTransform gridRect = grid.GetComponent<RectTransform>();
        gridRect.anchorMin = new Vector2(0.5f, 0.5f);
        gridRect.anchorMax = new Vector2(0.5f, 0.5f);
        gridRect.pivot = new Vector2(0.5f, 0.5f);
        gridRect.sizeDelta = new Vector2(744f, 300f);
        gridRect.anchoredPosition = new Vector2(0f, -25f);

        GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(76f, 76f);
        gridLayout.spacing = new Vector2(6f, 6f);
        gridLayout.constraint =
            GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 9;
        gridLayout.childAlignment = TextAnchor.MiddleCenter;

        for (int i = 0; i < PlayerInventory.BackpackSlotCount; i++)
        {
            int slotIndex = i;
            SlotView view = CreateSlot(grid.transform, false, slotIndex);

            grid.transform.GetChild(i).GetComponent<Button>().onClick
                .AddListener(() =>
                    playerInventory.SwapSelectedHotbarWithBackpack(slotIndex)
                );

            backpackSlots.Add(view);
        }
    }

    private SlotView CreateSlot(
        Transform parent,
        bool isHotbar,
        int index)
    {
        GameObject slot = CreateButton(parent, "");

        Image background = slot.GetComponent<Image>();
        background.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        GameObject iconObject = CreateUIObject("Icon", slot.transform);
        Image icon = iconObject.AddComponent<Image>();
        icon.raycastTarget = false;
        Stretch(iconObject.GetComponent<RectTransform>(), 8f);

        GameObject amountObject = CreateTextObject(
            "Amount",
            slot.transform,
            ""
        );

        RectTransform amountRect =
            amountObject.GetComponent<RectTransform>();

        amountRect.anchorMin = Vector2.zero;
        amountRect.anchorMax = new Vector2(1f, 0f);
        amountRect.pivot = new Vector2(1f, 0f);
        amountRect.anchoredPosition = new Vector2(-5f, 4f);
        amountRect.sizeDelta = new Vector2(-10f, 28f);

        Text amountText = amountObject.GetComponent<Text>();
        amountText.alignment = TextAnchor.LowerRight;
        amountText.fontSize = 20;
        amountText.color = Color.white;
        amountText.raycastTarget = false;

        GameObject selected = CreateUIObject("Selected", slot.transform);
        Image selectedImage = selected.AddComponent<Image>();
        selectedImage.raycastTarget = false;
        Stretch(selected.GetComponent<RectTransform>(), 0f);

        return new SlotView
        {
            icon = icon,
            amount = amountText,
            selected = selected,
            isHotbar = isHotbar,
            index = index
        };
    }

    private GameObject CreateButton(Transform parent, string text)
    {
        GameObject buttonObject = CreateUIObject("Button", parent);

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 76f;
        layout.preferredHeight = 76f;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        Button button = buttonObject.AddComponent<Button>();

        if (!string.IsNullOrEmpty(text))
        {
            GameObject textObject = CreateTextObject(
                "Text",
                buttonObject.transform,
                text
            );

            Stretch(textObject.GetComponent<RectTransform>(), 0f);

            Text uiText = textObject.GetComponent<Text>();
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.fontSize = 36;
            uiText.raycastTarget = false;
        }

        return buttonObject;
    }

    private GameObject CreateTextObject(
        string name,
        Transform parent,
        string content)
    {
        GameObject textObject = CreateUIObject(name, parent);
        Text text = textObject.AddComponent<Text>();

        text.font = defaultFont;
        text.text = content;
        text.color = Color.white;

        return textObject;
    }

    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject uiObject = new GameObject(
            name,
            typeof(RectTransform)
        );

        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }

    private void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private void RefreshAll()
    {
        RefreshList(hotbarSlots);
        RefreshList(backpackSlots);
    }

    private void RefreshList(List<SlotView> slots)
    {
        if (playerInventory == null)
        {
            return;
        }

        foreach (SlotView view in slots)
        {
            InventorySlotData data = view.isHotbar
                ? playerInventory.GetHotbarSlot(view.index)
                : playerInventory.GetBackpackSlot(view.index);

            bool hasItem = data != null && !data.IsEmpty;

            view.icon.enabled =
                hasItem &&
                data.block != null &&
                data.block.itemIcon != null;

            if (view.icon.enabled)
            {
                view.icon.sprite = data.block.itemIcon;
            }

            view.amount.text = hasItem && data.amount > 1
                ? data.amount.ToString()
                : "";

            view.selected.SetActive(
                view.isHotbar &&
                playerInventory.SelectedHotbarIndex == view.index
            );
        }
    }

    private void SetInventoryOpen(bool isOpen)
    {
        IsAnyInventoryOpen = isOpen;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
        }

        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        bool firstPerson =
            cameraModeController != null &&
            cameraModeController.IsFirstPerson;

        Cursor.lockState = firstPerson
            ? CursorLockMode.Locked
            : CursorLockMode.None;

        Cursor.visible = !firstPerson;
    }
}
```

### 5、在 Unity 中挂载组件

给 Hierarchy 中的 Player 添加组件：

```
Add Component > PlayerInventory
```

在`Player Inventory`组件中：把 Hierarchy 里的 `Player` 拖入 `Block Interaction`字段 



给 Hierarchy 中的 Canvas 添加组件：

```
Add Component > InventoryUI
```

在`Inventory UI`组件中，把 Hierarchy 中的 `Player`拖入`Player Inventory`字段

在`Inventory UI`组件中，把 Hierarchy 中的 `Main Camera`拖入`Camera Mode Controller` 字段



# 当前需求

