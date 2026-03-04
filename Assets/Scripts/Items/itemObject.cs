using System;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "Item Object", menuName = "Scriptable Objects/itemObject")]
    public class ItemObject : ScriptableObject
    {
        [field: SerializeField]
        public bool IsStackable { get; set; }

        //public int Id => GetInstanceID();

        [SerializeField]
        private string itemID; // persistent id

        // Use a stable string ID rather than GetInstanceID()
        public string Id
        {
            get
            {
                if (string.IsNullOrEmpty(itemID))
                    GenerateId();
                return itemID;
            }
        }

        [field: SerializeField]
        public int MaxStackSize { get; set; } = 1;
        [field: SerializeField]
        public string Name { get; set; }
        [field: SerializeField]
        [field: TextArea]
        public string Description { get; set; }
        [field: SerializeField]
        public Sprite ItemImage { get; set; }
        [field: SerializeField]
        public bool IsUsable { get; set; }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(itemID))
                GenerateId();
        }

        private void GenerateId()
        {
            // Keep IDs short but unique: name + GUID fragment
            itemID = $"{(string.IsNullOrEmpty(Name) ? "item" : Name)}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }
    }
}
