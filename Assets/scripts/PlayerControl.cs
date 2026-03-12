using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Движение")]
    public float walkSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;

    [Header("Кнопки движения")]
    public Key keyForward = Key.W;
    public Key keyBack    = Key.S;
    public Key keyLeft    = Key.A;
    public Key keyRight   = Key.D;
    public Key keyJump    = Key.Space;

    [Header("Камера")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;

    [Header("Покачивание камеры")]
    public float bobFrequency = 2.5f;
    public float bobAmplitudeY = 0.08f;
    public float bobAmplitudeX = 0.04f;
    public float bobReturnSpeed = 8f;
    public float bobSmoothSpeed = 10f;

    // Используется инструментами (Tools)
    [HideInInspector] public float fallSpeedMultiplier = 1f;
    public bool IsFalling          => !isGrounded && velocity.y < 0f;
    public bool IsGrounded         => isGrounded;
    public bool IsMoving           => isMoving;
    public float VerticalVelocity  => velocity.y;
    public float BobTimer          => bobTimer;
    // Мировой Y нижней точки капсулы (уровень ног)
    public float FeetY             => transform.position.y + controller.center.y - controller.height / 2f;
    public void MultiplyVerticalVelocity(float multiplier) => velocity.y *= multiplier;
    public void SetExternalVelocity(Vector3 v)  => externalVelocity = v;
    /// <summary>Полностью заменяет движение игрока на один кадр (для маятника).</summary>
    public void SetVelocityOverride(Vector3 v)  { velocityOverride = v; hasVelocityOverride = true; }
    /// <summary>Сбрасывает накопленную вертикальную скорость (например при отпускании верёвки).</summary>
    public void SetYVelocity(float y)            => velocity.y = y;
    public Vector3 CurrentVelocity               => velocity;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    private float yRotation = 0f;

    private Vector3 externalVelocity    = Vector3.zero;
    private Vector3 velocityOverride    = Vector3.zero;
    private bool    hasVelocityOverride = false;
    private float bobTimer = 0f;
    private Vector3 currentBobOffset = Vector3.zero;
    private Vector3 targetBobOffset = Vector3.zero;
    private Vector3 cameraInitialLocalPos;
    private bool isMoving = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        yRotation = transform.eulerAngles.y;
        cameraInitialLocalPos = cameraTransform.localPosition;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        HandleMouseLook();
        HandleMovement();
        HandleCameraBob();
    }

    void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.05f * mouseSensitivity;

        xRotation -= mouseDelta.y;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        yRotation += mouseDelta.x;
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Override: полный контроль движения (маятник, верёвка и т.п.)
        if (hasVelocityOverride)
        {
            controller.Move(velocityOverride * Time.deltaTime);
            hasVelocityOverride = false;
            isMoving = false;
            return;
        }

        Vector2 moveInput = Vector2.zero;
        if (Keyboard.current[keyForward].isPressed) moveInput.y += 1f;
        if (Keyboard.current[keyBack].isPressed)    moveInput.y -= 1f;
        if (Keyboard.current[keyLeft].isPressed)    moveInput.x -= 1f;
        if (Keyboard.current[keyRight].isPressed)   moveInput.x += 1f;

        isMoving = moveInput.magnitude > 0f && isGrounded;

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (move.magnitude > 1f) move.Normalize();

        controller.Move(move * walkSpeed * Time.deltaTime);

        if (Keyboard.current[keyJump].wasPressedThisFrame && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velocity.y += gravity * fallSpeedMultiplier * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        if (externalVelocity != Vector3.zero)
        {
            controller.Move(externalVelocity * Time.deltaTime);
            externalVelocity = Vector3.zero;
        }
    }

    void HandleCameraBob()
    {
        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency;
            float bobY = Mathf.Sin(bobTimer * 2f) * bobAmplitudeY;
            float bobX = Mathf.Sin(bobTimer) * bobAmplitudeX;
            targetBobOffset = new Vector3(bobX, bobY, 0f);
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * bobReturnSpeed);
            targetBobOffset = Vector3.zero;
        }

        currentBobOffset = Vector3.Lerp(currentBobOffset, targetBobOffset, Time.deltaTime * bobSmoothSpeed);
        cameraTransform.localPosition = cameraInitialLocalPos + currentBobOffset;
    }
}
