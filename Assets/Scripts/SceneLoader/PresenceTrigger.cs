// =============================================================================
// PresenceTrigger.cs — General Purpose Trigger Zone
// 
//
// PURPOSE:
//   A reusable 2D trigger zone that fires UnityEvents when a matching object
//   enters, stays in, or exits the collider. Can also trigger a direct scene
//   load if sceneToLoad is set, bypassing the UnityEvent entirely.
//   Useful for cutscene zones, area events, NPC detection, and scene exits.
//
// MODES:
//   • Standard (default)    — activates 0.5s after Start to avoid false triggers
//                             on scene load (e.g. player spawning inside the zone)
//   • activateAfterFirstExit — stays dormant until the player leaves the zone
//                             first. Use when the player starts inside the trigger.
//   • triggerContinuously   — fires onTriggerStay every FixedUpdate while active.
//                             Off by default; enable for sustained effects.
//
// SCENE LOAD MODE:
//   If sceneToLoad is set, OnTriggerEnter2D saves the current scene and
//   transitions — no UnityEvent is fired. Leave sceneToLoad empty to use events.
//
// LAYER FILTER:
//   Only objects on the configured triggerLayer can activate this trigger.
//   Set to the Player layer to make it player-only.
// =============================================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

namespace LevelObjects.Interactable
{
    [RequireComponent(typeof(Collider2D))]
    public class PresenceTrigger : MonoBehaviour
    {
        // --- Inspector Fields ---
        // Only objects on this layer can fire this trigger.
        [Header("Trigger Layer")]
        [Tooltip("What layer can activate this trigger")]
        [SerializeField] private LayerMask triggerLayer;

        // When true, onTriggerStay fires every frame while an object is inside.
        [Tooltip("Check if the trigger should happen continuously when object is inside")]
        [SerializeField] private readonly bool triggerContinuously = false;

        [Header("Activation Settings")]
        // Use this when the player starts inside the zone at scene load — prevents
        // the enter event from firing immediately on scene start.
        [Tooltip("If true, trigger will be inactive until the player exits the zone first")]
        [SerializeField] private bool activateAfterFirstExit = false;

        // Whether this trigger is ready to fire.
        private bool isActivated = false;

        // If set, entering the trigger saves and loads this scene instead of firing onTriggerEnter.
        [Header("Scene Settings")]
        [SerializeField] private string sceneToLoad = "";

        // --- Unity Events ---
        // Wire these up in the Inspector to respond to enter/exit/stay without any code changes.
        [Header("Trigger Events")]
        [SerializeField] private UnityEvent onTriggerEnter;
        [SerializeField] private UnityEvent onTriggerExit;
        [SerializeField] private UnityEvent onTriggerStay; // only fires when triggerContinuously = true

        // --- Initialization ---
        // Delay activation slightly so the player spawning inside the zone doesn't immediately fire the event.
        private void Start()
        {
            if (!activateAfterFirstExit)
            {
                StartCoroutine(ActivateAfterDelay());
            }
            // If activateAfterFirstExit is set, isActivated stays false until OnTriggerExit2D arms it.
        }

        private IEnumerator ActivateAfterDelay()
        {
            yield return new WaitForSeconds(0.5f);
            isActivated = true;
        }

        // Ensures the Collider2D is set to trigger mode when the component is first added.
        private void Reset()
        {
            Collider2D col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        // --- Trigger Enter ---
        // If sceneToLoad is set, saves and transitions immediately.
        // Otherwise fires the onTriggerEnter UnityEvent for custom behaviour.
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsInLayerMask(other.gameObject.layer) && isActivated)
            {
                if (!string.IsNullOrEmpty(sceneToLoad))
                {
                    GameManager.Instance?.SaveProgress(SceneManager.GetActiveScene().name);
                    GameManager.Instance?.TransitionToScene(sceneToLoad);
                }
                else
                {
                    onTriggerEnter?.Invoke();
                }
            }
        }

        // --- Trigger Exit ---
        // Arms the trigger if activateAfterFirstExit is set, then fires the exit event.
        private void OnTriggerExit2D(Collider2D other)
        {
            if (!IsInLayerMask(other.gameObject.layer)) return;

            // First exit arms the trigger — from here it will respond normally.
            if (activateAfterFirstExit && !isActivated)
                isActivated = true;

            if (isActivated)
                onTriggerExit?.Invoke();
        }

        // --- Trigger Stay ---
        // Only fires when triggerContinuously is true — useful for damage zones, healing areas, etc.
        private void OnTriggerStay2D(Collider2D other)
        {
            if (triggerContinuously && IsInLayerMask(other.gameObject.layer) && isActivated)
                onTriggerStay?.Invoke();
        }

        // --- Layer Check ---
        // Bitwise check against the LayerMask so only the configured layers can fire this trigger.
        private bool IsInLayerMask(int layer)
        {
            return (triggerLayer.value & (1 << layer)) != 0;
        }
    }
}