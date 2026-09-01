using UnityEngine;
using UnityEngine.Events;

public class AnimationEventTrigger : MonoBehaviour
{
    [Header("Event To Trigger")]
    [SerializeField] private UnityEvent onTriggered;

    [Header("Animation Completed Event")]
    [Tooltip("Invoked separately, typically via an Animation Event placed at the end of the clip.")]
    [SerializeField] private UnityEvent onAnimationCompleted;

    [Header("Navigation")]
    [Tooltip("If enabled, navigation will be unlocked when this event is triggered.")]
    [SerializeField] private bool enableNavigation = false;

    /// <summary>
    /// Call this method to invoke the event
    /// </summary>
    public void TriggerEvent()
    {
        // Invoke assigned event
        onTriggered?.Invoke();

        // Unlock navigation if enabled
        if (enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    /// <summary>
    /// Call this method (e.g. from an Animation Event at the end of a clip) 
    /// to invoke the "animation completed" event.
    /// </summary>
    public void TriggerAnimationCompleted()
    {
        onAnimationCompleted?.Invoke();
    }
}