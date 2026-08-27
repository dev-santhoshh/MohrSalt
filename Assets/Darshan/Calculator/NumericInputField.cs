using UnityEngine;
using TMPro;

public class NumericInputField : MonoBehaviour
{
    [Header("The real TMP Input Field")]
    public TMP_InputField inputField;

    [Header("Selection Visual")]
    [Tooltip("Optional - an outline/background image that highlights when this field is selected.")]
    public GameObject selectedHighlight;

    [Header("Validation Visuals")]
    public GameObject correctCheckmark;
    public GameObject incorrectMark;

    private NumPadController numPad;

    public bool IsCorrect { get; private set; }

    private void Awake()
    {
        if (inputField != null)
        {
            inputField.onSelect.AddListener(_ => OnFieldSelected());
        }

        ClearVisualState();
    }

    public void Init(NumPadController pad)
    {
        numPad = pad;
    }

    private void OnFieldSelected()
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
        if (inputField == null) return;
        inputField.text += digit;
        inputField.caretPosition = inputField.text.Length;
    }

    public void Backspace()
    {
        if (inputField == null) return;
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
            inputField.caretPosition = inputField.text.Length;
        }
    }

    public void Clear()
    {
        if (inputField != null)
            inputField.text = "";

        IsCorrect = false;
        ClearVisualState();
    }

    public void SetValue(string value)
    {
        if (inputField != null)
            inputField.text = value;
    }

    public string GetValue() => inputField != null ? inputField.text : "";

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