using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenericTrigger : MonoBehaviour
{
    public int numberOfHits;
    public int maxHits;

    PlayerScript playerScript;

    private void Start() {
        playerScript = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerScript>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cross"))
        {
            if (playerScript != null)
            {
                if(playerScript.isCasting == true)
                {
                    numberOfHits++;
                }
            }
        }
    }
}
