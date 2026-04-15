using System.Collections;
using NUnit.Framework;
using Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class PlayerManager_PlayModeTests
    {
        private GameObject pmGo;
        private PlayerManager pm;
        private GameObject prefab;

        [SetUp]
        public void SetUp()
        {
            pmGo = new GameObject("PlayerManager");
            pm = pmGo.AddComponent<PlayerManager>();
        }

        [TearDown]
        public void TearDown()
        {
            if (pmGo != null) Object.DestroyImmediate(pmGo);
            if (prefab != null) Object.DestroyImmediate(prefab);
            prefab = null;
        }

        [UnityTest]
        public IEnumerator SpawnPlayer_NoPrefab_LogsErrorAndDoesNotCreate()
        {
            LogAssert.Expect(LogType.Error, "Player Prefab is not assigned in PlayerManager!");

            pm.SpawnPlayer(Vector3.zero);
            yield return null;

            Assert.IsNull(pm.GetPlayer());
        }

        [UnityTest]
        public IEnumerator SpawnPlayer_PrefabMissingGroundCheck_LogsErrorAfterInstantiate()
        {
            prefab = new GameObject("PlayerPrefab");
            typeof(PlayerManager)
                .GetField("playerPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(pm, prefab);

            LogAssert.Expect(LogType.Error, "Spawned player prefab is missing a GroundCheck object!");

            pm.SpawnPlayer(Vector3.zero);
            yield return null;
        }
    }
}

