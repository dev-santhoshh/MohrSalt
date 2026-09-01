using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class TouchOrClickEvent : MonoBehaviour
{
    // ========================= BASE EVENT =========================
    [Header("Base Touch Event")]
    public UnityEvent OnTouched;

    // ========================= CONDITIONAL EVENTS =========================
    [System.Serializable]
    public class ConditionalEvent
    {
        [Header("Page Condition")]
        [Tooltip("Event will trigger only when current page index matches this value.")]
        public int requiredPageIndex;
        public UnityEvent onInvoked;

        [Header("Trigger Settings")]
        public bool allowMultipleTriggers = true;
        [HideInInspector] public bool hasTriggered;
    }

    [Header("Invoke When Page Index Matches")]
    public List<ConditionalEvent> conditionalEvents = new List<ConditionalEvent>();

    // ========================= CAMERA MOVE ON TOUCH =========================
    [Header("Camera Move On Touch")]
    [Tooltip("If enabled, clicking/touching this object will move the camera to the target transform.")]
    [SerializeField] private bool enableCameraMove = false;

    [Tooltip("Transform whose position (and rotation) the camera will move to.")]
    [SerializeField] private Transform cameraMoveTarget;

    [Tooltip("Delay (seconds) before the camera starts moving, after the object is touched.")]
    [SerializeField] private float cameraMoveWaitTime = 2f;

    [Tooltip("Duration (seconds) of the camera movement itself.")]
    [SerializeField] private float cameraMoveDuration = 1f;

    [Tooltip("If true, camera rotation will also be matched to the target transform.")]
    [SerializeField] private bool matchTargetRotation = true;

    [Tooltip("Invoked once the camera has finished moving and reached the target point.")]
    public UnityEvent OnCameraReachedTarget;

    private Coroutine cameraMoveRoutine;

    // ========================= SETTINGS =========================
    [Header("References")]
    [SerializeField] private Camera targetCamera;

    [Header("Behavior")]
    [SerializeField] private bool ignoreUI = true;

    [Header("Debug")]
    [Tooltip("Logs why a click was or wasn't registered on this object (UI blocking, wrong raycast hit, etc.).")]
    [SerializeField] private bool debugLogging = true;

    // ========================= INTERNAL =========================
    private Collider cachedCollider;

    // ========================= LIFECYCLE =========================
    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (targetCamera == null)
        {
            // Debug.LogError($"[{nameof(TouchOrClickEvent)}] Camera is not assigned on {gameObject.name}");
            targetCamera = Camera.main;
        }
    }

    private void OnEnable()
    {
        ResetAllConditionalTriggers();
    }

    private void Update()
    {
        if (targetCamera == null)
            return;

        // -------- MOUSE --------
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            ProcessPointer(Mouse.current.position.ReadValue());
        }

        // -------- TOUCH --------
        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                ProcessPointer(touch.position.ReadValue());
            }
        }
    }

    // ========================= INPUT PROCESSING =========================
    private void ProcessPointer(Vector2 screenPosition)
    {
        // Use the pointer id that matches the actual input source, so the
        // IsPointerOverGameObject() check and our debug raycast agree.
        // -1 = mouse. Touch uses its own finger/touch id.
        int activePointerId = -1;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            activePointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();

        bool overUI = EventSystem.current != null &&
            (activePointerId == -1
                ? EventSystem.current.IsPointerOverGameObject()
                : EventSystem.current.IsPointerOverGameObject(activePointerId));

        if (ignoreUI && overUI)
        {
            GameObject blockingObject = GetBlockingUIObject(screenPosition);

            // If the "blocking" UI element is actually this object itself (or a
            // child of it � e.g. it has a Graphic/Button on it for a world-space
            // canvas), that's not really a block: it just means EventSystem
            // registered the click via the UI raycaster instead of physics.
            // Treat it as a valid click on this object instead of bailing out.
            if (blockingObject != null && (blockingObject == gameObject || blockingObject.transform.IsChildOf(transform)))
            {
                if (debugLogging)
                    Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' click REGISTERED via UI raycaster " +
                               $"(hit '{blockingObject.name}', which is this object or a child of it � not a real blocker).");

                InvokeEvents();
                return;
            }

            if (debugLogging)
            {
                Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' click BLOCKED by UI panel: " +
                           $"{(blockingObject != null ? blockingObject.name : "unknown UI element (likely an invisible full-screen CanvasGroup/Image with Raycast Target enabled, or a canvas whose Event Camera doesn't match)")}. " +
                           $"(EventSystem.IsPointerOverGameObject({(activePointerId == -1 ? "" : activePointerId.ToString())}) returned true, so the raycast was skipped entirely.)");
            }
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            if (debugLogging)
                Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' click MISSED � raycast hit nothing at all.");
            return;
        }

        if (hit.collider != cachedCollider)
        {
            if (debugLogging)
                Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' click MISSED � raycast hit '{hit.collider.gameObject.name}' " +
                           $"(on layer '{LayerMask.LayerToName(hit.collider.gameObject.layer)}') instead of this object's collider. " +
                           $"That object is between the camera and '{gameObject.name}', or is overlapping it.");
            return;
        }

        if (debugLogging)
            Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' click REGISTERED successfully.");

        InvokeEvents();
    }

    // Best-effort lookup of which UI element the pointer is currently over,
    // for clearer debug output when EventSystem blocks a click.
    private GameObject GetBlockingUIObject(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return null;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        if (results.Count > 0)
            return results[0].gameObject;

        // Fallback: EventSystem.RaycastAll can miss a blocker if its canvas'
        // Event Camera doesn't match, or if it's an invisible full-screen
        // CanvasGroup (Blocks Raycasts = true, Alpha = 0) rather than a Graphic.
        // Manually check every active GraphicRaycaster/CanvasGroup as a fallback.
        GraphicRaycaster[] raycasters = FindObjectsOfType<GraphicRaycaster>();
        foreach (var raycaster in raycasters)
        {
            List<RaycastResult> manualResults = new List<RaycastResult>();
            raycaster.Raycast(pointerData, manualResults);
            if (manualResults.Count > 0)
                return manualResults[0].gameObject;
        }

        return null;
    }

    private void InvokeEvents()
    {
        OnTouched?.Invoke();

        // -------- OPTIONAL CAMERA MOVE --------
        if (enableCameraMove && cameraMoveTarget != null && targetCamera != null)
        {
            if (cameraMoveRoutine != null)
                StopCoroutine(cameraMoveRoutine);

            cameraMoveRoutine = StartCoroutine(MoveCameraRoutine());
        }

        int currentPage = PageNavigationController.CurrentIndex;
        foreach (var entry in conditionalEvents)
        {
            // PAGE INDEX CHECK
            if (entry.requiredPageIndex != currentPage)
            {
                if (debugLogging)
                    Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' � conditional event skipped: " +
                               $"requires page {entry.requiredPageIndex} but current page is {currentPage}.");
                continue;
            }

            if (!entry.allowMultipleTriggers && entry.hasTriggered)
            {
                if (debugLogging)
                    Debug.Log($"[TouchOrClickEvent] '{gameObject.name}' � conditional event for page {entry.requiredPageIndex} " +
                               $"already triggered once and allowMultipleTriggers is false, skipping.");
                continue;
            }

            entry.hasTriggered = true;
            entry.onInvoked?.Invoke();
        }
    }

    // ========================= CAMERA MOVE ROUTINE =========================
    private IEnumerator MoveCameraRoutine()
    {
        if (cameraMoveWaitTime > 0f)
            yield return new WaitForSeconds(cameraMoveWaitTime);

        Transform camTransform = targetCamera.transform;
        Vector3 startPos = camTransform.position;
        Quaternion startRot = camTransform.rotation;

        Vector3 endPos = cameraMoveTarget.position;
        Quaternion endRot = matchTargetRotation ? cameraMoveTarget.rotation : startRot;

        float elapsed = 0f;
        float duration = Mathf.Max(0.0001f, cameraMoveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            camTransform.position = Vector3.Lerp(startPos, endPos, t);
            if (matchTargetRotation)
                camTransform.rotation = Quaternion.Slerp(startRot, endRot, t);

            yield return null;
        }

        camTransform.position = endPos;
        if (matchTargetRotation)
            camTransform.rotation = endRot;

        cameraMoveRoutine = null;

        // -------- REACHED TARGET EVENT --------
        OnCameraReachedTarget?.Invoke();
    }

    // ========================= PUBLIC API =========================
    public void ResetAllConditionalTriggers()
    {
        foreach (var entry in conditionalEvents)
        {
            entry.hasTriggered = false;
        }
    }
}