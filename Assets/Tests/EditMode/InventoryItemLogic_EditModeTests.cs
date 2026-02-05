// filepath: c:\Users\ianko\Consume-Clearly\Assets\Tests\EditMode\InventoryItemLogic_EditModeTests.cs
using System.Reflection;
using NUnit.Framework;
using Inventory;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tests.EditMode
{
    [TestFixture]
    public class InventoryItemLogic_EditModeTests
    {
        private GameObject go;
        private InventoryItemLogic itemLogic;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("ItemLogicTest");
            itemLogic = go.AddComponent<InventoryItemLogic>();

            // add required serialized fields via reflection
            var itemIcon = new GameObject("Icon").AddComponent<Image>();
            itemIcon.transform.SetParent(go.transform);
            var qtyObj = new GameObject("Qty").AddComponent<TextMeshProUGUI>();
            qtyObj.transform.SetParent(go.transform);
            var border = new GameObject("Border").AddComponent<Image>();
            border.transform.SetParent(go.transform);
            // create real ItemActionPanel (matches Inventory.ItemActionPanel type expected by the script)
            var actionPanelObj = new GameObject("ActionPanel");
            var actionPanel = actionPanelObj.AddComponent<ItemActionPanel>();
            // ensure it has a child button GameObject the ItemActionPanel expects
            var buttonObj = new GameObject("Button");
            buttonObj.transform.SetParent(actionPanelObj.transform);
            actionPanelObj.transform.SetParent(go.transform);

            // assign private fields
            typeof(InventoryItemLogic).GetField("itemIcon", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemLogic, itemIcon);
            typeof(InventoryItemLogic).GetField("itemQuantity", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemLogic, qtyObj);
            typeof(InventoryItemLogic).GetField("itemBorder", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemLogic, border);
            typeof(InventoryItemLogic).GetField("itemActionPanel", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(itemLogic, actionPanel);
        }

        [TearDown]
        public void TearDown()
        {
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void SetData_SetsIconQuantityAndUsableColor()
        {
            var sprite = Sprite.Create(Texture2D.blackTexture, new Rect(0,0,1,1), Vector2.zero);
            itemLogic.SetData(sprite, 5, true);

            var itemIcon = (Image)typeof(InventoryItemLogic).GetField("itemIcon", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itemLogic);
            var qty = (TextMeshProUGUI)typeof(InventoryItemLogic).GetField("itemQuantity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itemLogic);

            Assert.IsTrue(itemIcon.gameObject.activeSelf);
            Assert.AreEqual(sprite, itemIcon.sprite);
            Assert.AreEqual("5", qty.text);
            Assert.AreEqual(Color.white, itemIcon.color);
        }

        [Test]
        public void ManipulateEventTrigger_EnablesEventTriggerAndTogglesVisuals()
        {
            // add EventTrigger to the object
            var trigger = go.AddComponent<EventTrigger>();

            itemLogic.SetEventTriggerEnabled(true);
            // read private ImageColor field immediately (ManipulateEventTrigger sets this directly)
            var imageColorField = typeof(InventoryItemLogic).GetField("imageColor", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(imageColorField, "Could not find ImageColor private field via reflection");

            var qty = (TextMeshProUGUI)typeof(InventoryItemLogic).GetField("itemQuantity", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itemLogic);

            var imageColor = (Color)imageColorField.GetValue(itemLogic);
            Debug.Log($"ImageColor after true: {imageColor}");

            Assert.IsTrue(trigger.enabled);
            Assert.IsTrue(qty.enabled);
            Assert.AreEqual(Color.white, imageColor);

            itemLogic.SetEventTriggerEnabled(false);

            var imageColorAfter = (Color)imageColorField.GetValue(itemLogic);
            Debug.Log($"ImageColor after false: {imageColorAfter}");

            Assert.IsFalse(trigger.enabled);
            Assert.IsFalse(qty.enabled);
            Assert.AreEqual(Color.gray, imageColorAfter);

            // Also assert the visible Image color matches the private ImageColor. If not, run Update() to apply it and re-check.
            var itemIconAfter = (Image)typeof(InventoryItemLogic).GetField("itemIcon", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(itemLogic);
            if (itemIconAfter.color != imageColorAfter)
            {
                // apply Update to copy ImageColor into itemIcon.color
                var updateMethod = typeof(InventoryItemLogic).GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
                updateMethod.Invoke(itemLogic, null);
            }

            Assert.AreEqual(imageColorAfter, itemIconAfter.color);
        }

        [Test]
        public void OnPointerClick_LeftButton_InvokesOnItemClicked()
        {
            bool called = false;
            itemLogic.SetEventTriggerEnabled(true);
            itemLogic.OnItemClicked += (l) => { called = true; };

            var pointer = new PointerEventData(null) { button = PointerEventData.InputButton.Left };
            itemLogic.OnPointerClick(pointer);

            Assert.IsTrue(called);
        }
    }
}
