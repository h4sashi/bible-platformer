using UnityEngine;

public class GameAudioStarter : MonoBehaviour
{
    [SerializeField]
    private AudioClip backgroundMusic;

    [SerializeField]
    private AudioClip endingSong;

    void Start()
    {
        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlayMusic(backgroundMusic);
    }

    public void PlaySecondaryMusic()
    {
        AudioManager.Instance.StopMusic();
        AudioManager.Instance.PlaySecondaryMusic(endingSong);
    }
}
