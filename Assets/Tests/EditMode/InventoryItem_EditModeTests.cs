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
        private Item item;

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
            item.Initialize("Wood", 3, null, "A plank", 10, "Untagged");
            int excess = slot.AddItem(item);
            Assert.AreEqual(0, excess);
        }

        [Test]
        public void AddItem_SetsQuantityCorrectly()
        {
            item.Initialize("Wood", 5, null, "A plank", 10, "Untagged");
            slot.AddItem(item);
            Assert.AreEqual(5, slot.quantity);
        }

        [Test]
        public void AddItem_SetsItemName()
        {
            item.Initialize("Wood", 1, null, "A plank", 10, "Untagged");
            slot.AddItem(item);
            Assert.AreEqual("Wood", slot.itemName);
        }

        [Test]
        public void AddItem_SetsItemId()
        {
            item.Initialize("Wood", 1, null, "A plank", 10, "Untagged");
            slot.AddItem(item);
            Assert.AreEqual("Wood", slot.itemID);
        }

        [Test]
        public void AddItem_ReturnsExcessWhenOverMaxStack()
        {
            item.Initialize("Stone", 15, null, "A plank", 10, "Untagged");
            int excess = slot.AddItem(item);
            Assert.AreEqual(5, excess);
        }

        [Test]
        public void AddItem_SetsIsFullWhenAtMaxStack()
        {
            item.Initialize("Stone", 10, null, "A plank", 10, "Untagged");
            slot.AddItem(item);
            Assert.IsTrue(slot.isFull);
        }

        [Test]
        public void AddItem_ClampsQuantityToMaxStack()
        {
            item.Initialize("Stone", 15, null, "", 10, "Untagged");
            slot.AddItem(item);
            Assert.AreEqual(10, slot.quantity);
        }

        [Test]
        public void AddItem_ReturnsAllQuantityWhenSlotIsAlreadyFull()
        {
            slot.isFull = true;
            item.Initialize("Stone", 7, null, "", 10, "Untagged");
            int excess = slot.AddItem(item);
            Assert.AreEqual(7, excess);
        }

        // ── RemoveItem ────────────────────────────────────────────────────────────

        [Test]
        public void RemoveItem_ReducesQuantity()
        {
            item.Initialize("Herb", 5, null, "", 10, "Untagged");
            slot.AddItem(item);
            slot.RemoveItem(2);
            Assert.AreEqual(3, slot.quantity);
        }

        [Test]
        public void RemoveItem_ReturnsZeroOnPartialRemove()
        {
            item.Initialize("Herb", 5, null, "", 10, "Untagged");
            slot.AddItem(item);
            int result = slot.RemoveItem(2);
            Assert.AreEqual(0, result);
        }

        [Test]
        public void RemoveItem_EmptiesSlotWhenExactAmount()
        {
            item.Initialize("Herb", 5, null, "", 10, "Untagged");
            slot.AddItem(item);
            slot.RemoveItem(5);
            Assert.AreEqual(0, slot.quantity);
            Assert.IsFalse(slot.isFull);
        }

        [Test]
        public void RemoveItem_ReturnsZeroOnExactRemove()
        {
            item.Initialize("Herb", 5, null, "", 10, "Untagged");
            slot.AddItem(item);
            int result = slot.RemoveItem(5);
            Assert.AreEqual(0, result);
        }
    }
}