using UnityEngine;
using TMPro;
using System.Collections;

public class TextValueAnimator : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text textComponent; // Drag your TextMeshPro object here

    [Header("Animation Settings")]
    [Tooltip("The final value the text should reach")]
    public float targetValue = 3.5000f;

    [Tooltip("Time in seconds to complete the animation")]
    public float duration = 2.0f;

    [Tooltip("Format string (e.g., 'F4' for 4 decimal places, 'F0' for whole numbers)")]
    public string numberFormat = "F4";

    private Coroutine animationCoroutine;

    // Call this public function from your Animation Event
    public void StartTextAnimation()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        animationCoroutine = StartCoroutine(AnimateValue());
    }

    private IEnumerator AnimateValue()
    {
        float elapsedTime = 0f;
        float startValue = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            // Interpolate the value from 0 to targetValue over the specified duration
            float currentValue = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);

            if (textComponent != null)
            {
                textComponent.text = currentValue.ToString(numberFormat);
            }

            yield return null;
        }

        // Snap to the exact target value at the end
        if (textComponent != null)
        {
            textComponent.text = targetValue.ToString(numberFormat);
        }
    }
}