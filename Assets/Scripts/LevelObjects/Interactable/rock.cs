using System.Collections;
using Save;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelObjects.Interactable
{
    public class Rock : Interactable
    {
        private Collider2D _collider;
        private SpriteRenderer _sprite;
        
        private Animator _poofAnimator;
        private List<SpriteRenderer> _poofRenderers = new List<SpriteRenderer>();
        private const string PoofClipName = "DustPoof";
        private const string PoofTriggerName = "Poof";

        private bool _used;
        private bool _poofPlayed;
        // Cached lookup: whether the poof animator has the trigger parameter
        private bool _poofAnimatorHasTrigger;
        private int _poofTriggerHash;

        // Ensure refs are ready early (LoadState may run before Start)
        protected override void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_collider == null) _collider = GetComponent<Collider2D>();
            if (_sprite == null) _sprite = GetComponent<SpriteRenderer>();
            if (_poofAnimator == null) _poofAnimator = GetComponentInChildren<Animator>(true);

            if (_poofAnimator != null)
            {
                // precompute hash for the Poof trigger (faster than repeated string lookups)
                _poofTriggerHash = Animator.StringToHash(PoofTriggerName);
                // Only populate renderers once
                if (_poofRenderers.Count == 0)
                {
                    var allPoofRenderers = _poofAnimator.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var r in allPoofRenderers)
                    {
                        if (r == _sprite) continue;
                        _poofRenderers.Add(r);
                    }
                }

                // Cache whether the animator has the 'Poof' trigger so we don't need to scan parameters on every interact
                _poofAnimatorHasTrigger = false;
                foreach (var p in _poofAnimator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Trigger && p.name == PoofTriggerName)
                    {
                        _poofAnimatorHasTrigger = true;
                        break;
                    }
                }

                // disable animator so it doesn't draw first frame
                _poofAnimator.enabled = false;

                // ensure poof renderers are disabled by default
                foreach (var r in _poofRenderers)
                    r.enabled = false;
            }

            // Determine used state from current visuals if we haven't already set it
            bool active = _sprite != null && _sprite.enabled && _collider != null && _collider.enabled;
            _used = !active;
        }

        void Start()
        {
            EnsureInitialized();
        }

        public override void Interact()
        {
            EnsureInitialized();

            if (_used) return;
            _used = true;

            if (_collider != null) _collider.enabled = false;

            if (_poofAnimator != null && !_poofPlayed)
            {
                _poofPlayed = true;
                
                foreach (var r in _poofRenderers)
                    r.enabled = true;
                
                _poofAnimator.enabled = true;
                
                if (_poofAnimatorHasTrigger)
                {
                    _poofAnimator.SetTrigger(_poofTriggerHash);
                }
                else
                {
                    _poofAnimator.Play(PoofClipName, -1, 0f);
                }

                StartCoroutine(DisableAfterPoofFromAnimator());
                return;
            }
            
            if (_sprite != null)
            {
                _sprite.enabled = false;
            }
        }

        private IEnumerator DisableAfterPoofFromAnimator()
        {
            float duration = 0f;
            var controller = _poofAnimator != null ? _poofAnimator.runtimeAnimatorController : null;
            if (controller != null && controller.animationClips != null && controller.animationClips.Length > 0)
            {
                foreach (var c in controller.animationClips)
                {
                    if (string.Equals(c.name, PoofClipName, StringComparison.OrdinalIgnoreCase))
                    {
                        duration = c.length;
                        break;
                    }
                }
                
                if (duration <= 0f)
                    duration = controller.animationClips[0].length;
            }

            if (duration > 0f)
                yield return new WaitForSeconds(duration);
            else
                yield return null;

            if (_sprite != null) _sprite.enabled = false;
            
            foreach (var r in _poofRenderers)
                r.enabled = false;

            if (_poofAnimator != null)
            {
                _poofAnimator.enabled = false;
            }
        }

        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                isActive = _sprite != null && _sprite.enabled && _collider != null && _collider.enabled,
                position = transform.position,
                rotation = transform.rotation
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null) return;

            // Only load state intended for this object
            if (string.IsNullOrEmpty(state.uniqueId) || state.uniqueId != GetUniqueId())
            {
                return;
            }

            EnsureInitialized();

            transform.position = state.position;
            transform.rotation = state.rotation;

            bool active = state.isActive;
            if (_sprite != null) _sprite.enabled = active;
            if (_collider != null) _collider.enabled = active;
            _used = !active;

            if (active)
            {
                _poofPlayed = false;
            }
            

            // Ensure poof visuals/animator are disabled on load to avoid accidental first-frame flashes
            if (_poofAnimator != null)
            {
                _poofAnimator.enabled = false;
            }
            foreach (var r in _poofRenderers)
                r.enabled = false;
        }
    }
}

