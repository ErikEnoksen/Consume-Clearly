using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class WorkshopRecipe_EditModeTests
    {
        private WorkshopRecipe recipe;

        [SetUp]
        public void SetUp()
        {
            recipe = ScriptableObject.CreateInstance<WorkshopRecipe>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void DefaultStationType_IsRecyclingBin()
        {
            Assert.AreEqual(StationType.RecyclingBin, recipe.stationType);
        }

        [Test]
        public void StationType_CanBeAssigned()
        {
            recipe.stationType = StationType.CraftingBench;
            Assert.AreEqual(StationType.CraftingBench, recipe.stationType);
        }

        [Test]
        public void Inputs_CanBePopulated()
        {
            recipe.inputs = new List<WorkshopRecipe.ItemStack>
            {
                new WorkshopRecipe.ItemStack { itemName = "Wood", amount = 3 },
                new WorkshopRecipe.ItemStack { itemName = "Stone", amount = 2 }
            };

            Assert.AreEqual(2, recipe.inputs.Count);
            Assert.AreEqual("Wood", recipe.inputs[0].itemName);
            Assert.AreEqual(3, recipe.inputs[0].amount);
        }

        [Test]
        public void OutputPrefabs_CanBePopulated()
        {
            recipe.outputPrefabs = new List<WorkshopRecipe.ItemOutput>
            {
                new WorkshopRecipe.ItemOutput { itemPrefab = null, amount = 1 }
            };

            Assert.AreEqual(1, recipe.outputPrefabs.Count);
            Assert.AreEqual(1, recipe.outputPrefabs[0].amount);
        }

        [Test]
        public void ItemStack_FieldsAreReadable()
        {
            var stack = new WorkshopRecipe.ItemStack
            {
                itemName = "Iron",
                amount = 5,
                icon = null
            };

            Assert.AreEqual("Iron", stack.itemName);
            Assert.AreEqual(5, stack.amount);
        }

        [Test]
        public void AllStationTypes_AreDistinct()
        {
            // Smoke-test that all enum values can be assigned without error
            foreach (StationType t in System.Enum.GetValues(typeof(StationType)))
            {
                recipe.stationType = t;
                Assert.AreEqual(t, recipe.stationType);
            }
        }
    }
}