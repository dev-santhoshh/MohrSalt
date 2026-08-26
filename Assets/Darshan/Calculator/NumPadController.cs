using UnityEngine;
using UnityEngine.UI;

// -----------------------------------------------------------------
// Wire each of the 9 number buttons (and optional '.' / '+-' /
// backspace buttons) to call OnDigitPressed("1") ... OnDigitPressed("9"),
// OnDecimalPressed(), OnBackspacePressed() from their OnClick() events.
//
// Call SetActiveField() when the player taps an input box - typically
// from NumericInputField itself (already wired via Init()).
// -----------------------------------------------------------------
public class NumPadController : MonoBehaviour
{
    [Header("All Input Fields On This Page")]
    public NumericInputField[] allFields;

    [Header("Manager Reference")]
    public CalculatorTaskManager taskManager;

    private NumericInputField activeField;

    private void Awake()
    {
        foreach (var field in allFields)
        {
            if (field != null)
                field.Init(this);
        }
    }

    public void SetActiveField(NumericInputField field)
    {
        if (activeField != null)
            activeField.SetSelected(false);

        activeField = field;

        if (activeField != null)
            activeField.SetSelected(true);
    }

    // Wire number buttons (1-9, 0) to call this with their digit string
    public void OnDigitPressed(string digit)
    {
        if (activeField == null) return;

        activeField.AppendDigit(digit);

        if (taskManager != null)
            taskManager.ValidateField(activeField);
    }

    public void OnDecimalPressed()
    {
        if (activeField == null) return;

        // Avoid multiple decimal points
        if (!activeField.GetValue().Contains("."))
        {
            activeField.AppendDigit(".");
        }
    }

    public void OnPlusMinusPressed()
    {
        if (activeField == null) return;

        string current = activeField.GetValue();
        if (current.StartsWith("-"))
        {
            // Remove leading minus - re-set the value by clearing and rebuilding
            activeField.Clear();
            activeField.AppendDigit(current.Substring(1));
        }
        else if (!string.IsNullOrEmpty(current))
        {
            activeField.Clear();
            activeField.AppendDigit("-" + current);
        }
    }

    public void OnBackspacePressed()
    {
        if (activeField == null) return;
        activeField.Backspace();
    }

    // Wire the Retry button to this
    public void OnRetryPressed()
    {
        foreach (var field in allFields)
        {
            if (field != null)
                field.Clear();
        }

        if (taskManager != null)
            taskManager.ResetTask();
    }
}