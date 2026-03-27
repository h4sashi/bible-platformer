using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public partial class PlayerScript
{
    // =====================
    // CLIMBING
    // =====================

    #region CLIMBING

    public void ClimbUpZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        moveSpeed = originalMoveSpeed;
        stormData.isInStorm = false;
        animator.speed = 1f;
        stormData.Reset();

        climbData.isInClimbZone = true;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb = false;
        climbData.hasReachedTop = false;

        isMoving = false;
        animator.applyRootMotion = true;

        if (climbData.ladderTransform != null)
        {
            transform.rotation = UnityEngine.Quaternion.Euler(climbData.climbSnapRotation);
            transform.position =
                climbData.ladderTransform.position
                + climbData.ladderTransform.TransformDirection(climbData.ladderAlignOffset);
        }
        else
        {
            if (climbData.positionOffset != Vector3.zero)
                transform.localPosition = climbData.positionOffset;

            if (climbData.rotationOffset != Vector3.zero)
                transform.localRotation = UnityEngine.Quaternion.Euler(climbData.rotationOffset);

            Debug.LogWarning("ClimbData: ladderTransform not assigned — using manual offsets.");
        }

        if (climbData.crossToClimbGO != null)
        {
            crossReferrence.SetActive(false);
            climbData.crossToClimbGO.SetActive(true);
        }

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("ClimbUpZone entered — aligned to ladder, climb idle active.");
    }

    public void OnClimbButtonDown()
    {
        if (!climbData.isInClimbZone || isDrinking || isCasting || isGliding || isBreathing)
            return;

        climbData.isHoldingClimb = true;
        climbData.isPlayerClimbing = true;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMBING, true);
        }

        Debug.Log("Climbing — hold to continue.");
    }

    public void OnClimbButtonUp()
    {
        if (!climbData.isInClimbZone || climbData.hasReachedTop)
            return;

        climbData.isHoldingClimb = false;
        climbData.isPlayerClimbing = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("Climb button released — climb idle.");
    }

    public void ClimbOffZoneOnTriggerEnter()
    {
        if (!climbData.isInClimbZone)
            return;

        climbData.hasReachedTop = true;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, true);
        }

        StartCoroutine(ClimbToTopRoutine());
        Debug.Log("ClimbOffZone reached — auto-playing ClimbToTop.");
    }

    private System.Collections.IEnumerator ClimbToTopRoutine()
    {
        yield return null;

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
        );

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
        );

        if (climbData.exitSnapTarget != null)
        {
            transform.position = climbData.exitSnapTarget.position;
            transform.rotation = climbData.exitSnapTarget.rotation;
            climbData.climbBtn.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ClimbData: exitSnapTarget not assigned.");
        }

        StopClimbing();
        Debug.Log("ClimbToTop complete — player snapped to exit position.");
    }

    private void StopClimbing()
    {
        climbData.isInClimbZone = false;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb = false;
        climbData.hasReachedTop = true;

        rb.isKinematic = false;
        animator.applyRootMotion = false;

        if (climbData.crossToClimbGO != null)
            climbData.crossToClimbGO.SetActive(false);

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, false);
        }

        Debug.Log("Climbing fully stopped — player free.");
    }

    public void AnimationEvent_ClimbStep()
    {
        Debug.Log("Climb step");
    }

    #endregion


    // =====================
    // MOUNTAIN CLIMB
    // =====================

    #region MOUNTAIN CLIMB


    void OnAnimatorMove()
    {
        // Mountain climb zone
        if (mountainClimbData.isInClimbZone)
        {
            if (mountainClimbData.isPlayerClimbing)
            {
                // Use the normalized slope direction instead of ladderUp alone
                Vector3 slopeDir = mountainClimbData.climbDirection.normalized;

                float delta = mountainClimbData.climbSpeed * Time.deltaTime;

                // Move along both Y and Z according to the slope direction
                transform.position += slopeDir * delta;
                transform.rotation = UnityEngine.Quaternion.Euler(
                    mountainClimbData.climbSnapRotation
                );
            }
            else
            {
                // Freeze — no positional change, just lock rotation
                transform.rotation = UnityEngine.Quaternion.Euler(
                    mountainClimbData.climbSnapRotation
                );
            }
            return;
        }

        // Regular climb zone — unchanged
        if (climbData.isInClimbZone)
        {
            if (climbData.isPlayerClimbing)
            {
                Vector3 ladderUp =
                    climbData.ladderTransform != null ? climbData.ladderTransform.up : Vector3.up;

                float upMagnitude = Vector3.Dot(animator.deltaPosition, ladderUp);

                if (Mathf.Abs(upMagnitude) < 0.0001f)
                    upMagnitude = climbData.climbSpeed * Time.deltaTime;

                transform.position += ladderUp * upMagnitude;
                transform.rotation = UnityEngine.Quaternion.Euler(climbData.climbSnapRotation);
            }
            else
            {
                transform.rotation = UnityEngine.Quaternion.Euler(climbData.climbSnapRotation);
            }
            return;
        }

        if (animator.applyRootMotion)
            transform.position += animator.deltaPosition;
    }

    public void MountainUpZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        mountainClimbData.isInClimbZone = true;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb = false;
        mountainClimbData.hasReachedTop = false;

        isMoving = false;
        moveSpeed = originalMoveSpeed;
        stormData.isInStorm = false;
        animator.speed = 1f;
        stormData.Reset();
        animator.applyRootMotion = true;

        if (mountainClimbData.ladderTransform != null)
        {
            transform.rotation = UnityEngine.Quaternion.Euler(mountainClimbData.climbSnapRotation);
            transform.position =
                mountainClimbData.ladderTransform.position
                + mountainClimbData.ladderTransform.TransformDirection(
                    mountainClimbData.ladderAlignOffset
                );
        }
        else
        {
            if (mountainClimbData.positionOffset != Vector3.zero)
                transform.localPosition = mountainClimbData.positionOffset;

            if (mountainClimbData.rotationOffset != Vector3.zero)
                transform.localRotation = UnityEngine.Quaternion.Euler(30f, 0, 0);

            Debug.LogWarning(
                "MountainClimbData: ladderTransform not assigned — using manual offsets."
            );
        }

        if (mountainClimbData.crossToClimbGO != null)
        {
            crossReferrence.SetActive(false);
            mountainClimbData.crossToClimbGO.SetActive(true);
        }

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("MountainUpZone entered — aligned to ladder, climb idle active.");
    }

    public void OnMountainClimbButtonDown()
    {
        if (!mountainClimbData.isInClimbZone || isDrinking || isCasting || isGliding || isBreathing)
            return;

        mountainClimbData.isHoldingClimb = true;
        mountainClimbData.isPlayerClimbing = true;

        // Re-enable kinematic movement along ladder
        rb.isKinematic = true; // keep kinematic throughout climb, OnAnimatorMove drives position
        rb.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMBING, true);
        }

        Debug.Log("Mountain climbing — hold to continue.");
    }

    public void OnMountainClimbButtonUp()
    {
        if (!mountainClimbData.isInClimbZone || mountainClimbData.hasReachedTop)
            return;

        mountainClimbData.isHoldingClimb = false;
        mountainClimbData.isPlayerClimbing = false;

        // Kill any residual vertical velocity so player doesn't drift down
        rb.linearVelocity = Vector3.zero;
        rb.isKinematic = true; // lock in place until climbing resumes

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("Mountain climb button released — climb idle.");
    }

    public void MountainClimbOffZoneOnTriggerEnter()
    {
        if (!mountainClimbData.isInClimbZone)
            return;

        mountainClimbData.hasReachedTop = true;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb = false;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, true);
        }

        StartCoroutine(MountainClimbToTopRoutine());
        Debug.Log("MountainClimbOffZone reached — auto-playing ClimbToTop.");
    }

    private System.Collections.IEnumerator MountainClimbToTopRoutine()
    {
        yield return null;

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
        );

        yield return new WaitUntil(() =>
            animator.GetCurrentAnimatorStateInfo(0).IsName("ClimbToTop")
            && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f
        );

        if (mountainClimbData.exitSnapTarget != null)
        {
            transform.position = mountainClimbData.exitSnapTarget.position;
            transform.rotation = mountainClimbData.exitSnapTarget.rotation;
            mountainClimbData.climbBtn.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("MountainClimbData: exitSnapTarget not assigned.");
        }

        StopMountainClimbing();
        Debug.Log("MountainClimbToTop complete — player snapped to exit position.");
    }

    private void StopMountainClimbing()
    {
        mountainClimbData.isInClimbZone = false;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb = false;
        mountainClimbData.hasReachedTop = true;

        rb.isKinematic = false;
        animator.applyRootMotion = false;

        if (mountainClimbData.crossToClimbGO != null)
            mountainClimbData.crossToClimbGO.SetActive(false);

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING, false);
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMB_TO_TOP, false);
        }

        Debug.Log("Mountain climbing fully stopped — player free.");
    }

    #endregion
}
