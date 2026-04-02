using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject mobileHud;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button soundButton;


    [SerializeField] private GameObject wolfPack_1;
    [SerializeField] private GameObject wolfPack_2;

    [Header("Sound UI")]
    [SerializeField] private TextMeshProUGUI soundText; // or TMP_Text if using TextMeshPro

    private bool isPaused = false;
    private bool isSoundOn = true;

    private const string SOUND_KEY = "SOUND_ON";

    private void Start()
    {
        pausePanel.SetActive(false);

        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);

        if (soundButton != null)
            soundButton.onClick.AddListener(ToggleSound);

        // 🔥 Load saved state
        isSoundOn = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        AudioListener.volume = isSoundOn ? 1f : 0f;

        UpdateSoundUI();
    }

    // ======================
    // ⏸ PAUSE
    // ======================

    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        mobileHud.SetActive(false);
        if(wolfPack_1 != null) wolfPack_1.SetActive(false);
        if(wolfPack_2 != null) wolfPack_2.SetActive(false);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        mobileHud.SetActive(true);
        if(wolfPack_1 != null) wolfPack_1.SetActive(true);
        if(wolfPack_2 != null) wolfPack_2.SetActive(true);
        Time.timeScale = 1f;
    }

    // ======================
    // 🔊 SOUND (TEXT BUTTON)
    // ======================

    void ToggleSound()
    {
        isSoundOn = !isSoundOn;

        AudioListener.volume = isSoundOn ? 1f : 0f;

        PlayerPrefs.SetInt(SOUND_KEY, isSoundOn ? 1 : 0);
        PlayerPrefs.Save();

        UpdateSoundUI();
    }

    void UpdateSoundUI()
    {
        if (soundText != null)
        {
            soundText.text = isSoundOn ? "Sound : On" : "Sound : Off";
        }
    }

    public void Home()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}