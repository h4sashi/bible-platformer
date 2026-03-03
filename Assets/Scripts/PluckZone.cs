using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PluckZone : MonoBehaviour
{
    [Header("Pluck Rig Settings")]
    public float rigRiseSpeed = 8f;
    public float rigFallSpeed = 2f;

    private PlayerScript player;
    private Coroutine pluckRigCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.GetComponent<PlayerScript>();

            if (player != null)
            {
                player.SetInPluckZone(true, this);
                Debug.Log("Player entered Tree Zone - Pluck button now available");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (player != null)
            {
                player.SetInPluckZone(false, null);
                Debug.Log("Player exited Tree Zone");
            }

            player = null;
        }
    }

    /// <summary>
    /// Called by PlayerScript when the pluck button is pressed.
    /// Instantly raises armPluckRig to 1, then gradually lowers back to 0.
    /// </summary>
    public void TriggerPluck(Rig armPluckRig)
    {
        if (armPluckRig == null) return;

        if (pluckRigCoroutine != null)
            StopCoroutine(pluckRigCoroutine);

        pluckRigCoroutine = StartCoroutine(PluckRigRoutine(armPluckRig));
    }

    private IEnumerator PluckRigRoutine(Rig armPluckRig)
    {
        // Instantly snap to 1
        armPluckRig.weight = 1f;

        // Gradually fall back to 0
        while (armPluckRig.weight > 0.001f)
        {
            armPluckRig.weight = Mathf.Lerp(armPluckRig.weight, 0f, Time.deltaTime * rigFallSpeed);
            yield return null;
        }

        armPluckRig.weight = 0f;
        Debug.Log("ArmPluckRig weight returned to 0");
    }
}