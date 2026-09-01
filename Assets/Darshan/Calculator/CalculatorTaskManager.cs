using UnityEngine;
using UnityEngine.UI;
using TMPro;
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

        // Tracks wrong attempts for this specific field. Not shown in Inspector.
        [HideInInspector] public int wrongAttempts;
    }

    [Header("Correct Answers Per Field")]
    public List<FieldAnswer> answers = new List<FieldAnswer>();

    [Header("Auto-Filled Result Field")]
    [Tooltip("A 4th field that is NOT typed by the player. Once every field in 'answers' is correct, this field is filled in and marked correct automatically.")]
    public NumericInputField resultField;
    [Tooltip("The value shown in the result field once unlocked (e.g. the final computed answer).")]
    public string resultValue;
    [Tooltip("Optional - a suffix/unit label (e.g. 'cm', '%', 'kg') that pops in alongside the result field. Leave the GameObject inactive by default; it will be enabled automatically.")]
    public TMP_Text resultSuffixText;
    [Tooltip("The text shown on the suffix label once the result is revealed.")]
    public string resultSuffixValue;

    [Header("Navigation")]
    [Tooltip("If true, unlocks Next automatically once every field is correct.")]
    public bool enableNavigation = true;

    [Header("Autofill Unlock")]
    [Tooltip("Number of wrong attempts on a single field before Autofill unlocks.")]
    public int wrongAttemptsToUnlockAutofill = 3;
    [Tooltip("The Autofill button — stays disabled/hidden until unlocked.")]
    public Button autofillButton;

    private bool autofillUnlocked = false;
    private bool resultRevealed = false;

    private void Start()
    {
        // Autofill starts locked.
        if (autofillButton != null)
        {
            autofillButton.interactable = false;
        }
    }

    // Called by NumPadController's Check button / auto-check on end edit -
    // validates only the field the player is currently typing in.
    public void CheckField(NumericInputField field)
    {
        if (field == null) return;

        FieldAnswer match = answers.Find(a => a.field == field);
        if (match == null) return;

        string enteredValue = field.GetValue();

        if (IsMatch(enteredValue, match.correctValue))
        {
            field.SetCorrect();
            match.wrongAttempts = 0; // reset on success
            Debug.Log($"[CalculatorTask] {field.name} — correct.");
        }
        else
        {
            field.SetIncorrect();
            match.wrongAttempts++;
            Debug.Log($"[CalculatorTask] {field.name} — incorrect. Attempt #{match.wrongAttempts}");

            if (!autofillUnlocked && match.wrongAttempts >= wrongAttemptsToUnlockAutofill)
            {
                UnlockAutofill();
            }
        }

        CheckAllCorrect();
    }

    private void UnlockAutofill()
    {
        autofillUnlocked = true;
        if (autofillButton != null)
        {
            autofillButton.interactable = true;
        }
        Debug.Log("[CalculatorTask] Autofill button unlocked after repeated wrong attempts.");
    }

    private void CheckAllCorrect()
    {
        foreach (var answer in answers)
        {
            if (answer.field == null || !answer.field.IsCorrect)
                return;
        }

        Debug.Log("[CalculatorTask] All fields correct! Unlocking navigation.");

        RevealResultField();

        if (enableNavigation)
        {
            PageNavigationController.RequestNavigationUnlock();
        }
    }

    // Auto-fills the 4th (non-typed) result field once every player-entered
    // field is correct. Only runs once per task attempt.
    private void RevealResultField()
    {
        if (resultRevealed) return;
        if (resultField == null) return;

        resultField.SetValue(resultValue);
        resultField.SetCorrect();
        resultRevealed = true;

        if (resultSuffixText != null)
        {
            resultSuffixText.text = resultSuffixValue;
            resultSuffixText.gameObject.SetActive(true);
        }

        Debug.Log($"[CalculatorTask] Result field auto-filled with '{resultValue}{resultSuffixValue}'.");
    }

    // Called by NumPadController's Autofill button - reveals the correct answers.
    // Only works once unlocked via wrong attempts (or you can bypass this check if
    // you also want a manual "always available" autofill elsewhere).
    public void AutofillAnswers()
    {
        if (!autofillUnlocked)
        {
            Debug.Log("[CalculatorTask] Autofill not yet unlocked — ignoring press.");
            return;
        }

        foreach (var answer in answers)
        {
            if (answer.field == null) continue;
            answer.field.SetValue(answer.correctValue);
            answer.field.SetCorrect();
            answer.wrongAttempts = 0;
        }

        Debug.Log("[CalculatorTask] Autofilled all answers.");

        RevealResultField();

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
        foreach (var answer in answers)
        {
            answer.wrongAttempts = 0;
        }
        autofillUnlocked = false;
        resultRevealed = false;

        if (resultField != null)
        {
            resultField.Clear();
        }

        if (resultSuffixText != null)
        {
            resultSuffixText.gameObject.SetActive(false);
        }

        if (autofillButton != null)
        {
            autofillButton.interactable = false;
        }
        Debug.Log("[CalculatorTask] Task reset via Retry.");
    }
}