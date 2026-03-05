using UnityEngine;

public class FruitZone : MonoBehaviour
{
    public GameObject fruits;
    [SerializeField] public int index = 4;
    [HideInInspector]public bool isFruitFallTrigger = false;

    public Transform fruitPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
   void Update()
{
    if (isFruitFallTrigger == true)
    {
        if (fruits == null)
            return;
        else
        {
            for (int i = 0; i < index; i++)
            {
                GameObject apple = Instantiate(fruits, fruitPosition.position, Quaternion.identity);
                apple.GetComponent<Fruit>().enabled = true;
            }
            isFruitFallTrigger = false; // ← reset the flag so it only runs once
        }
    }
}

    
}
