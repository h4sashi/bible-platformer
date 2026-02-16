using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasTrigger : MonoBehaviour
{

    public enum TriggerType
    {
        GlideZoneOn,
        GlideZoneOff,
        WaterFountain,
        RockObstacle,
    }

    public TriggerType triggerType;


    
    public GameObject waterCanvas;
    // public Button drinkBtn;
    
    public int drinkMax = 5;
    
    public void ActivateCanvas()
    {
        if (waterCanvas != null)
        {
            waterCanvas.SetActive(true);
        }
        this.gameObject.SetActive(true);
    }

    public void DeactivateCanvas()
    {
        if (waterCanvas != null)
        {
            Debug.Log("Deactivating water canvas");
              
            waterCanvas.SetActive(false);
        }
        this.gameObject.SetActive(false);
    }

  


    
}
