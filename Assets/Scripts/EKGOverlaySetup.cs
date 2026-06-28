using UnityEngine;
using UnityEngine.Rendering.Universal;

// Bu scripti HeartRateSystem ile aynı objeye ekle.
// Layer 31'i otomatik kullanır (Unity'de varsayılan olarak boştur).
// Editor'da elle bir şey yapman gerekmez.
//
// ÖNEMLİ: URP'de kamera üst üste bindirme Built-in pipeline'dan farklı çalışır.
// Sadece clearFlags=Depth + yüksek depth değeri YETERLİ DEĞİL — overlay kamera
// resmi olarak ana kameranın Camera Stack'ine eklenmek zorunda, aksi halde
// kendi başına bağımsız render eder (skybox + sadece kendi layer'ı görünür,
// ana kamera "ezilir").
[DefaultExecutionOrder(-100)]
public class EKGOverlaySetup : MonoBehaviour
{
    private const int OverlayLayer = 31;

    private void Awake()
    {
        // Bu objeyi ve tüm child'larını overlay layer'a taşı
        SetLayerRecursive(gameObject, OverlayLayer);

        Camera main = Camera.main;
        if (main == null) return;

        // Ana kamera overlay layer'ı renderlamamalı
        main.cullingMask &= ~(1 << OverlayLayer);

        var mainData = main.GetUniversalAdditionalCameraData();
        if (mainData == null)
        {
            Debug.LogError("EKGOverlaySetup: Ana kamerada UniversalAdditionalCameraData yok " +
                           "— URP kullanılmıyor olabilir ya da ana kamera Base tipi değil.", this);
            return;
        }

        // Ana kameranın kesinlikle Base tipinde olması gerekiyor (stack'in temeli)
        mainData.renderType = CameraRenderType.Base;

        GameObject camObj = new GameObject("EKG Overlay Camera");
        camObj.transform.SetParent(main.transform);
        camObj.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        Camera cam = camObj.AddComponent<Camera>();
        cam.cullingMask   = 1 << OverlayLayer; // sadece EKG layer'ını çizer
        cam.nearClipPlane = 0.01f;
        cam.fieldOfView   = main.fieldOfView;

        // URP'ye özgü: bu kamerayı "Overlay" tipi yap ve ana kameranın
        // stack'ine ekle. Bu olmadan kamera bağımsız render eder.
        var overlayData = cam.GetUniversalAdditionalCameraData();
        overlayData.renderType = CameraRenderType.Overlay;

        mainData.cameraStack.Add(cam);
    }

    private static void SetLayerRecursive(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursive(child.gameObject, layer);
    }
}
