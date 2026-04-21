using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindRebinder : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private TMP_Text bindingDisplay;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button rebindButton;
    [SerializeField] private Color waitingColor = Color.red;
    [SerializeField] private Color defaultColor = Color.black;

    private bool isListeningForInput = false;
    private string originalDisplayText;

    private void Start()
    {
        if (rebindButton == null)
            rebindButton = GetComponent<Button>();

        rebindButton?.onClick.AddListener(StartRebind);
        UpdateDisplay();
    }

    private void OnEnable()
    {
        // Update display when menu is opened
        ResetDisplay();
        UpdateDisplay();
    }

    private void OnDestroy()
    {
        if (rebindButton != null)
            rebindButton.onClick.RemoveListener(StartRebind);
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
        isListeningForInput = false;
        bindingDisplay.text = originalDisplayText;
        bindingDisplay.color = defaultColor;
        if (feedbackText) feedbackText.text = "";
    }

    private void ShowError(string message)
    {
        bindingDisplay.text = originalDisplayText;
        bindingDisplay.color = Color.red;
        if (feedbackText) feedbackText.text = message;
        Invoke(nameof(ResetDisplay), 2f);
    }

    private void ShowSuccess(string message)
    {
        bindingDisplay.color = Color.green;
        if (feedbackText) feedbackText.text = message;
        Invoke(nameof(ResetDisplay), 1f);
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
        UpdateDisplay();
    }
}