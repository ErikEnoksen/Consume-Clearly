using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Save;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class SaveSystem_PlayModeTests
    {
        [TearDown]
        public void TearDown()
        {
            SaveSystem.ClearSaveData();
        }

        // ── Baseline 

        [UnityTest]
        public IEnumerator SaveSystem_SavesAndLoadsGameCorrectly()
        {
            var saveData = new SaveData
            {
                GameTime = 100.5f,
                CurrentScene = "TestScene",
                PlayerPosition = new Vector3(1f, 2f, 3f)
            };

            SaveSystem.Save(saveData);
            yield return null;

            var loadedData = SaveSystem.Load();

            Assert.IsNotNull(loadedData);
            Assert.AreEqual(saveData.GameTime, loadedData.GameTime);
            Assert.AreEqual(saveData.CurrentScene, loadedData.CurrentScene);
            Assert.AreEqual(saveData.PlayerPosition, loadedData.PlayerPosition);
        }

        [UnityTest]
        public IEnumerator SaveSystem_InvalidSaveFileReturnsNull()
        {
            SaveSystem.ClearSaveData();
            yield return null;

            Assert.IsNull(SaveSystem.Load());
        }

        // ── Invalid scenes are blocked 

        [UnityTest]
        public IEnumerator SaveSystem_MainMenuScene_DoesNotSave()
        {
            LogAssert.Expect(LogType.Warning, "Save aborted: Scene is excluded from saving.");
            SaveSystem.Save(new SaveData { CurrentScene = "MainMenu" });
            yield return null;

            Assert.IsNull(SaveSystem.Load());
        }

        [UnityTest]
        public IEnumerator SaveSystem_CreditsScene_DoesNotSave()
        {
            LogAssert.Expect(LogType.Warning, "Save aborted: Scene is excluded from saving.");
            SaveSystem.Save(new SaveData { CurrentScene = "Credits" });
            yield return null;

            Assert.IsNull(SaveSystem.Load());
        }

        // ── Money 

        [UnityTest]
        public IEnumerator SaveSystem_Money_RoundTrip()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", Money = 250 });
            yield return null;

            Assert.AreEqual(250, SaveSystem.Load().Money);
        }

        // ── Day cycle ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveSystem_DayAndTimer_RoundTrip()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", CurrentDay = 5, DayTimer = 42.3f });
            yield return null;

            var loaded = SaveSystem.Load();
            Assert.AreEqual(5, loaded.CurrentDay);
            Assert.AreEqual(42.3f, loaded.DayTimer, 0.001f);
        }

        // ── Community meter ───────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveSystem_CommunityLevel_RoundTrip()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", CommunityLevel = 42, CommunityOverflowPoints = 7 });
            yield return null;

            var loaded = SaveSystem.Load();
            Assert.AreEqual(42, loaded.CommunityLevel);
            Assert.AreEqual(7, loaded.CommunityOverflowPoints);
        }

        // ── Hunger ───────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveSystem_HungerValue_RoundTrip()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", HungerValue = 63.5f });
            yield return null;

            Assert.AreEqual(63.5f, SaveSystem.Load().HungerValue, 0.001f);
        }

        // ── Inventory ─────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveSystem_Inventory_RoundTrip()
        {
            var saveData = new SaveData
            {
                CurrentScene = "TestScene",
                InventorySlots = new List<InventorySlotData>
                {
                    new InventorySlotData { itemName = "Bread", itemID = "item_bread", quantity = 3, maxStack = 10, itemTag = "Food" },
                    new InventorySlotData { itemName = "Key",   itemID = "item_key",   quantity = 1, maxStack = 1,  itemTag = "Quest" }
                }
            };

            SaveSystem.Save(saveData);
            yield return null;

            var loaded = SaveSystem.Load();
            Assert.AreEqual(2, loaded.InventorySlots.Count);
            Assert.AreEqual("Bread",      loaded.InventorySlots[0].itemName);
            Assert.AreEqual(3,            loaded.InventorySlots[0].quantity);
            Assert.AreEqual("item_key",   loaded.InventorySlots[1].itemID);
        }

        [UnityTest]
        public IEnumerator SaveSystem_EmptyInventory_RoundTrip()
        {
            SaveSystem.Save(new SaveData { CurrentScene = "TestScene", InventorySlots = new List<InventorySlotData>() });
            yield return null;

            var loaded = SaveSystem.Load();
            Assert.IsNotNull(loaded.InventorySlots);
            Assert.AreEqual(0, loaded.InventorySlots.Count);
        }

        // ── Dialogue stage ────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveSystem_DialogueState_RoundTrip()
        {
            var saveData = new SaveData
            {
                CurrentScene = "TestScene",
                InteractableStates = new List<InteractableObjectState>
                {
                    new InteractableObjectState
                    {
                        uniqueId = "Erik",
                        dialogueStageIndex = 2,
                        conversationsOnStage = 3
                    }
                }
            };

            SaveSystem.Save(saveData);
            yield return null;

            var npc = SaveSystem.Load().InteractableStates[0];
            Assert.AreEqual("Erik", npc.uniqueId);
            Assert.AreEqual(2, npc.dialogueStageIndex);
            Assert.AreEqual(3, npc.conversationsOnStage);
        }

        // ── Friendship ────────────────────────────────────────────────────────────

        [UnityTest]
        public IEnumerator SaveSystem_FriendshipState_RoundTrip()
        {
            var saveData = new SaveData
            {
                CurrentScene = "TestScene",
                InteractableStates = new List<InteractableObjectState>
                {
                    new InteractableObjectState
                    {
                        uniqueId = "companion_1",
                        friendshipLevel = 75,
                        friendshipMood = 2,
                        dailyConversationGiven = true
                    }
                }
            };

            SaveSystem.Save(saveData);
            yield return null;

            var companion = SaveSystem.Load().InteractableStates[0];
            Assert.AreEqual(75,   companion.friendshipLevel);
            Assert.AreEqual(2,    companion.friendshipMood);
            Assert.IsTrue(companion.dailyConversationGiven);
        }

        // ── Scene-specific save files (used by TransitionCoroutine) ───────────────

        [UnityTest]
        public IEnumerator SaveSystem_SceneNamedFile_SavesAndLoadsIndependently()
        {
            var mainSave = new SaveData { CurrentScene = "TestScene", Money = 100 };
            var sceneSave = new SaveData
            {
                CurrentScene = "Downtown",
                InteractableStates = new List<InteractableObjectState>
                {
                    new InteractableObjectState { uniqueId = "trashcan_01", isActive = false }
                }
            };

            SaveSystem.Save(mainSave);
            SaveSystem.Save(sceneSave, "Downtown");
            yield return null;

            var loadedMain  = SaveSystem.Load();
            var loadedScene = SaveSystem.Load("Downtown");

            Assert.AreEqual(100, loadedMain.Money);
            Assert.AreEqual(1, loadedScene.InteractableStates.Count);
            Assert.AreEqual("trashcan_01", loadedScene.InteractableStates[0].uniqueId);
            Assert.IsFalse(loadedScene.InteractableStates[0].isActive);

            SaveSystem.ClearSaveData("Downtown");
        }
    }
}