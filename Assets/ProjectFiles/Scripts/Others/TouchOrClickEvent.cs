using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
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
        if (ignoreUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
            return;

        if (hit.collider != cachedCollider)
            return;

        InvokeEvents();
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
                continue;

            if (!entry.allowMultipleTriggers && entry.hasTriggered)
                continue;

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