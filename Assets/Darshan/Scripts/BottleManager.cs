using UnityEngine;

public class BottleManager : MonoBehaviour
{
    [Header("Global UI Panels")]
    public GameObject wrongPanel;

    [Header("All Items Reference")]
    public SelectItem[] allItems = new SelectItem[4];

    private int correctItemsFound = 0;
    private int totalItemsClicked = 0;
    private int requiredCorrectItems = 2;

    private void Start()
    {
        if (wrongPanel != null) wrongPanel.SetActive(false);
    }

    public void RegisterClick(bool wasCorrect)
    {
        totalItemsClicked++;
        Debug.Log($"<color=cyan>Bottle Clicked! Total clicked so far: {totalItemsClicked} / 4</color>");

        if (wasCorrect)
        {
            if (wrongPanel != null) wrongPanel.SetActive(false);

            correctItemsFound++;
            Debug.Log($"<color=green>Correct bottle found! ({correctItemsFound}/{requiredCorrectItems})</color>");

            if (correctItemsFound >= requiredCorrectItems)
            {
                Debug.Log("<color=yellow>Both correct bottles found. Unlocking next page.</color>");
                PageNavigationController.RequestNavigationUnlock();
            }
        }
        else
        {
            Debug.Log("<color=red>Wrong bottle clicked. Showing wrong panel.</color>");
            if (wrongPanel != null) wrongPanel.SetActive(true);
        }

        if (totalItemsClicked >= 4)
        {
            Debug.Log("<color=magenta>4/4 Bottles clicked! Disabling the wrong ones now.</color>");
            DisableWrongBottles();
        }
    }

    private void DisableWrongBottles()
    {
        foreach (var item in allItems)
        {
            if (item != null && !item.isCorrect)
            {
                if (item.bottle3DModel != null)
                {
                    item.bottle3DModel.SetActive(false);

                    // Fallback: force-disable all renderers under it too
                    Renderer[] renderers = item.bottle3DModel.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in renderers)
                    {
                        r.enabled = false;
                    }
                }

                if (item.correctTick != null) item.correctTick.SetActive(false);
                if (item.wrongTick != null) item.wrongTick.SetActive(false);

                item.gameObject.SetActive(false);
            }
        }
    }
}