using UnityEngine;

// Yarı şeffaf bariyer duvarı için hafif, organik bir flicker (titreşim) efekti.
// Quad/Plane'e ekle, transparan yeşil materyal ata. Perlin noise kullanır,
// böylece sine dalgası gibi mekanik değil, ateş ışığı gibi düzensiz hisseder.
[RequireComponent(typeof(Renderer))]
public class BarrierWallEffect : MonoBehaviour
{
    [Header("Flicker")]
    [SerializeField] private float minAlpha = 0.12f;
    [SerializeField] private float maxAlpha = 0.30f;
    [SerializeField] private float flickerSpeed = 1.2f;

    private Renderer _renderer;
    private Color _baseColor;
    private float _noiseSeed;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _renderer.material = new Material(_renderer.sharedMaterial);
        _baseColor = _renderer.material.color;
        _noiseSeed = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float n = Mathf.PerlinNoise(_noiseSeed, Time.time * flickerSpeed);
        Color c = _baseColor;
        c.a = Mathf.Lerp(minAlpha, maxAlpha, n);
        _renderer.material.color = c;
    }
}
