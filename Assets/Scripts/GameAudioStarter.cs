using UnityEngine;
using UnityEngine.SceneManagement;

public class GameAudioStarter : MonoBehaviour
{
    public static GameAudioStarter Instance { get; private set; }

    [Header("Music")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField] private AudioClip endingSong;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float endingSongVolume = 1f;
    [SerializeField] private bool playThemeOnStart = true;
    [SerializeField] private bool refreshThemeOnSceneLoad = true;
    [SerializeField] private bool loopBackgroundMusic = true;
    [SerializeField] private bool loopEndingSong = false;
    [SerializeField] private bool stopThemeWhenEndingStarts = true;

    [Header("Scene SFX")]
    [SerializeField] private AudioClip endTriggerSfx;
    [SerializeField, Range(0f, 1f)] private float endTriggerSfxVolume = 1f;
    [SerializeField] private bool playEndingSongOnEndTrigger = true;
    [SerializeField] private bool playEndTriggerAudioOnlyOnce = true;

    [Header("Player SFX")]
    [SerializeField] private AudioClip smashSwingSfx;
    [SerializeField] private AudioClip smashHitSfx;
    [SerializeField, Range(0f, 1f)] private float smashSwingSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float smashHitSfxVolume = 1f;

    private bool endTriggerAudioPlayed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance.ApplySettingsFrom(this);
            Instance.ApplySceneAudioSettings();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void Start()
    {
        ApplySceneAudioSettings();
    }

    private void OnDestroy()
    {
        if (Instance != this)
            return;

        SceneManager.sceneLoaded -= HandleSceneLoaded;
        Instance = null;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (refreshThemeOnSceneLoad)
            ApplySceneAudioSettings();
    }

    public static void PlayEndTriggerAudio(Vector3 position)
    {
        if (TryGetInstance(out GameAudioStarter audioStarter))
            audioStarter.PlayEndTrigger(position);
    }

    public static void PlaySmashSwing(Vector3 position)
    {
        if (TryGetInstance(out GameAudioStarter audioStarter))
            audioStarter.PlaySmashSwingSfx(position);
    }

    public static void PlaySmashHit(Vector3 position)
    {
        if (TryGetInstance(out GameAudioStarter audioStarter))
            audioStarter.PlaySmashHitSfx(position);
    }

    public void PlaySecondaryMusic()
    {
        PlayEndTrigger(transform.position);
    }

    private static bool TryGetInstance(out GameAudioStarter audioStarter)
    {
        if (Instance == null)
            Instance = FindAnyObjectByType<GameAudioStarter>();

        audioStarter = Instance;
        return audioStarter != null;
    }

    private void ApplySettingsFrom(GameAudioStarter sceneAudio)
    {
        backgroundMusic = sceneAudio.backgroundMusic;
        endingSong = sceneAudio.endingSong;
        backgroundMusicVolume = sceneAudio.backgroundMusicVolume;
        endingSongVolume = sceneAudio.endingSongVolume;
        playThemeOnStart = sceneAudio.playThemeOnStart;
        refreshThemeOnSceneLoad = sceneAudio.refreshThemeOnSceneLoad;
        loopBackgroundMusic = sceneAudio.loopBackgroundMusic;
        loopEndingSong = sceneAudio.loopEndingSong;
        stopThemeWhenEndingStarts = sceneAudio.stopThemeWhenEndingStarts;
        endTriggerSfx = sceneAudio.endTriggerSfx;
        endTriggerSfxVolume = sceneAudio.endTriggerSfxVolume;
        playEndingSongOnEndTrigger = sceneAudio.playEndingSongOnEndTrigger;
        playEndTriggerAudioOnlyOnce = sceneAudio.playEndTriggerAudioOnlyOnce;
        smashSwingSfx = sceneAudio.smashSwingSfx;
        smashHitSfx = sceneAudio.smashHitSfx;
        smashSwingSfxVolume = sceneAudio.smashSwingSfxVolume;
        smashHitSfxVolume = sceneAudio.smashHitSfxVolume;
        endTriggerAudioPlayed = false;
    }

    private void ApplySceneAudioSettings()
    {
        if (!playThemeOnStart || AudioManager.Instance == null)
            return;

        endTriggerAudioPlayed = false;
        AudioManager.Instance.StopSecondaryMusic();
        AudioManager.Instance.PlayMusic(backgroundMusic, backgroundMusicVolume, loopBackgroundMusic);
    }

    private void PlayEndTrigger(Vector3 position)
    {
        if (AudioManager.Instance == null)
            return;

        if (playEndTriggerAudioOnlyOnce && endTriggerAudioPlayed)
            return;

        endTriggerAudioPlayed = true;
        AudioManager.Instance.PlaySFXAtPosition(endTriggerSfx, position, endTriggerSfxVolume);

        if (!playEndingSongOnEndTrigger)
            return;

        if (stopThemeWhenEndingStarts)
            AudioManager.Instance.StopMusic();

        AudioManager.Instance.PlaySecondaryMusic(endingSong, endingSongVolume, loopEndingSong);
    }

    private void PlaySmashSwingSfx(Vector3 position)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPosition(smashSwingSfx, position, smashSwingSfxVolume);
    }

    private void PlaySmashHitSfx(Vector3 position)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFXAtPosition(smashHitSfx, position, smashHitSfxVolume);
    }
}
