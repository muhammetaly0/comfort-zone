using UnityEngine;

public class FurnitureGrab : MonoBehaviour
{
    [Header("Grab")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float grabReach = 2.8f;
    [SerializeField] private float holdDistance = 1.9f; // küçük objeler için kameradan mesafe
    [SerializeField] private LayerMask grabbableMask = ~0;
    [SerializeField] private KeyCode grabKey = KeyCode.E;

    [Header("Drag Feel")]
    [SerializeField] private float pullSpeed = 5f;

    [Header("Small Object Rotation (Sağ Tık)")]
    [SerializeField] private float rotateSpeed = 120f; // derece/saniye

    private MotherController _mother;
    private Furniture _held;

    // Tutma noktası, FURNİTURE'IN local space'inde (obje dönünce güncellenir)
    private Vector3 _grabContactLocal;
    // Aynı tutma noktası, KARAKTERİN local space'inde (karakter hareket/dönünce güncellenir)
    // Bu sayede "el" karaktere bağlı kalır, kameraya değil.
    private Vector3 _grabHandLocal;
    // Küçük objeler için başlangıç ofseti (ani sıçramayı engeller), zamanla sıfıra iner
    private Vector3 _grabOffset;

    public bool IsRotating { get; private set; }
    public bool IsHolding => _held != null;
    public float HeldWeight => _held != null ? _held.Weight : 1f;

    private void Awake()
    {
        _mother = GetComponentInParent<MotherController>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    private void Update()
    {
        if (Input.GetKeyDown(grabKey))
        {
            if (_held == null) TryGrab();
            else Release();
        }

        if (_held != null)
        {
            HandleRotation();
            DragHeld();
        }
        else
        {
            IsRotating = false;
        }
    }

    private void TryGrab()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
        if (!Physics.Raycast(ray, out RaycastHit hit, grabReach, grabbableMask)) return;

        Furniture f = hit.collider.GetComponentInParent<Furniture>();
        if (f == null || !f.IsGrabbable) return;

        _held = f;
        _held.OnGrabbed();
        _held.OnRemoved += OnHeldRemoved; // exit trigger'dan geçince elimizi otomatik boşaltsın
        _mother.IsDragging = true;

        AudioManager.Instance?.PlayGrab();

        // Sürükleme loop sesi sadece büyük (yerde sürüklenen) objelerde — küçükler kaldırılıp taşınıyor, sürtünme yok
        if (_held.Size == FurnitureSize.Large)
            AudioManager.Instance?.StartDragLoop();

        // Temas noktası objenin local space'inde
        _grabContactLocal = _held.transform.InverseTransformPoint(hit.point);

        if (_held.Size == FurnitureSize.Large)
        {
            // Büyük: aynı nokta KARAKTERİN local space'inde — "el" karaktere bağlı, kameradan bağımsız
            _grabHandLocal = _mother.transform.InverseTransformPoint(hit.point);
        }
        else
        {
            // Küçük: kameraya bağlı, ani sıçramayı önlemek için başlangıç ofseti
            _grabOffset = _held.Body.position - GetCameraTargetPos();
        }
    }

    private void DragHeld()
    {
        Vector3 grabContactWorld = _held.transform.TransformPoint(_grabContactLocal);

        if (_held.Size == FurnitureSize.Large)
        {
            // Büyük: el karakterin pozisyon+rotasyonuna bağlı, Y sabit (yer seviyesi) → kamera bakışından etkilenmez
            Vector3 handWorld = GetHandWorldPos();
            handWorld.y = _held.Body.position.y;
            _held.PullToward(grabContactWorld, handWorld, pullSpeed);
        }
        else
        {
            // Küçük: kameranın önünde serbestçe taşınır, kaldırılabilir
            Vector3 target = GetCameraTargetPos() + _grabOffset;
            _grabOffset = Vector3.Lerp(_grabOffset, Vector3.zero, Time.deltaTime * 1.5f);
            _held.PullToward(grabContactWorld, target, pullSpeed);
        }
    }

    private void HandleRotation()
    {
        if (_held.Size != FurnitureSize.Small) { IsRotating = false; return; }

        IsRotating = Input.GetMouseButton(1);

        if (IsRotating)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            Vector3 torqueAxis = playerCamera.transform.up    * mouseX
                               - playerCamera.transform.right * mouseY;

            float speedRad = rotateSpeed * Mathf.Deg2Rad;
            _held.Body.angularVelocity = torqueAxis * speedRad;
        }
        else
        {
            _held.Body.angularVelocity = Vector3.Lerp(
                _held.Body.angularVelocity, Vector3.zero, Time.deltaTime * 20f);
        }
    }

    private void Release()
    {
        if (_held == null) return;
        _held.OnRemoved -= OnHeldRemoved;
        _held.OnReleased();
        _held = null;
        _mother.IsDragging = false;
        _grabOffset = Vector3.zero;
        AudioManager.Instance?.StopDragLoop();
    }

    // Furniture, exit trigger'dan geçip kaldırıldığında (Furniture.Remove())
    // tetiklenir. Oyuncu E'ye basmamış olsa da elimizi otomatik boşaltır —
    // aksi halde karakter artık var olmayan bir eşyayı "tutmaya" devam ederdi.
    private void OnHeldRemoved() => Release();

    // Büyük objeler için: "el" pozisyonu karakterin transform'una bağlı sabit bir local offset.
    // Karakter yürüdükçe ya da döndükçe bu nokta onunla birlikte hareket eder.
    // Kamera sadece bakış yönünü etkiler, bu hesaba katılmaz.
    private Vector3 GetHandWorldPos()
        => _mother.transform.TransformPoint(_grabHandLocal);

    // Küçük objeler için: kameranın önünde sabit bir mesafedeki nokta.
    private Vector3 GetCameraTargetPos()
        => playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

    private void OnDisable() => Release();
}
