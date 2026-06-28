using UnityEngine;
using System.Collections;

// Sahnede tek bir boş GameObject'e ekle. Tüm oyun için tek SFX,
// tek Music kaynağı — tüm ses klipleri ve volume ayarları buradan
// tek elden yönetilir. Her sesin kendi volume'u %50'den başlar,
// Inspector'dan artırılıp azaltılabilir.
//
// NOT: Tek SFX kaynağı olduğu için aynı anda İKİ farklı loop (sürekli
// dönen) ses çalışamaz. Kalp atışı (HeartRateSystem) bu kaynağın loop
// slotunu sürekli kullanıyor.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Kaynaklar")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;
    // Ambians ayrı bir kaynak: SFX zaten kalp atışını, Music zaten oyun
    // müziğini sürekli loop olarak kullanıyor, üçüncü bir loop için yer yok.
    [SerializeField] private AudioSource ambienceSource;
    // Sürükleme sesi de gerçek bir loop olmalı (PlayOneShot tekrarlamak,
    // klip kendi süresi aralıktan uzunsa üst üste binip hiç susmuyordu).
    [SerializeField] private AudioSource dragLoopSource;

    [Header("Ambians (loop)")]
    [SerializeField] private AudioClip ambienceClip;
    [Range(0f, 1f)] [SerializeField] private float ambienceVolume = 0.5f;

    [Header("Müzik — Menü")]
    [SerializeField] private AudioClip menuMusic;
    [Range(0f, 1f)] [SerializeField] private float menuMusicVolume = 0.5f;

    [Header("Müzik — Oyun İçi")]
    [SerializeField] private AudioClip gameplayMusic;
    [Range(0f, 1f)] [SerializeField] private float gameplayMusicVolume = 0.5f;

    [Header("SFX — Ayak Sesleri")]
    [SerializeField] private AudioClip[] footstepClips;
    [Range(0f, 1f)] [SerializeField] private float footstepVolume = 0.5f;

    [Header("SFX — Kalp Atışı (loop)")]
    [SerializeField] private AudioClip heartbeatClip;
    // HeartRateSystem bu temel volume'u korku oranıyla çarpar (%60-%120 arası dinamik aralık)
    [Range(0f, 1f)] [SerializeField] private float heartbeatBaseVolume = 0.5f;

    [Header("SFX — Baba Sayacı (tek seferlik)")]
    [SerializeField] private AudioClip fatherWarningStinger;
    [Range(0f, 1f)] [SerializeField] private float fatherWarningVolume = 0.5f;
    [SerializeField] private AudioClip doorKnockClip;
    [Range(0f, 1f)] [SerializeField] private float doorKnockVolume = 0.5f;

    [Header("SFX — Oyun Sonu")]
    [SerializeField] private AudioClip winSound;
    [Range(0f, 1f)] [SerializeField] private float winVolume = 0.5f;
    [SerializeField] private AudioClip loseSound;
    [Range(0f, 1f)] [SerializeField] private float loseVolume = 0.5f;

    [Header("SFX — Eşya Tutma")]
    [SerializeField] private AudioClip grabClip;
    [Range(0f, 1f)] [SerializeField] private float grabVolume = 0.5f;

    [Header("SFX — Eşya Sürükleme (loop)")]
    [SerializeField] private AudioClip dragClip;
    [Range(0f, 1f)] [SerializeField] private float dragVolume = 0.5f;

    [Header("SFX — Eşya Yere/Bir Yere Çarpma")]
    [SerializeField] private AudioClip dropClip;
    [Range(0f, 1f)] [SerializeField] private float dropVolume = 0.5f;

    [Header("SFX — Eşya Kapıdan Çıkış")]
    [SerializeField] private AudioClip exitClip;
    [Range(0f, 1f)] [SerializeField] private float exitVolume = 0.5f;

    [Header("Oyun Sonu Geçişi")]
    [SerializeField] private float musicFadeOutDuration = 1.2f;

    public AudioSource SFX => sfxSource;
    public AudioSource Music => musicSource;
    public AudioSource Ambience => ambienceSource;
    public AudioClip HeartbeatClip => heartbeatClip;
    public float HeartbeatBaseVolume => heartbeatBaseVolume;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Start()
    {
        PlayAmbience();
    }

    public void PlayAmbience()
    {
        if (ambienceClip == null || ambienceSource == null) return;
        ambienceSource.clip = ambienceClip;
        ambienceSource.loop = true;
        ambienceSource.volume = ambienceVolume;
        ambienceSource.Play();
    }

    public void PlayMenuMusic() => PlayMusic(menuMusic, menuMusicVolume);
    public void PlayGameplayMusic() => PlayMusic(gameplayMusic, gameplayMusicVolume);

    private void PlayMusic(AudioClip clip, float volume)
    {
        if (clip == null || musicSource == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.pitch = 1f;
        musicSource.volume = volume;
        musicSource.Play();
    }

    public void PlayRandomFootstep(float pitch)
    {
        if (footstepClips == null || footstepClips.Length == 0 || sfxSource == null) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        sfxSource.pitch = pitch;
        sfxSource.PlayOneShot(clip, footstepVolume);
    }

    public void PlayFatherWarning() => PlayOneShotSfx(fatherWarningStinger, fatherWarningVolume);
    public void PlayDoorKnock() => PlayOneShotSfx(doorKnockClip, doorKnockVolume);

    public void PlayGrab() => PlayOneShotSfx(grabClip, grabVolume);
    // intensity: çarpma hızına göre 0-1 arası şiddet çarpanı
    public void PlayDrop(float intensity = 1f) => PlayOneShotSfx(dropClip, dropVolume * Mathf.Clamp01(intensity));
    public void PlayExit() => PlayOneShotSfx(exitClip, exitVolume);

    public void StartDragLoop()
    {
        if (dragClip == null || dragLoopSource == null) return;
        if (dragLoopSource.isPlaying && dragLoopSource.clip == dragClip) return;

        dragLoopSource.clip = dragClip;
        dragLoopSource.loop = true;
        dragLoopSource.volume = dragVolume;
        dragLoopSource.Play();
    }

    public void StopDragLoop()
    {
        if (dragLoopSource != null) dragLoopSource.Stop();
    }

    private void PlayOneShotSfx(AudioClip clip, float volume)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, volume);
    }

    // Oyun bitince (kazanma/kaybetme): müzik ease-out ile söner, ardından ilgili ses çalar.
    public void FadeOutMusicAndPlayWin() => FadeOutMusicAndPlay(winSound, winVolume);
    public void FadeOutMusicAndPlayLose() => FadeOutMusicAndPlay(loseSound, loseVolume);

    // Sadece müziği ease-out ile kapatır, ardından hiçbir ses çalmaz
    // (örn. intro biterken menü müziğinin sönmesi için).
    public void FadeOutCurrentMusic(float duration)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutOnlyRoutine(duration));
    }

    private IEnumerator FadeOutOnlyRoutine(float duration)
    {
        if (musicSource == null || !musicSource.isPlaying) yield break;

        float startVolume = musicSource.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            float eased = 1f - Mathf.Pow(1f - k, 3f);
            musicSource.volume = Mathf.Lerp(startVolume, 0f, eased);
            yield return null;
        }

        musicSource.Stop();
    }

    private void FadeOutMusicAndPlay(AudioClip endClip, float endVolume)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeOutRoutine(endClip, endVolume));
    }

    private IEnumerator FadeOutRoutine(AudioClip endClip, float endVolume)
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float t = 0f;

            while (t < musicFadeOutDuration)
            {
                // Time.timeScale oyun bitince 0 olabilir — unscaled kullan ki fade donmasın
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / musicFadeOutDuration);
                float eased = 1f - Mathf.Pow(1f - k, 3f); // ease-out cubic
                musicSource.volume = Mathf.Lerp(startVolume, 0f, eased);
                yield return null;
            }

            musicSource.Stop();
        }

        PlayOneShotSfx(endClip, endVolume);
    }
}
