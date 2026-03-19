using System;
using System.Collections;
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

    private void OnEnable()
    {
        OnTypingFinishEvent += DisableUI;
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
}
