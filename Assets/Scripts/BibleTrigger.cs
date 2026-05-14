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

    IEnumerator TypeVerse()
    {
        bottomContainer.SetActive(true);
        verseText.text = "";

        foreach (char c in verse)
        {
            verseText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

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

        bottomContainer.SetActive(true);
        verseText.text = "";

        foreach (char c in verse)
        {
            verseText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        OnTypingFinishEvent?.Invoke();
        OnTypeFinishEvent?.Invoke();
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
