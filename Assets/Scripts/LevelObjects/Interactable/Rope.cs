using System;
using System.Collections.Generic;
using UnityEngine;

namespace LevelObjects.Interactable
{
    public class Rope : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private string unfurlTriggerName = "Unfurl";
        [SerializeField] private string ropeClipName = "RopeShort";

        private Animator _animator;
        private List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private bool _animatorHasTrigger;
        private int _unfurlTriggerHash;
        private bool _initialized;

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized) return;

            _animator = GetComponentInChildren<Animator>(true);

            if (_animator != null)
            {
                _unfurlTriggerHash = Animator.StringToHash(unfurlTriggerName);

                // collect sprite renderers under the animator so we can enable/disable them
                if (_renderers.Count == 0)
                {
                    var all = _animator.gameObject.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (var r in all)
                    {
                        _renderers.Add(r);
                    }
                }

                // detect trigger parameter once
                _animatorHasTrigger = false;
                foreach (var p in _animator.parameters)
                {
                    if (p.type == AnimatorControllerParameterType.Trigger && p.name == unfurlTriggerName)
                    {
                        _animatorHasTrigger = true;
                        break;
                    }
                }

                Debug.Log($"Rope EnsureInitialized: animator found. hasTrigger={_animatorHasTrigger}. controller={( _animator.runtimeAnimatorController != null ? _animator.runtimeAnimatorController.name : "(null)")}");

                // disable animator so it doesn't draw its first frame
                _animator.enabled = false;

                // ensure renderers disabled by default to avoid first-frame flash
                foreach (var r in _renderers)
                    r.enabled = false;
            }
            else
            {
                // No animator present: ensure any renderers on the prefab are visible by default
                var all = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var r in all)
                {
                    _renderers.Add(r);
                    r.enabled = true;
                }
                Debug.Log("Rope EnsureInitialized: no animator found on Rope prefab.");
            }

            _initialized = true;
        }
        
        public void PlayUnfurl()
        {
            EnsureInitialized();

            if (_animator != null)
            {
                foreach (var r in _renderers)
                    r.enabled = true;

                _animator.enabled = true;
                // Rebind animator and update immediately so parameters and states are ready to accept triggers/plays
                _animator.Rebind();
                _animator.Update(0f);
                // Ensure layer weights are active (some controllers may have default weight 0)
                for (int i = 0; i < _animator.layerCount; i++)
                {
                    _animator.SetLayerWeight(i, 1f);
                }
                Debug.Log($"Rope.PlayUnfurl: animator enabled and layer weights set (layers={_animator.layerCount}).");

                if (_animatorHasTrigger)
                {
                    Debug.Log($"Rope.PlayUnfurl: setting trigger '{unfurlTriggerName}' (hash={_unfurlTriggerHash})");
                    _animator.SetTrigger(_unfurlTriggerHash);
                    // Also force-play the state to ensure animation starts even if trigger processing is delayed
                    Debug.Log($"Rope.PlayUnfurl: also forcing Play('{ropeClipName}') to ensure playback");
                    _animator.Play(ropeClipName, -1, 0f);
                }
                else
                {
                    Debug.Log($"Rope.PlayUnfurl: trigger '{unfurlTriggerName}' not found, attempting Play('{ropeClipName}')");
                    // Try to play named state directly; if not present, try first clip on controller
                    _animator.Play(ropeClipName, -1, 0f);
                }

                // Do not disable animator/renderers after playing — rope should remain visible in final state.
            }
            else
            {
                foreach (var r in _renderers)
                    r.enabled = true;
                Debug.Log("Rope.PlayUnfurl: no animator, just enabling renderers.");
            }
        }
        
        public void ShowFinalState()
        {
            EnsureInitialized();

            if (_animator != null)
            {
                _animator.enabled = false;
                foreach (var r in _renderers)
                    r.enabled = true;
            }
            else
            {
                foreach (var r in _renderers)
                    r.enabled = true;
            }
        }
        
        public void Hide()
        {
            EnsureInitialized();
            if (_animator != null)
            {
                _animator.enabled = false;
            }
            foreach (var r in _renderers)
                r.enabled = false;
        }
    }
}
