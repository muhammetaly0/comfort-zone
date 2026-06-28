using UnityEngine;

// Her ses dalgası: zemin düzleminde yayılan daire.
// Line Renderer bu objeye ekli olmalı.
// SoundWaveEmitter bu prefab'ı spawn eder.
[RequireComponent(typeof(LineRenderer))]
public class SoundWave : MonoBehaviour
{
    [SerializeField] private int segments = 48;
    [SerializeField] private float expandSpeed = 4f;
    [SerializeField] private float maxRadius = 6f;
    [SerializeField] private float lifetime = 1.8f;

    private LineRenderer _lr;
    private float _radius;
    private float _age;
    private Color _startColor;
    private Color _endColor;

    public void Initialize(Vector3 position, float intensity)
    {
        transform.position = position + Vector3.up * 0.02f; // zeminin hemen üzeri
        expandSpeed *= Mathf.Clamp(intensity, 0.5f, 3f);
    }

    private void Awake()
    {
        _lr = GetComponent<LineRenderer>();
        _lr.useWorldSpace = true;
        _lr.loop = true;
        _lr.positionCount = segments;
        _startColor = _lr.startColor;
        _endColor = _lr.endColor;
    }

    private void Update()
    {
        _radius += expandSpeed * Time.deltaTime;
        _age += Time.deltaTime;

        float alpha = Mathf.Clamp01(1f - (_age / lifetime));
        _lr.startColor = new Color(_startColor.r, _startColor.g, _startColor.b, alpha);
        _lr.endColor   = new Color(_endColor.r,   _endColor.g,   _endColor.b,   alpha);

        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * _radius;
            float z = Mathf.Sin(angle) * _radius;
            _lr.SetPosition(i, transform.position + new Vector3(x, 0f, z));
        }

        if (_age >= lifetime || _radius >= maxRadius)
            Destroy(gameObject);
    }
}
