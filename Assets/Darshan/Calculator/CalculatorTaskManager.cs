using UnityEngine;
using System.Collections.Generic;

// -----------------------------------------------------------------
// Holds the correct value for each NumericInputField on this page.
// Validates a field the instant its value changes (checkmark/error
// shown immediately). Once every field is correct, unlocks navigation.
// -----------------------------------------------------------------
public class CalculatorTaskManager : MonoBehaviour
{
    [System.Serializable]
    public class FieldAnswer
    {
        [Tooltip("The input field this answer applies to.")]
        public NumericInputField field;

        [Tooltip("The exact correct value for this field, as a string (e.g. '3.5', '132', '278').")]
        public string correctValue;
    }

    [Header("Correct Answers Per Field")]
    public List<FieldAnswer> answers = new List<FieldAnswer>();

    [Header("Navigation")]
    [Tooltip("If true, unlocks Next automatically once every field is correct.")]
    public bool enableNavigation = true;

    public void ValidateField(NumericInputField field)
    {
        FieldAnswer match = answers.Find(a => a.field == field);
        if (match == null) return;

        string enteredValue = field.GetValue();

        if (IsMatch(enteredValue, match.correctValue))
        {
            field.SetCorrect();
        }
        else
        {
            field.SetIncorrect();
        }

        CheckAllCorrect();
    }

    private bool IsMatch(string entered, string correct)
    {
        // Try numeric comparison first (handles "3.50" vs "3.5" etc.)
        if (float.TryParse(entered, out float enteredNum) &&
            float.TryParse(correct, out float correctNum))
        {
            return Mathf.Approximately(enteredNum, correctNum);
        }

        // Fallback to exact string match
        return entered == correct;
    }

    private void CheckAllCorrect()
    {
        foreach (var answer in answers)
        {
            if (answer.field == null || !answer.field.IsCorrect)
                return; // at least one field isn't correct yet
        }

        Debug.Log("[CalculatorTask] All fields correct! Unlocking navigation.");

        if (enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    // Called by NumPadController's Retry button
    public void ResetTask()
    {
        // NumPadController already clears each field's display/value;
        // this just resets any manager-side state if needed later.
        Debug.Log("[CalculatorTask] Task reset via Retry.");
    }
}