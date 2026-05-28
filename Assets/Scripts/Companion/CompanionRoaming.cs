using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;

public class CompanionRoaming : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Animator animator;
    
    [SerializeField] private float speed;
    [SerializeField] private float leftPatrolX, rightPatrolX;
    
    [SerializeField] private float minPauseTime, maxPauseTime;
    [SerializeField] private float minWalkTime, maxWalkTime;
     
    [SerializeField] private int facingDirection = 1;

    private bool isFlipping;
    private float randomTime, timer;
    private bool isWalking = true;
    private bool inDialogue = false;
    private bool hasWalkingParam;
    
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

    private void Start()
    {
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        randomTime = Random.Range(minWalkTime, maxWalkTime);
        foreach (var p in animator.parameters)
            if (p.name == "isWalking") { hasWalkingParam = true; break; }
        if (hasWalkingParam) animator.SetBool("isWalking", isWalking);
    }

    // Update is called once per frame
    void Update()
    {
        if (inDialogue) return;
        
        timer += Time.deltaTime;

        if (timer >= randomTime)
            StateChange();
        
        if (!isFlipping && (transform.position.x > rightPatrolX || transform.position.x < leftPatrolX))
            StartCoroutine(Flip());
        rb.linearVelocity = isWalking ? Vector2.right * facingDirection * speed : Vector2.zero;
    }
    
    private void HandleDialogueStarted(CompanionFriendship companion)
    {
        if (companion != null && companion.transform.IsChildOf(transform))
        //if (companion != null && companion.gameObject == gameObject)
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
        //if (companion != null && companion.gameObject == gameObject)
        {
            inDialogue = false;
            isWalking = true;
            if (hasWalkingParam) animator.SetBool("isWalking", true);
            timer = 0f;
            randomTime = Random.Range(minWalkTime, maxWalkTime);
        }
    }
    
    IEnumerator Flip()
    {
        isFlipping = true;
        transform.Rotate(0f, 180f, 0f);
        facingDirection *= -1;
        yield return new WaitForSeconds(0.7f);
        isFlipping = false;
    }
    void StateChange()
    {
        isWalking = !isWalking;
        if (hasWalkingParam) animator.SetBool("isWalking", isWalking);
        randomTime = isWalking ? Random.Range(minWalkTime, maxWalkTime) : Random.Range(minPauseTime, maxPauseTime);
        timer = 0f;
    }
}
