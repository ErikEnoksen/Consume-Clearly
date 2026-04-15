using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Tests for CommunityMeter business logic.
    /// communityMeter (Slider) is left null – UpdateCommunityMeter() is null-safe.
    /// DayCycleManager is not present – the OnNewDay subscription is guarded by a null check.
    /// </summary>
    [TestFixture]
    public class CommunityMeter_EditModeTests
    {
        private GameObject go;
        private CommunityMeter meter;

        [SetUp]
        public void SetUp()
        {
            go = new GameObject("CommunityMeter");
            meter = go.AddComponent<CommunityMeter>();
            // Reset to known state
            meter.CommunityLevel = 0;
            meter.OverflowPoints = 0;
            meter.FriendCount = 0;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(go);
        }

        // ── IncreaseCommunityLevel ────────────────────────────────────────────────

        [Test]
        public void IncreaseCommunityLevel_IncreasesLevelWhenBelowCap()
        {
            // FriendCount = 0 → cap = 25
            meter.IncreaseCommunityLevel(10);
            Assert.AreEqual(10, meter.CommunityLevel);
        }

        [Test]
        public void IncreaseCommunityLevel_StoresOverflowWhenAboveCap()
        {
            // FriendCount = 0 → cap = 25; adding 30 puts 5 into overflow
            meter.IncreaseCommunityLevel(30);
            Assert.AreEqual(25, meter.CommunityLevel);
            Assert.AreEqual(5, meter.OverflowPoints);
        }

        [Test]
        public void IncreaseCommunityLevel_DoesNotExceedGlobalMax()
        {
            meter.FriendCount = 10; // Unlocks max cap (100)
            meter.IncreaseCommunityLevel(200);
            Assert.LessOrEqual(meter.CommunityLevel, 100);
        }

        // ── DecreaseCommunityLevel ────────────────────────────────────────────────

        [Test]
        public void DecreaseCommunityLevel_DecreasesLevel()
        {
            meter.CommunityLevel = 20;
            meter.DecreaseCommunityLevel(5);
            Assert.AreEqual(15, meter.CommunityLevel);
        }

        [Test]
        public void DecreaseCommunityLevel_ClampsAtZero()
        {
            meter.CommunityLevel = 5;
            meter.DecreaseCommunityLevel(100);
            Assert.AreEqual(0, meter.CommunityLevel);
        }

        // ── Cap logic via FriendCount ─────────────────────────────────────────────

        [Test]
        public void IncreaseCommunityLevel_CapIs25WithNoFriends()
        {
            meter.FriendCount = 0;
            meter.IncreaseCommunityLevel(50);
            Assert.AreEqual(25, meter.CommunityLevel);
            Assert.AreEqual(25, meter.OverflowPoints);
        }

        [Test]
        public void IncreaseCommunityLevel_CapIs50WithOneFriend()
        {
            meter.FriendCount = 1;
            meter.IncreaseCommunityLevel(60);
            Assert.AreEqual(50, meter.CommunityLevel);
            Assert.AreEqual(10, meter.OverflowPoints);
        }

        [Test]
        public void IncreaseCommunityLevel_CapIs75WithTwoFriends()
        {
            meter.FriendCount = 2;
            meter.IncreaseCommunityLevel(80);
            Assert.AreEqual(75, meter.CommunityLevel);
            Assert.AreEqual(5, meter.OverflowPoints);
        }

        [Test]
        public void IncreaseCommunityLevel_CapIs100WithThreeOrMoreFriends()
        {
            meter.FriendCount = 3;
            meter.IncreaseCommunityLevel(110);
            Assert.AreEqual(100, meter.CommunityLevel);
            Assert.AreEqual(10, meter.OverflowPoints);
        }

        // ── Overflow accumulation ─────────────────────────────────────────────────

        [Test]
        public void OverflowPoints_AccumulateAcrossMultipleCalls()
        {
            // FriendCount=0 → cap=25; first call maxes out the level and banks 15
            meter.IncreaseCommunityLevel(40);
            Assert.AreEqual(25, meter.CommunityLevel);
            Assert.AreEqual(15, meter.OverflowPoints);

            // Second call at the same cap: all 5 points go straight to overflow
            meter.IncreaseCommunityLevel(5);
            Assert.AreEqual(25, meter.CommunityLevel);
            Assert.AreEqual(20, meter.OverflowPoints);
        }
    }
}