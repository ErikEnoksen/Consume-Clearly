using System.Collections.Generic;
using System.Reflection;
using Assets.Scripts.Quests;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class QuestController_EditModeTests
    {
        private GameObject go;
        private QuestController controller;
        private Quest quest;

        // Helper to clear the static singleton between tests.
        private static void ClearSingleton()
        {
            typeof(QuestController)
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .GetSetMethod(nonPublic: true)
                .Invoke(null, new object[] { null });
        }

        [SetUp]
        public void SetUp()
        {
            ClearSingleton();

            go = new GameObject("QuestController");
            controller = go.AddComponent<QuestController>();

            quest = ScriptableObject.CreateInstance<Quest>();
            quest.questID = "quest_001";
            quest.questName = "Test Quest";
            quest.objectives = new List<QuestObjective>
            {
                new QuestObjective
                {
                    objectiveID = "collect_wood",
                    description = "Collect 3 wood",
                    type = objectiveType.CollectItem,
                    requiredAmount = 3,
                    currentAmount = 0
                }
            };
            quest.rewards = new QuestReward
            {
                money = 0,
                circularSatisfaction = 0f,
                items = new List<QuestRewardItem>()
            };
        }

        [TearDown]
        public void TearDown()
        {
            ClearSingleton();
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(quest);
        }

        // ── AcceptQuest ───────────────────────────────────────────────────────────

        [Test]
        public void AcceptQuest_AddsQuestToActiveList()
        {
            controller.AcceptQuest(quest);
            Assert.IsTrue(controller.IsQuestActive(quest.questID));
        }

        [Test]
        public void AcceptQuest_DoesNotAddDuplicate()
        {
            controller.AcceptQuest(quest);
            controller.AcceptQuest(quest);
            Assert.AreEqual(1, controller.ActiveQuests.Count);
        }

        [Test]
        public void AcceptQuest_NullQuestIsIgnored()
        {
            controller.AcceptQuest(null);
            Assert.AreEqual(0, controller.ActiveQuests.Count);
        }

        [Test]
        public void AcceptQuest_IgnoresAlreadyCompletedQuest()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 3);
            controller.TurnInQuest(quest.questID, null);

            controller.AcceptQuest(quest);
            Assert.IsFalse(controller.IsQuestActive(quest.questID));
        }

        // ── UpdateObjectiveProgress ───────────────────────────────────────────────

        [Test]
        public void UpdateObjectiveProgress_IncrementsObjective()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 2);

            var progress = controller.ActiveQuests.Find(q => q.questID == quest.questID);
            Assert.AreEqual(2, progress.objectives[0].currentAmount);
        }

        [Test]
        public void UpdateObjectiveProgress_ClampsAtRequired()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 10);

            var progress = controller.ActiveQuests.Find(q => q.questID == quest.questID);
            Assert.AreEqual(3, progress.objectives[0].currentAmount);
        }

        [Test]
        public void UpdateObjectiveProgress_DoesNothingForUnknownObjective()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("nonexistent_objective", objectiveType.CollectItem, 5);

            var progress = controller.ActiveQuests.Find(q => q.questID == quest.questID);
            Assert.AreEqual(0, progress.objectives[0].currentAmount);
        }

        // ── IsQuestCompleted ──────────────────────────────────────────────────────

        [Test]
        public void IsQuestCompleted_ReturnsTrueWhenAllObjectivesMet()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 3);
            Assert.IsTrue(controller.IsQuestCompleted(quest.questID));
        }

        [Test]
        public void IsQuestCompleted_ReturnsFalseWhenNotEnoughProgress()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 1);
            Assert.IsFalse(controller.IsQuestCompleted(quest.questID));
        }

        [Test]
        public void IsQuestCompleted_ReturnsFalseForUnknownQuest()
        {
            Assert.IsFalse(controller.IsQuestCompleted("unknown_id"));
        }

        [Test]
        public void IsQuestCompleted_ReturnsTrueForAlreadyCompletedQuest()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 3);
            controller.TurnInQuest(quest.questID, null);

            Assert.IsTrue(controller.IsQuestCompleted(quest.questID));
        }

        // ── TurnInQuest ───────────────────────────────────────────────────────────

        [Test]
        public void TurnInQuest_RemovesFromActiveAndMarksCompleted()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 3);

            bool result = controller.TurnInQuest(quest.questID, null);

            Assert.IsTrue(result);
            Assert.IsFalse(controller.IsQuestActive(quest.questID));
            Assert.IsTrue(controller.HasQuestBeenCompleted(quest.questID));
        }

        [Test]
        public void TurnInQuest_ReturnsFalseIfQuestNotActive()
        {
            bool result = controller.TurnInQuest("nonexistent_quest", null);
            Assert.IsFalse(result);
        }

        [Test]
        public void TurnInQuest_ReturnsFalseIfObjectivesNotComplete()
        {
            controller.AcceptQuest(quest);
            // Objectives not completed
            bool result = controller.TurnInQuest(quest.questID, null);
            Assert.IsFalse(result);
        }

        // ── HasQuestBeenCompleted ─────────────────────────────────────────────────

        [Test]
        public void HasQuestBeenCompleted_FalseForFreshController()
        {
            Assert.IsFalse(controller.HasQuestBeenCompleted("any_quest"));
        }

        [Test]
        public void HasQuestBeenCompleted_TrueAfterTurnIn()
        {
            controller.AcceptQuest(quest);
            controller.UpdateObjectiveProgress("collect_wood", objectiveType.CollectItem, 3);
            controller.TurnInQuest(quest.questID, null);

            Assert.IsTrue(controller.HasQuestBeenCompleted(quest.questID));
        }
    }
}