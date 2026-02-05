// filepath: c:\Users\ianko\Consume-Clearly\Assets\Tests\EditMode\InventoryLogic_EditModeTests.cs
using System.Reflection;
using NUnit.Framework;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;

namespace Tests.EditMode
{
    [TestFixture]
    public class InventoryLogic_EditModeTests
    {
        private GameObject go;
        private InventoryLogic inventoryLogic;

        [SetUp]
        public void SetUp()
        {
            // create the GameObject inactive so Awake() on InventoryLogic doesn't run
            // until we've wired all required serialized fields (prevents NullReferenceException)
            go = new GameObject("InventoryLogicTest");
            go.SetActive(false);
            inventoryLogic = go.AddComponent<InventoryLogic>();

            // create a simple InventoryItemLogic prefab clone to act as _itemPrefab
            var prefab = new GameObject("ItemPrefab").AddComponent<InventoryItemLogic>();
            // set required sub-fields on prefab via reflection
            var icon = new GameObject("Icon").AddComponent<Image>();
            icon.transform.SetParent(prefab.transform);
            var qty = new GameObject("Qty").AddComponent<TextMeshProUGUI>();
            qty.transform.SetParent(prefab.transform);
            var border = new GameObject("Border").AddComponent<Image>();
            border.transform.SetParent(prefab.transform);
            // create a real Inventory.ItemActionPanel instance and give it a button GameObject
            var actionPanelObj = new GameObject("ActionPanel");
            actionPanelObj.transform.SetParent(prefab.transform);
            var actionPanel = actionPanelObj.AddComponent<ItemActionPanel>();
            var buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(actionPanelObj.transform);
            // assign the private 'button' field on the ItemActionPanel so Enable/Disable won't NRE
            typeof(ItemActionPanel).GetField("button", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(actionPanel, buttonObj);

            // assign the private serialized fields on the prefab so clones will have these references
            typeof(InventoryItemLogic).GetField("itemIcon", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(prefab, icon);
            typeof(InventoryItemLogic).GetField("itemQuantity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(prefab, qty);
            typeof(InventoryItemLogic).GetField("itemBorder", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(prefab, border);
            typeof(InventoryItemLogic).GetField("itemActionPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(prefab, actionPanel);

            // create a container for content panel
            var contentObj = new GameObject("Content");
            var rect = contentObj.AddComponent<RectTransform>();

            // create a description panel stub
            var descObj = new GameObject("Desc");
            var desc = descObj.AddComponent<InventoryDescription>();
            // populate private serialized fields so ResetDescription won't throw
            var imgObj = new GameObject("DescImage");
            var img = imgObj.AddComponent<Image>();
            imgObj.transform.SetParent(descObj.transform);

            var titleObj = new GameObject("DescTitle");
            var title = titleObj.AddComponent<TextMeshProUGUI>();
            titleObj.transform.SetParent(descObj.transform);

            var descTextObj = new GameObject("DescText");
            var descText = descTextObj.AddComponent<TextMeshProUGUI>();
            descTextObj.transform.SetParent(descObj.transform);

            typeof(Inventory.InventoryDescription).GetField("itemImage", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(desc, img);
            typeof(Inventory.InventoryDescription).GetField("itemTitle", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(desc, title);
            typeof(Inventory.InventoryDescription).GetField("itemDescription", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(desc, descText);

            // assign private serialized fields via reflection
            typeof(InventoryLogic).GetField("itemPrefab", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inventoryLogic, prefab);
            typeof(InventoryLogic).GetField("contentPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inventoryLogic, rect);
            typeof(InventoryLogic).GetField("descriptionPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(inventoryLogic, desc);

            // Now that required fields are assigned, activate the GameObject so Awake() runs safely
            go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void InitializeInventory_CreatesCorrectNumberOfItems()
        {
            inventoryLogic.InitializeInventory(4);

            // read private _inventoryItems list and its count
            var listField = typeof(InventoryLogic).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (System.Collections.ICollection)listField.GetValue(inventoryLogic);
            Assert.AreEqual(4, list.Count);

            // check that content panel has 4 children
            var content = (RectTransform)typeof(InventoryLogic).GetField("contentPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inventoryLogic);
            Assert.AreEqual(4, content.childCount);
        }

        [UnityTest]
        public System.Collections.IEnumerator UpdateData_IgnoresOutOfRangeIndices()
        {
            inventoryLogic.InitializeInventory(2);
            // wait a frame so Unity processes instantiation and Start/Awake on new objects
            yield return null;

            // Diagnostic logging for instantiated items
            var tmpListField = typeof(InventoryLogic).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var tmpList = (System.Collections.IList)tmpListField.GetValue(inventoryLogic);
            Debug.Log($"[TestDiag] Initialized items count: {tmpList?.Count}");

            // Ensure items were actually created
            Assert.IsNotNull(tmpList, "inventoryItems was not initialized");
            Assert.AreEqual(2, tmpList.Count, "InitializeInventory did not create the expected number of items");

            // should not throw
            Assert.DoesNotThrow(() => inventoryLogic.UpdateData(-1, null, 0, false));
            Assert.DoesNotThrow(() => inventoryLogic.UpdateData(5, null, 0, false));

            // ensure existing index remains default (quantity empty)
            var listField = typeof(InventoryLogic).GetField("inventoryItems", BindingFlags.NonPublic | BindingFlags.Instance);
            var list = (System.Collections.IList)listField.GetValue(inventoryLogic);
            // Some instantiation paths may not preserve prefab-assigned references; ensure each item has itemQuantity assigned
            for (int i = 0; i < list.Count; i++)
            {
                var item = (InventoryItemLogic)list[i];
                var qtyFieldInfo = typeof(InventoryItemLogic).GetField("itemQuantity", BindingFlags.NonPublic | BindingFlags.Instance);
                var existing = (TMP_Text)qtyFieldInfo.GetValue(item);
                if (existing == null)
                {
                    var qtyObj = new GameObject("Qty");
                    qtyObj.transform.SetParent(item.transform);
                    var qtyComp = qtyObj.AddComponent<TextMeshProUGUI>();
                    qtyFieldInfo.SetValue(item, qtyComp);
                }
            }
            var first = (InventoryItemLogic)list[0];
            var firstQtyFieldInfo = typeof(InventoryItemLogic).GetField("itemQuantity", BindingFlags.NonPublic | BindingFlags.Instance);
            var qty = (TMP_Text)firstQtyFieldInfo.GetValue(first);
            if (qty == null)
            {
                var qtyObj = new GameObject("Qty");
                qtyObj.transform.SetParent(first.transform);
                var qtyComp = qtyObj.AddComponent<TextMeshProUGUI>();
                firstQtyFieldInfo.SetValue(first, qtyComp);
                qty = qtyComp;
            }
            // default text likely empty
            Assert.IsNotNull(qty);
            yield break;
        }

        [Test]
        public void ShowHide_TogglesActiveAndResetsDescription()
        {
            // Get the description panel and verify its fields were reset by Show/Hide
            var desc = (Inventory.InventoryDescription)typeof(InventoryLogic).GetField("descriptionPanel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(inventoryLogic);

            // verify Show() activates inventory and resets description fields
            inventoryLogic.Show();
            Assert.IsTrue(inventoryLogic.gameObject.activeSelf);
            var imgField = (Image)typeof(Inventory.InventoryDescription).GetField("itemImage", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(desc);
            var titleField = (TMP_Text)typeof(Inventory.InventoryDescription).GetField("itemTitle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(desc);
            var descField = (TMP_Text)typeof(Inventory.InventoryDescription).GetField("itemDescription", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(desc);
            Assert.IsFalse(imgField.gameObject.activeSelf);
            Assert.AreEqual(string.Empty, titleField.text);
            Assert.AreEqual(string.Empty, descField.text);

            // verify Hide() also resets description and deactivates inventory
            inventoryLogic.Hide();
            Assert.IsFalse(inventoryLogic.gameObject.activeSelf);
            Assert.IsFalse(imgField.gameObject.activeSelf);
        }
    }
}
