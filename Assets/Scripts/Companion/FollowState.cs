// =============================================================================
// FollowState.cs — Basic NavMeshAgent Follow Behaviour
// 
//
// PURPOSE:
//   A minimal follow state that drives a NavMeshAgent toward a target every
//   frame. Intended as a simple state component for companions that use a
//   NavMeshAgent directly (3D / NavMesh-backed), as opposed to the manual
//   physics-based pathfinding in CompanionFollow2D.
//
// NOTE:
//   updateRotation and updateUpAxis are disabled so Unity's NavMesh agent
//   does not rotate the 2D sprite — sprite flipping is handled elsewhere.
//   If target is null this will throw; assign it before enabling this component.
// =============================================================================

using UnityEngine;
using UnityEngine.AI;

namespace Companion
{
    public class FollowState : MonoBehaviour
    {
        // The transform this agent will chase every frame. Assign via Inspector or code.
        private Transform target;

        private NavMeshAgent agent;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            // Disable automatic rotation — 2D sprites handle facing direction manually.
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        // Every frame, redirect the agent toward the current target position.
        void Update()
        {
            agent.SetDestination(target.position);
        }
    }
}
