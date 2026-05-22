using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMenu : MonoBehaviour
{
    private bool isSoundOn = true;

    public GameObject soundOn,
        soundOff;

    public Button soundButton;

    private const string SOUND_KEY = "SOUND_ON";

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Start()
    {
        if (soundButton != null)
            soundButton.onClick.AddListener(ToggleSound);

        // 🔥 Load saved state
        isSoundOn = PlayerPrefs.GetInt(SOUND_KEY, 1) == 1;
        AudioListener.volume = isSoundOn ? 1f : 0f;

        UpdateSoundUI();
    }

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
        if (soundOn != null && soundOff != null && isSoundOn == true)
        {
            soundOn.gameObject.SetActive(true);
            soundOff.gameObject.SetActive(false);
        }
        else if (soundOn != null && soundOff != null && isSoundOn == false)
        {
            soundOn.gameObject.SetActive(false);
            soundOff.gameObject.SetActive(true);
        }
    }

    public void StartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Main");
    }

    // Update is called once per frame
    public void QuitGame()
    {
        Application.Quit();
    }
}
