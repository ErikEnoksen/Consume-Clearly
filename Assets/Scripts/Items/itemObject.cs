using System;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(fileName = "Item Object", menuName = "Scriptable Objects/itemObject")]
    public class ItemObject : ScriptableObject
    {
        [field: SerializeField]
        public bool IsStackable { get; set; }

        public int Id => GetInstanceID();

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
    }
}
