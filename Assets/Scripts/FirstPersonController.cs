using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public enum InputMode
    {
        Auto,
        LegacyInputManager,
        NewInputSystem
    }

    [Header("Input")]
    [SerializeField] private InputMode inputMode = InputMode.Auto;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float gamepadLookSensitivity = 120f;
    [SerializeField] private float minimumLookAngle = -85f;
    [SerializeField] private float maximumLookAngle = 85f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursorOnStart = true;

    private CharacterController characterController;

    private Vector2 movementInput;
    private Vector2 lookInput;

    private float verticalVelocity;
    private float cameraPitch;

    private bool jumpPressed;
    private bool sprintHeld;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void Start()
    {
        if (lockCursorOnStart)
        {
            LockCursor();
        }
    }

    private void Update()
    {
        ReadInput();
        HandleMouseLook();
        HandleMovement();
        HandleCursor();
    }

    private void ReadInput()
    {
        movementInput = Vector2.zero;
        lookInput = Vector2.zero;
        jumpPressed = false;
        sprintHeld = false;

        switch (inputMode)
        {
            case InputMode.LegacyInputManager:
                ReadLegacyInput();
                break;

            case InputMode.NewInputSystem:
                ReadNewInputSystem();
                break;

            case InputMode.Auto:
                ReadAutomaticInput();
                break;
        }

        movementInput = Vector2.ClampMagnitude(movementInput, 1f);
    }

    private void ReadAutomaticInput()
    {
#if ENABLE_INPUT_SYSTEM
        ReadNewInputSystem();
#elif ENABLE_LEGACY_INPUT_MANAGER
        ReadLegacyInput();
#else
        Debug.LogWarning("No input system is enabled. Check Active Input Handling in Project Settings.");
#endif
    }

    private void ReadLegacyInput()
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        lookInput = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );

        jumpPressed = Input.GetButtonDown("Jump");
        sprintHeld = Input.GetKey(KeyCode.LeftShift);
#else
        Debug.LogWarning("Legacy Input Manager is not enabled. Set Active Input Handling to Input Manager or Both.");
#endif
    }

    private void ReadNewInputSystem()
    {
#if ENABLE_INPUT_SYSTEM
        ReadNewKeyboardAndMouse();
        ReadNewGamepad();
#else
        Debug.LogWarning("New Input System is not enabled or installed. Install Input System and set Active Input Handling correctly.");
#endif
    }

#if ENABLE_INPUT_SYSTEM
    private void ReadNewKeyboardAndMouse()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed)
                horizontal -= 1f;

            if (keyboard.dKey.isPressed)
                horizontal += 1f;

            if (keyboard.sKey.isPressed)
                vertical -= 1f;

            if (keyboard.wKey.isPressed)
                vertical += 1f;

            movementInput = new Vector2(horizontal, vertical);

            jumpPressed = keyboard.spaceKey.wasPressedThisFrame;
            sprintHeld = keyboard.leftShiftKey.isPressed;
        }

        if (mouse != null)
        {
            lookInput = mouse.delta.ReadValue() * mouseSensitivity;
        }
    }

    private void ReadNewGamepad()
    {
        Gamepad gamepad = Gamepad.current;

        if (gamepad == null)
            return;

        Vector2 gamepadMovement = gamepad.leftStick.ReadValue();

        if (gamepadMovement.sqrMagnitude > 0.01f)
        {
            movementInput = gamepadMovement;
        }

        Vector2 gamepadLook = gamepad.rightStick.ReadValue();

        if (gamepadLook.sqrMagnitude > 0.01f)
        {
            lookInput = gamepadLook * gamepadLookSensitivity * Time.unscaledDeltaTime;
        }

        if (gamepad.buttonSouth.wasPressedThisFrame)
        {
            jumpPressed = true;
        }

        if (gamepad.leftStickButton.isPressed)
        {
            sprintHeld = true;
        }
    }
#endif

    private void HandleMovement()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        Vector3 movement =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        float currentSpeed = sprintHeld ? sprintSpeed : walkSpeed;

        if (jumpPressed && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity =
            movement * currentSpeed +
            Vector3.up * verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        if (cameraTransform == null)
            return;

        float horizontalLook = lookInput.x;
        float verticalLook = lookInput.y;

#if ENABLE_LEGACY_INPUT_MANAGER
        if (ShouldUseLegacyInput())
        {
            horizontalLook *= mouseSensitivity * 10f;
            verticalLook *= mouseSensitivity * 10f;
        }
#endif

        cameraPitch -= verticalLook;
        cameraPitch = Mathf.Clamp(cameraPitch, minimumLookAngle, maximumLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        transform.Rotate(Vector3.up * horizontalLook);
    }

    private bool ShouldUseLegacyInput()
    {
        if (inputMode == InputMode.LegacyInputManager)
            return true;

        if (inputMode == InputMode.NewInputSystem)
            return false;

#if ENABLE_INPUT_SYSTEM
        return false;
#else
        return true;
#endif
    }

    private void HandleCursor()
    {
        bool escapePressed = false;
        bool clickPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        if (Mouse.current != null)
        {
            clickPressed = Mouse.current.leftButton.wasPressedThisFrame;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        escapePressed = Input.GetKeyDown(KeyCode.Escape);
        clickPressed = Input.GetMouseButtonDown(0);
#endif

        if (escapePressed)
        {
            UnlockCursor();
        }

        if (clickPressed && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
