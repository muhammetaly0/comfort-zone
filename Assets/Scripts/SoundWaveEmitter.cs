using UnityEngine;

// Sahneye bir boş GameObject'e ekle.
// soundWavePrefab: SoundWave.cs + LineRenderer olan bir prefab.
public class SoundWaveEmitter : MonoBehaviour
{
    [SerializeField] private GameObject soundWavePrefab;
    [SerializeField] private int waveCount = 2;         // aynı anda kaç halka
    [SerializeField] private float wavDelay = 0.12f;    // halkalar arası gecikme

    public void Emit(Vector3 worldPosition, float intensity)
    {
        for (int i = 0; i < waveCount; i++)
            StartCoroutine(SpawnDelayed(worldPosition, intensity, i * wavDelay));
    }

    private System.Collections.IEnumerator SpawnDelayed(Vector3 pos, float intensity, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        if (soundWavePrefab == null) yield break;
        GameObject go = Instantiate(soundWavePrefab, pos, Quaternion.identity);
        go.GetComponent<SoundWave>()?.Initialize(pos, intensity);
    }
}
