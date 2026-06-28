using UnityEngine;

// Bu scripti kapının dışında bir Trigger Collider'a ekle.
// Furniture bu collider'ın içine girince oda dışına çıkmış sayılır.
public class FurnitureExitTrigger : MonoBehaviour
{
    [Header("Görsel Efekt (opsiyonel)")]
    [SerializeField] private ParticleSystem flameBurst;
    [SerializeField] private int burstCount = 40;

    private void OnTriggerEnter(Collider other)
    {
        Furniture f = other.GetComponentInParent<Furniture>();
        if (f == null) return;

        f.Remove();
        GameManager.Instance.OnFurnitureRemoved();
        AudioManager.Instance?.PlayExit();

        if (flameBurst != null)
            flameBurst.Emit(burstCount);
    }
}
