using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    public GameManager gameManager;

    public Transform camera;
    public Rigidbody rb;

    public Collider boxCollider;
    public Collider meshCollider;
    public PhysicMaterial slippy;

    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public Vector3 groundBox;

    public bool dead;
    public bool grounded;
    public bool bounce;
    public bool plank;
    public bool isJumping;
    bool isMoving = false;
    public bool isHiding;
    bool flip;

    public LayerMask groundMask;
    public LayerMask deathMask;
    public LayerMask bounceMask;
    public LayerMask plankMask;

    public float bounceUpward = 500;
    public float bounceForward = 100;
    public float walkSpeed = 9f;
    public float runSpeed = 14f;
    public float maxSpeed = 14f;
    public float jumpPower = 30f;
    public float rotationSpeed;
    public float extraGravity = 45;
    float speed;

    float bodyRotationX;
    float camRotationY;
    Vector3 dirIntentX;
    Vector3 dirIntentY;
    

    protected float timer;
    public float DelayAmount = 1;

    private RigidbodyConstraints rbConstraints;
    private Quaternion newRotation;
    private Vector3 resetDirection;

    private AnimationHandler anim;

    private Ray playerray;

    private void Start()
    {
        rbConstraints = rb.constraints;
        resetDirection = new Vector3(0, 0, 0);
        newRotation = Quaternion.LookRotation(resetDirection);
        anim = GetComponentInChildren<AnimationHandler>();

        
    }
    private void FixedUpdate()
    {
        

        ExtraGravity();
        if(!isHiding)
        {
            Movement();

        }
        else
        {
            rb.velocity = Vector3.zero;
        }
        //Bounce
     
        
        if(isJumping == true)
        {
            Jump();
            isJumping = false;
            runSpeed = 4;
            walkSpeed = 2;
        }else if (!isJumping)
        {
            anim.StopJump();
        }

        if (bounce)
        {
            rb.AddForce(0, bounceUpward, bounceForward);
            bounce = false;
        }
    }
    void Update()
    {
        Quaternion newRot = transform.rotation;

        GroundCheck();
        //Movement
        if (!isHiding && grounded)
        {
            bounceUpward = 6;
            rb.constraints = rbConstraints;
            StopFlipping();
        }
        else if(isHiding)
        {
            HideInShell();
            rb.constraints = RigidbodyConstraints.None;
        }

        if (plank)
        {
            anim.StopFalling();
        }

        //Reset Level
        if (dead)
        {
            gameManager.RestartScene();
        }
        //Start charging jump
        if (grounded && Input.GetKey(KeyCode.Space))
        {
            anim.StopRunning();
            anim.JumpCharge();
            timer += Time.deltaTime;
            runSpeed = 1;
            walkSpeed = 1;
            maxSpeed = 20;
        }
        //Jump
        if (grounded && Input.GetKeyUp(KeyCode.Space))
        {
            isJumping = true;
            anim.ReleaseCharge();
            maxSpeed = 20;
        }
        //Timer
        if(timer >= DelayAmount)
        {
            timer = 0;
            jumpPower++;
        }
        //Reset jump power
        if (!grounded)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            anim.StopRunning();
            if (!isHiding)
            {
                anim.Falling();
            }

            maxSpeed = 20;
            jumpPower = 5;
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                isHiding = true;
            }
        }
        if (grounded)
        {
            rb.interpolation = RigidbodyInterpolation.None;

            if (!isHiding)
            {
                anim.StopFalling();
            }
            rb.constraints = rbConstraints;
            if (!Input.GetKey(KeyCode.Space)) { maxSpeed = 4; }
      
            
        }
        //Max out the jump power
        if (jumpPower > 9)
        {
            jumpPower = 9;
        }

     
        if (Input.GetKey(KeyCode.Q) && grounded)
        {
            HideInShell();
            rb.constraints = RigidbodyConstraints.None;
            transform.forward = transform.forward;
            maxSpeed = 0;
        }
        if (Input.GetKeyUp(KeyCode.Q) && grounded)
        {
            GetOutOfShell();
            rb.constraints = rbConstraints;
            maxSpeed = 4;
        }

        if (bounce) 
        {
            isHiding = true;
        }

     

        
    }



    void Movement()
    {
        if (!isHiding)
        {
            dirIntentX = camera.right;
            dirIntentX.y = 0;
            dirIntentX.Normalize();

            dirIntentY = camera.forward;
            dirIntentY.y = 0;
            dirIntentY.Normalize();
            //
            rb.velocity = dirIntentY * Input.GetAxis("Vertical") * speed +
                        dirIntentX * Input.GetAxis("Horizontal") * speed +
                                              Vector3.up * rb.velocity.y;
            rb.velocity = Vector3.ClampMagnitude(rb.velocity, maxSpeed);
        }
        //
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = runSpeed;
        }
        if (!Input.GetKey(KeyCode.LeftShift))
        {
            speed = walkSpeed;
        }

        //Animations
        if (grounded)
        {
            if (Input.GetKey(KeyCode.W) && Input.GetKey(KeyCode.LeftShift) ||
               Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.LeftShift) ||
               Input.GetKey(KeyCode.S) && Input.GetKey(KeyCode.LeftShift) ||
               Input.GetKey(KeyCode.D) && Input.GetKey(KeyCode.LeftShift))
            {
                anim.Run();
            }
            else
            {
                anim.StopRunning();
            }

            if (Input.GetKey(KeyCode.W) ||
              Input.GetKey(KeyCode.A) ||
              Input.GetKey(KeyCode.S) ||
              Input.GetKey(KeyCode.D))
            {
                anim.Walk();
            }
            else
            {
                anim.StopWalking(); 
            }
        }
    }

    //Rotate the body to the direction of velocity
    void RotateBody()
    {
        float horDir = Input.GetAxisRaw("Horizontal");
        float vertDir = Input.GetAxisRaw("Vertical");



        Vector3 moveDirection = new Vector3(horDir, 0, vertDir);
        if (moveDirection != Vector3.zero)
        {
            if (!Input.GetKey(KeyCode.Q))
            {
                Quaternion newRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, newRotation, Time.deltaTime * rotationSpeed);
            }
        }

    }

    void ExtraGravity()
    {
        rb.AddForce(Vector3.down * extraGravity);
    }

    void GroundCheck()
    {
        grounded = Physics.CheckBox(groundCheck.position, groundBox, transform.rotation, groundMask);
        dead = Physics.CheckBox(groundCheck.position, groundBox, transform.rotation, deathMask);
        plank = Physics.CheckBox(groundCheck.position, groundBox, transform.rotation, plankMask);
    }

    void Jump()
    {
        rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        anim.Jump();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawCube(groundCheck.position, groundBox);
    }

    

    void HideInShell()
    {
        isHiding = true;
        boxCollider.enabled = false;
        meshCollider.enabled = true;
        anim.HideInShellAnimation();
    }
    void GetOutOfShell()
    {
        isHiding = false;
        boxCollider.enabled = true;
        meshCollider.enabled = false;
        anim.StopHiding();
    }

    void StopFlipping()
    {
        RotateBody();
        rb.constraints = rbConstraints;
    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            rb.constraints = rbConstraints;
               if (!grounded)
            {
                collision.collider.sharedMaterial = slippy;

            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            if (isHiding)
            {
                GetOutOfShell();
            }
            isHiding = false;
            bounce = false;
        }
        if (collision.gameObject.CompareTag("bounce"))
        {
            bounce = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddForce(0, bounceUpward, bounceForward, ForceMode.Impulse);
        }
    }


    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("ground"))
        {
            grounded = false;
        }

        if (collision.gameObject.CompareTag("bounce"))
        {
            bounce = false;
        }

    }



}
