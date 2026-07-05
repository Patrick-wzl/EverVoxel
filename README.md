# 游戏背景
这款游戏参考如下两个游戏：
- 我的世界
- 我的世界地下城

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

```
☑ Microsoft Visual Studio Community

☑ Windows Build Support (IL2CPP)

☑ Android Build Support
    ☑ Android SDK & NDK Tools
    ☑ OpenJDK
```

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



# 路线

```
第一阶段（地基）
    Unity基础
    ↓
    玩家移动
    ↓
    摄像机
    ↓
    方块世界
    ↓
    放置/破坏方块

第二阶段（MC）
    Chunk
    无限地图
    Perlin Noise
    树
    光照
    保存地图

第三阶段（地下城）
    怪物
    AI
    掉落
    装备
    技能
    血条

第四阶段（大型）
    蓝图系统
    地牢
    Boss
    联机
```



# 已实现功能

角色在一片方块地形，wsad上下左右移动，空格跳跃

## 地形

Assets/Materials 创建 3 个材质：右键 -> Create > Material

```
Grass.mat   绿色
Dirt.mat   棕色
Stone.mat   灰色
```

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
```

在 Hierarchy 里右键 -> Create Empty 命名为 World

把 `VoxelWorld.cs` 拖到 `World` 上

把 3 个材质拖到脚本对应位置

```
Grass Material
Dirt Material
Stone Material
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

在Assets/Scripts 创建 PlayerController.cs

```c#
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpHeight = 4f;
    public float gravity = -20f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool canJump;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (controller.isGrounded)
        {
            canJump = true;
            velocity.y = -2f;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(horizontal, 0f, vertical);
        move = Vector3.ClampMagnitude(move, 1f);

        if (canJump && Input.GetKeyDown(KeyCode.Space))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            canJump = false;
        }

        velocity.y += gravity * Time.deltaTime;

        Vector3 finalMove = move * moveSpeed;
        finalMove.y = velocity.y;

        controller.Move(finalMove * Time.deltaTime);
    }
}
```

把 `PlayerController.cs` 拖到 `Player` 上



## 摄像机

选中场景里的 `Main Camera`。设置初始位置：

```
Position X: 16
Position Y: 18
Position Z: 6

Rotation X: 60
Rotation Y: 0
Rotation Z: 0
```

Assets/Scripts 创建 CameraFollow.cs

```c#
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 10f, -10f);
    public float smoothSpeed = 8f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.LookAt(target.position + Vector3.up);
    }
}
```

把 `CameraFollow.cs` 拖到 `Main Camera` 上。

把 Hierarchy 里的 `Player` 拖到 `CameraFollow` 的 `Target` 字段
