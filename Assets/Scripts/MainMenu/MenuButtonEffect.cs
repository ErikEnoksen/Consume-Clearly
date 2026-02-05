using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MainMenu
{
    public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private bool isEnabled = true;
    
        private TMP_Text textMeshPro;
        private Button button;
        private string originalText;
        private static readonly Color DisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private void Start()
        {
            textMeshPro = GetComponent<TMP_Text>();
            button = GetComponentInParent<Button>();
        
            if (textMeshPro != null)
            {
                originalText = textMeshPro.text;
                UpdateButtonState();
            }
        }

        public void SetEnabled(bool enabled)
        {
            isEnabled = enabled;
            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (button != null)
            {
                button.interactable = isEnabled;
                textMeshPro.color = isEnabled ? Color.white : DisabledColor;
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isEnabled && textMeshPro != null)
            { 
                textMeshPro.text = $">{originalText}";
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (textMeshPro != null)
            {
                textMeshPro.text = originalText;
            }
        }
    }
}
