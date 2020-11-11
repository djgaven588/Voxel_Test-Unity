using UnityEngine;

public class PlayerController : Entity
{
    [Header("Player Controller")]
    [Header("Movement Speed")]
    public float MovementSpeed;
    [Range(0, 1)]
    public float CrouchingSpeedModifier;
    public float JumpVelocity;

    [Header("Control")]
    public float GroundedControl;
    public float InAirControl;

    [Header("Camera Settings")]
    public float RotationSpeed;
    public Vector2 CameraXExtents;

    [Header("Camera Offsets")]
    [Range(0, 1)]
    public float StandingCameraOffset;
    [Range(0, 1)]
    public float CrouchingCameraOffset;

    [Header("References")]
    public Camera Camera;

    private Vector2 PreviousCameraRotation;
    private bool mouseLocked = true;

    private void Start()
    {
        LocalPosition = transform.position;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            mouseLocked = !mouseLocked;
            Cursor.lockState = mouseLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        Vector3 movementInput = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));

        movementInput = transform.localRotation * movementInput;

        bool crouching = Input.GetKey(KeyCode.LeftShift);
        bool grounded = Mathf.Approximately(Velocity.y, 0);
        float control = grounded ? GroundedControl : InAirControl;

        Vector3 movement = (movementInput.magnitude > 0.01f ? movementInput.normalized : Vector3.zero);

        if (crouching && grounded)
        {
            movement *= CrouchingSpeedModifier;
        }

        Velocity.x += (movement.x * MovementSpeed - Velocity.x) * control * Time.deltaTime;
        Velocity.z += (movement.z * MovementSpeed - Velocity.z) * control * Time.deltaTime;
        if (grounded && Input.GetKey(KeyCode.Space))
        {
            Velocity.y = JumpVelocity;
        }

        Velocity.y += PhysicsEngine.Gravity * Time.deltaTime;

        Camera.transform.localPosition = Vector3.up * (crouching ? CrouchingCameraOffset : StandingCameraOffset);

        if (!mouseLocked)
            return;

        Vector2 mouseMovement = new Vector2(-Input.GetAxisRaw("Mouse Y"), Input.GetAxisRaw("Mouse X"));

        PreviousCameraRotation += mouseMovement * RotationSpeed;
        PreviousCameraRotation.x = Mathf.Clamp(PreviousCameraRotation.x, CameraXExtents.x, CameraXExtents.y);

        transform.localRotation = Quaternion.Euler(0, PreviousCameraRotation.y, 0);
        Camera.transform.localRotation = Quaternion.Euler(PreviousCameraRotation.x, 0, 0);
    }
}
