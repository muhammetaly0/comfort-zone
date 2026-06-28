using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.Events;
using TMPro;

// Menü (Oyna/Çıkış) + intro yazıları + HUD'u tamamen otomatik kurar,
// GameIntroSequence'a ve GameManager/HeartRateSystem/FatherTimer'a bağlar.
// Hiçbir UI elemanını elle sürüklemen gerekmez.
public static class IntroSetup
{
    [MenuItem("Tools/Kafkas/Intro + HUD Kur")]
    private static void Build()
    {
        GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
        HeartRateSystem heartRate = Object.FindAnyObjectByType<HeartRateSystem>();
        FatherTimer fatherTimer = Object.FindAnyObjectByType<FatherTimer>();
        MotherController mother = Object.FindAnyObjectByType<MotherController>();
        FurnitureGrab furnitureGrab = Object.FindAnyObjectByType<FurnitureGrab>();
        GregorAI gregorAI = Object.FindAnyObjectByType<GregorAI>();

        if (gameManager == null || heartRate == null || fatherTimer == null || mother == null)
        {
            Debug.LogError("Sahnede GameManager, HeartRateSystem, FatherTimer veya MotherController bulunamadı. " +
                            "Önce bunların hepsinin sahnede olduğundan emin ol.");
            return;
        }

        EnsureEventSystem();

        // IntroCanvas, MenüCanvas'ın ÜSTÜNDE render olmalı — siyah ekran
        // fade-in olurken menünün üstünü kapatması gerekiyor.
        Canvas menuCanvas = CreateCanvas("MenuCanvas", 20);
        CanvasGroup menuGroup = menuCanvas.gameObject.AddComponent<CanvasGroup>();

        Canvas introCanvas = CreateCanvas("IntroCanvas", 30);
        CanvasGroup introGroup = introCanvas.gameObject.AddComponent<CanvasGroup>();
        introCanvas.gameObject.SetActive(false);

        Canvas hudCanvas = CreateCanvas("HUDCanvas", 10);
        CanvasGroup hudGroup = hudCanvas.gameObject.AddComponent<CanvasGroup>();
        hudCanvas.gameObject.SetActive(false);

        // ---------------- MENÜ ----------------
        CreateFullscreenPanel(menuCanvas.transform, new Color(0f, 0f, 0f, 0.85f));
        Button playButton = CreateButton(menuCanvas.transform, "PlayButton", "OYNA", new Vector2(0f, 20f));
        Button exitButton = CreateButton(menuCanvas.transform, "ExitButton", "ÇIKIŞ", new Vector2(0f, -60f));

        // ---------------- INTRO (siyah ekran + yazılar) ----------------
        CreateFullscreenPanel(introCanvas.transform, Color.black);
        TMP_Text titleText = CreateText(introCanvas.transform, "TitleText", "",
            new Vector2(0f, 60f), new Vector2(1200f, 120f), 56, FontStyles.Bold);
        TMP_Text subtitleText = CreateText(introCanvas.transform, "SubtitleText", "",
            new Vector2(0f, -40f), new Vector2(1000f, 100f), 26, FontStyles.Normal);

        // ---------------- HUD ----------------
        TMP_Text questText = CreateAnchoredText(hudCanvas.transform, "QuestText", "Mobilya: 0 / 0",
            new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(300f, 50f), TextAlignmentOptions.TopLeft, 28);

        TMP_Text timerText = CreateAnchoredText(hudCanvas.transform, "TimerText", "00:00",
            new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(200f, 50f), TextAlignmentOptions.Top, 32);

        TMP_Text bpmText = CreateAnchoredText(hudCanvas.transform, "BpmText", "75",
            new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(150f, 50f), TextAlignmentOptions.BottomRight, 28);

        Image fearBarFill = CreateFearBar(hudCanvas.transform);

        // ---------------- GameIntroSequence ----------------
        GameObject introGO = new GameObject("GameIntroSequence", typeof(GameIntroSequence));
        GameIntroSequence intro = introGO.GetComponent<GameIntroSequence>();

        // Ayrı bir menü kamerası — Mother'ın kendi kamerasından bağımsız.
        // Konumu varsayılan bir tahmin; sahnede elle daha sinematik bir
        // açıya getirmen gerekecek.
        Camera mainCam = Camera.main;
        GameObject menuCamGO = new GameObject("MenuCamera", typeof(Camera), typeof(AudioListener));
        Camera menuCamera = menuCamGO.GetComponent<Camera>();
        if (mainCam != null)
        {
            menuCamGO.transform.position = mainCam.transform.position
                                          - mainCam.transform.forward * 3f
                                          + Vector3.up * 1.5f;
            menuCamGO.transform.rotation = Quaternion.LookRotation(mainCam.transform.forward, Vector3.up);
        }

        SerializedObject introSO = new SerializedObject(intro);
        SetRef(introSO, "menuCamera", menuCamera);
        SetRef(introSO, "motherObject", mother.gameObject);
        SetRef(introSO, "menuCanvasGroup", menuGroup);
        SetRef(introSO, "introCanvasGroup", introGroup);
        SetRef(introSO, "hudCanvasGroup", hudGroup);
        SetRef(introSO, "introTitleText", titleText);
        SetRef(introSO, "introSubtitleText", subtitleText);
        SetRef(introSO, "mother", mother);
        SetRef(introSO, "furnitureGrab", furnitureGrab);
        SetRef(introSO, "fatherTimer", fatherTimer);
        SetRef(introSO, "gregorAI", gregorAI);
        SetRef(introSO, "heartRate", heartRate);
        introSO.ApplyModifiedProperties();

        // Buton tıklamalarını bağla (persistent listener — Inspector'da görünür)
        UnityEventTools.AddVoidPersistentListener(playButton.onClick, intro.OnPlayPressed);
        UnityEventTools.AddVoidPersistentListener(exitButton.onClick, intro.OnExitPressed);

        // ---------------- HUD/Timer/Quest referanslarını mevcut scriptlere bağla ----------------
        BindField(gameManager, "questText", questText);
        BindField(heartRate, "bpmText", bpmText);
        BindField(heartRate, "fearBarFill", fearBarFill);
        BindField(fatherTimer, "timerText", timerText);

        Selection.activeGameObject = introGO;
        Debug.Log("Intro + HUD kuruldu. 'MenuCamera' objesinin konumunu/açısını sahnede elle ayarlamanı öneririm.");
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private static Canvas CreateCanvas(string name, int sortOrder)
    {
        GameObject go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        return canvas;
    }

    private static void CreateFullscreenPanel(Transform parent, Color color)
    {
        GameObject go = new GameObject("Background", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = color;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(240f, 64f);

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        Button button = go.AddComponent<Button>();
        button.targetGraphic = img;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return button;
    }

    private static TMP_Text CreateText(Transform parent, string name, string content,
        Vector2 anchoredPos, Vector2 size, float fontSize, FontStyles style)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tmp;
    }

    private static TMP_Text CreateAnchoredText(Transform parent, string name, string content,
        Vector2 anchor, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align, float fontSize)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;

        return tmp;
    }

    private static Image CreateFearBar(Transform parent)
    {
        GameObject container = new GameObject("FearBar", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 40f);
        containerRect.sizeDelta = new Vector2(420f, 28f);

        Image background = container.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject fillBG = new GameObject("FillBackground", typeof(RectTransform));
        fillBG.transform.SetParent(container.transform, false);
        RectTransform fillBGRect = fillBG.GetComponent<RectTransform>();
        fillBGRect.anchorMin = Vector2.zero;
        fillBGRect.anchorMax = Vector2.one;
        fillBGRect.offsetMin = new Vector2(4f, 4f);
        fillBGRect.offsetMax = new Vector2(-4f, -4f);
        fillBG.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillBG.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.05f, 0.05f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;

        return fillImage;
    }

    private static void SetRef(SerializedObject so, string fieldName, Object value)
    {
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null) prop.objectReferenceValue = value;
        else Debug.LogWarning($"GameIntroSequence'da '{fieldName}' alanı bulunamadı.");
    }

    private static void BindField(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning($"{target.GetType().Name}'da '{fieldName}' alanı bulunamadı.");
        }
    }
}
