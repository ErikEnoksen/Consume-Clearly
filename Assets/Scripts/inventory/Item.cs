using Items;
using Save;
using System;
using UnityEngine;

public class Item : MonoBehaviour, ISaveable
{
    [SerializeField]
    private string itemID;
    [SerializeField]
    private string itemName;
    [SerializeField]
    private int quantity = 1;
    [SerializeField]
    private int maxStack = 10;
    [SerializeField]
    private Sprite sprite;

    [Header("Save System")]
    [SerializeField] private string uniqueSceneId;

    public string Id
    {
        get
        {
            if (string.IsNullOrEmpty(itemID))
                GenerateId();
            return itemID;
        }
    }
    public string ItemName {  get { return itemName; } set { itemName = value; } }
    public int Quantity { get { return quantity; } set { quantity = value; } }
    public Sprite Sprite { get { return sprite; } set { sprite = value; } }
    public int MaxStack { get { return maxStack; } set { maxStack = value; } }

    [TextArea]
    [SerializeField]
    private string itemDescription;

    [SerializeField]
    private GameObject eKeySprite;

    public string ItemDescription { get { return itemDescription; } set { itemDescription = value; } }
    public GameObject EKeySprite { get { return eKeySprite; } set { eKeySprite = value; } }

    private InventoryManager inventory;
    private SpriteRenderer _spriteRenderer;
    private Collider2D _collider;
    [SerializeField]
    private bool _collected;

    private void Awake()
    {
        if (string.IsNullOrEmpty(uniqueSceneId))
            uniqueSceneId = Guid.NewGuid().ToString();
    }

    void Start()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<Collider2D>();
        inventory = GameObject.Find("InventorySelector").GetComponent<InventoryManager>();
        if (eKeySprite != null) eKeySprite.SetActive(false);
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemID) || itemID == "1")
            GenerateId();
        if (string.IsNullOrEmpty(uniqueSceneId))
            uniqueSceneId = Guid.NewGuid().ToString();
    }

    public void Initialize(string itemName, int quantity, Sprite sprite, string itemDescription, int maxStack, string itemTag, GameObject eKeySprite)
    {
        ItemName = itemName;
        itemID = itemName;
        Quantity = quantity;
        Sprite = sprite;
        ItemDescription = itemDescription;
        MaxStack = maxStack;
        tag = itemTag;
        EKeySprite = eKeySprite;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (_collected) return;

        if (other.gameObject.tag == "Player")
        {
            if (eKeySprite != null) eKeySprite.SetActive(true);
            if (Input.GetKey(KeybindManager.Instance.GetKey("Interact")))
            {
                int excessItems = inventory.AddItem(this, Quantity);
                if (excessItems <= 0)
                {
                    CollectItem();
                }
                else
                {
                    quantity = excessItems;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (eKeySprite != null) eKeySprite.SetActive(false);
    }

    private void CollectItem()
    {
        _collected = true;
        if (_spriteRenderer != null) _spriteRenderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
        if (eKeySprite != null) eKeySprite.SetActive(false);
    }

    private void GenerateId()
    {
        itemID = $"{(string.IsNullOrEmpty(ItemName) ? "item" : ItemName)}";
    }

    public string GetUniqueId() => uniqueSceneId;

    public InteractableObjectState SaveState()
    {
        return new InteractableObjectState
        {
            uniqueId = uniqueSceneId,
            isActive = !_collected,
            position = transform.position,
            rotation = transform.rotation
        };
    }

    public void LoadState(InteractableObjectState state)
    {
        if (state == null || state.uniqueId != uniqueSceneId) return;

        if (!state.isActive)
            CollectItem();
    }
}
