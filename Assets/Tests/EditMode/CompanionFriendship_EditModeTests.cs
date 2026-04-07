using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class CompanionFriendship_EditModeTests
    {
        private GameObject go;
        private CompanionFriendship friendship;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("Companion");
            friendship = go.AddComponent<CompanionFriendship>();
            friendship.CurrentFriendshipLevel = 0;
            friendship.MaxFriendshipLevel = 1000;
            friendship.DailyConversationReward = 25;
            friendship.MissedConversationSubstraction = 10;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        // ── IncreaseFriendship ────────────────────────────────────────────────────

        [Test]
        public void IncreaseFriendship_IncreasesLevel()
        {
            friendship.IncreaseFriendship(100);
            Assert.AreEqual(100, friendship.CurrentFriendshipLevel);
        }

        [Test]
        public void IncreaseFriendship_ClampsAtMax()
        {
            friendship.IncreaseFriendship(5000);
            Assert.AreEqual(friendship.MaxFriendshipLevel, friendship.CurrentFriendshipLevel);
        }

        [Test]
        public void IncreaseFriendship_AppliesHappyMultiplier()
        {
            friendship.CurrentMood = CompanionFriendship.CompanionMood.Happy;
            friendship.IncreaseFriendship(100);
            // 100 * 1.25 = 125, rounded
            Assert.AreEqual(125, friendship.CurrentFriendshipLevel);
        }

        [Test]
        public void IncreaseFriendship_AppliesAngryMultiplier()
        {
            friendship.CurrentMood = CompanionFriendship.CompanionMood.Angry;
            friendship.IncreaseFriendship(100);
            // 100 * 0.75 = 75, rounded
            Assert.AreEqual(75, friendship.CurrentFriendshipLevel);
        }

        [Test]
        public void IncreaseFriendship_AppliesNeutralMultiplier()
        {
            friendship.CurrentMood = CompanionFriendship.CompanionMood.Neutral;
            friendship.IncreaseFriendship(100);
            Assert.AreEqual(100, friendship.CurrentFriendshipLevel);
        }

        // ── DecreaseFriendship ────────────────────────────────────────────────────

        [Test]
        public void DecreaseFriendship_DecreasesLevel()
        {
            friendship.CurrentFriendshipLevel = 200;
            friendship.DecreaseFriendship(50);
            Assert.AreEqual(150, friendship.CurrentFriendshipLevel);
        }

        [Test]
        public void DecreaseFriendship_ClampsAtZero()
        {
            friendship.CurrentFriendshipLevel = 10;
            friendship.DecreaseFriendship(100);
            Assert.AreEqual(0, friendship.CurrentFriendshipLevel);
        }

        // ── CurrentState thresholds ───────────────────────────────────────────────

        [Test]
        public void CurrentState_IsStranger_WhenBelow100()
        {
            friendship.CurrentFriendshipLevel = 0;
            Assert.AreEqual(CompanionFriendship.FriendshipState.Stranger, friendship.CurrentState);

            friendship.CurrentFriendshipLevel = 99;
            Assert.AreEqual(CompanionFriendship.FriendshipState.Stranger, friendship.CurrentState);
        }

        [Test]
        public void CurrentState_IsAcquaintance_WhenBetween100And399()
        {
            friendship.CurrentFriendshipLevel = 100;
            Assert.AreEqual(CompanionFriendship.FriendshipState.Acquaintance, friendship.CurrentState);

            friendship.CurrentFriendshipLevel = 399;
            Assert.AreEqual(CompanionFriendship.FriendshipState.Acquaintance, friendship.CurrentState);
        }

        [Test]
        public void CurrentState_IsFriend_WhenBetween400And849()
        {
            friendship.CurrentFriendshipLevel = 400;
            Assert.AreEqual(CompanionFriendship.FriendshipState.Friend, friendship.CurrentState);

            friendship.CurrentFriendshipLevel = 849;
            Assert.AreEqual(CompanionFriendship.FriendshipState.Friend, friendship.CurrentState);
        }

        [Test]
        public void CurrentState_IsBestFriend_At850OrAbove()
        {
            friendship.CurrentFriendshipLevel = 850;
            Assert.AreEqual(CompanionFriendship.FriendshipState.BestFriend, friendship.CurrentState);

            friendship.CurrentFriendshipLevel = 1000;
            Assert.AreEqual(CompanionFriendship.FriendshipState.BestFriend, friendship.CurrentState);
        }

        // ── Multiplier ────────────────────────────────────────────────────────────

        [Test]
        public void Multiplier_IsOne_ForNeutralMood()
        {
            friendship.CurrentMood = CompanionFriendship.CompanionMood.Neutral;
            Assert.AreEqual(1.0, friendship.multiplier);
        }

        [Test]
        public void Multiplier_Is1Point25_ForHappyMood()
        {
            friendship.CurrentMood = CompanionFriendship.CompanionMood.Happy;
            Assert.AreEqual(1.25, friendship.multiplier);
        }

        [Test]
        public void Multiplier_Is0Point75_ForAngryMood()
        {
            friendship.CurrentMood = CompanionFriendship.CompanionMood.Angry;
            Assert.AreEqual(0.75, friendship.multiplier);
        }

        // ── GiveDailyConversationBonus ────────────────────────────────────────────

        [Test]
        public void GiveDailyConversationBonus_IncreasesFriendship()
        {
            friendship.GiveDailyConversationBonus();
            Assert.AreEqual(friendship.DailyConversationReward, friendship.CurrentFriendshipLevel);
        }

        [Test]
        public void GiveDailyConversationBonus_OnlyAppliesToFirstCallPerDay()
        {
            friendship.GiveDailyConversationBonus();
            int levelAfterFirst = friendship.CurrentFriendshipLevel;
            friendship.GiveDailyConversationBonus();
            Assert.AreEqual(levelAfterFirst, friendship.CurrentFriendshipLevel,
                "Daily bonus should only apply once per day");
        }

        // ── Events ────────────────────────────────────────────────────────────────

        [Test]
        public void OnFriendshipLevelChanged_FiresWhenIncreased()
        {
            int receivedLevel = -1;
            friendship.OnFriendshipLevelChanged += level => receivedLevel = level;
            friendship.IncreaseFriendship(50);
            Assert.AreEqual(50, receivedLevel);
        }

        [Test]
        public void OnFriendshipLevelChanged_FiresWhenDecreased()
        {
            friendship.CurrentFriendshipLevel = 200;
            int receivedLevel = -1;
            friendship.OnFriendshipLevelChanged += level => receivedLevel = level;
            friendship.DecreaseFriendship(50);
            Assert.AreEqual(150, receivedLevel);
        }

        [Test]
        public void OnStateChanged_FiresWhenStateChanges()
        {
            CompanionFriendship.FriendshipState? receivedState = null;
            friendship.OnStateChanged += state => receivedState = state;

            // Push past the Stranger threshold (100)
            friendship.IncreaseFriendship(100);

            Assert.IsNotNull(receivedState, "OnStateChanged should have fired");
            Assert.AreEqual(CompanionFriendship.FriendshipState.Acquaintance, receivedState.Value);
        }
    }
}