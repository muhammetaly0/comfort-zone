using UnityEngine;
using TMPro;
using System.Collections;

// Oyun açılış akışını yönetir:
// Menü (Oyna/Çıkış) -> ekran kararır (menü kamerası kapanır, Mother aktif olur)
// -> intro yazıları -> menü müziği ease-out -> kontrol oyuncuya
// -> ekran + yazılar fade out -> HUD aktif.
// Tüm referanslar IntroSetup.cs (Editor script) tarafından otomatik bağlanır.
[DefaultExecutionOrder(-100)]
public class GameIntroSequence : MonoBehaviour
{
    [Header("Kameralar / Oyuncu")]
    [SerializeField] private Camera menuCamera;     // menü/intro sırasında aktif olan ayrı kamera
    [SerializeField] private GameObject motherObject; // Mother'ın kök objesi (kendi kamerası + kontrolleri)

    [Header("Canvaslar")]
    [SerializeField] private CanvasGroup menuCanvasGroup;
    [SerializeField] private CanvasGroup introCanvasGroup; // siyah arka plan + yazılar burada
    [SerializeField] private CanvasGroup hudCanvasGroup;

    [Header("Ekran Kararma")]
    [SerializeField] private float screenFadeDuration = 0.7f;

    [Header("Intro Yazıları")]
    [SerializeField] private TMP_Text introTitleText;
    [SerializeField] private TMP_Text introSubtitleText;
    [SerializeField] private string titleLine = "Gregor'un Rahatlığını Sağla";
    [SerializeField] private float subtitleLineDuration = 3f;
    [SerializeField] private float subtitleFadeDuration = 0.4f;

    [TextArea]
    [SerializeField]
    private string[] subtitleLines =
    {
        "Baba gelmeden odadan mobilyaları çıkar",
        "Odada uzun süre kalmak ve Gregor'u görmek korku verir.",
        "Bayılmamak için korku seviyeni gözet, odanın dışında korkun azalır."
    };

    [Header("Menü Müziği")]
    [SerializeField] private float menuMusicFadeOutDuration = 2f; // intro bitmeye yakın ease-out

    [Header("Oyuncu Kontrolü (intro bitene kadar kapalı)")]
    [SerializeField] private MotherController mother;
    [SerializeField] private FurnitureGrab furnitureGrab;

    [Header("Oyun Mantığı (intro bitene kadar duraklasın)")]
    [SerializeField] private FatherTimer fatherTimer;
    [SerializeField] private GregorAI gregorAI;
    [SerializeField] private HeartRateSystem heartRate;

    private void Awake()
    {
        // Menü kamerası aktif, Mother (ve kendi kamerası) tamamen kapalı başlar.
        if (menuCamera != null) menuCamera.gameObject.SetActive(true);
        if (motherObject != null) motherObject.SetActive(false);

        if (furnitureGrab != null) furnitureGrab.enabled = false;
        if (fatherTimer != null) fatherTimer.enabled = false;
        if (gregorAI != null) gregorAI.enabled = false;
        if (heartRate != null) heartRate.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SetCanvasVisible(menuCanvasGroup, true);
        SetCanvasVisible(introCanvasGroup, false);
        SetCanvasVisible(hudCanvasGroup, false);
        SetTextAlpha(introTitleText, 0f);
        SetTextAlpha(introSubtitleText, 0f);
    }

    private void Start()
    {
        AudioManager.Instance?.PlayMenuMusic();
    }

    // Play butonunun OnClick'ine bağlanır (IntroSetup.cs tarafından otomatik)
    public void OnPlayPressed() => StartCoroutine(IntroRoutine());

    // Exit butonunun OnClick'ine bağlanır
    public void OnExitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private IEnumerator IntroRoutine()
    {
        SetInteractable(menuCanvasGroup, false);

        // 1. Ekran kararır (siyah arka plan fade in) — menü görünür kalır,
        // siyahlık onun üstüne biner ve gizler.
        if (introCanvasGroup != null)
        {
            introCanvasGroup.gameObject.SetActive(true);
            introCanvasGroup.alpha = 0f;
        }
        yield return Fade(introCanvasGroup, 0f, 1f, screenFadeDuration);

        // 2. Ekran tam siyah: menü canvası kapanır, MENÜ KAMERASI deaktif
        // olur, MOTHER (kendi kamerasıyla) aktif olur — hepsi siyah ekran
        // arkasında, görünmeden.
        if (menuCanvasGroup != null) menuCanvasGroup.gameObject.SetActive(false);
        if (menuCamera != null) menuCamera.gameObject.SetActive(false);
        if (motherObject != null) motherObject.SetActive(true);

        // 3. Başlık + alt yazılar (siyah ekran üzerinde)
        if (introTitleText != null) introTitleText.text = titleLine;
        yield return FadeText(introTitleText, 0f, 1f, subtitleFadeDuration);

        for (int i = 0; i < subtitleLines.Length; i++)
        {
            // Son satıra geçerken menü müziği ease-out ile kapanmaya başlar —
            // intronun bitimiyle aynı zamanda sönmüş olur.
            if (i == subtitleLines.Length - 1)
                AudioManager.Instance?.FadeOutCurrentMusic(menuMusicFadeOutDuration);

            if (introSubtitleText != null)
            {
                introSubtitleText.text = subtitleLines[i];
                yield return FadeText(introSubtitleText, 0f, 1f, subtitleFadeDuration);
            }
            yield return new WaitForSeconds(subtitleLineDuration);

            if (introSubtitleText != null)
                yield return FadeText(introSubtitleText, 1f, 0f, subtitleFadeDuration);
        }

        // 4. Yazılar bitti — kontrol oyuncuya geçer (ekran hâlâ siyah)
        GiveControlToPlayer();

        // 5. Siyah ekran + kalan yazılar (başlık) birlikte fade out
        yield return Fade(introCanvasGroup, 1f, 0f, screenFadeDuration);
        if (introCanvasGroup != null) introCanvasGroup.gameObject.SetActive(false);

        // 6. HUD aktif olur
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.gameObject.SetActive(true);
            hudCanvasGroup.alpha = 0f;
        }
        yield return Fade(hudCanvasGroup, 0f, 1f, screenFadeDuration);
    }

    private void GiveControlToPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (fatherTimer != null) fatherTimer.enabled = true;
        if (gregorAI != null) gregorAI.enabled = true;
        if (heartRate != null) heartRate.enabled = true; // OnEnable burada gameplay müziğini başlatır

        if (mother != null) mother.enabled = true;
        if (furnitureGrab != null) furnitureGrab.enabled = true;
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float t = 0f;
        group.alpha = from;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        group.alpha = to;
    }

    private IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
    {
        if (text == null) yield break;

        float t = 0f;
        SetTextAlpha(text, from);

        while (t < duration)
        {
            t += Time.deltaTime;
            SetTextAlpha(text, Mathf.Lerp(from, to, t / duration));
            yield return null;
        }
        SetTextAlpha(text, to);
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null) return;
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    private void SetCanvasVisible(CanvasGroup group, bool visible)
    {
        if (group == null) return;
        group.gameObject.SetActive(visible);
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    private void SetInteractable(CanvasGroup group, bool interactable)
    {
        if (group == null) return;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }
}
