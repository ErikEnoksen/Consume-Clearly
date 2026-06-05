// =============================================================================
// SceneManager.cs — Scene Transition Trigger
// 
//
// PURPOSE:
//   An Interactable placed at a scene exit point (door, path boundary, etc.).
//   When the player enters its collider and triggers an interaction, it plays
//   a walk-away animation, saves the current scene's state to disk, then
//   hands off to GameManager to load the next scene.
//
// FLOW:
//   Player enters collider → Interact()
//     → player turns back (walk-away animation)
//     → WaitAnimFinish() coroutine delays for transitionDelay seconds
//     → SaveProgress(currentScene) — writes scene-specific save file
//     → TransitionToScene(sceneToLoad) — GameManager handles the async load
//
// TRANSITION GUARD:
//   _isTransitioning and GameManager.IsTransitioning both guard against
//   double-firing if the player re-triggers the collider mid-transition.
//
// SAVE / LOAD:
//   Saves only its position and ID. There is no meaningful per-trigger state
//   to restore — the trigger just needs to exist as a registered saveable.
// =============================================================================

using UnityEngine;
using Save;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;
using Player;

namespace LevelObjects.Interactable
{
    [RequireComponent(typeof(Collider2D))]
    public class InteractableScene : Interactable
    {
        // --- Inspector Fields ---
        [Header("Scene to load")]
        [Tooltip("Choose which scene needs to be loaded")]
        [SerializeField] private string sceneToLoad;

        // How long to wait after the turn-back animation starts before transitioning.
        [SerializeField] private float transitionDelay = 1.0f;

        // --- Scene References ---
        private AnimationController animationController;
        private GameManager gameManager;

        // Prevents re-triggering while a transition is already in progress.
        private bool _isTransitioning;

        // --- Initialization ---
        // Cache the GameManager and set the player's animation to facing-forward at start.
        private void Start()
        {
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            animationController = GameObject.FindGameObjectWithTag("Player").GetComponent<AnimationController>();
            animationController.SetTurnBack(false);
        }

        // --- Interact ---
        // Fires when the player triggers this exit point.
        // Guards against double-triggers then kicks off the animation + transition coroutine.
        public override void Interact()
        {
            if (_isTransitioning) return;
            if (gameManager != null && gameManager.IsTransitioning) return;
            _isTransitioning = true;
            animationController.SetTurnBack(true);
            StartCoroutine(WaitAnimFinish());
        }

        // --- Transition Coroutine ---
        // Waits for the turn-back animation to play, saves the current scene to its
        // own slot, then asks GameManager to load the destination scene.
        private IEnumerator WaitAnimFinish()
        {
            yield return new WaitForSeconds(transitionDelay);
            gameManager.SaveProgress(SceneManager.GetActiveScene().name);
            yield return null;
            gameManager.TransitionToScene(sceneToLoad);
        }

        // --- Save / Load ---
        // Only position and ID are stored — enough for the save system to register this object.
        public override InteractableObjectState SaveState()
        {
            return new InteractableObjectState
            {
                uniqueId = GetUniqueId(),
                position = transform.position,
            };
        }

        public override void LoadState(InteractableObjectState state)
        {
            if (state == null || state.uniqueId != GetUniqueId()) return;
            Debug.Log("load interactive Scene");
        }
    }
}
