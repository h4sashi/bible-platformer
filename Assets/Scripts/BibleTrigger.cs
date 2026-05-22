using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class BibleTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        GlideZoneOn,
        GlideZoneOff,
        WaterFountain,
        RockObstacle,
    }

    public TriggerType triggerType;

    [TextArea]
    public string verse;

    public TextMeshProUGUI verseText;
    public GameObject bottomContainer;

    public float typingSpeed = 0.04f;

    [Header("Verse Audio")]
    [SerializeField]
    private AudioClip verseAudioClip;

    [SerializeField]
    private AudioSource verseAudioSource;

    [SerializeField, Range(0f, 1f)]
    private float verseAudioVolume = 1f;

    private Coroutine typingRoutine;
    private bool hasTriggered;

    public event Action OnTypingFinishEvent;

    public UnityEvent OnTypeFinishEvent;

    public UnityEvent enableEvents;

    public UnityEvent disableEvents;

    [Header("Wolf Encounter")]
    [SerializeField]
    private bool activateWolvesSequentially = false;

    [SerializeField]
    private Transform wolfPack;

    [SerializeField]
    private WolfFSM[] wolves;

    [SerializeField]
    private float nextWolfActivationDelay = 0.5f;

    [Header("Wolf Pack Clear")]
    [SerializeField]
    private bool liftBlockadeWhenWolfPackCleared = true;

    [SerializeField]
    private GameObject blockade;

    public UnityEvent wolfPackClearedEvents;

    private int currentWolfIndex = -1;
    private bool wolfEncounterStarted;
    private bool wolfPackCleared;
    private Coroutine nextWolfRoutine;
    private readonly HashSet<WolfFSM> defeatedWolves = new HashSet<WolfFSM>();

    [Header("Checkpoint")]
    [SerializeField]
    private float checkpointRespawnDistance = 3f;

    private void OnEnable()
    {
        OnTypingFinishEvent += DisableUI;
    }

    private void OnDisable()
    {
        OnTypingFinishEvent -= DisableUI;
        UnsubscribeFromWolves();
    }

    private void Start()
    {
        PrepareWolfEncounter();

        if (!activateWolvesSequentially)
        {
            SubscribeToWolves();
        }
    }

    private void DisableUI()
    {
        StartCoroutine(delayDisableUI());
    }

    IEnumerator delayDisableUI()
    {
        yield return new WaitForSeconds(3f);
        verseText.text = "";
        bottomContainer.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        SavePlayerCheckpoint(other);

        if (hasTriggered)
            return;

        hasTriggered = true;

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeVerse());

        if (triggerType == TriggerType.GlideZoneOn)
        {
            enableEvents?.Invoke();
            disableEvents?.Invoke();
        }

        StartWolfEncounter();
    }

    private void SavePlayerCheckpoint(Collider playerCollider)
    {
        PlayerScript playerScript = playerCollider.GetComponentInParent<PlayerScript>();
        if (playerScript == null)
            return;

        Vector3 respawnPosition = GetCheckpointRespawnPosition(playerScript.transform);
        playerScript.SaveCheckpoint(respawnPosition, playerScript.transform.rotation);
    }

    private Vector3 GetCheckpointRespawnPosition(Transform playerTransform)
    {
        float respawnDistance = Mathf.Max(0f, checkpointRespawnDistance);
        Vector3 directionFromTrigger = playerTransform.position - transform.position;
        directionFromTrigger.y = 0f;

        if (directionFromTrigger.sqrMagnitude <= 0.001f)
        {
            directionFromTrigger = -playerTransform.forward;
            directionFromTrigger.y = 0f;
        }

        if (directionFromTrigger.sqrMagnitude <= 0.001f)
            directionFromTrigger = Vector3.back;

        Vector3 respawnPosition =
            transform.position + directionFromTrigger.normalized * respawnDistance;
        respawnPosition.y = playerTransform.position.y;
        return respawnPosition;
    }

    IEnumerator TypeVerse()
    {
        yield return TypeVerseWithAudio();
    }

    private IEnumerator TypeVerseWithAudio()
    {
        AudioSource audioSource = PlayVerseAudio();

        bottomContainer.SetActive(true);
        verseText.text = verse;
        verseText.ForceMeshUpdate();
        verseText.maxVisibleCharacters = 0;

        int characterCount = verseText.textInfo.characterCount;
        float audioDuration = GetVerseAudioDuration(audioSource);

        if (characterCount > 0)
        {
            if (audioDuration > 0f)
                yield return RevealVerseOverDuration(characterCount, audioDuration);
            else
                yield return RevealVerseAtTypingSpeed(characterCount);
        }

        verseText.maxVisibleCharacters = characterCount;

        OnTypingFinishEvent?.Invoke();
        OnTypeFinishEvent?.Invoke();
    }

    public void CallNormalTypeVerse()
    {
        StartCoroutine(TypeVerse());
    }

    IEnumerator TypeVerseAux()
    {
        yield return new WaitForSeconds(6f);

        yield return TypeVerseWithAudio();
    }

    public void CallTyping()
    {
        Debug.Log("Called TypeVerse()");

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeVerse());
    }

    public void CallTypingAux()
    {
        Debug.Log("Called CallTypingAux()");

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeVerseAux());
    }

    private IEnumerator RevealVerseOverDuration(int characterCount, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            verseText.maxVisibleCharacters = Mathf.FloorToInt(progress * characterCount);
            yield return null;
        }
    }

    private IEnumerator RevealVerseAtTypingSpeed(int characterCount)
    {
        for (int i = 1; i <= characterCount; i++)
        {
            verseText.maxVisibleCharacters = i;
            yield return new WaitForSeconds(typingSpeed);
        }
    }

    private float GetVerseAudioDuration(AudioSource audioSource)
    {
        if (audioSource == null || audioSource.clip == null)
            return 0f;

        float playbackSpeed = Mathf.Abs(audioSource.pitch);
        if (playbackSpeed <= 0f)
            return 0f;

        return audioSource.clip.length / playbackSpeed;
    }

    private AudioSource PlayVerseAudio()
    {
        if (verseAudioClip == null)
            return null;

        AudioSource audioSource = GetVerseAudioSource();
        audioSource.Stop();
        audioSource.clip = verseAudioClip;
        audioSource.volume = verseAudioVolume;
        audioSource.loop = false;
        audioSource.Play();
        return audioSource;
    }

    private AudioSource GetVerseAudioSource()
    {
        if (verseAudioSource != null)
            return verseAudioSource;

        verseAudioSource = gameObject.AddComponent<AudioSource>();
        verseAudioSource.playOnAwake = false;
        verseAudioSource.spatialBlend = 0f;
        return verseAudioSource;
    }

    private void StartWolfEncounter()
    {
        if (!activateWolvesSequentially || wolfEncounterStarted)
            return;

        PrepareWolfEncounter();

        if (wolves == null || wolves.Length == 0)
            return;

        if (wolfPack != null)
            wolfPack.gameObject.SetActive(true);

        wolfEncounterStarted = true;
        currentWolfIndex = -1;

        ActivateNextWolf();
    }

    private void PrepareWolfEncounter()
    {
        if (!activateWolvesSequentially || wolfEncounterStarted)
            return;

        CacheWolves();

        if (wolves == null)
            return;

        for (int i = 0; i < wolves.Length; i++)
        {
            if (wolves[i] == null)
                continue;

            wolves[i].Died -= HandleWolfDied;
            wolves[i].gameObject.SetActive(false);
        }
    }

    private void CacheWolves()
    {
        if (wolfPack == null && transform.parent != null)
            wolfPack = transform.parent.Find("WolfPack");

        if ((wolves == null || wolves.Length == 0) && wolfPack != null)
            wolves = wolfPack.GetComponentsInChildren<WolfFSM>(true);
    }

    private void ActivateNextWolf()
    {
        currentWolfIndex++;

        while (wolves != null && currentWolfIndex < wolves.Length && wolves[currentWolfIndex] == null)
            currentWolfIndex++;

        if (wolves == null || currentWolfIndex >= wolves.Length)
            return;

        WolfFSM wolf = wolves[currentWolfIndex];
        wolf.Died -= HandleWolfDied;
        wolf.Died += HandleWolfDied;
        wolf.gameObject.SetActive(true);
    }

    private void HandleWolfDied(WolfFSM wolf)
    {
        if (wolf != null)
        {
            wolf.Died -= HandleWolfDied;
            defeatedWolves.Add(wolf);
        }

        if (TryLiftBlockadeIfWolfPackCleared())
            return;

        if (!activateWolvesSequentially)
            return;

        if (nextWolfRoutine != null)
            StopCoroutine(nextWolfRoutine);

        nextWolfRoutine = StartCoroutine(ActivateNextWolfAfterDelay());
    }

    private IEnumerator ActivateNextWolfAfterDelay()
    {
        if (nextWolfActivationDelay > 0f)
            yield return new WaitForSeconds(nextWolfActivationDelay);

        ActivateNextWolf();
        nextWolfRoutine = null;
    }

    private void UnsubscribeFromWolves()
    {
        if (wolves == null)
            return;

        for (int i = 0; i < wolves.Length; i++)
        {
            if (wolves[i] != null)
                wolves[i].Died -= HandleWolfDied;
        }
    }

    private void SubscribeToWolves()
    {
        CacheWolves();

        if (wolves == null)
            return;

        for (int i = 0; i < wolves.Length; i++)
        {
            if (wolves[i] == null)
                continue;

            wolves[i].Died -= HandleWolfDied;
            wolves[i].Died += HandleWolfDied;
        }
    }

    private bool TryLiftBlockadeIfWolfPackCleared()
    {
        if (!liftBlockadeWhenWolfPackCleared || wolfPackCleared)
            return wolfPackCleared;

        CacheWolves();

        if (wolves == null || wolves.Length == 0)
            return false;

        for (int i = 0; i < wolves.Length; i++)
        {
            if (
                wolves[i] != null
                && !wolves[i].IsDead
                && !defeatedWolves.Contains(wolves[i])
            )
                return false;
        }

        wolfPackCleared = true;

        if (blockade != null)
            blockade.SetActive(false);

        wolfPackClearedEvents?.Invoke();
        return true;
    }
}
