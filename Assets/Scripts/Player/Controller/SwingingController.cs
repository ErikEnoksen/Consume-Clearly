using System;
using LevelObjects.Interactable;
using UnityEngine;

public class SwingingController : MonoBehaviour
{

    [Header("Swing Settings")]
    [SerializeField] private float swingForce = 0.2f;
    [SerializeField] private float jumpReleaseForce  = 8f;
    
    private bool isSwinging = false;
    private Rigidbody2D rb;
    private DistanceJoint2D _distanceJoint2D;
    private Hook currentHook;
    
    public bool IsSwinging => isSwinging;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _distanceJoint2D = GetComponent<DistanceJoint2D>();
        _distanceJoint2D.enabled = false;
    }

    private void Update()
    {
        if (!isSwinging) return;
        
        float input = Input.GetAxisRaw("Horizontal");
        if (Mathf.Abs(input) > 0.1f)
        {
            rb.AddForce(new Vector2(input * swingForce, 0f));
        }
        
        if (Input.GetButtonDown("Jump"))
        {
            ReleaseSwing();
        }
    }

    public void AttachToHook(Hook hook)
    {
        currentHook = hook;
        isSwinging = true;

        _distanceJoint2D.connectedAnchor = hook.transform.position;
        _distanceJoint2D.distance = Vector2.Distance(transform.position, hook.transform.position);
        _distanceJoint2D.enabled = true;
    }

    private void ReleaseSwing()
    {
        isSwinging = false;
        _distanceJoint2D.enabled = false;
        
        rb.AddForce(rb.linearVelocity.normalized * jumpReleaseForce, ForceMode2D.Impulse);

        currentHook = null;
    }
}


