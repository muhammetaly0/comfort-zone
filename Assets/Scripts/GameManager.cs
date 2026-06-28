using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Furniture — Rastgele Görev Sayısı")]
    [SerializeField] private int minFurnitureGoal = 6;
    [SerializeField] private int maxFurnitureGoal = 8; // dahil (Random.Range max+1 ile çağrılır)

    [Header("Quest HUD")]
    [SerializeField] private TMP_Text questText;
    [SerializeField] private string questFormat = "Mobilya: {0} / {1}";

    [Header("References")]
    [SerializeField] private HeartRateSystem heartRate;
    [SerializeField] private FatherTimer fatherTimer;
    [SerializeField] private MotherController mother;

    [Header("End Screens")]
    [SerializeField] private GameObject winUI;
    [SerializeField] private GameObject faintUI;
    [SerializeField] private GameObject timeUpUI;

    private int _furnitureRemoved;
    private int _totalFurnitureGoal;
    private bool _gameOver;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
    }

    private void Update()
    {
        // Oyun bittiyse herhangi bir tuşa basınca sahne sıfırlanıp baştan başlar.
        // Ayrı bir menü sahnesi yok — her şey aynı sahnede dönüyor.
        if (_gameOver && Input.anyKeyDown)
            Restart();
    }

    private void Start()
    {
        if (heartRate != null) heartRate.OnFaint += TriggerFaint;
        if (fatherTimer != null) fatherTimer.OnTimeUp += TriggerTimeUp;

        // Her level başında 4-6 (dahil) arası rastgele bir görev sayısı seçilir
        _totalFurnitureGoal = Random.Range(minFurnitureGoal, maxFurnitureGoal + 1);
        UpdateQuestText();
    }

    public void OnFurnitureRemoved()
    {
        _furnitureRemoved++;
        UpdateQuestText();

        if (_furnitureRemoved >= _totalFurnitureGoal)
            TriggerWin();
    }

    private void UpdateQuestText()
    {
        if (questText != null)
            questText.text = string.Format(questFormat, _furnitureRemoved, _totalFurnitureGoal);
    }

    private void TriggerWin()
    {
        if (_gameOver) return;
        _gameOver = true;
        AudioManager.Instance?.FadeOutMusicAndPlayWin();
        SetEndScreen(winUI);
    }

    private void TriggerFaint()
    {
        if (_gameOver) return;
        _gameOver = true;
        if (mother != null) mother.enabled = false;
        AudioManager.Instance?.FadeOutMusicAndPlayLose();
        SetEndScreen(faintUI);
    }

    private void TriggerTimeUp()
    {
        if (_gameOver) return;
        _gameOver = true;
        AudioManager.Instance?.FadeOutMusicAndPlayLose();
        SetEndScreen(timeUpUI);
    }

    private void SetEndScreen(GameObject screen)
    {
        if (screen != null) screen.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
