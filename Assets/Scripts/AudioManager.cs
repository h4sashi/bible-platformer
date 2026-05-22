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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureCoreSources();
        InitPool();
    }

    private void EnsureCoreSources()
    {
        musicSource = EnsureAudioSource(musicSource, "Music Source");
        secondaryMusicSource = EnsureAudioSource(secondaryMusicSource, "Secondary Music Source");

        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f;
        secondaryMusicSource.playOnAwake = false;
        secondaryMusicSource.spatialBlend = 0f;
    }

    private AudioSource EnsureAudioSource(AudioSource source, string sourceName)
    {
        if (source != null)
            return source;

        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform);
        sourceObject.transform.localPosition = Vector3.zero;
        return sourceObject.AddComponent<AudioSource>();
    }

    private void InitPool()
    {
        if (sfxPool.Count > 0)
            return;

        int sourceCount = Mathf.Max(1, poolSize);
        poolSize = sourceCount;

        for (int i = 0; i < sourceCount; i++)
        {
            AudioSource src = CreateSfxSource(i);
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            sfxPool.Add(src);
        }
    }

    private AudioSource CreateSfxSource(int index)
    {
        AudioSource source;

        if (sfxPrefab != null)
        {
            source = Instantiate(sfxPrefab, transform);
            source.name = $"SFX Source {index + 1}";
        }
        else
        {
            GameObject sourceObject = new GameObject($"SFX Source {index + 1}");
            sourceObject.transform.SetParent(transform);
            sourceObject.transform.localPosition = Vector3.zero;
            source = sourceObject.AddComponent<AudioSource>();
        }

        return source;
    }

    private AudioSource GetAvailableSource()
    {
        if (sfxPool.Count == 0)
            InitPool();

        AudioSource src = sfxPool[poolIndex];
        poolIndex = (poolIndex + 1) % sfxPool.Count;
        return src;
    }

    // ======================
    //  MUSIC
    // ======================

    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip == clip)
        {
            musicSource.volume = volume;
            musicSource.loop = loop;

            if (!musicSource.isPlaying)
                musicSource.Play();

            return;
        }

        musicSource.clip = clip;
        musicSource.volume = volume;
        musicSource.loop = loop;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource != null)
            musicSource.Stop();
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
            musicSource.volume = volume;
    }

    // ======================
    // SFX
    // ======================

    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return;

        AudioSource src = GetAvailableSource();
        src.transform.localPosition = Vector3.zero;
        src.spatialBlend = 0f;
        src.clip = clip;
        src.volume = volume;
        src.loop = false;
        src.Play();
    }

    public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null)
            return;

        AudioSource src = GetAvailableSource();
        src.transform.position = position;
        src.spatialBlend = 1f;
        src.clip = clip;
        src.volume = volume;
        src.loop = false;
        src.Play();
    }

    public void PlayLoopingSFX(AudioClip clip, out AudioSource loopSource, float volume = 1f)
    {
        if (clip == null)
        {
            loopSource = null;
            return;
        }

        loopSource = GetAvailableSource();
        loopSource.spatialBlend = 0f;
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
    // SECONDARY MUSIC
    // ======================

    public void PlaySecondaryMusic(AudioClip clip, float volume = 1f, bool loop = false)
    {
        if (clip == null || secondaryMusicSource == null)
            return;

        if (secondaryMusicSource.clip == clip)
        {
            secondaryMusicSource.volume = volume;
            secondaryMusicSource.loop = loop;

            if (!secondaryMusicSource.isPlaying)
                secondaryMusicSource.Play();

            return;
        }

        secondaryMusicSource.clip = clip;
        secondaryMusicSource.volume = volume;
        secondaryMusicSource.loop = loop;
        secondaryMusicSource.Play();
    }

    public void StopSecondaryMusic()
    {
        if (secondaryMusicSource != null)
            secondaryMusicSource.Stop();
    }
}
