using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using DG.Tweening;

// Gregor'un tavan ve duvardaki rotaları.
// Birden fazla SplineContainer ekle (her biri ayrı bir GameObject'te,
// ayrı bir yol çizilmiş) — her tetiklemede bunlardan rastgele biri seçilir.
// Tur bitince Gregor, oyunun başındaki orijinal (saklı) pozisyonuna
// DOTween ile yavaşça geri döner.
[RequireComponent(typeof(SplineAnimate))]
public class GregorSplineWalker : MonoBehaviour
{
    [Header("Spline - Rastgele Rotalar")]
    [SerializeField] private SplineContainer[] possibleRoutes;
    // Tur süresi buradan AYARLANMAZ — Spline Animate component'indeki
    // "Duration" alanı neyse o kullanılır.

    [Header("Orientation")]
    [SerializeField] private bool alignToSurface = true;
    [SerializeField] private float orientSmoothing = 6f;

    [Header("Başlangıca Dönüş")]
    [SerializeField] private Transform startPositionAnchor; // boş bırakılırsa Gregor'un kendi başlangıç transformu kullanılır
    [SerializeField] private float returnDuration = 1.5f;
    [SerializeField] private Ease returnEase = Ease.InOutSine;

    private SplineAnimate _anim;
    private SplineContainer _currentRoute;
    private bool _initialized;
    private bool _returning;
    private Vector3 _startPos;
    private Quaternion _startRot;
    private Sequence _returnSeq;

    // Tamamlanmayı _anim.NormalizedTime okuyarak DEĞİL, kendi zamanlayıcımızla
    // tespit ediyoruz — Once modu bazı paket sürümlerinde tur bitince
    // NormalizedTime'ı kendi içinde sıfırlayabiliyor, biz hiç ">= 1f" göremeden
    // donmuş gibi kalıyordu.
    private float _lapStartTime;
    private float _lapDuration;

    public bool IsMoving { get; private set; }

    private void Awake()
    {
        _anim = GetComponent<SplineAnimate>();

        // Gregor'un pozisyonu tamamen spline'dan geliyor (kinematik).
        // Üzerinde fiziksel bir Rigidbody varsa, duvar/zemin collider'larıyla
        // çakışıp her FixedUpdate'te geri itilir → titreme + "duvarda donma".
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Hata durumunda bile sahne açılır açılmaz otomatik oynamasın
        _anim.PlayOnAwake = false;
        _anim.enabled = false;

        if (possibleRoutes == null || possibleRoutes.Length == 0)
        {
            Debug.LogError($"{name}: 'possibleRoutes' boş! Inspector'dan en az bir SplineContainer ekle.", this);
            return;
        }

        for (int i = 0; i < possibleRoutes.Length; i++)
        {
            var route = possibleRoutes[i];
            if (route == null || route.Spline == null || route.Spline.Count < 2)
            {
                Debug.LogError($"{name}: possibleRoutes[{i}] geçersiz (boş ya da 2'den az nokta var).", this);
                return;
            }
        }

        _anim.enabled = true;
        // Once: tetiklenince seçilen rotayı tek tur dolaşır, sonunda kendiliğinden durur.
        _anim.Loop = SplineAnimate.LoopMode.Once;
        _anim.PlayOnAwake = false;
        _anim.Pause();
        _initialized = true;
    }

    private void Start()
    {
        // Oyunun başındaki (saklı/örtü altındaki) pozisyon ve rotasyon kaydedilir.
        // Inspector'dan bir anchor sürüklendiyse onun konumu, yoksa Gregor'un
        // kendi başlangıç pozisyonu kullanılır. Tur bitince buraya geri döner.
        if (startPositionAnchor != null)
        {
            _startPos = startPositionAnchor.position;
            _startRot = startPositionAnchor.rotation;
        }
        else
        {
            _startPos = transform.position;
            _startRot = transform.rotation;
        }

        if (!_initialized) return;
        _anim.Pause();
        _anim.enabled = false;
    }

    public void StartWalking()
    {
        if (!_initialized)
        {
            Debug.LogWarning($"{name}: Rota atanmadığı için Gregor hareket edemiyor.", this);
            return;
        }

        _returnSeq?.Kill();
        _returning = false;

        // Her tetiklemede rastgele bir rota seç ve baştan başlat
        _currentRoute = possibleRoutes[UnityEngine.Random.Range(0, possibleRoutes.Length)];
        _anim.Container = _currentRoute;

        _anim.enabled = true;
        _anim.NormalizedTime = 0f;
        _anim.Play();

        _lapStartTime = Time.time;
        _lapDuration = Mathf.Max(0.05f, _anim.Duration);
        IsMoving = true;
    }

    // Dışarıdan (GregorAI) zorla durdurulduğunda — örn. güvenlik ağı timeout'u.
    // Doğal tur tamamlanması BeginReturnToStart() üzerinden ayrı yönetilir.
    public void StopWalking()
    {
        if (!_initialized) return;
        _returnSeq?.Kill();
        _returning = false;
        _anim.Pause();
        _anim.enabled = false;
        IsMoving = false;
    }

    private void LateUpdate()
    {
        if (!_initialized || !IsMoving || _returning) return;

        if (Time.time - _lapStartTime >= _lapDuration)
        {
            BeginReturnToStart();
            return;
        }

        if (alignToSurface) AlignToSplineSurface();
    }

    private void BeginReturnToStart()
    {
        _returning = true;

        // Tur bitti: Spline Animate'i kapat, artık pozisyonu o kontrol etmiyor.
        _anim.Pause();
        _anim.enabled = false;

        _returnSeq?.Kill();
        _returnSeq = DOTween.Sequence();
        _returnSeq.Join(transform.DOMove(_startPos, returnDuration).SetEase(returnEase));
        _returnSeq.Join(transform.DORotateQuaternion(_startRot, returnDuration).SetEase(returnEase));
        _returnSeq.OnComplete(() =>
        {
            // Gregor yerine ulaştı: Spline Animate'i tekrar aç (paused durumda,
            // bir sonraki StartWalking() çağrısı normal şekilde devam eder).
            _anim.enabled = true;
            _returning = false;
            IsMoving = false;
        });
    }

    private void AlignToSplineSurface()
    {
        if (_currentRoute == null) return;

        SplineUtility.Evaluate(
            _currentRoute.Spline,
            _anim.NormalizedTime,
            out float3 pos,
            out float3 tangent,
            out float3 up);

        Vector3 fwd = ((Vector3)tangent).normalized;
        Vector3 upV = ((Vector3)up).normalized;
        if (fwd == Vector3.zero || upV == Vector3.zero) return;

        Quaternion target = Quaternion.LookRotation(fwd, upV);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * orientSmoothing);
    }

    private void OnDestroy() => _returnSeq?.Kill();
}
