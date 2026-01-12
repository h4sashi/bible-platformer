using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class BibleTrigger : MonoBehaviour
{
    
    public string verse;
    public TextMeshProUGUI verseText;

    public GameObject bottomContainer;


    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            bottomContainer.SetActive(true);
            verseText.text = verse;
        }
    }
}
