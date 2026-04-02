using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;

    [SerializeField] private AudioSource secondaryMusicSource;
    [SerializeField] private AudioSource sfxPrefab;

    [Header("Pooling")]
    [SerializeField] private int poolSize = 10;
    private List<AudioSource> sfxPool = new List<AudioSource>();

    private int poolIndex = 0;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitPool();
    }

    void InitPool()
    {
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource src = Instantiate(sfxPrefab, transform);
            src.playOnAwake = false;
            sfxPool.Add(src);
        }
    }

    AudioSource GetAvailableSource()
    {
        AudioSource src = sfxPool[poolIndex];
        poolIndex = (poolIndex + 1) % poolSize;
        return src;
    }

    // ======================
    //  MUSIC
    // ======================

    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (musicSource.clip == clip) return;

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        musicSource.volume = volume;
    }

    // ======================
    // SFX
    // ======================

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        AudioSource src = GetAvailableSource();
        src.clip = clip;
        src.volume = volume;
        src.loop = false;
        src.Play();
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        AudioSource src = GetAvailableSource();
        src.transform.position = position;
        src.spatialBlend = 1f; // 3D sound
        src.clip = clip;
        src.volume = volume;
        src.loop = false;
        src.Play();
    }

    public void PlayLoopingSFX(AudioClip clip, out AudioSource loopSource, float volume = 1f)
    {
        loopSource = GetAvailableSource();
        loopSource.clip = clip;
        loopSource.volume = volume;
        loopSource.loop = true;
        loopSource.Play();
    }

    public void StopLoopingSFX(AudioSource source)
    {
        if (source != null)
            source.Stop();
    }


    // ======================
// 🎵 SECONDARY MUSIC
// ======================

public void PlaySecondaryMusic(AudioClip clip, float volume = 1f, bool loop = false)
{
    secondaryMusicSource.clip = clip;
    secondaryMusicSource.volume = volume;
    secondaryMusicSource.loop = loop;
    secondaryMusicSource.Play();
}

public void StopSecondaryMusic()
{
    secondaryMusicSource.Stop();
}


}