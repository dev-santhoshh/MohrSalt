using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
        PageNavController.OnPageChanged += HandlePageChanged;
    }

    public void OnDisable()
    {
        PageNavController.OnPageChanged -= HandlePageChanged;
    }

    public void Start()
    {
        HandlePageChanged(PageNavController.CurrentIndex);
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

[System.Serializable]
public class PageData
{
    [Header("Page Name / Page No")]
    public string pageName;

    [TextArea]
    public string pageText;

    [Header("Display Options")]
    public bool showDialogBox;
    public bool showAlternatePanels;

    [Header("Alternate Panels For This Page")]
    public List<AlternatePanelData> alternatePanels;

    // 🚀 NEW
    [Header("Object Visibility For This Page")]
    [Tooltip("List of GameObjects to hide/unhide when this page is shown")]
    public List<PageObjectVisibility> objectVisibilityList;
}

[System.Serializable]
public class AlternatePanelData
{
    public GameObject panel;

    [Tooltip("If enabled, this panel will remain active in upcoming pages")]
    public bool stayInUpcomingPages;

    [Header("Enable Once Feature")]
    [Tooltip("If enabled, panel will activate only once and never again on revisit")]
    public bool enableOnce;

    [HideInInspector] public bool hasBeenEnabledOnce;
}

// 🚀 NEW: one GameObject + one bool, per element
[System.Serializable]
public class PageObjectVisibility
{
    public GameObject targetObject;

    [Tooltip("ON = object is hidden on this page. OFF = object is shown on this page.")]
    public bool hideOnThisPage;
}

#if UNITY_EDITOR
[CustomEditor(typeof(UIControllerPrompt))]
[CanEditMultipleObjects]
public class UIControllerPromptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Name Pages"))
        {
            foreach (var t in targets)
            {
                UIControllerPrompt controller = (UIControllerPrompt)t;
                NamePages(controller);
            }
        }
    }

    private void NamePages(UIControllerPrompt controller)
    {
        SerializedObject so = new SerializedObject(controller);
        SerializedProperty pagesProp = so.FindProperty("pages");

        if (pagesProp == null || pagesProp.arraySize == 0)
        {
            Debug.LogWarning("No pages found to rename.");
            return;
        }

        for (int i = 0; i < pagesProp.arraySize; i++)
        {
            SerializedProperty page = pagesProp.GetArrayElementAtIndex(i);
            SerializedProperty nameProp = page.FindPropertyRelative("pageName");

            if (nameProp != null)
            {
                nameProp.stringValue = $"Page {i + 1}";
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(controller);
    }
}
#endif