using TMPro;
using UnityEngine;
using UnityEngine.Assertions.Must;
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
    public Image image;

    [SerializeField]
    private ShopMenu shopMenu;


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
        image.sprite = item.Sprite;
        image.enabled = true;

        shopMenu.Item = item;
        shopMenu.Price = price;
    }

}
