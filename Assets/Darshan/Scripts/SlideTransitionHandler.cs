using UnityEngine;
using System.Collections;

public class SlideTransitionHandler : MonoBehaviour
{
    [Header("Step 1: Disable This Slide's Objects")]
    public GameObject[] objectsToDisable;

    [Header("Step 2: Camera Movement")]
    public bool moveCamera = false;
    public Transform cameraTransform;
    public Transform targetCameraPosition;
    public float cameraMoveDuration = 1f;

    [Header("Step 3: Enable Next Slide's Objects")]
    public GameObject[] nextSlideObjects;

    [Header("Step 4: Navigation")]
    public bool enableNavigation = true;

    // Called by AnimationEventTrigger when the animation finishes
    public void RunTransition()
    {
        StartCoroutine(TransitionSequence());
        Debug.Log("RunTransition called");
    }

    private IEnumerator TransitionSequence()
    {
        // 1. Disable old slide's objects (also disables all their children)
        foreach (var obj in objectsToDisable)
        {
            if (obj != null) obj.SetActive(false);
        }

        // 2. Move camera once, wait until fully done
        if (moveCamera && cameraTransform != null && targetCameraPosition != null)
        {
            if (cameraMoveDuration <= 0f)
            {
                cameraTransform.position = targetCameraPosition.position;
                cameraTransform.rotation = targetCameraPosition.rotation;
            }
            else
            {
                yield return StartCoroutine(MoveCameraSmooth());
            }
        }

        // 3. Enable next slide's objects
        foreach (var obj in nextSlideObjects)
        {
            if (obj != null) obj.SetActive(true);
        }

        // 4. Only now unlock the Next button
        if (enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    private IEnumerator MoveCameraSmooth()
    {
        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;
        float elapsed = 0f;

        while (elapsed < cameraMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / cameraMoveDuration;
            cameraTransform.position = Vector3.Lerp(startPos, targetCameraPosition.position, t);
            cameraTransform.rotation = Quaternion.Lerp(startRot, targetCameraPosition.rotation, t);
            yield return null;
        }

        cameraTransform.position = targetCameraPosition.position;
        cameraTransform.rotation = targetCameraPosition.rotation;
    }
}