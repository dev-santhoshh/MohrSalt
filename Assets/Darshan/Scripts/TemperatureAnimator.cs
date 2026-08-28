using UnityEngine;
using TMPro;
using System.Collections;

public class TemperatureAnimator : MonoBehaviour
{
    [Header("UI Reference")]
    public TMP_Text temperatureText;

    [Header("Trigger Settings")]
    [Tooltip("If checked, the temperature will start increasing immediately when this object turns on.")]
    public bool playOnEnable = true;

    [Header("Animation Settings")]
    public float startValue = 0f;
    public float targetValue = 40f;
    public float duration = 3.0f;
    [Tooltip("Text added after the number, e.g., '°C' or ' degrees'")]
    public string suffix = "°C";

    [Header("Slide Transition")]
    [Tooltip("Check this to automatically jump to the next slide when it hits 40. Uncheck to just unlock the Next button.")]
    public bool autoGoToNextSlide = true;

    private void OnEnable()
    {
        // Automatically start when the object is activated
        if (playOnEnable)
        {
            StartTemperatureIncrease();
        }
    }

    public void StartTemperatureIncrease()
    {
        if (temperatureText == null) return;
        StopAllCoroutines();
        StartCoroutine(AnimateTemperature());
    }

    private IEnumerator AnimateTemperature()
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float currentValue = Mathf.Lerp(startValue, targetValue, elapsedTime / duration);

            // Updates text to a whole number
            temperatureText.text = Mathf.RoundToInt(currentValue).ToString() + suffix;

            yield return null;
        }

        // Snap to exactly 40 at the end
        temperatureText.text = Mathf.RoundToInt(targetValue).ToString() + suffix;

        // Unlock navigation
        PageNavigationController.RequestNavigationUnlock();

        // Automatically move to the next slide
        if (autoGoToNextSlide && PageNavigationController.Instance != null)
        {
            PageNavigationController.Instance.NextPage();
        }
    }
}