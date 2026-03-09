using UnityEngine;

namespace Player
{
    public class AnimationController : MonoBehaviour
    {
        private Animator animator;
        private SpriteRenderer spriteRenderer;

        // Animation parameter names
        private static readonly string IS_WALKING = "IsWalking";
        private static readonly string TRIGGER_JUMP = "TriggerJump";
        private static readonly string IS_IDLE = "IsIdle";
        private static readonly string CLIMB_ROPE = "ClimbRope";
        private static readonly string CLIMB_LADDER = "ClimbLadder";
        private static readonly string CLIMB_ACTIVE = "ClimbActive";
        private static readonly string TURN_BACK="TurnBack";
        

        // Animation state names
        private static readonly string JUMP_STATE_NAME = "Jump";

        void Start()
        {
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        
            if (ValidateComponents()) return;

            // Initialize parameters
            SetIdle(true);
            SetWalking(false);
        }

        private bool ValidateComponents()
        {
            if (animator == null)
            {
                Debug.LogError("Animator component missing from player!");
                enabled = false;
                return true;
            }

            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteRenderer component missing from player!");
                enabled = false;
                return true;
            }

            return false;
        }

        public void SetWalking(bool isWalking)
        {
            animator.SetBool(IS_WALKING, isWalking);
            if (isWalking)
            {
                SetIdle(false);
            }
        }

        public void TriggerJump()
        {
            animator.SetTrigger(TRIGGER_JUMP);
            SetIdle(false);
        }
        
        public void SetClimbRope(bool isClimbing)
        {
            if (animator == null) return;
            animator.SetBool(CLIMB_ROPE, isClimbing);
            if (isClimbing) SetIdle(false);
        }

        public void SetClimbLadder(bool isClimbing)
        {
            if (animator == null) return;
            animator.SetBool(CLIMB_LADDER, isClimbing);
            if (isClimbing) SetIdle(false);
        }
        
        public bool IsInJumpState()
        {
            if (animator == null) return false;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName(JUMP_STATE_NAME);
        }
        
        public float GetCurrentStateNormalizedTime()
        {
            if (animator == null) return 0f;
            return animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        public void SetIdle(bool isIdle)
        {
            animator.SetBool(IS_IDLE, isIdle);
        }

        public void FlipSprite(float horizontalInput)
        {
            if (horizontalInput != 0)
            {
                spriteRenderer.flipX = horizontalInput < 0;
            }
        }

        // Added: expose CLIMB_ACTIVE animator parameter so other systems can toggle climb states.
        public void SetClimbActive(bool isActive)
        {
            if (animator == null) return;
            animator.SetBool(CLIMB_ACTIVE, isActive);
            if (isActive) SetIdle(false);
        }

        public void SetTurnBack(bool turnBack)
        {
            if (animator == null) return;
            animator.SetBool(TURN_BACK, turnBack);
            if (turnBack)
            {
                SetIdle(false);
                SetWalking(false);
            }
        }
    }
}