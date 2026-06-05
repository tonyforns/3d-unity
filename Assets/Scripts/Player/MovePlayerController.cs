using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovePlayerController : MonoBehaviour
{
    private const string PlayerActionMapName = "Player";

    [Header("Input")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Camera")]
    [SerializeField] private Transform cameraPivot;
    [SerializeField] private float lookSensitivity = 0.1f;
    [SerializeField] private float minPitch = -40f;
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool lockCursorOnStart;

    private bool gameplayInputEnabled = true;

    private CharacterController characterController;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;

    private float yaw;
    private float pitch;
    private Vector3 verticalVelocity;

    // Permite aplicar modificadores de velocidad temporalmente (p.ej. montar una moto).
    private float moveSpeedMultiplier = 1f;

    public float MoveSpeedMultiplier => moveSpeedMultiplier;

    public bool IsGrounded => characterController != null && characterController.isGrounded;

    public bool HasMoveInput
    {
        get
        {
            if (!gameplayInputEnabled || moveAction == null)
            {
                return false;
            }

            return moveAction.ReadValue<Vector2>().sqrMagnitude > 0.0001f;
        }
    }

    public void SetMoveSpeedMultiplier(float multiplier)
    {
        moveSpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetGameplayInputEnabled(bool enabled)
    {
        gameplayInputEnabled = enabled;
    }

    public void RestoreCursorFromGameplaySettings()
    {
        if (lockCursorOnStart)
        {
            LockCursor();
            return;
        }

        UnlockCursor();
    }

    public void AlignViewTo(float yawDegrees, float pitchDegrees)
    {
        yaw = yawDegrees;
        pitch = NormalizePitch(pitchDegrees);
        ApplyViewRotation();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        yaw = transform.eulerAngles.y;

        if (cameraPivot != null)
        {
            pitch = NormalizePitch(cameraPivot.localEulerAngles.x);
        }
    }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            Debug.LogError($"{nameof(MovePlayerController)} requires an InputActionAsset.", this);
            enabled = false;
            return;
        }

        InputActionMap playerMap = inputActions.FindActionMap(PlayerActionMapName, true);
        moveAction = playerMap.FindAction("Move", true);
        lookAction = playerMap.FindAction("Look", true);
        sprintAction = playerMap.FindAction("Sprint", true);
        playerMap.Enable();

        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void OnDisable()
    {
        if (inputActions == null)
        {
            return;
        }

        InputActionMap playerMap = inputActions.FindActionMap(PlayerActionMapName);
        playerMap?.Disable();
    }

    private void Update()
    {
        if (!gameplayInputEnabled)
        {
            return;
        }

        HandleCursorToggle();
        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        if (cameraPivot == null)
        {
            return;
        }

        Vector2 lookInput = lookAction.ReadValue<Vector2>();
        if (lookInput.sqrMagnitude < 0.0001f)
        {
            return;
        }

        float deltaMultiplier = IsMouseDeviceActive() ? 1f : Time.deltaTime;
        float lookScale = lookSensitivity * deltaMultiplier;

        yaw += lookInput.x * lookScale;
        pitch -= lookInput.y * lookScale;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        ApplyViewRotation();
    }

    private void ApplyViewRotation()
    {
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraPivot != null)
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        float baseSpeed = moveSpeed * moveSpeedMultiplier;
        float speed = sprintAction.IsPressed() ? baseSpeed * sprintMultiplier : baseSpeed;

        if (characterController.isGrounded)
        {
            verticalVelocity.y = groundedStickForce;
        }

        Vector3 horizontalVelocity = moveDirection * speed;
        Vector3 motion = (horizontalVelocity + verticalVelocity) * Time.deltaTime;
        characterController.Move(motion);
    }

    private void HandleCursorToggle()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnlockCursor();
        }

        if (lockCursorOnStart && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            LockCursor();
        }
    }

    private bool IsMouseDeviceActive()
    {
        return lookAction.activeControl?.device is Mouse;
    }

    private static float NormalizePitch(float eulerPitch)
    {
        return eulerPitch > 180f ? eulerPitch - 360f : eulerPitch;
    }

    private static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
