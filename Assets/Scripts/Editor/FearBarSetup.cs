using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

// Ekranın orta-üstünde bir korku barı (arka plan + dolum) oluşturup
// HeartRateSystem'in fearBarFill alanına otomatik bağlar.
public static class FearBarSetup
{
    [MenuItem("Tools/Kafkas/Korku Barı Oluştur")]
    private static void CreateFearBar()
    {
        HeartRateSystem heartRate = Object.FindAnyObjectByType<HeartRateSystem>();
        if (heartRate == null)
        {
            Debug.LogError("Sahnede HeartRateSystem bulunamadı. Önce onu sahneye ekle.");
            return;
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.EventSystems.StandaloneInputModule));
        }

        // --- Container (konumlandırma + boyut) ---
        GameObject container = new GameObject("FearBar", typeof(RectTransform));
        container.transform.SetParent(canvas.transform, false);

        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0f);
        containerRect.anchorMax = new Vector2(0.5f, 0f);
        containerRect.pivot = new Vector2(0.5f, 0f);
        containerRect.anchoredPosition = new Vector2(0f, 40f);
        containerRect.sizeDelta = new Vector2(420f, 28f);

        // --- Arka plan ---
        Image background = container.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.55f);

        // --- Dolum çerçevesi (kenar payı ile) ---
        GameObject fillBG = new GameObject("FillBackground", typeof(RectTransform));
        fillBG.transform.SetParent(container.transform, false);
        RectTransform fillBGRect = fillBG.GetComponent<RectTransform>();
        fillBGRect.anchorMin = Vector2.zero;
        fillBGRect.anchorMax = Vector2.one;
        fillBGRect.offsetMin = new Vector2(4f, 4f);
        fillBGRect.offsetMax = new Vector2(-4f, -4f);

        Image fillBGImage = fillBG.AddComponent<Image>();
        fillBGImage.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // --- Dolum (Image Type = Filled) ---
        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillBG.transform, false);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.05f, 0.05f, 1f); // kırmızı, korku rengi
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.fillAmount = 0f;

        // --- HeartRateSystem'in private fearBarFill alanına bağla ---
        SerializedObject so = new SerializedObject(heartRate);
        SerializedProperty prop = so.FindProperty("fearBarFill");
        if (prop != null)
        {
            prop.objectReferenceValue = fillImage;
            so.ApplyModifiedProperties();
        }
        else
        {
            Debug.LogWarning("HeartRateSystem'de 'fearBarFill' alanı bulunamadı — elle bağlaman gerekebilir.");
        }

        Undo.RegisterCreatedObjectUndo(canvas.gameObject.transform.Find("FearBar") != null
            ? container : canvas.gameObject, "Korku Barı Oluştur");

        Selection.activeGameObject = container;
        Debug.Log("Korku barı oluşturuldu ve HeartRateSystem'e bağlandı.");
    }
}
