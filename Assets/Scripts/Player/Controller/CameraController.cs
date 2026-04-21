using Unity.Cinemachine;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Player
{
    public class CameraController : MonoBehaviour
    {
        private CinemachineCamera cinemachineCamera;

        private Transform player;


        private void Start()
        {
            player = GameObject.FindGameObjectWithTag("Player").transform;
            if(player == null)
            {
                Debug.Log("not found");
            }

            Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out var brain);
            if(brain == null) 
            { 
                brain = Camera.main.gameObject.GetComponent<CinemachineBrain>(); 
            }

            cinemachineCamera = gameObject.GetComponent<CinemachineCamera>();
            cinemachineCamera.Follow = player;
        }
    }
}