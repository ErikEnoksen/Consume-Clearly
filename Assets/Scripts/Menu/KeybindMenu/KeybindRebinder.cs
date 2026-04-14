using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeybindRebinder : MonoBehaviour
{
    [SerializeField] private string actionName;
    [SerializeField] private Button rebindButton;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Color waitingColor = Color.yellow;
    [SerializeField] private Color defaultColor = Color.white;

    private TMP_Text displayText;
    private bool isListeningForInput = false;
    private string originalDisplayText;

    private void Start()
    {
        displayText = GetComponent<TMP_Text>();

        if (rebindButton == null)
            rebindButton = GetComponentInParent<Button>();

        if (rebindButton == null)
        {
            Debug.LogError("KeybindRebinder requires a Button component", gameObject);
            return;
        }

        rebindButton.onClick.AddListener(StartRebind);
    }

    private void OnEnable()
    {
        // Reinitialize display when menu opens
        if (displayText != null && KeybindManager.Instance != null)
        {
            ResetDisplay();
        }
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

        // Listen for any key press
        foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                // Ignore UI navigation keys
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
        if (KeybindManager.Instance == null)
        {
            Debug.LogError("KeybindManager instance not found!");
            return;
        }

        isListeningForInput = true;
        originalDisplayText = displayText.text;
        displayText.text = "Waiting for input...";
        displayText.color = waitingColor;

        if (feedbackText != null)
            feedbackText.text = "Press any key to rebind (ESC to cancel)";
    }

    private void RebindKey(KeyCode newKey)
    {
        isListeningForInput = false;

        // Check if key is already in use
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
        ShowSuccess($"Rebound to {newKey}");
    }

    private void CancelRebind()
    {
        isListeningForInput = false;
        displayText.text = originalDisplayText;
        displayText.color = defaultColor;

        if (feedbackText != null)
            feedbackText.text = "";
    }

    private void ShowError(string message)
    {
        displayText.text = originalDisplayText;
        displayText.color = Color.red;

        if (feedbackText != null)
            feedbackText.text = message;

        Invoke(nameof(ResetDisplay), 2f);
    }

    private void ShowSuccess(string message)
    {
        displayText.color = Color.green;

        if (feedbackText != null)
            feedbackText.text = message;

        Invoke(nameof(ResetDisplay), 1f);
    }

    private void ResetDisplay()
    {
        if (displayText == null || KeybindManager.Instance == null)
            return;

        string keyAsString = KeybindManager.Instance.GetKeyAsString(actionName);
        displayText.text = $"{actionName}: {keyAsString}";
        displayText.color = defaultColor;

        if (feedbackText != null)
            feedbackText.text = "";
    }
}