using System;
using TMPro;
using UnityEngine;
using Inventory;
using Items;

namespace PlayerResources
{
    public class Transaction : MonoBehaviour
    {
        [Header("Resource Manager Object")]
        [Tooltip("Resource Manager is Usually Located on the Scene's Game Manager Object")]
        [SerializeField] public ResourceManager GameManagerObject;
        
        [Header("Item Price Text")]
        [SerializeField] public TMP_Text ItemPriceText;
        
        [Header("Unit Values")]
        [SerializeField] public float MoneyValue;
        [SerializeField] public float RSValue;
        [SerializeField] public float MSValue;
        [SerializeField] public float CSValue;

        [SerializeField] private InventoryObject inventory;
        [SerializeField] private ItemObject itemObject;

        private void Start()
        {
            ItemPriceText.text = "€" + Mathf.Abs(MoneyValue);
        }

        public void ChangeCSValue()
        {
            GameManagerObject.UpdateCommunitySpirit(CSValue);
        }
        
        public void PurchaseRS()
        {
            GameManagerObject.UpdateMoney(MoneyValue);
            GameManagerObject.UpdateRS(RSValue);
            inventory.AddItem(itemObject, 1);
        }
        public void PurchaseMS()
        {
            GameManagerObject.UpdateMoney(MoneyValue);
            GameManagerObject.UpdateMS(MSValue);
            inventory.AddItem(itemObject, 1);
        }
    }
}
