using UnityEngine;

public class GregorAI : MonoBehaviour
{
    public enum State { Hiding, Emerging, Visible, Retreating }

    [Header("Timing")]
    [SerializeField] private float hideTimeMin = 7f;
    [SerializeField] private float hideTimeMax = 14f;
    [SerializeField] private float emergeDuration = 1.2f;
    [SerializeField] private float visibleDuration = 8f; // güvenlik ağı — gerçek süre tur tamamlanınca biter
    [SerializeField] private float retreatDuration = 0.8f;

    [Header("Fear Output")]
    [SerializeField] private float fearPerSecond = 22f;
    [SerializeField] private float sightRange = 6f;
    [SerializeField] private LayerMask sightBlockMask;

    [Header("Sheet Animation")]
    [SerializeField] private Transform sheetTransform;
    [SerializeField] private Vector3 sheetHiddenScale = Vector3.one;
    [SerializeField] private Vector3 sheetRevealedScale = new Vector3(1f, 0.25f, 1f);

    [Header("References")]
    [SerializeField] private HeartRateSystem heartRate;
    [SerializeField] private Transform motherTransform;
    [SerializeField] private GregorSplineWalker splineWalker;
    [SerializeField] private SlimeTrail slimeTrail; // boş bırakılırsa aynı obje/child'larda aranır

    private SlimeTrail _slime;
    private State _state = State.Hiding;
    private float _timer;

    private void Awake()
    {
        _slime = slimeTrail != null ? slimeTrail : GetComponentInChildren<SlimeTrail>();
        if (_slime == null)
            Debug.LogWarning($"{name}: SlimeTrail bulunamadı — iz bırakılmayacak. Inspector'dan ata.", this);

        // Bu ikisi boşsa korku ASLA tetiklenmez (TryApplyFear sessizce çıkar).
        if (heartRate == null)
            Debug.LogError($"{name}: 'Heart Rate' alanı boş! Korku hiç eklenmeyecek. Inspector'dan ata.", this);
        if (motherTransform == null)
            Debug.LogError($"{name}: 'Mother Transform' alanı boş! Korku hiç eklenmeyecek. Inspector'dan ata.", this);
    }

    private void Start() => EnterState(State.Hiding);

    private void Update()
    {
        _timer -= Time.deltaTime;

        switch (_state)
        {
            case State.Hiding:
                if (_timer <= 0f) EnterState(State.Emerging);
                break;

            case State.Emerging:
                AnimateSheet(1f - _timer / emergeDuration);
                if (_timer <= 0f) EnterState(State.Visible);
                break;

            case State.Visible:
                TryApplyFear();
                UpdateScareSignal();
                if (_slime != null) _slime.AddPoint(transform.position);

                // Tur tamamlandıysa (spline doğal olarak durdu) hemen geri çekil.
                // _timer burada sadece güvenlik ağı: spline atanmamışsa sonsuza
                // kadar Visible'da kalmasın diye.
                bool lapFinished = splineWalker == null || !splineWalker.IsMoving;
                if (lapFinished || _timer <= 0f) EnterState(State.Retreating);
                break;

            case State.Retreating:
                AnimateSheet(_timer / retreatDuration);
                if (_timer <= 0f) EnterState(State.Hiding);
                break;
        }
    }

    private void EnterState(State next)
    {
        _state = next;

        switch (next)
        {
            case State.Hiding:
                _timer = Random.Range(hideTimeMin, hideTimeMax);
                splineWalker?.StopWalking();
                SetSheetScale(sheetHiddenScale);
                break;

            case State.Emerging:
                _timer = emergeDuration;
                splineWalker?.StartWalking();
                break;

            case State.Visible:
                _timer = visibleDuration;
                break;

            case State.Retreating:
                _timer = retreatDuration;
                splineWalker?.StopWalking();
                break;
        }
    }

    // Anne'nin Gregor'u görüp görmediğini ve ne kadar yakın olduğunu hesaplar.
    // 0 = göremiyor/menzil dışı, 1 = tam yakınında/karşısında.
    private float GetVisibilityFactor()
    {
        if (motherTransform == null) return 0f;

        Vector3 toMother = motherTransform.position - transform.position;
        float dist = toMother.magnitude;
        if (dist > sightRange) return 0f;

        // Görüş engeli var mı?
        if (Physics.Raycast(transform.position, toMother.normalized, dist, sightBlockMask)) return 0f;

        return 1f - Mathf.Clamp01(dist / sightRange);
    }

    private void TryApplyFear()
    {
        if (heartRate == null) return;
        float distFactor = GetVisibilityFactor();
        if (distFactor > 0f)
            heartRate.AddFear(fearPerSecond * distFactor * Time.deltaTime);
    }

    // Gregor görünür olduğu sürece EKG'yi anlık olarak sık/yüksek tutar
    // (Fear'ın yavaş birikmesinden bağımsız, anlık tepki).
    private void UpdateScareSignal()
    {
        if (heartRate == null) return;
        heartRate.SetScareIntensity(GetVisibilityFactor());
    }

    private void AnimateSheet(float t)
    {
        if (sheetTransform == null) return;
        sheetTransform.localScale = Vector3.Lerp(sheetHiddenScale, sheetRevealedScale, t);
    }

    private void SetSheetScale(Vector3 scale)
    {
        if (sheetTransform != null) sheetTransform.localScale = scale;
    }

    public State CurrentState => _state;
}
