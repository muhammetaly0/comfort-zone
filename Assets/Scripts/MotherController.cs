using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MotherController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.2f;
    [SerializeField] private float dragSpeedMultiplier = 0.38f;
    [SerializeField] private float gravity = -12f;

    [Header("Mouse Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 1.8f;
    [SerializeField] private float dragLookMultiplier = 0.45f; // sürüklerken bakış hassasiyeti çarpanı
    [SerializeField] private float minPitch = -75f;
    [SerializeField] private float maxPitch = 75f;

    [Header("Weight Effect")]
    [SerializeField] private float weightFloor = 1f; // ağırlık bundan düşükse etkisi yok (0'a bölünmeyi engeller)

    [Header("Footsteps")]
    [SerializeField] private float stepInterval = 0.68f;
    [SerializeField] private float stepIntervalDrag = 1.05f;
    [SerializeField] private float stepPitchMin = 0.88f;
    [SerializeField] private float stepPitchMax = 1.05f;

    [Header("Camera Bob")]
    [SerializeField] private float bobFrequency = 1.6f;
    [SerializeField] private float bobAmplitude = 0.035f;
    [SerializeField] private float bobDragMultiplier = 0.5f;

    [Header("References")]
    [SerializeField] private FurnitureGrab furnitureGrab;

    private CharacterController _cc;
    private float _pitch;
    private float _yVelocity;
    private float _stepTimer;
    private float _bobTimer;
    private Vector3 _cameraRestPos;

    public bool IsDragging { get; set; }

    // Hız/bakış doğrudan ağırlıkla ters orantılı: ağırlık 2x → hız 2x yavaş.
    private float WeightFactor()
    {
        if (!IsDragging || furnitureGrab == null) return 1f;
        return Mathf.Max(furnitureGrab.HeldWeight, weightFloor);
    }

    public float CurrentSpeed
        => IsDragging ? walkSpeed * dragSpeedMultiplier / WeightFactor() : walkSpeed;

    private float CurrentMouseSensitivity
        => IsDragging ? mouseSensitivity * dragLookMultiplier / WeightFactor() : mouseSensitivity;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform != null)
            _cameraRestPos = cameraTransform.localPosition;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        // Sağ tıkla obje döndürülürken kamera kilitli kalır
        if (furnitureGrab != null && furnitureGrab.IsRotating) return;

        float sensitivity = CurrentMouseSensitivity;
        float mouseX = Input.GetAxis("Mouse X") * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity;

        transform.Rotate(Vector3.up, mouseX);

        _pitch -= mouseY;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = Vector3.ClampMagnitude(transform.right * h + transform.forward * v, 1f);
        move *= CurrentSpeed;

        if (_cc.isGrounded && _yVelocity < 0f)
            _yVelocity = -2f;
        else
            _yVelocity += gravity * Time.deltaTime;

        move.y = _yVelocity;
        _cc.Move(move * Time.deltaTime);

        bool isMoving = (h != 0f || v != 0f) && _cc.isGrounded;

        if (isMoving)
        {
            TickFootsteps();
            TickBob();
        }
        else
        {
            ResetBob();
        }
    }

    private void TickFootsteps()
    {
        _stepTimer -= Time.deltaTime;
        if (_stepTimer > 0f) return;

        _stepTimer = IsDragging ? stepIntervalDrag : stepInterval;

        if (AudioManager.Instance == null) return;
        AudioManager.Instance.PlayRandomFootstep(Random.Range(stepPitchMin, stepPitchMax));
    }

    private void TickBob()
    {
        float freq = IsDragging ? bobFrequency * bobDragMultiplier : bobFrequency;
        _bobTimer += Time.deltaTime * freq;

        if (cameraTransform == null) return;
        float y = Mathf.Sin(_bobTimer) * bobAmplitude;
        float x = Mathf.Sin(_bobTimer * 0.5f) * bobAmplitude * 0.4f;
        cameraTransform.localPosition = _cameraRestPos + new Vector3(x, y, 0f);
    }

    private void ResetBob()
    {
        if (cameraTransform == null) return;
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, _cameraRestPos, Time.deltaTime * 6f);
    }
}
