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

    // ─────────────────────────────────────────────────────────────────
    // HOW THIS WORKS  (CodeMonkey-style gravity-off approach)
    //
    //  OLD approach  → rb.isKinematic = true, OnAnimatorMove drives position
    //  NEW approach  → rb.useGravity  = false while in zone
    //                  rb.linearVelocity set each frame:
    //                    • climbing  → ladderUp * climbSpeed
    //                    • idle      → Vector3.zero  (player "sticks" in place)
    //                  OnAnimatorMove still locks rotation but no longer
    //                  translates the player, so the two systems don't fight.
    //
    //  ClimbData fields are UNCHANGED — only the methods below change.
    // ─────────────────────────────────────────────────────────────────

    public void ClimbUpZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        moveSpeed = originalMoveSpeed;
        stormData.isInStorm = false;
        animator.speed = 1f;
        stormData.Reset();

        climbData.isInClimbZone     = true;
        climbData.isPlayerClimbing  = false;
        climbData.isHoldingClimb    = false;
        climbData.hasReachedTop     = false;

        isMoving = false;

        // ── CodeMonkey: disable gravity instead of going kinematic ──
        rb.useGravity    = false;
        rb.linearVelocity = Vector3.zero;
        // ────────────────────────────────────────────────────────────

        animator.applyRootMotion = true;

        // Snap position / rotation to the ladder
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
            animator.SetBool(IS_CLIMBING,   false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("ClimbUpZone entered — gravity off, snapped to ladder, climb idle active.");
    }

    public void OnClimbButtonDown()
    {
        if (!climbData.isInClimbZone || isDrinking || isCasting || isGliding || isBreathing)
            return;

        climbData.isHoldingClimb   = true;
        climbData.isPlayerClimbing = true;

        // Velocity will be set each frame inside HandleClimbVelocity()
        if (animator != null)
        {
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMBING,   true);
        }

        Debug.Log("Climbing — hold to continue.");
    }

    public void OnClimbButtonUp()
    {
        if (!climbData.isInClimbZone || climbData.hasReachedTop)
            return;

        climbData.isHoldingClimb   = false;
        climbData.isPlayerClimbing = false;

        // ── CodeMonkey: zero velocity so player sticks to the rung ──
        rb.linearVelocity = Vector3.zero;
        // ────────────────────────────────────────────────────────────

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING,   false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("Climb button released — climb idle.");
    }

    // ── CodeMonkey core: called every frame from Update while in zone ──
    // Add   HandleClimbVelocity();   to the Update() block, inside the
    // existing "if (climbData.isInClimbZone)" guard or alongside it.
    public void HandleClimbVelocity()
    {
        if (!climbData.isInClimbZone)
            return;

        // Keep gravity disabled each frame (safe to set repeatedly)
        rb.useGravity = false;

        if (climbData.isPlayerClimbing)
        {
            // Drive position via velocity — CodeMonkey style
            Vector3 ladderUp = climbData.ladderTransform != null
                ? climbData.ladderTransform.up
                : Vector3.up;

            rb.linearVelocity = ladderUp * climbData.climbSpeed;
        }
        else
        {
            // Idle on ladder: zero all velocity so the player doesn't drift
            rb.linearVelocity = Vector3.zero;
        }

        // Lock rotation every frame (same as before)
        transform.rotation = UnityEngine.Quaternion.Euler(climbData.climbSnapRotation);
    }
    // ──────────────────────────────────────────────────────────────────

    public void ClimbOffZoneOnTriggerEnter()
    {
        if (!climbData.isInClimbZone)
            return;

        climbData.hasReachedTop    = true;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb   = false;

        // Stop upward motion before the top animation plays
        rb.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING,     false);
            animator.SetBool(IS_CLIMB_IDLE,   false);
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
        climbData.isInClimbZone    = false;
        climbData.isPlayerClimbing = false;
        climbData.isHoldingClimb   = false;
        climbData.hasReachedTop    = true;

        // ── CodeMonkey: restore gravity instead of disabling kinematic ──
        rb.useGravity     = true;
        rb.linearVelocity = Vector3.zero;
        // ─────────────────────────────────────────────────────────────────

        animator.applyRootMotion = false;

        if (climbData.crossToClimbGO != null)
            climbData.crossToClimbGO.SetActive(false);

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING,     false);
            animator.SetBool(IS_CLIMB_IDLE,   false);
            animator.SetBool(IS_CLIMB_TO_TOP, false);
        }

        Debug.Log("Climbing fully stopped — gravity restored, player free.");
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

    // OnAnimatorMove now only handles rotation locks.
    // Regular climb velocity is driven by HandleClimbVelocity() above.
    // Mountain climb velocity is driven by HandleMountainClimbVelocity() below.
    void OnAnimatorMove()
    {
        // Mountain climb zone — rotation lock only (velocity handled separately)
        if (mountainClimbData.isInClimbZone)
        {
            transform.rotation = UnityEngine.Quaternion.Euler(
                mountainClimbData.climbSnapRotation
            );
            return;
        }

        // Regular climb zone — rotation lock only (velocity handled separately)
        if (climbData.isInClimbZone)
        {
            transform.rotation = UnityEngine.Quaternion.Euler(climbData.climbSnapRotation);
            return;
        }

        if (animator.applyRootMotion)
            transform.position += animator.deltaPosition;
    }

    // ── CodeMonkey core for mountain: called every frame from Update ──
    public void HandleMountainClimbVelocity()
    {
        if (!mountainClimbData.isInClimbZone)
            return;

        rb.useGravity = false;

        if (mountainClimbData.isPlayerClimbing)
        {
            Vector3 slopeDir = mountainClimbData.climbDirection.normalized;
            rb.linearVelocity = slopeDir * mountainClimbData.climbSpeed;
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }

        transform.rotation = UnityEngine.Quaternion.Euler(mountainClimbData.climbSnapRotation);
    }
    // ──────────────────────────────────────────────────────────────────

    public void MountainUpZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        mountainClimbData.isInClimbZone    = true;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb   = false;
        mountainClimbData.hasReachedTop    = false;

        isMoving = false;
        moveSpeed = originalMoveSpeed;
        stormData.isInStorm = false;
        animator.speed = 1f;
        stormData.Reset();
        animator.applyRootMotion = true;

        // ── CodeMonkey: gravity off, velocity zeroed ──
        rb.useGravity     = false;
        rb.linearVelocity = Vector3.zero;
        // ─────────────────────────────────────────────

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
            animator.SetBool(IS_CLIMBING,   false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("MountainUpZone entered — gravity off, snapped to slope, climb idle active.");
    }

    public void OnMountainClimbButtonDown()
    {
        if (!mountainClimbData.isInClimbZone || isDrinking || isCasting || isGliding || isBreathing)
            return;

        mountainClimbData.isHoldingClimb   = true;
        mountainClimbData.isPlayerClimbing = true;

        // No longer need rb.isKinematic — gravity is already off
        rb.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMB_IDLE, false);
            animator.SetBool(IS_CLIMBING,   true);
        }

        Debug.Log("Mountain climbing — hold to continue.");
    }

    public void OnMountainClimbButtonUp()
    {
        if (!mountainClimbData.isInClimbZone || mountainClimbData.hasReachedTop)
            return;

        mountainClimbData.isHoldingClimb   = false;
        mountainClimbData.isPlayerClimbing = false;

        // ── CodeMonkey: zero velocity so player holds position ──
        rb.linearVelocity = Vector3.zero;
        // ────────────────────────────────────────────────────────

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING,   false);
            animator.SetBool(IS_CLIMB_IDLE, true);
        }

        Debug.Log("Mountain climb button released — climb idle.");
    }

    public void MountainClimbOffZoneOnTriggerEnter()
    {
        if (!mountainClimbData.isInClimbZone)
            return;

        mountainClimbData.hasReachedTop    = true;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb   = false;

        rb.linearVelocity = Vector3.zero;

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING,     false);
            animator.SetBool(IS_CLIMB_IDLE,   false);
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
        mountainClimbData.isInClimbZone    = false;
        mountainClimbData.isPlayerClimbing = false;
        mountainClimbData.isHoldingClimb   = false;
        mountainClimbData.hasReachedTop    = true;

        // ── CodeMonkey: restore gravity ──
        rb.useGravity     = true;
        rb.linearVelocity = Vector3.zero;
        // ─────────────────────────────────

        animator.applyRootMotion = false;

        if (mountainClimbData.crossToClimbGO != null)
            mountainClimbData.crossToClimbGO.SetActive(false);

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        if (animator != null)
        {
            animator.SetBool(IS_CLIMBING,     false);
            animator.SetBool(IS_CLIMB_IDLE,   false);
            animator.SetBool(IS_CLIMB_TO_TOP, false);
        }

        Debug.Log("Mountain climbing fully stopped — gravity restored, player free.");
    }

    #endregion
}