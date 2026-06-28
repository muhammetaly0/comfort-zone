using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using System.Collections;

// Mobilya kapıdan çıkınca kamera spline üzerinde kısa bir sinematik hareket yapar.
// firstPersonCamera: oyuncunun kamerası (devre dışı kalır)
// cinematicCamera: sinematik kamera (aktif olur)
// transitionSpline: kameranın izleyeceği SplineContainer
public class CameraSplineTransition : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera firstPersonCamera;
    [SerializeField] private Camera cinematicCamera;

    [Header("Spline")]
    [SerializeField] private SplineContainer transitionSpline;
    [SerializeField] private float travelDuration = 1.8f;
    [SerializeField] private AnimationCurve speedCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Look At")]
    [SerializeField] private Transform lookAtTarget; // mobilyanın çıkış noktası

    private bool _busy;

    // Furniture'ın OnRemoved event'ine ya da FurnitureExitTrigger'dan çağır
    public void PlayTransition()
    {
        if (_busy || transitionSpline == null) return;
        StartCoroutine(DoTransition());
    }

    private IEnumerator DoTransition()
    {
        _busy = true;

        // Oyuncu kontrolünü durdur
        if (firstPersonCamera != null) firstPersonCamera.gameObject.SetActive(false);
        if (cinematicCamera != null)   cinematicCamera.gameObject.SetActive(true);

        float elapsed = 0f;

        while (elapsed < travelDuration)
        {
            elapsed += Time.deltaTime;
            float t = speedCurve.Evaluate(Mathf.Clamp01(elapsed / travelDuration));

            SplineUtility.Evaluate(
                transitionSpline.Spline,
                t,
                out float3 pos,
                out float3 tangent,
                out _);

            if (cinematicCamera != null)
            {
                cinematicCamera.transform.position = pos;

                Vector3 lookDir = lookAtTarget != null
                    ? (lookAtTarget.position - (Vector3)pos).normalized
                    : (Vector3)tangent;

                if (lookDir != Vector3.zero)
                    cinematicCamera.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            yield return null;
        }

        // Oyuncuya geri dön
        if (cinematicCamera != null)   cinematicCamera.gameObject.SetActive(false);
        if (firstPersonCamera != null) firstPersonCamera.gameObject.SetActive(true);

        _busy = false;
    }
}
