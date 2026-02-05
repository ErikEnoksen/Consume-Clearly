using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace Inventory
{
    public class InventoryItemLogic : MonoBehaviour
    {
        [Header("Unity objects")]
        [SerializeField]
        private Image itemIcon;
        [SerializeField]
        private TMP_Text itemQuantity;
        [SerializeField]
        private Image itemBorder;
        [SerializeField]
        private ItemActionPanel itemActionPanel;
        private Color imageColor = Color.white;
        private bool leftClickEnabled = true;

        public event Action<InventoryItemLogic> OnItemClicked, OnRightMouseBtnClick;
        public int SlotIndex { get; private set; } = -1;
        public void SetSlotIndex(int index) => SlotIndex = index;
        public bool HasItem => itemIcon != null && itemIcon.gameObject.activeSelf;

        private void Start()
        {
            Deselect();
        }

        private void Update()
        {
            itemIcon.color = imageColor;
        }

        public void Deselect()
        {
            itemBorder.enabled = false;
        }

        public void SetEventTriggerEnabled(bool isUsable)
        {
            Debug.Log("manipulateEventTrigger called with emptying: " + isUsable);
            if (this == null || gameObject == null) return;
            leftClickEnabled = isUsable;
            Debug.Log("Left Click Trigger enabled : " + (leftClickEnabled));
            itemQuantity.enabled = isUsable;
            Debug.Log("itemQuantity enabled : " + (itemQuantity.enabled));
            imageColor = isUsable ? Color.white : Color.gray;
            Debug.Log("Image color set to : " + (imageColor));
            
            var eventTrigger = GetComponent<EventTrigger>();
            if (eventTrigger != null)
            {
                eventTrigger.enabled = isUsable;
                Debug.Log("Toggled EventTrigger on GameObject to " + isUsable);
            }
            else
            {
                var childEventTrigger = GetComponentInChildren<EventTrigger>(true);
                if (childEventTrigger != null)
                {
                    childEventTrigger.enabled = isUsable;
                    Debug.Log("Toggled EventTrigger on child to " + isUsable);
                }
            }
        }

        public void SetData(Sprite sprite, int quantity, bool isUsable)
        {
            this.itemIcon.gameObject.SetActive(true);
            this.itemIcon.sprite = sprite;
            itemIcon.color = isUsable ? Color.white : Color.gray;
            imageColor = itemIcon.color;
            this.itemQuantity.text = quantity.ToString();
        }

        public void Select() 
        {
            itemBorder.enabled = true;
        }

        public void OnPointerClick(BaseEventData eventData)
        {
            PointerEventData pointerEventData = eventData as PointerEventData;
            if (pointerEventData == null) return;

            if (pointerEventData.button == PointerEventData.InputButton.Left)
            {
                if (leftClickEnabled)
                {
                    Debug.Log("Left mouse button clicked");
                    OnItemClicked?.Invoke(this);
                }
                else
                {
                    Debug.Log("Left-click ignored (, leftClickEnabled: " + leftClickEnabled + ")");
                }
            }
            else if (pointerEventData.button == PointerEventData.InputButton.Right)
            {
                Debug.Log("Right mouse button clicked");
                OnRightMouseBtnClick?.Invoke(this);
            }
        }

        public void EnableActionPanel()
        {
            itemActionPanel.Enable();
        }

        public void DisableActionPanel()
        {
            itemActionPanel.Disable();
        }

    }
}
