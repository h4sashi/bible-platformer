using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraTrigger : MonoBehaviour
{
    public GameObject cameraToEnable;
    public GameObject cameraToDisable;

    public GameObject objectToEnable;
    public UnityEvent enableEvents;



    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if(cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if(cameraToDisable != null)
                cameraToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().glideRig.weight = 0;
            if(objectToEnable != null)
            {
                objectToEnable.SetActive(true);
                enableEvents?.Invoke();
            }
            else
            {
                return;
            }
            // objectToEnable.SetActive(true);
        }
    }
}
