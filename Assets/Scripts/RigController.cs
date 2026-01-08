using UnityEngine;
using UnityEngine.Animations.Rigging;

public class RigController : MonoBehaviour
{
    public Rig walkRig; // Assign your rig component in Inspector
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        
        // Enable rig only during walk animation
        if (stateInfo.IsName("Walk"))
        {
            walkRig.weight = 1f; // Full rig influence
        }
        else
        {
            walkRig.weight = 0f; // No rig influence - hands follow animation
        }
    }
}
