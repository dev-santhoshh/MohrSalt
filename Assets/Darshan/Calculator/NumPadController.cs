using UnityEngine;

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

    public void OnDigitPressed(string digit)
    {
        if (activeField == null) return;
        activeField.AppendDigit(digit);
    }

    public void OnDecimalPressed()
    {
        if (activeField == null) return;

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

    // Wire the Check button to this - validates ONLY the currently active field
    public void OnCheckPressed()
    {
        if (activeField == null)
        {
            Debug.LogWarning("[NumPad] Check pressed but no field is selected.");
            return;
        }

        if (taskManager != null)
            taskManager.CheckField(activeField);
    }

    // Wire the Autofill button to this - fills every field with the correct answer
    public void OnAutofillPressed()
    {
        if (taskManager != null)
            taskManager.AutofillAnswers();
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