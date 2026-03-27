using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

public partial class PlayerScript
{
    // =====================
    // SLEEPING
    // =====================

    #region SLEEPING

    public void StartSleeping(float duration)
    {
        if (!isSleeping && !isDrinking && !isCasting && !isGliding && !isPulling)
            StartCoroutine(SleepRoutine(duration));
    }

    private IEnumerator SleepRoutine(float duration)
    {
        isSleeping = true;
        isMoving = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, true);

        transform.position += sleepOffset;
        rb.isKinematic = true;

        if (crossReferrence != null)
            crossReferrence.SetActive(false);

        Debug.Log($"Player is sleeping for {duration} seconds...");
        yield return new WaitForSeconds(duration);

        StopSleeping();
    }

    public void StopSleeping()
    {
        isSleeping = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, false);

        transform.position -= sleepOffset;
        rb.isKinematic = false;

        if (crossReferrence != null)
            crossReferrence.SetActive(true);

        Debug.Log("Player woke up!");
    }

    public void AnimationEvent_EndSleeping()
    {
        Debug.Log("Sleep animation ended via Animation Event");
    }

    // ── After Storm Sleep ──

    public void AfterStormStartSleeping(float duration)
    {
        if (!isSleeping && !isDrinking && !isCasting && !isGliding && !isPulling)
            StartCoroutine(AfterStormSleepRoutine(duration));
    }

    private IEnumerator AfterStormSleepRoutine(float duration)
    {
        isSleeping = true;
        isMoving = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, true);

        transform.position += sleepOffset;
        rb.isKinematic = true;

        if (crossReferrence != null)
            crossReferrence.SetActive(false);

        Debug.Log($"Player is sleeping for {duration} seconds...");
        yield return new WaitForSeconds(duration);

        StopStormSleeping();
    }

    public void StopStormSleeping()
    {
        isSleeping = false;

        if (animator != null)
            animator.SetBool(IS_SLEEPING, false);

        transform.position -= sleepOffset;
        rb.isKinematic = false;

        if (crossReferrence != null && !climbData.isInClimbZone)
            crossReferrence.SetActive(true);
        else
            crossReferrence.SetActive(false);

        Debug.Log("Player woke up!");
    }

    public void AnimationEvent_EndSleepingAfterStorm()
    {
        Debug.Log("Sleep animation ended via Animation Event");
    }

    #endregion


    // =====================
    // DRINKING
    // =====================

    #region DRINKING

    void StartDrinking()
    {
        isDrinking = true;
        animator.SetBool(IS_DRINKING, true);
        isMoving = false;
        crossReferrence.SetActive(false);
        cupGO.SetActive(true);
        Debug.Log("Started drinking water");
    }

    public void StopDrinking()
    {
        isDrinking = false;
        animator.SetBool(IS_DRINKING, false);
        crossReferrence.SetActive(true);
        cupGO.SetActive(false);
    }

    public void OnDrinkButtonDown()
    {
        if (
            isNearWaterFountain
            && !isDrinking
            && !isCasting
            && !isBreathing
            && !isGliding
            && !isPulling
        )
        {
            StartDrinking();
            Debug.Log("Drink button pressed - Starting drinking");
        }
    }

    public void AnimationEvent_EndDrinking()
    {
        Debug.Log("Drinking animation ended");
        currentWaterAmount++;
        Debug.Log($"Current water: {currentWaterAmount}/{canvasTrigger.drinkMax}");
        OnDrinkComplete();
    }

    private void OnDrinkComplete()
    {
        if (currentWaterAmount >= canvasTrigger.drinkMax)
        {
            Debug.Log("Max water reached! Drinking complete.");
            CompleteDrinking();
        }
        else
        {
            Debug.Log($"Need more water. ({currentWaterAmount}/{canvasTrigger.drinkMax})");
            crossReferrence.SetActive(true);
            isDrinking = false;
            animator.SetBool(IS_DRINKING, false);
        }
    }

    private void CompleteDrinking()
    {
        isDrinking = false;
        isNearWaterFountain = false;
        currentWaterAmount = 0;

        animator.SetBool(IS_DRINKING, false);
        cupGO.SetActive(false);
        crossReferrence.SetActive(true);

        Debug.Log("Drinking fully complete!");
        canvasTrigger.DeactivateCanvas();
        currentWaterAmount = 0;
        canvasTrigger = null;

        if (drinkButton != null)
            drinkButton.gameObject.SetActive(false);
    }

    private void OnDrinkingBenefits()
    {
        Debug.Log("Player received drinking benefits!");
    }

    #endregion


    // =====================
    // EATING
    // =====================

    #region EATING

    public void OnEatButtonDown()
    {
        if (
            !pluck.isEating
            && !isDrinking
            && !isCasting
            && !isBreathing
            && !isGliding
            && !isPulling
        )
        {
            StartEating();
            Debug.Log("Eat button pressed - Starting eating");
        }
    }

    private void StartEating()
    {
        pluck.isEating = true;
        animator.SetBool(IS_DRINKING, true); // reuses drink animation
        isMoving = false;
        crossReferrence.SetActive(false);

        if (pluck.apple != null)
            pluck.apple.SetActive(true);

        Debug.Log("Started eating");
    }

    public void StopEating()
    {
        pluck.isEating = false;
        animator.SetBool(IS_DRINKING, false);
        crossReferrence.SetActive(true);

        if (pluck.apple != null)
            pluck.apple.SetActive(false);
    }

    public void AnimationEvent_EndEating()
    {
        if (pluck.hasTakenFruit)
        {
            Debug.Log("Eating animation ended");
            OnEatComplete();
        }
    }

    private void OnEatComplete()
    {
        OnFastStopTriggerEnter(transform.position, false, false);
        OnFastStopTriggerExit();
        pluck.isEating = false;
        animator.SetBool(IS_DRINKING, false);

        if (pluck.apple != null)
            pluck.apple.SetActive(false);

        crossReferrence.SetActive(true);
        eatBlockade.SetActive(false);

        Debug.Log("Eating complete!");

        pluck.blockade.SetActive(false);

        if (pluck.eatButton != null)
            pluck.eatButton.gameObject.SetActive(false);

       
    }

    #endregion


    // =====================
    // PLUCK ZONE
    // =====================

    #region PLUCK ZONE

    public void SetInPluckZone(bool inZone, PluckZone zone)
    {
        pluck.isInPluckZone = inZone;
        pluck.currentPluckZone = zone;

        if (inZone)
        {
            pluck.hitScore = 0;
            pluck.hasTakenFruit = false;
            pluck.isPlucking = false;
            pluck.isPluckRigUp = false;
            pluck.pluckButton.gameObject.SetActive(true);

            if (armPluckRig != null)
                armPluckRig.weight = 1f;

            if (crossReferrence != null && !crossReferrence.activeSelf)
                crossReferrence.SetActive(true);

            crossReferrence.transform.localPosition = initialTransformCrossOffset;
            crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
                initialRotationCrossOffset
            );

            if (animator != null)
                animator.SetBool(PluckData.IS_PLUCK, false);

            InitRigBeforePluckCompletion();
        }
        else
        {
            pluck.ExitZone();
             pluck.pluckButton.gameObject.SetActive(false);
            InitRigAfterPluckCompletion();

            if (animator != null)
                animator.SetBool(PluckData.IS_PLUCK, false);

            if (armPluckRig != null)
                armPluckRig.weight = 0f;
        }
    }

    public void OnPluckButtonDown()
    {
        if (
            !pluck.isInPluckZone
            || isDrinking
            || isCasting
            || isGliding
            || isPulling
            || isSleeping
            || isBreathing
        )
            return;

        pluck.hitScore++;

        if (pluck.hitScore == pluck.maxHitScore)
        {
            FruitZone fruitZone = GameObject.FindAnyObjectByType<FruitZone>();
            fruitZone.isFruitFallTrigger = true;
            // pluck.eatButton.gameObject.SetActive(true);
            Debug.Log($"Hit score reached: {pluck.hitScore}");
            pluck.pluckButton.gameObject.SetActive(false);
            ResetPluckAnimationStateToDefault();
            InitRigAfterPluckCompletion();
        }

        crossReferrence.transform.localPosition = pluck.crossPluckOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            pluck.crossPluckRotationOffset
        );

        pluck.isPlucking = true;
        pluck.isPluckRigUp = false;
        isMoving = false;

        if (animator != null)
            animator.SetBool(PluckData.IS_PLUCK, true);

        StartCoroutine(PluckRigRoutine());
        CameraShake.Instance.ShakeMedium();
        Debug.Log("Pluck button pressed");
    }

    private IEnumerator PluckRigRoutine()
    {
        // Debug.Log("PluckRigRoutine() -- called");
        armPluckRig.weight = 1f;
        yield return null;
        pluck.isPluckRigUp = true;
    }

    private void HandlePluckRigDrop()
    {
        // Debug.Log("HandlePluckRigDrop -- called");

        if (!pluck.isPluckRigUp || armPluckRig == null)
            return;

        armPluckRig.weight = Mathf.Lerp(
            armPluckRig.weight,
            0f,
            Time.deltaTime * pluck.rigFallSpeed
        );

        if (armPluckRig.weight <= 0.01f)
        {
            armPluckRig.weight = 0f;
            pluck.isPluckRigUp = false;
            pluck.isPlucking = false;

            Debug.Log("Pluck cycle complete — ready for next press");

            if (pluck.hitScore == pluck.maxHitScore)
            {
                ResetPluckAnimationStateToDefault();
                InitRigAfterPluckCompletion();
            }
        }
    }

    private void InitRigAfterPluckCompletion()
    {
        rigBuilder.layers[0].active = true;
        rigBuilder.layers[1].active = true;
        rigBuilder.layers[2].active = true;
        rigBuilder.layers[3].active = false;
        rigBuilder.Build();
    }

    public void InitRigBeforePluckCompletion()
    {
        Debug.Log("InitRigBeforePluckCompletion() called");
        rigBuilder.layers[0].active = false;
        rigBuilder.layers[1].active = false;
        rigBuilder.layers[2].active = false;
        rigBuilder.layers[3].active = true;
        rigBuilder.Build();
    }

    private void ResetPluckAnimationStateToDefault()
    {
        Debug.Log("ResetPluckAnimationStateToDefault() called");
        
        OnFastStopTriggerEnter(this.transform.position, false, false);
        if (animator != null)
            animator.SetBool(PluckData.IS_PLUCK, false);

        crossReferrence.transform.localPosition = initialTransformCrossOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            initialRotationCrossOffset
        );
    }

    public void PlayerHasFruitTaken()
    {
        Debug.Log("Player has taken fruit");
        pluck.eatButton.gameObject.SetActive(true);
        pluck.hasTakenFruit = true;
    }

    #endregion
}
