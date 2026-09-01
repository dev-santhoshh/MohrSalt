using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class LiquidFillController : MonoBehaviour
{
    [Header("Increase Settings")]
    [Tooltip("Renderer used when increasing liquid.")]
    [SerializeField] private Renderer increaseRenderer;

    [Tooltip("Material used when increasing liquid.")]
    [SerializeField] private Material increaseMaterial;

    [Tooltip("Active ONLY while filling/increasing liquid.")]
    [SerializeField] private GameObject increaseFlowObject;

    [Tooltip("The local _FillHeight to reach when filling (e.g., 0.15).")]
    [SerializeField] private float targetIncreaseFillHeight = 0.15f;

    [Tooltip("Time in seconds to complete the filling process.")]
    [SerializeField] private float increaseDuration = 2.0f;

    [Header("Decrease Settings")]
    [Tooltip("Renderer used when decreasing liquid.")]
    [SerializeField] private Renderer decreaseRenderer;

    [Tooltip("Material used when decreasing liquid.")]
    [SerializeField] private Material decreaseMaterial;

    [Tooltip("Active ONLY while draining/decreasing liquid.")]
    [SerializeField] private GameObject decreaseFlowObject;

    [Tooltip("The local _FillHeight to reach when draining (e.g., 0.0 or -0.05).")]
    [SerializeField] private float targetDecreaseFillHeight = 0.0f;

    [Tooltip("Time in seconds to complete the draining process.")]
    [SerializeField] private float decreaseDuration = 2.0f;

    [Header("Completion Events")]
    [SerializeField] private UnityEvent onIncreaseCompleted;
    [SerializeField] private UnityEvent onDecreaseCompleted;

    // Shader Property Hash
    private static readonly int FillHeightProperty = Shader.PropertyToID("_FillHeight");

    // IMPORTANT: separate coroutine slots so Increase and Decrease never cancel each other
    private Coroutine increaseRoutine;
    private Coroutine decreaseRoutine;

    // Separate property blocks too, since two renderers can be animated in the same frame
    private MaterialPropertyBlock increasePropertyBlock;
    private MaterialPropertyBlock decreasePropertyBlock;

    private void Awake()
    {
        increasePropertyBlock = new MaterialPropertyBlock();
        decreasePropertyBlock = new MaterialPropertyBlock();

        if (increaseFlowObject != null) increaseFlowObject.SetActive(false);
        if (decreaseFlowObject != null) decreaseFlowObject.SetActive(false);
    }

    // =========================================================================
    // ANIMATION EVENT-FRIENDLY WRAPPERS (no-arg, shows up cleanly in the dropdown)
    // =========================================================================

    /// <summary>Call this from an Animation Event to fill/increase.</summary>
    public void PlayIncrease() => StartFill(true);

    /// <summary>Call this from an Animation Event to drain/decrease.</summary>
    public void PlayDecrease() => StartFill(false);

    // =========================================================================
    // SINGLE COMBINED PUBLIC API FUNCTION
    // =========================================================================

    /// <param name="isIncreasing">True = Fill / Increase, False = Drain / Decrease</param>
    public void StartFill(bool isIncreasing)
    {
        Renderer targetRenderer = isIncreasing ? increaseRenderer : decreaseRenderer;
        Material targetMaterial = isIncreasing ? increaseMaterial : decreaseMaterial;
        GameObject flowObject = isIncreasing ? increaseFlowObject : decreaseFlowObject;
        float targetHeight = isIncreasing ? targetIncreaseFillHeight : targetDecreaseFillHeight;
        float duration = isIncreasing ? increaseDuration : decreaseDuration;

        if (targetRenderer == null)
        {
            Debug.LogWarning($"[LiquidFillController] {(isIncreasing ? "Increase" : "Decrease")}Renderer is not assigned on '{name}'. Skipping fill call.", this);
            return;
        }

        if (targetMaterial == null)
        {
            Debug.LogWarning($"[LiquidFillController] {(isIncreasing ? "Increase" : "Decrease")}Material is not assigned on '{name}'. Will animate using the renderer's current material instead.", this);
        }

        SetupAndAnimate(isIncreasing, targetRenderer, targetMaterial, flowObject, targetHeight, duration);
    }

    // =========================================================================
    // CORE LOGIC
    // =========================================================================

    private void SetupAndAnimate(bool isIncreasing, Renderer targetRenderer, Material targetMaterial, GameObject flowObject, float targetHeight, float duration)
    {
        if (targetMaterial != null)
        {
            targetRenderer.material = targetMaterial;
        }

        // Only stop the coroutine belonging to THIS direction, never the other one
        if (isIncreasing)
        {
            if (increaseRoutine != null) StopCoroutine(increaseRoutine);
            increaseRoutine = StartCoroutine(AnimateFill(isIncreasing, targetRenderer, flowObject, targetHeight, duration));
        }
        else
        {
            if (decreaseRoutine != null) StopCoroutine(decreaseRoutine);
            decreaseRoutine = StartCoroutine(AnimateFill(isIncreasing, targetRenderer, flowObject, targetHeight, duration));
        }
    }

    private IEnumerator AnimateFill(bool isIncreasing, Renderer activeRenderer, GameObject flowObject, float targetHeight, float duration)
    {
        MaterialPropertyBlock block = isIncreasing ? increasePropertyBlock : decreasePropertyBlock;

        float startHeight = GetCurrentFillHeight(activeRenderer, block);
        float elapsedTime = 0f;

        if (flowObject != null)
            flowObject.SetActive(true);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);

            float currentHeight = Mathf.Lerp(startHeight, targetHeight, Mathf.SmoothStep(0f, 1f, t));
            SetFillHeight(activeRenderer, currentHeight, block);

            yield return null;
        }

        SetFillHeight(activeRenderer, targetHeight, block);

        if (flowObject != null)
            flowObject.SetActive(false);

        if (isIncreasing)
        {
            increaseRoutine = null;
            onIncreaseCompleted?.Invoke();
        }
        else
        {
            decreaseRoutine = null;
            onDecreaseCompleted?.Invoke();
        }
    }

    // =========================================================================
    // MATERIAL PROPERTY HELPERS
    // =========================================================================

    private float GetCurrentFillHeight(Renderer rend, MaterialPropertyBlock block)
    {
        if (rend == null) return 0f;

        rend.GetPropertyBlock(block);

        if (rend.HasPropertyBlock() && block.GetFloat(FillHeightProperty) != 0f)
        {
            return block.GetFloat(FillHeightProperty);
        }

        return rend.sharedMaterial != null
            ? rend.sharedMaterial.GetFloat(FillHeightProperty)
            : 0f;
    }

    private void SetFillHeight(Renderer rend, float value, MaterialPropertyBlock block)
    {
        if (rend == null) return;

        rend.GetPropertyBlock(block);
        block.SetFloat(FillHeightProperty, value);
        rend.SetPropertyBlock(block);
    }
}