using System;
using UnityEngine;

namespace Save
{
    [Serializable]
    public class InteractableObjectState
    {
        public string uniqueId;
        public bool isActive;
        public Vector3 position;
        public Quaternion rotation;
        // Optional: name of the rope variant (ItemObject.name) attached to a Hook so it can be restored on load
        public string ropeVariantName;
    }
}
