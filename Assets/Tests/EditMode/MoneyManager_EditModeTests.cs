using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class MoneyManager_EditModeTests
    {
        private GameObject go;
        private MoneyManager manager;
        private TextMeshProUGUI display;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("MoneyManager");
            go.SetActive(false); // defer Awake so we can wire moneyDisplay first

            var displayGO = new GameObject("MoneyDisplay");
            display = displayGO.AddComponent<TextMeshProUGUI>();

            manager = go.AddComponent<MoneyManager>();

            // Wire the private serialized moneyDisplay field before activating
            typeof(MoneyManager)
                .GetField("moneyDisplay", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(manager, display);

            // In edit-mode tests AddComponent may call Awake immediately (before the
            // field is wired). Invoke Awake explicitly now that moneyDisplay is set.
            typeof(MoneyManager)
                .GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(manager, null);

            go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(display.gameObject);
        }

        // ── Awake ─────────────────────────────────────────────────────────────────

        [Test]
        public void Awake_SetsInitialDisplayText()
        {
            Assert.AreEqual("$0", display.text);
        }

        // ── ChangeMoneyAmount (add) ───────────────────────────────────────────────

        [Test]
        public void ChangeMoneyAmount_Positive_IncreasesMoneyCount()
        {
            manager.ChangeMoneyAmount(50);
            int count = (int)typeof(MoneyManager)
                .GetField("moneyCount", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(manager);
            Assert.AreEqual(50, count);
        }

        [Test]
        public void ChangeMoneyAmount_Positive_UpdatesDisplayText()
        {
            manager.ChangeMoneyAmount(100);
            Assert.AreEqual("$100", display.text);
        }

        [Test]
        public void ChangeMoneyAmount_Positive_ReturnsTrue()
        {
            bool result = manager.ChangeMoneyAmount(10);
            Assert.IsTrue(result);
        }

        // ── ChangeMoneyAmount (remove) ────────────────────────────────────────────

        [Test]
        public void ChangeMoneyAmount_Negative_DecreasesMoneyCount()
        {
            manager.ChangeMoneyAmount(100);
            manager.ChangeMoneyAmount(-40);
            int count = (int)typeof(MoneyManager)
                .GetField("moneyCount", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(manager);
            Assert.AreEqual(60, count);
        }

        [Test]
        public void ChangeMoneyAmount_Negative_UpdatesDisplayText()
        {
            manager.ChangeMoneyAmount(100);
            manager.ChangeMoneyAmount(-30);
            Assert.AreEqual("$70", display.text);
        }

        [Test]
        public void ChangeMoneyAmount_NegativeBeyondBalance_ReturnsFalse()
        {
            // moneyCount = 0, trying to remove 10 should fail
            bool result = manager.ChangeMoneyAmount(-10);
            Assert.IsFalse(result);
        }

        [Test]
        public void ChangeMoneyAmount_NegativeBeyondBalance_DoesNotChangeMoney()
        {
            manager.ChangeMoneyAmount(-10);
            int count = (int)typeof(MoneyManager)
                .GetField("moneyCount", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(manager);
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ChangeMoneyAmount_ExactBalance_ReturnsTrue()
        {
            manager.ChangeMoneyAmount(50);
            bool result = manager.ChangeMoneyAmount(-50);
            Assert.IsTrue(result);
        }
    }
}
