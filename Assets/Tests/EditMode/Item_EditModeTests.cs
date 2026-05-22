using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class Item_EditModeTests
    {
        private GameObject go;
        private Item item;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Item");
            item = go.AddComponent<Item>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        // ── Initialize ────────────────────────────────────────────────────────────

        [Test]
        public void Initialize_SetsItemName()
        {
            item.Initialize("Wood", 1, null, "A plank of wood", 10, "Untagged", item.EKeySprite);
            Assert.AreEqual("Wood", item.ItemName);
        }

        [Test]
        public void Initialize_SetsQuantity()
        {
            item.Initialize("Stone", 5, null, "", 20, "Untagged", item.EKeySprite);
            Assert.AreEqual(5, item.Quantity);
        }

        [Test]
        public void Initialize_SetsMaxStack()
        {
            item.Initialize("Stone", 1, null, "", 15, "Untagged", item.EKeySprite);
            Assert.AreEqual(15, item.MaxStack); 
        }

        [Test]
        public void Initialize_SetsDescription()
        {
            item.Initialize("Herb", 1, null, "A healing herb", 5, "Untagged", item.EKeySprite);
            Assert.AreEqual("A healing herb", item.ItemDescription);
        }

        [Test]
        public void Initialize_SetsSprite()
        {
            var sprite = Sprite.Create(
                new Texture2D(1, 1),
                new Rect(0, 0, 1, 1),
                Vector2.zero);

            item.Initialize("Icon", 1, sprite, "", 1, "Untagged", item.EKeySprite);
            Assert.AreEqual(sprite, item.Sprite);

            Object.DestroyImmediate(sprite.texture);
        }

        // ── Id / GenerateId ───────────────────────────────────────────────────────

        [Test]
        public void Id_UsesItemNameWhenAvailable()
        {
            item.Initialize("Plank", 1, null, "", 10, "Untagged", item.EKeySprite);
            Assert.AreEqual("Plank", item.Id);
        }

        [Test]
        public void Id_FallsBackToItemLiteral_WhenNameIsEmpty()
        {
            // ItemName not set, so name is empty → GenerateId returns "item"
            Assert.AreEqual("item", item.Id);
        }

        [Test]
        public void Id_IsStableOnRepeatedAccess()
        {
            item.Initialize("Rock", 1, null, "", 5, "Untagged", item.EKeySprite);
            string first = item.Id;
            string second = item.Id;
            Assert.AreEqual(first, second);
        }

        // ── Property setters ──────────────────────────────────────────────────────

        [Test]
        public void Quantity_CanBeSetViaProperty()
        {
            item.Quantity = 42;
            Assert.AreEqual(42, item.Quantity);
        }

        [Test]
        public void MaxStack_CanBeSetViaProperty()
        {
            item.MaxStack = 99;
            Assert.AreEqual(99, item.MaxStack);
        }
    }
}