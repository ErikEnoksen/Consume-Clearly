using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.EditMode
{
    [TestFixture]
    public class InventoryItem_EditModeTests
    {
        private GameObject slotGO;
        private InventoryItem slot;

        private TextMeshProUGUI quantityText;
        private Image itemImage;

        // Public UI fields accessed by EmptySlot
        private TextMeshProUGUI descriptionTitleText;
        private TextMeshProUGUI descriptionBodyText;
        private Image infoImage;
        private GameObject selectedShaders;

        [SetUp]
        public void SetUp()
        {
            slotGO = new GameObject("InventorySlot");
            slot = slotGO.AddComponent<InventoryItem>();

            // Create child GameObjects with required UI components
            var qtGO = new GameObject("QuantityText");
            quantityText = qtGO.AddComponent<TextMeshProUGUI>();

            var imgGO = new GameObject("ItemImage");
            itemImage = imgGO.AddComponent<Image>();

            var titleGO = new GameObject("DescTitle");
            descriptionTitleText = titleGO.AddComponent<TextMeshProUGUI>();

            var bodyGO = new GameObject("DescBody");
            descriptionBodyText = bodyGO.AddComponent<TextMeshProUGUI>();

            var infoGO = new GameObject("InfoImage");
            infoImage = infoGO.AddComponent<Image>();

            selectedShaders = new GameObject("SelectedShaders");

            // Wire private serialized fields via reflection
            var t = typeof(InventoryItem);
            t.GetField("quantityText", BindingFlags.NonPublic | BindingFlags.Instance)
             .SetValue(slot, quantityText);
            t.GetField("itemImage", BindingFlags.NonPublic | BindingFlags.Instance)
             .SetValue(slot, itemImage);

            // Wire public fields
            slot.itemDescriptionTitle = descriptionTitleText;
            slot.itemDescriptionText = descriptionBodyText;
            slot.infoImage = infoImage;
            slot.selectedShaders = selectedShaders;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(slotGO);
            Object.DestroyImmediate(quantityText.gameObject);
            Object.DestroyImmediate(itemImage.gameObject);
            Object.DestroyImmediate(descriptionTitleText.gameObject);
            Object.DestroyImmediate(descriptionBodyText.gameObject);
            Object.DestroyImmediate(infoImage.gameObject);
            Object.DestroyImmediate(selectedShaders);
        }

        // ── AddItem ───────────────────────────────────────────────────────────────

        [Test]
        public void AddItem_ReturnsZeroWhenSpaceAvailable()
        {
            int excess = slot.AddItem("wood", "Wood", 3, null, "A plank", 10, "Untagged");
            Assert.AreEqual(0, excess);
        }

        [Test]
        public void AddItem_SetsQuantityCorrectly()
        {
            slot.AddItem("wood", "Wood", 5, null, "", 10, "Untagged");
            Assert.AreEqual(5, slot.quantity);
        }

        [Test]
        public void AddItem_SetsItemName()
        {
            slot.AddItem("wood", "Wood", 1, null, "", 10, "Untagged");
            Assert.AreEqual("Wood", slot.itemName);
        }

        [Test]
        public void AddItem_SetsItemId()
        {
            slot.AddItem("wood_id", "Wood", 1, null, "", 10, "Untagged");
            Assert.AreEqual("wood_id", slot.itemID);
        }

        [Test]
        public void AddItem_ReturnsExcessWhenOverMaxStack()
        {
            int excess = slot.AddItem("stone", "Stone", 15, null, "", 10, "Untagged");
            Assert.AreEqual(5, excess);
        }

        [Test]
        public void AddItem_SetsIsFullWhenAtMaxStack()
        {
            slot.AddItem("stone", "Stone", 10, null, "", 10, "Untagged");
            Assert.IsTrue(slot.isFull);
        }

        [Test]
        public void AddItem_ClampsQuantityToMaxStack()
        {
            slot.AddItem("stone", "Stone", 15, null, "", 10, "Untagged");
            Assert.AreEqual(10, slot.quantity);
        }

        [Test]
        public void AddItem_ReturnsAllQuantityWhenSlotIsAlreadyFull()
        {
            slot.isFull = true;
            int excess = slot.AddItem("stone", "Stone", 7, null, "", 10, "Untagged");
            Assert.AreEqual(7, excess);
        }

        // ── RemoveItem ────────────────────────────────────────────────────────────

        [Test]
        public void RemoveItem_ReducesQuantity()
        {
            slot.AddItem("herb", "Herb", 5, null, "", 10, "Untagged");
            slot.RemoveItem(2);
            Assert.AreEqual(3, slot.quantity);
        }

        [Test]
        public void RemoveItem_ReturnsZeroOnPartialRemove()
        {
            slot.AddItem("herb", "Herb", 5, null, "", 10, "Untagged");
            int result = slot.RemoveItem(2);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void RemoveItem_EmptiesSlotWhenExactAmount()
        {
            slot.AddItem("herb", "Herb", 5, null, "", 10, "Untagged");
            slot.RemoveItem(5);
            Assert.AreEqual(0, slot.quantity);
            Assert.IsFalse(slot.isFull);
        }

        [Test]
        public void RemoveItem_ReturnsZeroOnExactRemove()
        {
            slot.AddItem("herb", "Herb", 5, null, "", 10, "Untagged");
            int result = slot.RemoveItem(5);
            Assert.AreEqual(0, result);
        }
    }
}