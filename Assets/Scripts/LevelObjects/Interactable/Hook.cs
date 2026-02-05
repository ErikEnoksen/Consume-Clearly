using System.Collections;
using System;
using Save;
using Inventory;
using Items;
using UnityEngine;

namespace LevelObjects.Interactable
{
    public class Hook : Interactable
    {
        private InventoryController _inventoryController;
        private InventoryObject _inventoryData;
        private bool _ropeAttached;
        private bool _enableRopeLogs = true;

        [Header("Rope Settings")]
        [SerializeField] private GameObject ropePrefab;
        [SerializeField] private Transform ropeParent;

        [Serializable]
        public class RopeVariant
        {
            public ItemObject itemAsset; // reference to the ItemObject asset for this rope variant
            public GameObject prefab; // prefab to instantiate for this rope type
        }

        [Header("Rope Variants")]
        [SerializeField] private RopeVariant[] ropeVariants;

        // Runtime rope instance
        private GameObject _ropeInstance;
        private GameObject _hookSpriteObject;

        private string _currentVariantName;

        void RopeLog(string message)
        {
            if (_enableRopeLogs)
                Debug.Log(message);
        }

        private void Start()
        {
            _inventoryController = FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
            RopeLog("Inventory Controller found: " + (_inventoryController != null));
            _inventoryData = _inventoryController != null ? _inventoryController.InventoryObject : null;
            RopeLog("Inventory Data found: " + (_inventoryData != null));

            // Cache the hook sprite GameObject under ropeParent (or fallback to a child sprite on this object)
            if (ropeParent != null)
            {
                var sr = ropeParent.GetComponentInChildren<SpriteRenderer>(true);
                _hookSpriteObject = sr != null ? sr.gameObject : null;
            }
            else
            {
                var sr = GetComponentInChildren<SpriteRenderer>(true);
                _hookSpriteObject = sr != null ? sr.gameObject : null;
            }

            // If loaded with rope attached, ensure visuals exist
            if (_ropeAttached)
            {
                EnsureRopeCreated(false);
                if (_hookSpriteObject != null) _hookSpriteObject.SetActive(false);
            }
        }

        public override void Interact()
        {
            StartCoroutine(UseHook());
        }

        public IEnumerator UseHook()
        {
            if (_inventoryData == null)
            {
                RopeLog("Inventory data not found.");
                yield break;
            }
            var (ropeIndex, selectedPrefab) = FindRopeVariantInInventory();
            if (ropeIndex == -1)
            {
                RopeLog("Rope item not found in inventory (no variant match).");
                yield break;
            }
            var ropeItem = _inventoryData.GetItemAt(ropeIndex);
            bool hasRope = !ropeItem.IsEmpty && ropeItem.Quantity > 0;
            if (hasRope && !_ropeAttached)
            {
                // consume exactly one rope
                int newAmount = ropeItem.Quantity - 1;
                if (newAmount < 0) newAmount = 0;
                _inventoryData.ChangeQuantityAt(ropeIndex, newAmount);

                // create rope prefab and play unfurl (use selected variant prefab if present)
                EnsureRopeCreated(true, selectedPrefab);
                // remember which variant we used (by ItemObject name) for saving
                _currentVariantName = selectedPrefab != null ? _inventoryData.GetItemAt(ropeIndex).Item.name : null;

                // hide the original hook sprite GameObject so the rope animation's hook sprite is the only visible one
                if (_hookSpriteObject != null)
                    _hookSpriteObject.SetActive(false);

                _ropeAttached = true;
                RopeLog("Rope attached to the hook.");
            }
            else if (_ropeAttached)
            {
                // restore exactly one rope to that slot
                var current = _inventoryData.GetItemAt(ropeIndex);
                int restore = current.Quantity + 1;
                _inventoryData.ChangeQuantityAt(ropeIndex, restore);

                // remove visual immediately
                if (_ropeInstance != null)
                {
                    Destroy(_ropeInstance);
                    _ropeInstance = null;
                }

                // restore original hook sprite GameObject
                if (_hookSpriteObject != null)
                    _hookSpriteObject.SetActive(true);

                _ropeAttached = false;
                RopeLog("Rope removed from the hook.");
            }
            else
            {
                RopeLog("No rope available in inventory or to take from hook.");
                RopeLog("has rope= " + hasRope + " ropeattached= " + _ropeAttached);
            }
            // coroutine finished
        }

        private void EnsureRopeCreated(bool playUnfurl, GameObject selectedPrefab = null)
        {
            if (_ropeInstance != null) return;
            var prefabToUse = selectedPrefab != null ? selectedPrefab : ropePrefab;
            if (prefabToUse == null)
            {
                RopeLog("ropePrefab is not assigned on Hook (nor variant selected).");
                return;
            }

            var parent = ropeParent != null ? ropeParent : transform;
            _ropeInstance = Instantiate(prefabToUse, parent);
            _ropeInstance.transform.localPosition = Vector3.zero;
            _ropeInstance.transform.localRotation = Quaternion.identity;

            var ropeComp = _ropeInstance.GetComponentInChildren<Rope>(true);
            if (ropeComp != null)
            {
                RopeLog("EnsureRopeCreated: Rope component found on spawned prefab.");
                if (playUnfurl)
                {
                    RopeLog("EnsureRopeCreated: scheduling PlayUnfurl on Rope component next frame.");
                    StartCoroutine(DelayedPlay(ropeComp));
                }
                else
                {
                    RopeLog("EnsureRopeCreated: calling ShowFinalState on Rope component.");
                    ropeComp.ShowFinalState();
                }
            }
            else
            {
                // The project should use Rope.cs on the rope prefab. Log a clear warning so user can fix the prefab.
                RopeLog("Warning: spawned rope prefab does not contain a Rope component. Add Rope.cs to the prefab to control animation properly.");
            }
        }

        private IEnumerator DelayedPlay(Rope ropeComp)
        {
            // wait one frame so Animator component is fully initialized in the scene
            yield return null;
            if (ropeComp != null)
            {
                ropeComp.PlayUnfurl();
            }
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = _ropeAttached,
                ropeVariantName = _currentVariantName,
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null || state.uniqueId != GetUniqueId()) return;
            StopAllCoroutines();

            _ropeAttached = state.isActive;
            RopeLog("Loaded ropeAttached (from isActive): " + _ropeAttached);

            if (_ropeAttached)
            {
                // choose prefab based on saved variant name if we have one
                GameObject variantPrefab = null;
                if (!string.IsNullOrEmpty(state.ropeVariantName) && ropeVariants != null)
                {
                    foreach (var v in ropeVariants)
                    {
                        if (v != null && v.itemAsset != null && v.itemAsset.name == state.ropeVariantName)
                        {
                            variantPrefab = v.prefab;
                            break;
                        }
                    }
                }
                EnsureRopeCreated(false, variantPrefab);
                // hide original hook sprite if present since rope visuals are active
                if (_hookSpriteObject != null)
                    _hookSpriteObject.SetActive(false);
            }
            else
            {
                if (_ropeInstance != null)
                {
                    Destroy(_ropeInstance);
                    _ropeInstance = null;
                }
                // ensure hook sprite visible when no rope attached
                if (_hookSpriteObject != null)
                    _hookSpriteObject.SetActive(true);
            }
        }

        // Find the first inventory slot that contains any of the rope variant item names.
        // Returns tuple: (index in inventory, variant prefab or null)
        private (int index, GameObject prefab) FindRopeVariantInInventory()
        {
            if (_inventoryData == null) return (-1, null);

            // First search by configured variants (explicit mapping)
            if (ropeVariants != null)
            {
                foreach (var variant in ropeVariants)
                {
                    if (variant == null || variant.itemAsset == null) continue;
                    int idx = _inventoryData.FindItemIndexWithName(variant.itemAsset.name);
                    if (idx != -1)
                    {
                        return (idx, variant.prefab);
                    }
                }
            }

            // Fallback: try finding generic "Rope" item
            int fallback = _inventoryData.FindItemIndexWithName("Rope");
            if (fallback != -1) return (fallback, ropePrefab);
            return (-1, null);
        }
    }
}
