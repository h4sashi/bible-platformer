using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CameraTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        GlideZoneOn,
        GlideZoneOff,
        WaterFountain,
        RockObstacle,
        OasisEnter,
        OasisExit,
        SandStormEnter,
        SandStormExit,
        SandStormRockEnter,
        AfterSandStormEnter,
        ClimbUpZone,
        ClimbOffZone,
        MountainClimbUpZone,
        MountainClimbOffZone,

        LedgeZone,
        LedgeZoneOff,
        SecondLedgeZone,
        SecondLedgeZoneOff,
    }

    public TriggerType triggerType;

    [Header("Glide Settings")]
    public string glideDataId = "default";

    public GameObject cameraToEnable;
    public GameObject cameraToDisable;

    public GameObject objectToEnable;
    public GameObject objectToDisable;
    public UnityEvent enableEvents;
    public UnityEvent disableEvents;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && triggerType == TriggerType.OasisEnter)
        {
            Debug.Log("Player has enetered Oasis Zone");

            enableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().glideData.glideRig.weight = 0;
            other.GetComponent<PlayerScript>().OasisZoneOnTrigger(this);
            if (objectToEnable != null)
            {
                objectToEnable.SetActive(true);
            }
            else
            {
                return;
            }
            if (objectToDisable != null)
            {
                objectToDisable.SetActive(false);
            }
            else
            {
                return;
            }
            // objectToEnable.SetActive(true);
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.OasisExit)
        {
            Debug.Log("Player has exit Oasis Zone");

            enableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(false);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(true);

            other.GetComponent<PlayerScript>().glideData.glideRig.weight = 0;
            other.GetComponent<PlayerScript>().OasisZoneOnExitTrigger(this);
            if (objectToEnable != null)
            {
                objectToEnable.SetActive(false);
            }
            else
            {
                return;
            }
            if (objectToDisable != null)
            {
                objectToDisable.SetActive(true);
            }
            else
            {
                return;
            }
            // objectToEnable.SetActive(true);
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.GlideZoneOn)
        {
            Debug.Log("Player has entered Glide Zone");
            enableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(true);
            if (objectToDisable != null)
                objectToDisable.SetActive(false);

            PlayerScript ps = other.GetComponent<PlayerScript>();
            GlideData data = ps.GetGlideDataById(glideDataId); // ← resolves to glideData or mountainGlideData
            ps.glideData.glideRig.weight = 0;
            ps.GlideZoneOnTrigger(this, data);
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.GlideZoneOff)
        {
            Debug.Log("Player has exited Glide Zone");
            disableEvents?.Invoke();
            PlayerScript ps = other.GetComponent<PlayerScript>();
            GlideData data = ps.GetGlideDataById(glideDataId); // ← same lookup
            ps.StopGliding(data);
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.SandStormEnter)
        {
            enableEvents?.Invoke();
            Debug.Log("Player entered sandstorm zone");
            enableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            other.GetComponent<PlayerScript>().EnterSandStorm();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.SandStormExit)
        {
            Debug.Log("Player exited sandstorm zone");
            disableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(false);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(true);
            other.GetComponent<PlayerScript>().ExitSandStorm();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.SandStormRockEnter)
        {
            enableEvents?.Invoke();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.AfterSandStormEnter)
        {
            enableEvents?.Invoke();
            Debug.Log("Player entered sandstorm zone");
            enableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            other.GetComponent<PlayerScript>().AfterEnterSandStorm();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.ClimbUpZone)
        {
            enableEvents?.Invoke();

            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(true);
            if (objectToDisable != null)
                objectToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().ClimbUpZoneOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.ClimbOffZone)
        {
            disableEvents?.Invoke();

            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(false);
            if (objectToDisable != null)
                objectToDisable.SetActive(true);

            other.GetComponent<PlayerScript>().ClimbOffZoneOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.MountainClimbUpZone)
        {
            enableEvents?.Invoke();

            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(true);
            if (objectToDisable != null)
                objectToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().MountainUpZoneOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.MountainClimbOffZone)
        {
            disableEvents?.Invoke();

            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(false);
            if (objectToDisable != null)
                objectToDisable.SetActive(true);

            other.GetComponent<PlayerScript>().MountainClimbOffZoneOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.LedgeZone)
        {
            enableEvents?.Invoke();

            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(true);
            if (objectToDisable != null)
                objectToDisable.SetActive(false);

            other.GetComponent<PlayerScript>().LedgeZoneOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.LedgeZoneOff)
        {
            disableEvents?.Invoke();

            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(false);
            if (objectToDisable != null)
                objectToDisable.SetActive(true);

            other.GetComponent<PlayerScript>().LedgeZoneOffOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.SecondLedgeZone)
        {
            enableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(true);
            if (objectToDisable != null)
                objectToDisable.SetActive(false);
            other.GetComponent<PlayerScript>().SecondLedgeZoneOnTriggerEnter();
        }

        if (other.CompareTag("Player") && triggerType == TriggerType.SecondLedgeZoneOff)
        {
            disableEvents?.Invoke();
            if (cameraToEnable != null)
                cameraToEnable.SetActive(true);
            if (cameraToDisable != null)
                cameraToDisable.SetActive(false);
            if (objectToEnable != null)
                objectToEnable.SetActive(false);
            if (objectToDisable != null)
                objectToDisable.SetActive(true);
            other.GetComponent<PlayerScript>().SecondLedgeZoneOffOnTriggerEnter();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && triggerType == TriggerType.SandStormRockEnter)
            disableEvents?.Invoke();

        if (other.CompareTag("Player") && triggerType == TriggerType.LedgeZone)
            other.GetComponent<PlayerScript>().LedgeZoneOnTriggerExit();

        if (other.CompareTag("Player") && triggerType == TriggerType.SecondLedgeZone)
            other.GetComponent<PlayerScript>().SecondLedgeZoneOnTriggerExit();
    }
}
