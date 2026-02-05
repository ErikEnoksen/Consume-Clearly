// filepath: c:\Users\ianko\Consume-Clearly\Assets\Tests\PlayMode\PlayerManager_PlayModeTests.cs
using System.Collections;
using NUnit.Framework;
using Player;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    public class PlayerManager_PlayModeTests
    {
        [UnityTest]
        public IEnumerator SpawnPlayer_NoPrefab_LogsErrorAndDoesNotCreate()
        {
            var pmGo = new GameObject("PlayerManager");
            var pm = pmGo.AddComponent<PlayerManager>();

            // Ensure playerPrefab is null
            // Capture logs
            LogAssert.Expect(UnityEngine.LogType.Error, "Player Prefab is not assigned in PlayerManager!");

            pm.SpawnPlayer(Vector3.zero);
            yield return null;

            Assert.IsNull(pm.GetPlayer());

            Object.DestroyImmediate(pmGo);
        }

        [UnityTest]
        public IEnumerator SpawnPlayer_PrefabMissingGroundCheck_LogsErrorAfterInstantiate()
        {
            var pmGo = new GameObject("PlayerManager2");
            var pm = pmGo.AddComponent<PlayerManager>();

            // create a prefab without GroundCheck
            var prefab = new GameObject("PlayerPrefab");
            // assign via reflection
            typeof(PlayerManager).GetField("playerPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(pm, prefab);

            LogAssert.Expect(UnityEngine.LogType.Error, "Spawned player prefab is missing a GroundCheck object!");

            pm.SpawnPlayer(Vector3.zero);
            yield return null;

            // Even though it may have been instantiated, it should have logged the error and left player as null or destroyed; just ensure the error was logged
            Object.DestroyImmediate(pmGo);
            Object.DestroyImmediate(prefab);
        }
    }
}

