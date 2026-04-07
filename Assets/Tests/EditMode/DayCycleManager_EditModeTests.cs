using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class DayCycleManager_EditModeTests
    {
        private GameObject go;
        private DayCycleManager manager;

        private static void ClearSingleton()
        {
            typeof(DayCycleManager)
                .GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                .GetSetMethod(nonPublic: true)
                .Invoke(null, new object[] { null });
        }

        [SetUp]
        public void SetUp()
        {
            ClearSingleton();
            go = new GameObject("DayCycleManager");
            manager = go.AddComponent<DayCycleManager>();
        }

        [TearDown]
        public void TearDown()
        {
            ClearSingleton();
            Object.DestroyImmediate(go);
        }

        [Test]
        public void CurrentDay_StartsAtOne()
        {
            Assert.AreEqual(1, manager.CurrentDay);
        }

        [Test]
        public void DayLength_HasPositiveDefaultValue()
        {
            Assert.Greater(manager.dayLength, 0f);
        }

        [Test]
        public void Singleton_IsSetToManagerAfterAwake()
        {
            Assert.AreEqual(manager, DayCycleManager.Instance);
        }

        [Test]
        public void Singleton_SecondInstanceIsDestroyed()
        {
            // Create a second DayCycleManager - it should be destroyed, leaving Instance as the first.
            var go2 = new GameObject("DayCycleManager2");
            var manager2 = go2.AddComponent<DayCycleManager>();

            Assert.AreEqual(manager, DayCycleManager.Instance,
                "Instance should remain the first manager when a duplicate is added");

            Object.DestroyImmediate(go2);
        }

        [Test]
        public void OnNewDay_CanSubscribeAndUnsubscribe_WithoutException()
        {
            void Handler(int day) { }

            Assert.DoesNotThrow(() =>
            {
                manager.OnNewDay += Handler;
                manager.OnNewDay -= Handler;
            });
        }

        [Test]
        public void CurrentDay_IncrementedWhenDayTimerElapsed()
        {
            // Use reflection to inject a dayTimer value past dayLength
            manager.dayLength = 60f;

            var dayTimerField = typeof(DayCycleManager)
                .GetField("dayTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            dayTimerField.SetValue(manager, 61f);

            // Manually trigger the Update logic through reflection
            var updateMethod = typeof(DayCycleManager)
                .GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            updateMethod.Invoke(manager, null);

            Assert.AreEqual(2, manager.CurrentDay);
        }

        [Test]
        public void OnNewDay_EventFiredWithCorrectDay()
        {
            manager.dayLength = 1f;
            int receivedDay = -1;
            manager.OnNewDay += day => receivedDay = day;

            var dayTimerField = typeof(DayCycleManager)
                .GetField("dayTimer", BindingFlags.NonPublic | BindingFlags.Instance);
            dayTimerField.SetValue(manager, 2f);

            typeof(DayCycleManager)
                .GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance)
                .Invoke(manager, null);

            Assert.AreEqual(2, receivedDay);
        }
    }
}