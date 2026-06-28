using UnityEngine;

// Gregor'un odasını kaplayan bir Trigger Collider'a ekle (Is Trigger ✓).
// Anne bu alana girip çıktıkça HeartRateSystem'e bildirir — odada kaldıkça
// korku yavaşça birikir, dışarı çıkınca hızla boşalır.
public class RoomFearZone : MonoBehaviour
{
    [SerializeField] private HeartRateSystem heartRate;

    private void Awake()
    {
        if (heartRate == null)
            Debug.LogError($"{name}: 'Heart Rate' alanı boş! Oda korkusu hiç çalışmayacak.", this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (heartRate == null) return;
        if (other.GetComponent<MotherController>() != null)
            heartRate.SetInRoom(true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (heartRate == null) return;
        if (other.GetComponent<MotherController>() != null)
            heartRate.SetInRoom(false);
    }
}
