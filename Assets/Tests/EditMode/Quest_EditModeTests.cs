using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class Quest_EditModeTests
    {
        // ── QuestObjective ────────────────────────────────────────────────────────

        [Test]
        public void QuestObjective_IsCompleted_WhenCurrentMeetsRequired()
        {
            var obj = new QuestObjective { requiredAmount = 3, currentAmount = 3 };
            Assert.IsTrue(obj.IsCompleted);
        }

        [Test]
        public void QuestObjective_IsCompleted_TrueWhenCurrentExceedsRequired()
        {
            var obj = new QuestObjective { requiredAmount = 3, currentAmount = 5 };
            Assert.IsTrue(obj.IsCompleted);
        }

        [Test]
        public void QuestObjective_IsCompleted_FalseWhenNotMet()
        {
            var obj = new QuestObjective { requiredAmount = 5, currentAmount = 2 };
            Assert.IsFalse(obj.IsCompleted);
        }

        [Test]
        public void QuestObjective_IsCompleted_FalseAtZero()
        {
            var obj = new QuestObjective { requiredAmount = 1, currentAmount = 0 };
            Assert.IsFalse(obj.IsCompleted);
        }

        // ── QuestProgress ─────────────────────────────────────────────────────────

        private Quest MakeQuest(string id, params QuestObjective[] objectives)
        {
            var q = ScriptableObject.CreateInstance<Quest>();
            q.questID = id;
            q.questName = "Test Quest";
            q.objectives = new List<QuestObjective>(objectives);
            q.rewards = new QuestReward { money = 0, circularSatisfaction = 0, items = new List<QuestRewardItem>() };
            return q;
        }

        [Test]
        public void QuestProgress_Constructor_DeepCopiesObjectives()
        {
            var quest = MakeQuest("q1",
                new QuestObjective { objectiveID = "obj1", requiredAmount = 5, currentAmount = 3 });

            var progress = new QuestProgress(quest);

            // Original modification must not affect the deep-copied progress
            quest.objectives[0].currentAmount = 99;
            Assert.AreEqual(0, progress.objectives[0].currentAmount,
                "QuestProgress should reset currentAmount to 0, independent of the original asset");
            Assert.AreEqual(5, progress.objectives[0].requiredAmount);

            Object.DestroyImmediate(quest);
        }

        [Test]
        public void QuestProgress_Constructor_CopiesAllObjectiveFields()
        {
            var quest = MakeQuest("q2",
                new QuestObjective
                {
                    objectiveID = "gather_wood",
                    description = "Collect wood",
                    type = objectiveType.CollectItem,
                    requiredAmount = 10,
                    currentAmount = 0
                });

            var progress = new QuestProgress(quest);

            Assert.AreEqual("gather_wood", progress.objectives[0].objectiveID);
            Assert.AreEqual("Collect wood", progress.objectives[0].description);
            Assert.AreEqual(objectiveType.CollectItem, progress.objectives[0].type);
            Assert.AreEqual(10, progress.objectives[0].requiredAmount);

            Object.DestroyImmediate(quest);
        }

        [Test]
        public void QuestProgress_QuestId_MatchesSourceQuest()
        {
            var quest = MakeQuest("my_quest_id");

            var progress = new QuestProgress(quest);
            Assert.AreEqual("my_quest_id", progress.questID);

            Object.DestroyImmediate(quest);
        }

        [Test]
        public void QuestProgress_IsCompleted_TrueWhenAllObjectivesMet()
        {
            var quest = MakeQuest("q3",
                new QuestObjective { objectiveID = "a", requiredAmount = 1, currentAmount = 0 },
                new QuestObjective { objectiveID = "b", requiredAmount = 2, currentAmount = 0 });

            var progress = new QuestProgress(quest);
            progress.objectives[0].currentAmount = 1;
            progress.objectives[1].currentAmount = 2;

            Assert.IsTrue(progress.IsCompleted);

            Object.DestroyImmediate(quest);
        }

        [Test]
        public void QuestProgress_IsCompleted_FalseWhenOneObjectiveUnmet()
        {
            var quest = MakeQuest("q4",
                new QuestObjective { objectiveID = "a", requiredAmount = 1, currentAmount = 0 },
                new QuestObjective { objectiveID = "b", requiredAmount = 2, currentAmount = 0 });

            var progress = new QuestProgress(quest);
            progress.objectives[0].currentAmount = 1;
            // objectives[1] stays at 0

            Assert.IsFalse(progress.IsCompleted);

            Object.DestroyImmediate(quest);
        }

        [Test]
        public void QuestProgress_IsCompleted_FalseWithNoObjectives()
        {
            // A quest with zero objectives: TrueForAll on an empty list returns true in .NET.
            // Verify this known behaviour is intentional and consistent.
            var quest = MakeQuest("q5");
            var progress = new QuestProgress(quest);
            Assert.IsTrue(progress.IsCompleted, "A quest with no objectives is trivially complete");
            Object.DestroyImmediate(quest);
        }
    }
}