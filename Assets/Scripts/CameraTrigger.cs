using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraTrigger : MonoBehaviour
{
    public enum TriggerType{ GlideZoneOn, GlideZoneOff, WaterFountain, RockObstacle}
   

    public TriggerType triggerType;

    public GameObject cameraToEnable;
    public GameObject cameraToDisable;

    public GameObject objectToEnable;
    public GameObject objectToDisable;
    public UnityEvent enableEvents;





    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && triggerType == TriggerType.GlideZoneOn)
        {
            Debug.Log("Player has enetered Glide Zone");

             enableEvents?.Invoke();
            if(cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if(cameraToDisable != null)
                cameraToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().glideRig.weight = 0;
            other.GetComponent<PlayerScript>().GlideZoneOnTrigger(this);
            if(objectToEnable != null)
            {
                objectToEnable.SetActive(true);
               
            }
            else
            {
                return;
            }
            if(objectToDisable != null)
            {
                objectToDisable.SetActive(false);
               
            }
            else
            {
                return;
            }
            // objectToEnable.SetActive(true);
        }
    }
}
