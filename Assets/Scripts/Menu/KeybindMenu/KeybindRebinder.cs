using System.Collections;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindRebinder : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private TMP_Text bindingDisplay;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Color waitingColor = Color.red;
    [SerializeField] private Color defaultColor = Color.black;

    private bool isListeningForInput = false;
    private string originalDisplayText;
    private string defaultFeedbackText = "";
    private Coroutine resetDisplayRoutine;

    private void Awake()
    {
        if (feedbackText == null)
        {
            var found = GameObject.Find("FeedbackText");
            if (found != null)
                feedbackText = found.GetComponent<TMP_Text>();
        }

        if (feedbackText != null)
            defaultFeedbackText = feedbackText.text;
    }

    private void Start()
    {
        if (rebindButton == null)
            rebindButton = GetComponent<Button>();

        rebindButton?.onClick.AddListener(StartRebind);

        if (actionLabel != null)
            actionLabel.text = Regex.Replace(actionName, "([a-z])([A-Z])", "$1 $2");

        UpdateDisplay();

        // Subscribe to keybind changes from reset or other sources
        KeybindManager.OnKeybindChanged += OnKeybindChanged;
    }

    private void OnEnable()
    {
        // Update display when menu is opened
        ResetDisplay();
    }

    private void OnDisable()
    {
        CancelPendingReset();
    }

    private void OnDestroy()
    {
        if (rebindButton != null)
            rebindButton.onClick.RemoveListener(StartRebind);

        KeybindManager.OnKeybindChanged -= OnKeybindChanged;
    }

    private void Update()
    {
        if (!isListeningForInput)
            return;

        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                if (keyCode == KeyCode.Escape)
                {
                    CancelRebind();
                    return;
                }

                if (keyCode == KeyCode.Return || keyCode == KeyCode.Tab)
                    continue;

                RebindKey(keyCode);
                return;
            }
        }
    }

    private void StartRebind()
    {
        CancelPendingReset();
        isListeningForInput = true;
        originalDisplayText = bindingDisplay.text;
        bindingDisplay.text = "Waiting for input...";
        bindingDisplay.color = waitingColor;
        if (feedbackText) feedbackText.text = "Press any key (ESC to cancel)";
    }

    private void RebindKey(KeyCode newKey)
    {
        isListeningForInput = false;

        var allKeybinds = KeybindManager.Instance.GetAllKeybinds();
        foreach (var pair in allKeybinds)
        {
            if (pair.Value == newKey && pair.Key != actionName)
            {
                ShowError($"Key already in use for {pair.Key}!");
                return;
            }
        }

        KeybindManager.Instance.SetKey(actionName, newKey);
        ShowSuccess("Rebound successfully!");
    }

    private void CancelRebind()
    {
        CancelPendingReset();
        isListeningForInput = false;
        bindingDisplay.text = originalDisplayText;
        bindingDisplay.color = defaultColor;
        if (feedbackText) feedbackText.text = defaultFeedbackText;
    }

    private void ShowError(string message)
    {
        bindingDisplay.text = originalDisplayText;
        bindingDisplay.color = Color.red;
        if (feedbackText) feedbackText.text = message;
        ScheduleReset(2f);
    }

    private void ShowSuccess(string message)
    {
        bindingDisplay.color = Color.green;
        if (feedbackText) feedbackText.text = message;
        ScheduleReset(1f);
    }

    private void UpdateDisplay()
    {
        if (KeybindManager.Instance == null)
            return;

        string keyAsString = KeybindManager.Instance.GetKeyAsString(actionName);
        bindingDisplay.text = keyAsString;
    }

    private void ResetDisplay()
    {
        bindingDisplay.color = defaultColor;
        if (feedbackText) feedbackText.text = defaultFeedbackText;
        UpdateDisplay();
    }

    private void ScheduleReset(float delay)
    {
        CancelPendingReset();
        resetDisplayRoutine = StartCoroutine(ResetDisplayAfterDelay(delay));
    }

    private void CancelPendingReset()
    {
        if (resetDisplayRoutine == null)
            return;

        StopCoroutine(resetDisplayRoutine);
        resetDisplayRoutine = null;
    }

    private IEnumerator ResetDisplayAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        resetDisplayRoutine = null;
        ResetDisplay();
    }

    private void OnKeybindChanged(string changedAction, KeyCode newKey)
    {
        // Update display if this keybind was changed
        if (changedAction == actionName)
        {
            UpdateDisplay();
        }
    }
}
