using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTrigger : MonoBehaviour
{
    public GameObject cameraToEnable;
    public GameObject cameraToDisable;

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if(cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if(cameraToDisable != null)
                cameraToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().glideRig.weight = 0;
        }
    }
}
