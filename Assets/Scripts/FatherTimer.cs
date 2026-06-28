using UnityEngine;
using UnityEngine.Events;
using TMPro;

public class FatherTimer : MonoBehaviour
{
    [Header("Time")]
    [SerializeField] private float totalSeconds = 180f;
    [SerializeField] private float warningAt = 60f;

    [Header("HUD")]
    [SerializeField] private TMP_Text timerText; // ekranın üstünde, "dakika:saniye" formatında

    [Header("Events")]
    public UnityEvent OnWarning;
    public event System.Action OnTimeUp;

    private float _remaining;
    private bool _warningFired;
    private bool _done;

    public float Remaining => _remaining;
    public float Progress => 1f - _remaining / totalSeconds;

    private void Start()
    {
        _remaining = totalSeconds;
    }

    private void Update()
    {
        if (_done) return;

        _remaining -= Time.deltaTime;

        if (!_warningFired && _remaining <= warningAt)
        {
            _warningFired = true;
            OnWarning?.Invoke();
            AudioManager.Instance?.PlayFatherWarning();
        }

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _done = true;
            AudioManager.Instance?.PlayDoorKnock();
            OnTimeUp?.Invoke();
        }

        UpdateTimerText();
    }

    private void UpdateTimerText()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(_remaining / 60f);
        int seconds = Mathf.FloorToInt(_remaining % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void Pause() => enabled = false;
    public void Resume() => enabled = true;
}
