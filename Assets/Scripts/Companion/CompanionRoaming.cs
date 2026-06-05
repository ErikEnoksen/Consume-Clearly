// =============================================================================
// CompanionRoaming.cs — NPC Idle Patrol Behaviour
//  Currently only used på NPCs in the Downtown Area
//
// PURPOSE:
//   Gives a stationary companion life by making them walk back and forth
//   between two X boundary points, pausing for a random duration before
//   walking again. Fully pauses during dialogue so the companion stands
//   still while the player is talking to them.
//
// PATROL LOOP:
//   The companion walks in facingDirection at speed until either:
//     (a) they reach leftPatrolX or rightPatrolX → Flip() coroutine turns them around
//     (b) the walk timer expires → StateChange() toggles walk/pause and picks a new timer
//   Walk duration and pause duration are randomised within min/max ranges each cycle.
//
// DIALOGUE INTEGRATION:
//   Subscribes to global Dialogue events. When dialogue starts on THIS companion
//   (checked via IsChildOf), roaming stops. When dialogue ends, it resumes with
//   a fresh timer so it doesn't immediately flip or pause.
//
// ANIMATION:
//   Drives an "isWalking" bool on the Animator if the parameter exists.
//   hasWalkingParam is checked at Start so missing the parameter never throws.
// =============================================================================

using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

public class CompanionRoaming : MonoBehaviour
{
    // --- Inspector Fields ---
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;

    [SerializeField] private float speed;
    [SerializeField] private float leftPatrolX, rightPatrolX; // X world boundaries for the patrol range

    [SerializeField] private float minPauseTime, maxPauseTime; // random pause duration each idle cycle
    [SerializeField] private float minWalkTime, maxWalkTime;   // random walk duration each walk cycle

    [SerializeField] private int facingDirection = 1; // 1 = right, -1 = left

    // --- Runtime State ---
    private bool isFlipping;   // prevents multiple Flip() coroutines stacking
    private float randomTime, timer;
    private bool isWalking = true;
    private bool inDialogue = false;
    private bool hasWalkingParam; // cached check so we never call SetBool on a missing parameter

    // --- Event Wiring ---
    // Subscribe on enable so dialogue events are caught even if the object is toggled.
    private void OnEnable()
    {
        Dialogue.OnDialogueStarted += HandleDialogueStarted;
        Dialogue.OnDialogueEndedCompanion += HandleDialogueEnded;
    }

    private void OnDisable()
    {
        Dialogue.OnDialogueStarted -= HandleDialogueStarted;
        Dialogue.OnDialogueEndedCompanion -= HandleDialogueEnded;
    }

    // --- Initialization ---
    // NeverSleep keeps the rigidbody active even when off-screen.
    // hasWalkingParam is cached here so Update never has to iterate animator.parameters.
    private void Start()
    {
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        randomTime = Random.Range(minWalkTime, maxWalkTime);
        foreach (var p in animator.parameters)
            if (p.name == "isWalking") { hasWalkingParam = true; break; }
        if (hasWalkingParam) animator.SetBool("isWalking", isWalking);
    }

    // --- Patrol Update ---
    // Ticks the state timer, flips at boundaries, and drives velocity each frame.
    void Update()
    {
        if (inDialogue) return;

        timer += Time.deltaTime;

        // Switch between walking and pausing when the current duration expires.
        if (timer >= randomTime)
            StateChange();

        // Turn around when reaching the patrol boundary.
        if (!isFlipping && (transform.position.x > rightPatrolX || transform.position.x < leftPatrolX))
            StartCoroutine(Flip());

        rb.linearVelocity = isWalking ? Vector2.right * facingDirection * speed : Vector2.zero;
    }

    // --- Dialogue Handlers ---
    // Checked against IsChildOf so each companion only reacts to dialogue on itself,
    // not to other companions talking nearby.
    private void HandleDialogueStarted(CompanionFriendship companion)
    {
        if (companion != null && companion.transform.IsChildOf(transform))
        {
            inDialogue = true;
            isWalking = false;
            if (hasWalkingParam) animator.SetBool("isWalking", false);
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleDialogueEnded(CompanionFriendship companion)
    {
        if (companion != null && companion.transform.IsChildOf(transform))
        {
            inDialogue = false;
            isWalking = true;
            if (hasWalkingParam) animator.SetBool("isWalking", true);
            // Reset timer so the companion gets a fresh walk cycle after dialogue ends.
            timer = 0f;
            randomTime = Random.Range(minWalkTime, maxWalkTime);
        }
    }

    // --- Flip Coroutine ---
    // Rotates 180° on Y to reverse facing direction, then waits briefly before
    // allowing another flip so the companion doesn't jitter at the boundary.
    IEnumerator Flip()
    {
        isFlipping = true;
        transform.Rotate(0f, 180f, 0f);
        facingDirection *= -1;
        yield return new WaitForSeconds(0.7f);
        isFlipping = false;
    }

    // --- State Change ---
    // Toggles walk/pause, picks a new random duration for the next cycle, and resets the timer.
    void StateChange()
    {
        isWalking = !isWalking;
        if (hasWalkingParam) animator.SetBool("isWalking", isWalking);
        randomTime = isWalking ? Random.Range(minWalkTime, maxWalkTime) : Random.Range(minPauseTime, maxPauseTime);
        timer = 0f;
    }
}
