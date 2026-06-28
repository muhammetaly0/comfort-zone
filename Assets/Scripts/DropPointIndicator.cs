using UnityEngine;

// Zemine yatık bir Quad/Plane'e ekle. Mobilyanın taşınması gereken
// noktayı işaretleyen, nefes alır gibi büyüyüp küçülen yarı şeffaf gösterge.
[RequireComponent(typeof(Renderer))]
public class DropPointIndicator : MonoBehaviour
{
    [Header("Nefes Alma (Scale)")]
    [SerializeField] private float pulseSpeed = 1.5f;
    [SerializeField] private float minScale = 0.85f;
    [SerializeField] private float maxScale = 1.1f;

    [Header("Şeffaflık Pulse")]
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 0.55f;

    [Header("Dönüş (opsiyonel)")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private float rotateSpeed = 20f; // derece/saniye

    private Vector3 _baseScale;
    private Renderer _renderer;
    private Color _baseColor;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _renderer = GetComponent<Renderer>();

        // Paylaşılan materyal asset'ini bozmamak için instance kopya oluştur
        _renderer.material = new Material(_renderer.sharedMaterial);
        _baseColor = _renderer.material.color;
    }

    private void Update()
    {
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1

        transform.localScale = _baseScale * Mathf.Lerp(minScale, maxScale, t);

        Color c = _baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, t);
        _renderer.material.color = c;

        // World space'te döndürüyoruz ki obje hangi açıyla yatık konsa
        // da zemin düzleminde düzgün "spin" etkisi versin.
        if (rotate)
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }
}
