using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasTrigger : MonoBehaviour
{
    public GameObject waterCanvas;
    public int drinkMax = 5;
    
    public void ActivateCanvas()
    {
        if (waterCanvas != null)
        {
            waterCanvas.SetActive(true);
        }
    }

    public void DeactivateCanvas()
    {
        if (waterCanvas != null)
        {
            waterCanvas.SetActive(false);
        }
    }
}
