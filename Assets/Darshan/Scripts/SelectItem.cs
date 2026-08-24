using UnityEngine;

public class SelectItem : MonoBehaviour
{
    [Header("Settings")]
    public bool isCorrect;

    [Header("3D Model Reference")]
    public GameObject bottle3DModel;

    [Header("Visuals")]
    public GameObject correctTick;
    public GameObject wrongTick;

    [Header("Manager Reference")]
    public BottleManager manager;

    private bool hasBeenSelected = false;

    private void Start()
    {
        if (correctTick != null) correctTick.SetActive(false);
        if (wrongTick != null) wrongTick.SetActive(false);
    }

    public void OnBottleClicked()
    {
        if (hasBeenSelected) return;

        hasBeenSelected = true;

        if (isCorrect)
        {
            if (correctTick != null) correctTick.SetActive(true);
            if (manager != null) manager.RegisterClick(true);
        }
        else
        {
            if (wrongTick != null) wrongTick.SetActive(true);
            if (manager != null) manager.RegisterClick(false);
        }
    }
}