using System.Collections;
using UnityEngine;
using UnityEngine.Animations.Rigging;

//PlayerScript.Glide.cs
public partial class PlayerScript
{
    // =====================
    // OASIS / SAIL
    // =====================

    #region SAIL

    public void OasisZoneOnTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StartSail();
            ct.enableEvents?.Invoke();
            sailData.sailCross.GetComponent<GlideTrigger>().IsPlayerGliding = true;
        }
    }

    public void OasisZoneOnExitTrigger(CameraTrigger ct)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StopSail();
            ct.disableEvents?.Invoke();
            sailData.sailCross.GetComponent<GlideTrigger>().IsPlayerGliding = false;
        }
    }

    public void StartSail()
    {
        sailData.isSailing = true;
        animator.SetBool(IS_GLIDING, true);
        isMoving = false;
        crossReferrence.SetActive(false);
        InitSail();
    }

    public void StopSail()
    {
        sailData.isSailing = false;
        this.transform.parent = null;
        animator.SetBool(IS_GLIDING, false);
        crossReferrence.SetActive(true);
        sailData.sailCross.SetActive(false);
    }

    private void InitSail()
    {
        sailData.sailCross.SetActive(true);
        this.transform.SetParent(sailData.sailCross.transform);
        glideData.glideRig.weight = 1;
    }

    #endregion

    // =====================
    // GLIDING — generic
    // =====================
    #region GLIDING

    public void GlideZoneOnTrigger(CameraTrigger ct, GlideData data)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StartGliding(data);
            ct.enableEvents?.Invoke();
            data.gliderCross.GetComponent<GlideTrigger>().IsPlayerGliding = true;
        }
    }

    public void GlideZoneOnExitTrigger(CameraTrigger ct, GlideData data)
    {
        if (!isDrinking && !isBreathing && !isCasting)
        {
            StopGliding(data);
            ct.disableEvents?.Invoke();
            data.gliderCross.GetComponent<GlideTrigger>().IsPlayerGliding = false;
        }
    }

    public void StartGliding(GlideData data)
    {
        isGliding = true;
        animator.SetBool(IS_GLIDING, true);
        isMoving = false;
        crossReferrence.SetActive(false);
        InitGlider(data);
    }

    public void StopGliding(GlideData data)
    {
        isGliding = false;
        this.transform.parent = null;
        animator.SetBool(IS_GLIDING, false);
        data.glideRig.weight = 0;
        transform.localScale = initialPlayerScale;

        crossReferrence.SetActive(true);
        crossReferrence.transform.SetParent(handTransform);
        crossReferrence.transform.localPosition = initialTransformCrossOffset;
        crossReferrence.transform.localRotation = UnityEngine.Quaternion.Euler(
            initialRotationCrossOffset
        );
        data.gliderCross.SetActive(false);
    }

    private void InitGlider(GlideData data)
    {
        data.gliderCross.SetActive(true);
        this.transform.SetParent(data.gliderCross.transform);
        data.glideRig.weight = 1;
    }

    public GlideData GetGlideDataById(string id)
    {
        if (mountainGlideData.id == id)
            return mountainGlideData;
        return glideData; // "default" fallback
    }

    #endregion
}
