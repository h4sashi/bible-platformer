using UnityEngine;
using System.Collections;

public class SleepTrigger : MonoBehaviour
{
    [Header("Sleep Settings")]
    [SerializeField] private float sleepDuration = 5f; // Time to sleep in seconds
    [SerializeField] private KeyCode sleepKey = KeyCode.E; // Key to initiate sleep
    
    [Header("UI Settings")]
    [SerializeField] private GameObject sleepPromptUI; // Optional: "Press E to Sleep" UI
    
    private PlayerScript player;
    private bool playerInTrigger = false;
    
    void Start()
    {
        if (sleepPromptUI != null)
        {
            sleepPromptUI.SetActive(false);
        }
    }
    
    void Update()
    {
        // Check for sleep input when player is in trigger
        // if (playerInTrigger && Input.GetKeyDown(sleepKey))
        // {
            if (player != null && !player.IsSleeping)
            {
                player.StartSleeping(sleepDuration);
            }
        // }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerScript>();
            playerInTrigger = true;
            
            if (sleepPromptUI != null)
            {
                sleepPromptUI.SetActive(true);
            }
            
         
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;
            playerInTrigger = false;
            
            if (sleepPromptUI != null)
            {
                sleepPromptUI.SetActive(false);
            }
        }
    }
    
    // Optional: Public method for UI button
    public void OnSleepButtonPressed()
    {
        if (player != null && !player.IsSleeping)
        {
            player.StartSleeping(sleepDuration);
        }
    }
}



