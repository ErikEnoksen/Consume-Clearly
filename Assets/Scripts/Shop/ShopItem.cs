// =============================================================================
// ShopItem.cs - The invidivual slots of items in the shop
//
// PURPOSE:
//   When selecting an item it will be displayed on the side.
//   This way the player knows what they are buying.
// =============================================================================
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    private Item item;
    [SerializeField]
    private int price;

    public int Price { get { return price; } }
    public Item Item { get { return item; } }

    [SerializeField]
    private TMP_Text itemNameTxt;
    [SerializeField] 
    private TMP_Text itemPrice;
    [SerializeField]
    private Image selectedImage;

    [SerializeField]
    private ShopMenu shopMenu;

    private void Start()
    {
        gameObject.GetComponent<Image>().sprite = item.Sprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }
    }

    private void OnLeftClick()
    {
        itemNameTxt.text = item.ItemName;
        itemPrice.text = price.ToString();
        selectedImage.sprite = item.Sprite;
        selectedImage.enabled = true;

        shopMenu.Item = item;
        shopMenu.Price = price;
    }

}
