using System;
using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class BibleTrigger : MonoBehaviour
{
    [TextArea]
    public string verse;

    public TextMeshProUGUI verseText;
    public GameObject bottomContainer;

    public float typingSpeed = 0.04f;

    private Coroutine typingRoutine;
    private bool hasTriggered;

    public event Action OnTypingFinishEvent;

    private void OnEnable() {
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

        bottomContainer.SetActive(true);

        if (typingRoutine != null)
            StopCoroutine(typingRoutine);

        typingRoutine = StartCoroutine(TypeVerse());
    }

    IEnumerator TypeVerse()
    {
        verseText.text = "";

        foreach (char c in verse)
        {
            verseText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        OnTypingFinishEvent?.Invoke();
    }
}
