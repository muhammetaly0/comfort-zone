using UnityEngine;
using System;

public enum FurnitureSize { Small, Large }

[RequireComponent(typeof(Rigidbody))]
public class Furniture : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private bool isGrabbable = true;
    [SerializeField] private FurnitureSize size = FurnitureSize.Small;
    [SerializeField] private float weight = 1f;

    [Header("Large Object – Force Settings")]
    [SerializeField] private float slideForceMult = 40f;

    [Header("Noise on Impact")]
    [SerializeField] private float noiseImpactThreshold = 1.8f;
    [SerializeField] private float noiseIntensity = 1f;

    public bool IsGrabbable => isGrabbable;
    public FurnitureSize Size => size;
    public float Weight => weight;
    public Rigidbody Body { get; private set; }

    public event Action OnRemoved;

    private SoundWaveEmitter _soundEmitter;
    private bool _held;
    private bool _removed;

    private void Awake()
    {
        Body = GetComponent<Rigidbody>();
        _soundEmitter = FindAnyObjectByType<SoundWaveEmitter>();

        // Büyük objeler her zaman dik kalır (X/Z rotasyonu kilitli)
        if (size == FurnitureSize.Large)
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void OnGrabbed()
    {
        _held = true;

        if (size == FurnitureSize.Large)
        {
            Body.useGravity = false;
            // Y pozisyonu kilitli → kalkmaz, sadece zeminde kayar
            Body.constraints = RigidbodyConstraints.FreezePositionY
                             | RigidbodyConstraints.FreezeRotationX
                             | RigidbodyConstraints.FreezeRotationZ;
            Body.linearDamping = 2f;
            Body.angularDamping = 5f;
        }
        else
        {
            Body.useGravity = false;
            Body.constraints = RigidbodyConstraints.None;
            Body.linearDamping = 10f;
            Body.angularDamping = 8f;
        }
    }

    public void OnReleased()
    {
        _held = false;
        Body.useGravity = true;

        if (size == FurnitureSize.Large)
        {
            // Bırakılınca da dik kalmaya devam eder
            Body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            Body.linearDamping = 1f;
            Body.angularDamping = 2f;
        }
        else
        {
            Body.constraints = RigidbodyConstraints.None;
            Body.linearDamping = 0.05f;
            Body.angularDamping = 0.05f;
        }
    }

    // grabContact: oyuncunun objeye dokunduğu nokta (world space)
    // target: o noktanın gitmesi gereken yer (world space)
    public void PullToward(Vector3 grabContact, Vector3 target, float speed)
    {
        if (size == FurnitureSize.Large)
        {
            Vector3 force = target - grabContact;
            force.y = 0f; // dikey kuvvet yok
            // Kenardan tutunca tork oluşur → dolap doğal döner
            Body.AddForceAtPosition(
                force * (slideForceMult / Mathf.Max(weight, 0.1f)),
                grabContact,
                ForceMode.Force);
        }
        else
        {
            // Küçük: ağırlıktan bağımsız, hep hedefe yapışık kalır.
            // Ağırlık burada değil, MotherController'ın hareket/bakış hızında hissedilir.
            Vector3 dir = target - Body.position;
            Body.linearVelocity = dir * speed;
        }
    }

    public void Remove()
    {
        if (_removed) return;
        _removed = true;
        OnRemoved?.Invoke();
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision col)
    {
        if (_held || _removed) return;
        float impact = col.relativeVelocity.magnitude;
        if (impact <= noiseImpactThreshold) return;

        if (_soundEmitter != null)
            _soundEmitter.Emit(transform.position, impact * noiseIntensity);

        // Çarpma şiddetini 0-1 aralığına ölçekle (yüksek hızlarda tavan yapsın)
        float intensity = Mathf.Clamp01(impact / (noiseImpactThreshold * 4f));
        AudioManager.Instance?.PlayDrop(intensity);
    }
}
