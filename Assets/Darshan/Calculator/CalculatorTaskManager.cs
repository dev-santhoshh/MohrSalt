using UnityEngine;
using System.Collections.Generic;

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

    // Called by NumPadController's Check button - validates only the
    // field the player is currently typing in.
    public void CheckField(NumericInputField field)
    {
        if (field == null) return;

        FieldAnswer match = answers.Find(a => a.field == field);
        if (match == null) return;

        string enteredValue = field.GetValue();

        if (IsMatch(enteredValue, match.correctValue))
        {
            field.SetCorrect();
            Debug.Log($"[CalculatorTask] {field.name} — correct.");
        }
        else
        {
            field.SetIncorrect();
            Debug.Log($"[CalculatorTask] {field.name} — incorrect.");
        }

        CheckAllCorrect();
    }

    private void CheckAllCorrect()
    {
        foreach (var answer in answers)
        {
            if (answer.field == null || !answer.field.IsCorrect)
                return;
        }

        Debug.Log("[CalculatorTask] All fields correct! Unlocking navigation.");

        if (enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    // Called by NumPadController's Autofill button - reveals the correct answers
    public void AutofillAnswers()
    {
        foreach (var answer in answers)
        {
            if (answer.field == null) continue;

            answer.field.SetValue(answer.correctValue);
            answer.field.SetCorrect();
        }

        Debug.Log("[CalculatorTask] Autofilled all answers.");

        if (enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    private bool IsMatch(string entered, string correct)
    {
        if (float.TryParse(entered, out float enteredNum) &&
            float.TryParse(correct, out float correctNum))
        {
            return Mathf.Approximately(enteredNum, correctNum);
        }

        return entered == correct;
    }

    // Called by NumPadController's Retry button
    public void ResetTask()
        {
        Debug.Log("[CalculatorTask] Task reset via Retry.");
    }
}