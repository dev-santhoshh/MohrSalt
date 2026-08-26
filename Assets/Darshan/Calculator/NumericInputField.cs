using UnityEngine;
using UnityEngine.UI;
using TMPro;

// -----------------------------------------------------------------
// One of these per input box (the 3 blanks + the final answer box).
// Tap it to select it, then num-pad digits get appended here.
// Manager calls SetCorrect()/SetIncorrect() to show a checkmark or
// error state once the value is validated.
// -----------------------------------------------------------------
public class NumericInputField : MonoBehaviour
{
    [Header("Display")]
    public TextMeshProUGUI valueText;

    [Header("Selection Visual")]
    [Tooltip("Optional - an outline/background image that highlights when this field is selected.")]
    public GameObject selectedHighlight;

    [Header("Validation Visuals")]
    public GameObject correctCheckmark;
    public GameObject incorrectMark;

    [Header("Button (tap to select this field)")]
    public Button selectButton;

    private string currentValue = "";
    private NumPadController numPad;

    public bool IsCorrect { get; private set; }

    private void Awake()
    {
        if (selectButton != null)
            selectButton.onClick.AddListener(SelectThisField);

        ClearVisualState();
    }

    public void Init(NumPadController pad)
    {
        numPad = pad;
    }

    private void SelectThisField()
    {
        if (numPad != null)
            numPad.SetActiveField(this);
    }

    public void SetSelected(bool isSelected)
    {
        if (selectedHighlight != null)
            selectedHighlight.SetActive(isSelected);
    }

    public void AppendDigit(string digit)
    {
        currentValue += digit;
        UpdateDisplay();
    }

    public void Backspace()
    {
        if (currentValue.Length > 0)
        {
            currentValue = currentValue.Substring(0, currentValue.Length - 1);
            UpdateDisplay();
        }
    }

    public void Clear()
    {
        currentValue = "";
        IsCorrect = false;
        UpdateDisplay();
        ClearVisualState();
    }

    public string GetValue() => currentValue;

    private void UpdateDisplay()
    {
        if (valueText != null)
            valueText.text = currentValue;
    }

    public void SetCorrect()
    {
        IsCorrect = true;
        if (correctCheckmark != null) correctCheckmark.SetActive(true);
        if (incorrectMark != null) incorrectMark.SetActive(false);
    }

    public void SetIncorrect()
    {
        IsCorrect = false;
        if (correctCheckmark != null) correctCheckmark.SetActive(false);
        if (incorrectMark != null) incorrectMark.SetActive(true);
    }

    private void ClearVisualState()
    {
        if (correctCheckmark != null) correctCheckmark.SetActive(false);
        if (incorrectMark != null) incorrectMark.SetActive(false);
    }
}