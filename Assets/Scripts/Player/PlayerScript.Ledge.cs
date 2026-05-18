using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public partial class PlayerScript
{
    private LedgeZoneData activeLedgeData;

    // =====================
    // LEDGE — SHARED CORE
    // =====================

    #region LEDGE SHARED

    /// <summary>
    /// Shared entry point for any ledge instance.
    /// Pass ledgeZoneData for Obstacle 8, secondLedgeZoneData for Obstacle 9.
    /// </summary>
    private void ExecuteLedge(LedgeZoneData data)
    {
        if (data.isLedgeActive || data.isLedgeFinished)
            return;
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling)
            return;

        data.isLedgeActive = true;
        activeLedgeData = data;

        OnFastStopTriggerEnter(transform.position, false, false);
        data.blockade.GetComponent<FastStopUtils>().isFastStoppingLeft = false;
        data.blockade.GetComponent<FastStopUtils>().isFastStoppingRight = false;

        if (data.descriptiveCanvas != null)
            data.descriptiveCanvas.SetActive(false);

        isMoving = false;

        if (data.ledgeBtn != null)
            data.ledgeBtn.gameObject.SetActive(false);

        crossReferrence.SetActive(false);

        if (data.crossLedge != null)
        {
            data.crossLedge.SetActive(true);
            if (data.crossLedgeFinalPosition != Vector3.zero)
                data.crossLedge.transform.localPosition = data.crossLedgeFinalPosition;
        }

        if (animator != null)
            animator.SetBool(LEDGE_TRIGGER, true);

        StartCoroutine(LedgeRoutineFor(data));
        Debug.Log("Ledge button pressed — playing ledge animation.");
    }

    /// <summary>
    /// Shared coroutine — works identically for both ledge instances.
    /// </summary>
    private IEnumerator LedgeRoutineFor(LedgeZoneData data)
    {
        yield return null;

        float timeout = 2f;
        float elapsed = 0f;

        while (!animator.GetCurrentAnimatorStateInfo(0).IsName("Ledge"))
        {
            elapsed += Time.deltaTime;
            if (elapsed >= timeout)
            {
                Debug.LogWarning("LedgeRoutine: Timed out waiting for Ledge state.");
                yield break;
            }
            yield return null;
        }

        float clipLength = animator.GetCurrentAnimatorStateInfo(0).length;
        Debug.Log($"Ledge state started — clip length: {clipLength:F2}s");

        yield return new WaitForSeconds(clipLength);
        Debug.Log("Ledge animation complete — transitioning to no-cross walk.");
    }

    /// <summary>
    /// Shared stop — call from Animation Event, passing the correct data instance.
    /// </summary>
    private void StopLedgeInternal(LedgeZoneData data)
    {
        // Ignore duplicate stop callbacks when multiple animation events fire.
        if (data == null || !data.isLedgeActive || data.isLedgeFinished)
            return;

        // Only stop the currently active ledge instance.
        if (activeLedgeData != null && activeLedgeData != data)
            return;

        data.isLedgeActive = false;
        data.isLedgeFinished = true;
        data.isNoCrossWalk = true;

        if (animator != null)
        {
            animator.ResetTrigger(LEDGE_TRIGGER);
            animator.SetBool(IS_NO_CROSS_WALK, true);
            animator.SetBool(IS_MOVING, false);
        }

        if (data.crossLedge != null)
            data.crossLedge.SetActive(false);

        if (data.crossLedgeDefault != null)
        {
            data.crossLedgeDefault.SetActive(true);
            PlayLedgeVFX(data);
        }

        if (activeLedgeData == data)
            activeLedgeData = null;

        if (crossReferrence != null)
            crossReferrence.SetActive(false);

        Debug.Log("Ledge complete — entering no-cross walk mode.");
    }

    private void PlayLedgeVFX(LedgeZoneData data)
    {
        if (data == null || data.LedgeVFX == null)
            return;

        Transform anchor = data.ledgeVFXAnchorTransform != null ? data.ledgeVFXAnchorTransform : transform;
        GameObject ledgeVFXInstance = Instantiate(data.LedgeVFX, anchor.position, anchor.rotation);
        ParticleSystem[] particleSystems = ledgeVFXInstance.GetComponentsInChildren<ParticleSystem>(true);

        float destroyDelay = 0f;
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.Play(true);

            if (!main.loop)
            {
                destroyDelay = Mathf.Max(
                    destroyDelay,
                    main.startDelay.constantMax + main.duration + main.startLifetime.constantMax
                );
            }
        }

        if (particleSystems.Length == 0)
        {
            Debug.LogWarning("LedgeVFX was spawned, but no ParticleSystem was found on it or its children.");
            return;
        }

        if (destroyDelay > 0f)
            Destroy(ledgeVFXInstance, destroyDelay);
    }

    /// <summary>
    /// Shared off-zone restore — works for both ledge instances.
    /// </summary>
    private void LedgeZoneOffInternal(LedgeZoneData data)
    {
        if (!data.isNoCrossWalk && !data.isLedgeFinished)
            return;

        data.isNoCrossWalk = false;
        data.isLedgeFinished = false;
        isNoCrossMoving = false;

        if (animator != null)
        {
            animator.SetBool(IS_NO_CROSS_WALK, false);
            animator.SetBool(IS_NO_CROSS_IDLE, false);
            animator.SetBool(IS_MOVING, false);
        }

        if (walkRig != null)
            walkRig.weight = 1f;
        if (armRig != null)
            armRig.weight = 1f;

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        Debug.Log("LedgeZoneOff — locomotion and rig restored.");
    }

    #endregion


    // =====================
    // LEDGE — OBSTACLE 8
    // =====================

    #region LEDGE OBSTACLE 8

    public void LedgeZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        ledgeZoneData.isLedgeActive = false;
        ledgeZoneData.isLedgeFinished = false;

        if (ledgeZoneData.ledgeBtn != null)
            ledgeZoneData.ledgeBtn.gameObject.SetActive(true);

        Debug.Log("LedgeZone (Obs 8) entered — ledge button active.");
    }

    public void LedgeZoneOnTriggerExit()
    {
        if (ledgeZoneData.isLedgeActive)
            return;

        if (ledgeZoneData.ledgeBtn != null)
            ledgeZoneData.ledgeBtn.gameObject.SetActive(false);

        Debug.Log("LedgeZone (Obs 8) exited without pressing.");
    }

    public void OnLedgeButtonDown()
    {
        ExecuteLedge(ledgeZoneData);
    }

    /// <summary>
    /// Wire this to an Animation Event on the Obstacle 8 Ledge clip's final frame.
    /// </summary>
    public void StopLedge()
    {
        StopLedgeInternal(ledgeZoneData);
    }

    public void LedgeZoneOffOnTriggerEnter()
    {
        LedgeZoneOffInternal(ledgeZoneData);
    }

    #endregion


    // =====================
    // LEDGE — OBSTACLE 9
    // =====================

    #region LEDGE OBSTACLE 9

    public void SecondLedgeZoneOnTriggerEnter()
    {
        if (isDrinking || isBreathing || isCasting || isGliding || isPulling || isSleeping)
            return;

        secondLedgeZoneData.isLedgeActive = false;
        secondLedgeZoneData.isLedgeFinished = false;

        if (secondLedgeZoneData.ledgeBtn != null)
            secondLedgeZoneData.ledgeBtn.gameObject.SetActive(true);

        Debug.Log("LedgeZone (Obs 9) entered — ledge button active.");
    }

    public void SecondLedgeZoneOnTriggerExit()
    {
        if (secondLedgeZoneData.isLedgeActive)
            return;

        if (secondLedgeZoneData.ledgeBtn != null)
            secondLedgeZoneData.ledgeBtn.gameObject.SetActive(false);

        Debug.Log("LedgeZone (Obs 9) exited without pressing.");
    }

    public void OnSecondLedgeButtonDown()
    {
        ExecuteLedge(secondLedgeZoneData);
    }

    /// <summary>
    /// Wire this to an Animation Event on the Obstacle 9 Ledge clip's final frame.
    /// </summary>
    public void StopSecondLedge()
    {
        StopLedgeInternal(secondLedgeZoneData);
    }

    public void SecondLedgeZoneOffOnTriggerEnter()
    {
        LedgeZoneOffInternal(secondLedgeZoneData);
    }

    #endregion


    // =====================
    // NO-CROSS WALK
    // =====================

    #region NO-CROSS WALK

    private void HandleNoCrossMovement()
    {
        bool anyNoCrossWalk = ledgeZoneData.isNoCrossWalk || secondLedgeZoneData.isNoCrossWalk;
        if (!anyNoCrossWalk)
            return;

        float input = useMobileControls ? mobileHorizontalInput : Input.GetAxisRaw("Horizontal");

        isNoCrossMoving = Mathf.Abs(input) > 0.01f;

        if (isNoCrossMoving)
        {
            Vector3 dir = new Vector3(0, 0, input);
            transform.Translate(dir * moveSpeed * Time.deltaTime, Space.World);

            Quaternion targetRot =
                input < 0 ? Quaternion.Euler(0, 180, 0) : Quaternion.Euler(0, 0, 0);

            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRot,
                smoothRotation * Time.deltaTime
            );
        }
    }

    private void HandleNoCrossAnimation()
    {
        bool anyNoCrossWalk = ledgeZoneData.isNoCrossWalk || secondLedgeZoneData.isNoCrossWalk;
        if (!anyNoCrossWalk)
            return;

        animator.SetBool(IS_NO_CROSS_WALK, true);
        animator.SetBool(IS_MOVING, false);
        animator.SetBool(IS_NO_CROSS_IDLE, !isNoCrossMoving);
        animator.SetBool(IS_MOVING, isNoCrossMoving);
    }

    #endregion
}
