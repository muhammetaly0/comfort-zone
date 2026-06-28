using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using TMPro;
using System;

// Line Renderer bu scriptin olduğu GameObject'e ekle.
// EKG grafiği bu objenin yerel koordinatından yatay olarak çizilir.
// Objeyi odada bir monitör konumuna ya da kameraya child yap (HUD olarak).
[RequireComponent(typeof(LineRenderer))]
public class HeartRateSystem : MonoBehaviour
{
    [Header("Fear")]
    [SerializeField] private float faintThreshold = 95f;

    [Header("Oda İçi Korku (Pasif Birikme)")]
    // Anne odadayken korku yavaşça dolar, odadan çıkınca hızla boşalır.
    // RoomFearZone.cs bu objeyi SetInRoom() ile çağırır.
    [SerializeField] private float roomFearGainRate = 3f;
    [SerializeField] private float outOfRoomDecayRate = 18f;
    // Varsayılan true: RoomFearZone hiç bağlanmasa/atanmasa bile sistem
    // çökmesin — oyun zaten tek bir odada geçiyor, "odada" başlamak güvenli varsayım.
    [SerializeField] private bool startInRoom = true;
    private bool _inRoom;

    [Header("Korku Barı (HUD, ekranın üstü)")]
    [SerializeField] private Image fearBarFill; // Image Type = Filled olmalı

    [Header("EKG Display")]
    [SerializeField] private int resolution = 180;
    [SerializeField] private float displayWidth = 2f;
    [SerializeField] private float displayHeight = 0.25f;
    [SerializeField] private float baseScrollSpeed = 0.4f;
    [SerializeField] private float panicScrollMultiplier = 10f;   // korkunca kaç kat hızlanır
    [SerializeField] private float panicAmplitudeMultiplier = 3.5f; // korkunca kaç kat yükselir

    [Header("BPM Sayısı (EKG yanında)")]
    [SerializeField] private TMP_Text bpmText;
    [SerializeField] private float baseBPM = 75f;
    [SerializeField] private float maxBPM = 180f;

    [Header("Anlık Korku (Gregor görünürken)")]
    // GregorAI her frame SetScareIntensity() çağırır. Bu, yavaş biriken
    // Fear'dan AYRI, anlık bir sinyal — Gregor görünür olduğu sürece
    // EKG'yi hemen ve sürekli sık/yüksek tutar. Görsel tepki bu ikisinin
    // büyüğünü kullanır.
    [SerializeField] private float scareDecaySpeed = 4f; // Gregor kaybolunca ne kadar çabuk söner

    [Header("Kalp Atış Sesi")]
    // Klip artık AudioManager'da tutuluyor (Inspector'dan oraya sürükle).
    // Normal durumda %60 hız/ses, bayılmaya yaklaşırken %120'ye çıkar (pitch/volume 1.0 = %100)
    [SerializeField] private float minPitch = 0.6f;
    [SerializeField] private float maxPitch = 1.2f;
    [SerializeField] private float minBeatVolume = 0.6f;
    [SerializeField] private float maxBeatVolume = 1.2f;

    private AudioSource _sfx;
    private AudioClip _heartbeatClip;
    private float _heartbeatBaseVolume = 0.5f;

    [Header("Müzik Yavaşlama + Ses Artışı (Bayılmaya Yaklaşma)")]
    // Oyun içi müzik klibi de AudioManager'da — Start'ta otomatik çalınır.
    [SerializeField] private float normalMusicPitch = 1f;
    [SerializeField] private float nearFaintMusicPitch = 0.55f; // bayılma anında müzik bu kadar yavaş/kalın çalar
    [SerializeField] private float maxMusicVolumeMultiplier = 1.5f; // korku arttıkça müzik bu kata kadar yükselir

    private AudioSource _music;
    private float _baseMusicVolume = 0.5f; // AudioManager'ın ayarladığı orijinal volume

    [Header("Görsel Distortion (URP Volume)")]
    [SerializeField] private Volume ppVolume;
    [SerializeField] private float maxLensDistortion = 0.45f; // -1..1 aralığı
    [SerializeField] private float maxVignetteIntensity = 0.45f;
    [SerializeField] private float baseVignetteIntensity = 0.15f;

    // Bir EKG darbesi: normalleştirilmiş [0,1] değerler, bir tam döngü
    private static readonly float[] Pulse =
    {
        0f, 0f, 0f, 0f, 0f, 0f,
        // P dalgası
        0.05f, 0.12f, 0.15f, 0.12f, 0.05f,
        0f, 0f, 0f,
        // QRS kompleksi
        -0.08f, -0.15f, 0f,
        0.35f, 0.75f, 1f, 0.75f, 0.35f,
        0f, -0.28f, -0.40f, -0.20f, -0.05f,
        0f, 0f, 0f, 0f,
        // T dalgası
        0.05f, 0.13f, 0.18f, 0.13f, 0.05f,
        0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f
    };

    private LineRenderer _lr;
    private float _fear;
    private float _scrollT;
    private bool _fainted;
    private float _scareIntensity; // 0-1, GregorAI tarafından her frame set edilir

    private LensDistortion _lensDistortion;
    private Vignette _vignette;

    public float Fear => _fear;
    public float FearNormalized => _fear / 100f;
    public bool HasFainted => _fainted;

    public event Action OnFaint;

    private void Awake()
    {
        _inRoom = startInRoom;

        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = false;
        _lr.positionCount = resolution;

        if (ppVolume != null && ppVolume.profile != null)
        {
            ppVolume.profile.TryGet(out _lensDistortion);
            ppVolume.profile.TryGet(out _vignette);

            // Override checkbox işaretli olmasa da kod üzerinden zorla aktif et
            if (_lensDistortion != null) _lensDistortion.intensity.overrideState = true;
            if (_vignette != null) _vignette.intensity.overrideState = true;
        }

        if (AudioManager.Instance == null)
            Debug.LogWarning($"{name}: AudioManager bulunamadı — ses efektleri çalışmayacak.", this);
    }

    // GameIntroSequence bu component'i intro bitene kadar enabled=false yapar.
    // OnEnable, Awake'in tersine sadece component AÇIK olduğunda çalışır —
    // bu sayede oyun içi müzik menü/intro sırasında değil, kontrol oyuncuya
    // geçtiği anda (heartRate.enabled=true yapıldığında) başlar.
    private void OnEnable()
    {
        if (AudioManager.Instance == null) return;

        _sfx = AudioManager.Instance.SFX;
        _music = AudioManager.Instance.Music;
        _heartbeatClip = AudioManager.Instance.HeartbeatClip;
        _heartbeatBaseVolume = AudioManager.Instance.HeartbeatBaseVolume;

        AudioManager.Instance.PlayGameplayMusic();
        if (_music != null)
        {
            _music.pitch = normalMusicPitch;
            _baseMusicVolume = _music.volume; // AudioManager'ın o anki ayarladığı değeri baz al
        }
    }

    private void Update()
    {
        if (_fainted)
        {
            DrawFlatline();
            UpdateHeartbeatAudio();
            UpdateFearBar();
            if (bpmText != null) bpmText.text = "0";
            return;
        }

        // Pasif taban: odadayken korku yavaşça birikir, dışarıdayken hızla boşalır.
        // Gregor'un anlık korkutmaları (AddFear) bunun üstüne ayrıca eklenir.
        if (_inRoom)
            _fear = Mathf.Clamp(_fear + roomFearGainRate * Time.deltaTime, 0f, 100f);
        else
            _fear = Mathf.Max(0f, _fear - outOfRoomDecayRate * Time.deltaTime);

        if (_fear >= faintThreshold)
        {
            _fainted = true;
            OnFaint?.Invoke();
            return;
        }

        // Gregor görünmüyorsa scare sinyali zamanla söner (GregorAI çağırmayı kesince)
        _scareIntensity = Mathf.MoveTowards(_scareIntensity, 0f, scareDecaySpeed * Time.deltaTime);

        float speed = Mathf.Lerp(baseScrollSpeed, baseScrollSpeed * panicScrollMultiplier, VisualFearFactor);
        _scrollT += speed * Time.deltaTime;

        DrawEKG();
        UpdateBpmText();
        UpdateFearBar();
        UpdateHeartbeatAudio();
        UpdateMusicSlowdown();
        UpdateDistortion();
    }

    private void UpdateMusicSlowdown()
    {
        if (_music == null) return;
        _music.pitch = Mathf.Lerp(normalMusicPitch, nearFaintMusicPitch, VisualFearFactor);
        _music.volume = _baseMusicVolume * Mathf.Lerp(1f, maxMusicVolumeMultiplier, VisualFearFactor);
    }

    private void UpdateFearBar()
    {
        if (fearBarFill != null) fearBarFill.fillAmount = VisualFearFactor;
    }

    // RoomFearZone.cs anne odaya girip çıktıkça çağırır.
    public void SetInRoom(bool inRoom) => _inRoom = inRoom;

    private void UpdateHeartbeatAudio()
    {
        if (_sfx == null || _heartbeatClip == null) return;

        if (_fainted)
        {
            if (_sfx.clip == _heartbeatClip) _sfx.Stop();
            return;
        }

        if (_sfx.clip != _heartbeatClip)
        {
            _sfx.clip = _heartbeatClip;
            _sfx.loop = true;
        }
        if (!_sfx.isPlaying) _sfx.Play();

        _sfx.pitch = Mathf.Lerp(minPitch, maxPitch, VisualFearFactor);
        // AudioManager'daki temel volume (%50 varsayılan) korku oranıyla (%60-%120) çarpılır
        _sfx.volume = _heartbeatBaseVolume * Mathf.Lerp(minBeatVolume, maxBeatVolume, VisualFearFactor);
    }

    private void UpdateDistortion()
    {
        if (_lensDistortion != null)
            _lensDistortion.intensity.value = Mathf.Lerp(0f, maxLensDistortion, VisualFearFactor);

        if (_vignette != null)
            _vignette.intensity.value = Mathf.Lerp(baseVignetteIntensity, maxVignetteIntensity, VisualFearFactor);
    }

    public void AddFear(float amount)
    {
        if (_fainted) return;
        _fear = Mathf.Clamp(_fear + amount, 0f, 100f);
    }

    // GregorAI her frame çağırır: 0 = görünmüyor, 1 = çok yakın/tam karşıda.
    // Anlık görsel tepki için kullanılır, Fear'ı (kalp atışı sesi/distortion) etkilemez.
    public void SetScareIntensity(float intensity) => _scareIntensity = Mathf.Clamp01(intensity);

    // EKG'nin görselini sürükleyen değer: birikmiş korku VEYA anlık "Gregor görünüyor"
    // sinyalinden hangisi daha büyükse o kullanılır.
    private float VisualFearFactor => Mathf.Max(FearNormalized, _scareIntensity);

    private void UpdateBpmText()
    {
        if (bpmText == null) return;
        int bpm = Mathf.RoundToInt(Mathf.Lerp(baseBPM, maxBPM, VisualFearFactor));
        bpmText.text = $"{bpm}";
    }

    private void DrawEKG()
    {
        float halfW = displayWidth * 0.5f;

        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            float x = Mathf.Lerp(-halfW, halfW, t);

            float sample = (_scrollT + t) % 1f;
            float y = SamplePulse(sample) * displayHeight * (1f + VisualFearFactor * panicAmplitudeMultiplier);

            _lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private void DrawFlatline()
    {
        float halfW = displayWidth * 0.5f;
        for (int i = 0; i < resolution; i++)
        {
            float t = (float)i / (resolution - 1);
            _lr.SetPosition(i, new Vector3(Mathf.Lerp(-halfW, halfW, t), 0f, 0f));
        }
    }

    private float SamplePulse(float t)
    {
        float scaled = t * Pulse.Length;
        int i0 = (int)scaled % Pulse.Length;
        int i1 = (i0 + 1) % Pulse.Length;
        return Mathf.Lerp(Pulse[i0], Pulse[i1], scaled - (int)scaled);
    }
}
