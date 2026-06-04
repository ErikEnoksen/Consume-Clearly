// =============================================================================
// CameraController.cs - Responsible for configuring the camera
//
// PURPOSE: 
//   This script sets the tracking target for the camera to the player.
// =============================================================================
using Unity.Cinemachine;
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