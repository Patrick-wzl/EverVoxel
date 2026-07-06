using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public float jumpHeight = 4f;
    public float gravity = -20f;

    [Header("View")]
    public CameraModeController cameraModeController;

    private CharacterController controller;
    private Vector3 velocity;
    private bool canJump;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (cameraModeController == null)
        {
            cameraModeController = Camera.main.GetComponent<CameraModeController>();
        }
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

        Vector3 move;

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
        else
        {
            move = new Vector3(horizontal, 0f, vertical);
        }

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