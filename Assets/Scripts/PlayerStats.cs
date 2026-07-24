using System;
using UnityEngine;

// 人物基础属性系统
// 负责：
// 1. 保存当前生命值和生命上限
// 2. 保存护甲值和魔抗值
// 3. 提供受到伤害、恢复生命、提升生命上限的方法
// 4. 通知UI刷新生命显示
public class PlayerStats : MonoBehaviour
{
    [Header("生命值")]
    // 人物初始生命上限
    // 以后可以通过道具、天赋等调用 IncreaseMaxHealth 提升
    [Min(1f)]
    [SerializeField] private float maxHealth = 100f;

    // 人物当前生命值
    // 初始值为100，代表满血
    [Min(0f)]
    [SerializeField] private float currentHealth = 100f;

    [Header("防御属性")]
    // 当前护甲值
    // 没有装备时为0
    [Min(0f)]
    [SerializeField] private float armor = 0f;

    // 当前魔抗值
    // 没有装备时为0
    [Min(0f)]
    [SerializeField] private float magicResistance = 0f;

    [Header("测试")]
    // 勾选后可以在运行时测试生命值变化
    // 减号键：受到10点物理伤害
    // 等号键：恢复10点生命
    public bool enableDebugKeys;

    // 外部只读当前生命上限
    public float MaxHealth => maxHealth;

    // 外部只读当前生命值
    public float CurrentHealth => currentHealth;

    // 外部只读当前护甲值
    public float Armor => armor;

    // 外部只读当前魔抗值
    public float MagicResistance => magicResistance;

    // 生命值改变事件
    // 参数依次为：当前生命值、生命上限
    // UI会监听这个事件，生命变化时自动刷新
    public event Action<float, float> HealthChanged;

    private void Awake()
    {
        // 防止Inspector里填写的当前生命超过生命上限
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Start()
    {
        // 游戏开始时通知UI显示初始生命
        NotifyHealthChanged();
    }

    private void Update()
    {
        // 仅用于开发测试
        // 正式游戏时不勾选 enableDebugKeys 即可
        if (!enableDebugKeys)
        {
            return;
        }

        // 减号键：受到10点物理伤害
        if (Input.GetKeyDown(KeyCode.Minus))
        {
            TakePhysicalDamage(10f);
        }

        // 等号键：恢复10点生命
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            RestoreHealth(10f);
        }
    }

    // 受到物理伤害
    // 例如近战怪物、箭矢、陷阱等伤害
    public void TakePhysicalDamage(float rawDamage)
    {
        // 伤害不能小于0
        if (rawDamage <= 0f)
        {
            return;
        }

        // 护甲减伤公式：
        // 护甲为0时，受到100%伤害
        // 护甲越高，伤害越低，但不会直接变成无敌
        float finalDamage = CalculateReducedDamage(rawDamage, armor);

        ChangeHealth(-finalDamage);
    }

    // 受到魔法伤害
    // 例如法术、元素攻击、Boss技能等伤害
    public void TakeMagicDamage(float rawDamage)
    {
        // 伤害不能小于0
        if (rawDamage <= 0f)
        {
            return;
        }

        // 魔抗减伤公式与护甲相同
        float finalDamage = CalculateReducedDamage(
            rawDamage,
            magicResistance
        );

        ChangeHealth(-finalDamage);
    }

    // 恢复生命
    // 例如食物、药水、治疗技能等
    public void RestoreHealth(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        ChangeHealth(amount);
    }

    // 提升生命上限
    // futureItemIncreaseHealth：未来道具可以调用此方法
    // increaseCurrentHealthToo 为 true 时，提升上限的同时补充同等生命
    public void IncreaseMaxHealth(
        float amount,
        bool increaseCurrentHealthToo = true)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxHealth += amount;

        if (increaseCurrentHealthToo)
        {
            currentHealth += amount;
        }

        // 防止当前生命超过新的上限
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        NotifyHealthChanged();
    }

    // 设置护甲值
    // 未来装备系统计算完总护甲后调用
    public void SetArmor(float newArmor)
    {
        armor = Mathf.Max(0f, newArmor);
    }

    // 设置魔抗值
    // 未来装备系统计算完总魔抗后调用
    public void SetMagicResistance(float newMagicResistance)
    {
        magicResistance = Mathf.Max(0f, newMagicResistance);
    }

    // 改变生命值
    // amount为正数时回血，为负数时扣血
    private void ChangeHealth(float amount)
    {
        currentHealth = Mathf.Clamp(
            currentHealth + amount,
            0f,
            maxHealth
        );

        NotifyHealthChanged();

        // 当前生命值归零时触发死亡逻辑
        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // 根据防御值计算减伤后的伤害
    private float CalculateReducedDamage(
        float rawDamage,
        float defense)
    {
        // 防御值为0时：
        // finalDamage = rawDamage
        //
        // 防御值越高，伤害越低
        // 例如100点防御时，伤害约减少50%
        return rawDamage * 100f / (100f + defense);
    }

    // 通知所有监听生命值的系统刷新
    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    // 人物死亡
    // 现在只输出提示，之后可扩展重生、死亡界面、掉落物等功能
    private void Die()
    {
        Debug.Log("人物生命值归零，人物死亡。");
    }
}