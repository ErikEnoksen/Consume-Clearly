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
        
        private AnimationController animationController;
        private IEnumerator coroutine;
        private GameManager gameManager;

        private void Start()
        {
            gameManager=GameObject.Find("GameManager").GetComponent<GameManager>();
            animationController=GameObject.FindGameObjectWithTag("Player").GetComponent<AnimationController>();
            animationController.SetTurnBack(false);
        }

        public override void Interact()
        {
            animationController.SetTurnBack(true);
            coroutine=WaitAnimFinish(1.0f);
            gameManager.SaveProgress(SceneManager.GetActiveScene().name);
            StartCoroutine(coroutine);
        }

        private IEnumerator WaitAnimFinish(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            gameManager.LoadScene(sceneToLoad);
            gameManager.LoadProgress(sceneToLoad);

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
