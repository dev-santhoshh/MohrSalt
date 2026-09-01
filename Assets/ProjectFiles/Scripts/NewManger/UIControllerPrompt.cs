using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIControllerPrompt : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField] public PageData[] pages;

    [Header("Dialog UI")]
    [SerializeField] public GameObject dialogPanel;
    [SerializeField] public Image dialogImage;
    [SerializeField] public TextMeshProUGUI dialogText;

    [Header("Common Dialog Sprite")]
    [SerializeField] public Sprite commonDialogSprite;

    public int currentPageIndex = -1;

    public void OnEnable()
    {
        PageNavigationController.OnPageChanged += HandlePageChanged;
    }

    public void OnDisable()
    {
        PageNavigationController.OnPageChanged -= HandlePageChanged;
    }

    public void Start()
    {
        HandlePageChanged(PageNavigationController.CurrentIndex);
    }

    public void HandlePageChanged(int index)
    {
        if (index < 0 || index >= pages.Length)
            return;

        currentPageIndex = index;
        ShowPage(index);
    }

    public void ShowPage(int index)
    {
        PageData page = pages[index];

        if (dialogPanel)
            dialogPanel.SetActive(false);

        ResetAllPanels();

        // 🚀 NEW: apply this page's own GameObject hide/unhide list (does not affect anything below)
        ApplyPageObjectVisibility(index);

        if (!page.showDialogBox && !page.showAlternatePanels)
            return;

        if (page.showDialogBox)
        {
            if (dialogPanel)
                dialogPanel.SetActive(true);

            if (dialogText)
                dialogText.text = page.pageText;

            if (dialogImage)
                dialogImage.sprite = commonDialogSprite;
        }

        ApplyPanelVisibility(index);
    }

    public void ResetAllPanels()
    {
        foreach (var p in pages)
        {
            if (p.alternatePanels == null)
                continue;

            foreach (var panelData in p.alternatePanels)
            {
                if (panelData != null && panelData.panel != null)
                    panelData.panel.SetActive(false);
            }
        }
    }

    public void ApplyPanelVisibility(int currentIndex)
    {
        for (int i = 0; i <= currentIndex; i++)
        {
            PageData page = pages[i];

            if (!page.showAlternatePanels || page.alternatePanels == null)
                continue;

            foreach (var panelData in page.alternatePanels)
            {
                if (panelData == null || panelData.panel == null)
                    continue;

                // 🚀 NEW: Enable Once Logic
                if (panelData.enableOnce && panelData.hasBeenEnabledOnce)
                    continue;

                if (i == currentIndex)
                {
                    panelData.panel.SetActive(true);

                    if (panelData.enableOnce)
                        panelData.hasBeenEnabledOnce = true;
                }
                else if (panelData.stayInUpcomingPages)
                {
                    // Only allow staying panels if not restricted by enableOnce
                    if (!panelData.enableOnce || !panelData.hasBeenEnabledOnce)
                    {
                        panelData.panel.SetActive(true);
                    }
                }
            }
        }
    }

    // 🚀 NEW: independent per-page GameObject visibility list (GameObject + one bool each)
    public void ApplyPageObjectVisibility(int index)
    {
        PageData page = pages[index];

        if (page.objectVisibilityList == null)
            return;

        foreach (var entry in page.objectVisibilityList)
        {
            if (entry == null || entry.targetObject == null)
                continue;

            // hideOnThisPage == true  -> object is DISABLED on this page
            // hideOnThisPage == false -> object is ENABLED on this page
            entry.targetObject.SetActive(!entry.hideOnThisPage);
        }
    }
}
