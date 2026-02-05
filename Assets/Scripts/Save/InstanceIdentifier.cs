using System;
using UnityEngine;

namespace Save
{
    [DisallowMultipleComponent]
    public class InstanceIdentifier : MonoBehaviour
    {
        [SerializeField] private string id;
        public string Id => id;

#if UNITY_EDITOR
        // When adding the component or placing the prefab in the scene, give it a GUID
        private void Reset()
        {
            EnsureId();
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            // Only assign in editor (so prefab asset itself can keep the field empty)
            if (!Application.isPlaying)
            {
                EnsureId();
                UnityEditor.EditorUtility.SetDirty(this);
            }
        }
#endif

        private void Awake()
        {
            // Ensure runtime has an id if one wasn't set
            EnsureId();
        }

        private void EnsureId()
        {
            if (string.IsNullOrEmpty(id))
                id = Guid.NewGuid().ToString();
        }
    }
}