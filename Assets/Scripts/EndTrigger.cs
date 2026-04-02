using UnityEngine;
using UnityEngine.Events;

public class EndTrigger : MonoBehaviour
{
    public UnityEvent executeEvent;

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            executeEvent?.Invoke();
        }
    }
}
