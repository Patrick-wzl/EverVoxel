using UnityEngine;

public class CameraModeController : MonoBehaviour
{
    public enum ViewMode
    {
        TopDown,
        FirstPerson
    }

    [Header("References")]
    public Transform target;

    [Header("Switch")]
    public KeyCode switchKey = KeyCode.V;
    public ViewMode currentMode = ViewMode.TopDown;

    [Header("Top Down")]
    public Vector3 topDownOffset = new Vector3(0f, 10f, -10f);
    public float topDownSmoothSpeed = 8f;

    [Header("First Person")]
    public Vector3 firstPersonOffset = new Vector3(0f, 0.75f, 0f);
    public float mouseSensitivity = 2.5f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float yaw;
    private float pitch;

    public bool IsFirstPerson => currentMode == ViewMode.FirstPerson;

    private void Start()
    {
        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }

        ApplyCursorState();
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ToggleMode();
        }

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

    private void ToggleMode()
    {
        currentMode = IsFirstPerson ? ViewMode.TopDown : ViewMode.FirstPerson;

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }

        pitch = 0f;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        if (IsFirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
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
        transform.position = target.position + target.TransformDirection(firstPersonOffset);
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