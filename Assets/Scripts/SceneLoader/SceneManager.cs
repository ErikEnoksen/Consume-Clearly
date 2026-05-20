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
        [Header("Scene to load")] [Tooltip("Choose which scene needs to be load")] [SerializeField]
        private string sceneToLoad;
        
        [SerializeField] private float transitionDelay = 1.0f;

        private AnimationController animationController;
        private GameManager gameManager;
        private bool _isTransitioning;

        private void Start()
        {
            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
            animationController = GameObject.FindGameObjectWithTag("Player").GetComponent<AnimationController>();
            animationController.SetTurnBack(false);
        }

        public override void Interact()
        {
            if (_isTransitioning) return;
            if (gameManager != null && gameManager.IsTransitioning) return;
            _isTransitioning = true;
            animationController.SetTurnBack(true);
            StartCoroutine(WaitAnimFinish());
        }

        private IEnumerator WaitAnimFinish()
        {
            yield return new WaitForSeconds(transitionDelay);
            gameManager.SaveProgress(SceneManager.GetActiveScene().name);
            yield return null;
            gameManager.TransitionToScene(sceneToLoad);
        }

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
