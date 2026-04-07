using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.EditMode
{
    [TestFixture]
    public class WorkshopStation_EditModeTests
    {
        private GameObject stationGO;
        private WorkshopStation station;
        private InventoryManager inventoryManager;
        private GameObject inventoryGO;

        [SetUp]
        public void SetUp()
        {
            // Create a minimal InventoryManager with one slot
            inventoryGO = new GameObject("InventorySelector");
            inventoryManager = inventoryGO.AddComponent<InventoryManager>();

            // Build one InventoryItem slot
            var slotGO     = new GameObject("Slot");
            var slot       = slotGO.AddComponent<InventoryItem>();

            // Wire the two private serialized UI fields so AddItem/RemoveItem don't throw
            var qtGO = new GameObject("QtyText");
            var qt   = qtGO.AddComponent<TextMeshProUGUI>();
            var imgGO = new GameObject("Img");
            var img   = imgGO.AddComponent<Image>();
            var ti = typeof(InventoryItem);
            ti.GetField("quantityText", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(slot, qt);
            ti.GetField("itemImage",    BindingFlags.NonPublic | BindingFlags.Instance).SetValue(slot, img);

            // Wire public fields needed by EmptySlot
            slot.itemDescriptionTitle = new GameObject("T").AddComponent<TextMeshProUGUI>();
            slot.itemDescriptionText  = new GameObject("B").AddComponent<TextMeshProUGUI>();
            slot.infoImage            = new GameObject("I").AddComponent<Image>();
            slot.selectedShaders      = new GameObject("S");

            inventoryManager.inventoryItems = new[] { slot };
            inventoryManager.itemSOs        = new ItemSO[0];

            // Create the WorkshopStation under test
            stationGO = new GameObject("WorkshopStation");
            station   = stationGO.AddComponent<WorkshopStation>();

            // Inject inventoryManager directly (bypasses Start() / GameObject.Find)
            typeof(WorkshopStation)
                .GetField("inventoryManager", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(station, inventoryManager);

            // Configure a recipe list
            typeof(WorkshopStation)
                .GetField("recipes", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(station, new List<WorkshopRecipe>());

            // Set station type to CraftingBench for test recipes
            typeof(WorkshopStation)
                .GetField("stationType", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(station, StationType.CraftingBench);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(stationGO);
            Object.DestroyImmediate(inventoryGO);
        }

        // ── GetAvailableRecipes ───────────────────────────────────────────────────

        [Test]
        public void GetAvailableRecipes_ReturnsOnlyMatchingStationType()
        {
            var matching    = ScriptableObject.CreateInstance<WorkshopRecipe>();
            var nonMatching = ScriptableObject.CreateInstance<WorkshopRecipe>();
            matching.stationType    = StationType.CraftingBench;
            nonMatching.stationType = StationType.RecyclingBin;

            typeof(WorkshopStation)
                .GetField("recipes", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(station, new List<WorkshopRecipe> { matching, nonMatching });

            var result = station.GetAvailableRecipes();
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(matching, result[0]);

            Object.DestroyImmediate(matching);
            Object.DestroyImmediate(nonMatching);
        }

        [Test]
        public void GetAvailableRecipes_EmptyWhenNoRecipesRegistered()
        {
            var result = station.GetAvailableRecipes();
            Assert.AreEqual(0, result.Count);
        }

        // ── CanProcess ────────────────────────────────────────────────────────────

        [Test]
        public void CanProcess_ReturnsFalse_WhenRequiredItemsNotInInventory()
        {
            var recipe = ScriptableObject.CreateInstance<WorkshopRecipe>();
            recipe.stationType = StationType.CraftingBench;
            recipe.inputs = new List<WorkshopRecipe.ItemStack>
            {
                new WorkshopRecipe.ItemStack { itemName = "Wood", amount = 3 }
            };

            // Inventory slot is empty — CanProcess should return false
            bool result = station.CanProcess(recipe);
            Assert.IsFalse(result);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void CanProcess_ReturnsTrue_WhenRequiredItemsPresent()
        {
            var slot = inventoryManager.inventoryItems[0];
            slot.itemName = "Wood";
            slot.quantity = 5;

            var recipe = ScriptableObject.CreateInstance<WorkshopRecipe>();
            recipe.stationType = StationType.CraftingBench;
            recipe.inputs = new List<WorkshopRecipe.ItemStack>
            {
                new WorkshopRecipe.ItemStack { itemName = "Wood", amount = 3 }
            };

            bool result = station.CanProcess(recipe);
            Assert.IsTrue(result);

            Object.DestroyImmediate(recipe);
        }

        // ── TryProcess ────────────────────────────────────────────────────────────

        [Test]
        public void TryProcess_ReturnsFalse_WhenStationTypeMismatch()
        {
            var recipe = ScriptableObject.CreateInstance<WorkshopRecipe>();
            recipe.stationType = StationType.RecyclingBin; // station is CraftingBench
            recipe.inputs      = new List<WorkshopRecipe.ItemStack>();
            recipe.outputPrefabs = new List<WorkshopRecipe.ItemOutput>();

            bool result = station.TryProcess(recipe);
            Assert.IsFalse(result);

            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void TryProcess_ReturnsFalse_WhenItemsMissing()
        {
            var recipe = ScriptableObject.CreateInstance<WorkshopRecipe>();
            recipe.stationType = StationType.CraftingBench;
            recipe.inputs      = new List<WorkshopRecipe.ItemStack>
            {
                new WorkshopRecipe.ItemStack { itemName = "Wood", amount = 5 }
            };
            recipe.outputPrefabs = new List<WorkshopRecipe.ItemOutput>();

            bool result = station.TryProcess(recipe);
            Assert.IsFalse(result);

            Object.DestroyImmediate(recipe);
        }

        // ── Interactable base ─────────────────────────────────────────────────────

        [Test]
        public void GetUniqueId_IsNonEmpty_AfterAwake()
        {
            Assert.IsFalse(string.IsNullOrEmpty(station.GetUniqueId()));
        }

        [Test]
        public void SaveState_ReturnsStateWithMatchingUniqueId()
        {
            var state = station.SaveState();
            Assert.AreEqual(station.GetUniqueId(), state.uniqueId);
        }

        [Test]
        public void LoadState_DoesNotThrow()
        {
            var state = station.SaveState();
            Assert.DoesNotThrow(() => station.LoadState(state));
        }
    }
}
