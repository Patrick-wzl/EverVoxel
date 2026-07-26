using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Block))]
public class GrassGrowth : MonoBehaviour
{
    [Header("Growth Time")]
    // 泥土上方保持为空多久后变绿
    public float growTime = 5f;

    // 最短检查间隔
    public float minCheckInterval = 0.8f;

    // 最长检查间隔
    public float maxCheckInterval = 1.2f;

    private VoxelWorld voxelWorld;
    private Block block;
    private BlockDefinition dirtBlock;
    private BlockDefinition grassBlock;

    // 泥土上方已经保持为空的时间
    private float uncoveredTime;

    // 当前正在运行的检查协程
    private Coroutine checkCoroutine;

    // 初始化泥土长草系统
    public void Initialize(
        VoxelWorld world,
        BlockDefinition dirtDefinition,
        BlockDefinition grassDefinition)
    {
        voxelWorld = world;
        dirtBlock = dirtDefinition;
        grassBlock = grassDefinition;

        block = GetComponent<Block>();

        // 防止同一个方块重复启动检查协程
        if (checkCoroutine != null)
        {
            StopCoroutine(checkCoroutine);
        }

        checkCoroutine =
            StartCoroutine(CheckGrassState());
    }

    // 定期检查泥土上方是否存在方块
    private IEnumerator CheckGrassState()
    {
        while (true)
        {
            // 每次使用略有差异的随机间隔
            // 防止大量泥土在同一帧同时检查
            float checkInterval = Random.Range(
                minCheckInterval,
                maxCheckInterval
            );

            yield return new WaitForSeconds(
                checkInterval
            );

            if (voxelWorld == null ||
                block == null ||
                dirtBlock == null ||
                grassBlock == null)
            {
                continue;
            }

            // 获取当前方块的整数坐标
            Vector3Int currentPosition =
                new Vector3Int(
                    Mathf.RoundToInt(
                        transform.position.x
                    ),
                    Mathf.RoundToInt(
                        transform.position.y
                    ),
                    Mathf.RoundToInt(
                        transform.position.z
                    )
                );

            // 当前方块正上方一格
            Vector3Int abovePosition =
                currentPosition + Vector3Int.up;

            // 检查正上方是否存在方块
            bool hasBlockAbove =
                voxelWorld.TryGetBlockAt(
                    abovePosition,
                    out Block blockAbove
                );

            // 当前是普通泥土
            if (block.Definition == dirtBlock)
            {
                UpdateDirtState(
                    hasBlockAbove,
                    checkInterval
                );
            }
            // 当前是绿色泥土
            else if (block.Definition == grassBlock)
            {
                UpdateGrassState(hasBlockAbove);
            }
        }
    }

    // 更新普通泥土的长草状态
    private void UpdateDirtState(
        bool hasBlockAbove,
        float checkInterval)
    {
        // 泥土上方存在方块
        // 清空已经累计的长草时间
        if (hasBlockAbove)
        {
            uncoveredTime = 0f;
            return;
        }

        // 泥土上方为空
        // 累计暴露在空气中的时间
        uncoveredTime += checkInterval;

        // 尚未达到长草时间
        if (uncoveredTime < growTime)
        {
            return;
        }

        // 泥土变成绿色状态
        block.Initialize(grassBlock);
        uncoveredTime = 0f;
    }

    // 更新绿色泥土的状态
    private void UpdateGrassState(
        bool hasBlockAbove)
    {
        // 上方仍然为空时保持绿色
        if (!hasBlockAbove)
        {
            return;
        }

        // 上方出现方块时
        // 绿色泥土变回普通泥土
        block.Initialize(dirtBlock);
        uncoveredTime = 0f;
    }
}