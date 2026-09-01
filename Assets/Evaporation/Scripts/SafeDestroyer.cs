using UnityEngine;

public class SafeDestroyer : MonoBehaviour
{
    private bool isDestroyed = false;

    // 🎯 Call this from UnityEvent
    public void DestroySelf()
    {
        if (isDestroyed) return;

        isDestroyed = true;

        // 🔒 Disable first to avoid interaction before destroy
        gameObject.SetActive(false);

        // 🗑 Destroy safely
        Destroy(gameObject);
    }

    // 🎯 Destroy with delay (optional)
    public void DestroyWithDelay(float delay)
    {
        if (isDestroyed) return;

        isDestroyed = true;

        gameObject.SetActive(false);
        Destroy(gameObject, delay);
    }
}