using UnityEngine;

public class Fruit : MonoBehaviour
{
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.SendMessage("PlayerHasFruitTaken");
            this.gameObject.SetActive(false);
            
        }
    }
}
