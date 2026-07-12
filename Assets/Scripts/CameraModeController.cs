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