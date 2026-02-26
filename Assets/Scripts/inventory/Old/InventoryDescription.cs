using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Inventory
{
    public class InventoryDescription : MonoBehaviour
    {
        [SerializeField]
        private Image itemImage;
        [SerializeField]
        private TMP_Text itemTitle;
        [SerializeField]
        private TMP_Text itemDescription;

        private void Awake()
        {
            ResetDescription();
        }

        public void ResetDescription()
        {
            itemImage.gameObject.SetActive(false);
            itemTitle.text = string.Empty;
            itemDescription.text = string.Empty;
        }

        public void SetDescription(Sprite sprite, string title, string description)
        {
            itemImage.gameObject.SetActive(true);
            itemImage.sprite = sprite;
            itemTitle.text = title;
            itemDescription.text = description;
        }
    }
}
