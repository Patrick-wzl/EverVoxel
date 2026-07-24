using UnityEngine;
using UnityEngine.UI;

// 人物心形生命UI
// 负责：
// 1. 自动创建左上角纯心形生命UI
// 2. 自动生成原创心形精灵，不需要额外图片
// 3. 根据当前生命值显示红色填充比例
// 4. 不显示数字、文字、背景板和心形边框
public class PlayerHealthUI : MonoBehaviour
{
    [Header("References")]
    // 玩家的人物属性脚本
    public PlayerStats playerStats;

    [Header("UI设置")]
    // 心形容器在屏幕上的大小
    public float heartSize = 118f;

    // 心形UI距离屏幕左上角的距离
    public Vector2 screenOffset = new Vector2(34f, -34f);

    // 未填满生命值时显示的暗红色
    // 它不是边框，只用于表示生命上限容器
    public Color emptyHealthColor = new Color(
        0.22f,
        0.035f,
        0.045f,
        0.8f
    );

    // 当前生命值显示的红色
    public Color healthColor = new Color(
        0.95f,
        0.05f,
        0.08f,
        1f
    );

    // 红色生命填充图片
    private Image healthFillImage;

    // 自动生成的完整心形精灵
    private Sprite heartSprite;

    private void Start()
    {
        // 如果没有在Inspector手动拖入玩家属性脚本，则自动查找
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        // 生成完整心形精灵
        // 不再生成任何边框精灵
        heartSprite = CreateHeartSprite();

        // 创建生命UI
        CreateHealthUI();

        // 监听生命值变化
        if (playerStats != null)
        {
            playerStats.HealthChanged += RefreshHealthUI;

            // 初始刷新
            RefreshHealthUI(
                playerStats.CurrentHealth,
                playerStats.MaxHealth
            );
        }
    }

    private void OnDestroy()
    {
        // 移除事件监听，避免对象销毁后仍然收到通知
        if (playerStats != null)
        {
            playerStats.HealthChanged -= RefreshHealthUI;
        }
    }

    // 创建完整的生命UI
    private void CreateHealthUI()
    {
        // 创建根节点
        GameObject root = CreateUIObject("HeartHealthUI", transform);

        RectTransform rootRect = root.GetComponent<RectTransform>();

        // 固定在屏幕左上角
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = screenOffset;
        rootRect.sizeDelta = new Vector2(heartSize, heartSize);

        // 创建完整的暗红色心形
        // 用来表示人物的生命上限
        Image emptyHeartImage = CreateHeartImage(
            "EmptyHeart",
            root.transform,
            emptyHealthColor
        );

        Stretch(emptyHeartImage.rectTransform);

        // 创建红色生命填充
        // 红色会从下往上填充，不再有任何边框
        healthFillImage = CreateHeartImage(
            "HealthFill",
            root.transform,
            healthColor
        );

        Stretch(healthFillImage.rectTransform);

        // 设置为从下往上填充
        healthFillImage.type = Image.Type.Filled;
        healthFillImage.fillMethod = Image.FillMethod.Vertical;
        healthFillImage.fillOrigin = 0;
        healthFillImage.fillAmount = 1f;
    }

    // 刷新生命显示
    private void RefreshHealthUI(
        float currentHealth,
        float maxHealth)
    {
        if (healthFillImage == null)
        {
            return;
        }

        // 计算生命百分比
        float healthPercent = maxHealth > 0f
            ? currentHealth / maxHealth
            : 0f;

        // 设置红色填充比例
        healthFillImage.fillAmount = Mathf.Clamp01(healthPercent);
    }

    // 创建一个心形Image
    private Image CreateHeartImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject imageObject = CreateUIObject(objectName, parent);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = heartSprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = false;

        return image;
    }

    // 创建UI对象
    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(
            objectName,
            typeof(RectTransform)
        );

        uiObject.transform.SetParent(parent, false);

        return uiObject;
    }

    // 让UI填满父物体
    private void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }

    // 创建原创心形精灵
    // 只有完整心形，不生成边框
    private Sprite CreateHeartSprite()
    {
        const int textureSize = 128;

        Texture2D texture = new Texture2D(
            textureSize,
            textureSize,
            TextureFormat.RGBA32,
            false
        );

        // 使用双线性过滤，让心形边缘更平滑
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32[] pixels = new Color32[textureSize * textureSize];

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // 将像素坐标转换为心形数学公式使用的坐标
                float normalizedX =
                    x / (float)(textureSize - 1) * 2.4f - 1.2f;

                float normalizedY =
                    y / (float)(textureSize - 1) * 2.5f - 1.25f;

                // 判断像素是否在完整心形内
                bool isInsideHeart = IsInsideHeart(
                    normalizedX,
                    normalizedY
                );

                int index = y * textureSize + x;

                // 白色区域会被Image组件的颜色染色
                // 透明区域不会显示
                pixels[index] = isInsideHeart
                    ? new Color32(255, 255, 255, 255)
                    : new Color32(255, 255, 255, 0);
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, textureSize, textureSize),
            new Vector2(0.5f, 0.5f),
            textureSize
        );
    }

    // 心形数学公式
    // 返回true代表指定坐标位于心形区域内
    private bool IsInsideHeart(float x, float y)
    {
        float value =
            Mathf.Pow(x * x + y * y - 1f, 3f) -
            x * x * y * y * y;

        return value <= 0f;
    }
}