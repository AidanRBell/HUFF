using UnityEngine;
using UnityEngine.InputSystem;

public class HuffMovement : MonoBehaviour
{
    // Components
    Rigidbody2D body;
    BoxCollider2D boxCollider;
    Animator anim;

    // Controls
    PlayerControls controls; // our input actions script
    bool left, right, up, down, jumpHeld, toggleHeld; // bools for if their respective buttons are being held
    Vector2 analogInput = Vector2.zero; // inputs of analog stick / d-pad

    // Mechanical
    public bool canMove = true;
    public float groundLinDamp, airLinDamp;
    private float defaultGravityScale;
    private Vector2 currentVelocity, targetVelocity;

    const int DIR_LEFT = -1, DIR_RIGHT = 1;
    float spriteScaleVal;
    
    // Ground Detection
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundDetectOffsetY = 0f;
    private Vector2 groundDetectBoxSize = Vector2.zero;

    // Character State
    public enum CharacterState
    {
        Unknown,

        Idle,
        Running,
        Jumping,
        MidAir,
        HitStun,
        Dead,
        Dialogue,
        Cutscene,

        Puff,
        RoundAbout,
        Spiral
    }

    public CharacterState huffState;


    // Movement
    public float groundMaxVel, groundAccelRate, airMaxVel, airAccelRate;

    // Jumps
    [SerializeField] float initialJumpForce, heldJumpForce, maxJumpHeldTime;
    private float holdingJumpTimer = 0f;

    // Volt Jumps
    const int PUFF = 0,
        ROUND_ABOUT = 1,
        BOUNCE = 2,
        NIBBLE = 3,
        SPIRAL = 4,
        ZAP_LINE = 5,
        CATCH_N_CHUCK = 6,
        GLIDE = 7;

    int voltageCount = 0; // number of voltage bolts
    public int currentJumpIndex = PUFF; // index of the current voltage jump
    

    // Puff
    bool inPuff = false;
    private float savedMomentumY = 0;

    // RoundAbout
    bool inRoundAbout = false;
    [SerializeField] private float maxRoundAboutSpeed = 5f;
    [SerializeField] private float roundAboutSmoothTime = 0.1f;
    float roundAboutTime;
    [SerializeField] private Vector2 roundAboutForce;
    [SerializeField] Vector2 roundAboutHitboxSize = new Vector2(2,2);
    bool wallBouncing = false;


    // Spiral
    bool inSpiral = false;
    [SerializeField] private float spiralGravityScale;

    // Respawning
    [SerializeField] private RespawnManager respawnManager;

    // Debugging
    float triggerTestTime = 0f;


    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

        groundDetectBoxSize = new Vector2(boxCollider.size.x, boxCollider.size.y / 3);

        spriteScaleVal = transform.localScale.x;
        defaultGravityScale = body.gravityScale;

        roundAboutTime = roundAboutSmoothTime; // make amount of time the max smooth time
    }

    private void OnEnable()
    {
        /* Initializing the controls */
        controls = new PlayerControls();
        controls.Enable();

        controls.Huff.Up.performed += ctx => up = true;
        controls.Huff.Up.canceled += ctx => up = false;

        controls.Huff.Down.performed += ctx => down = true;
        controls.Huff.Down.canceled += ctx => down = false;

        controls.Huff.Left.performed += ctx => left = true;
        controls.Huff.Left.canceled += ctx => left = false;

        controls.Huff.Right.performed += ctx => right = true;
        controls.Huff.Right.canceled += ctx => right = false;

        controls.Huff.Jump.performed += Jump;
        controls.Huff.Jump.performed += ctx => jumpHeld = true;
        controls.Huff.Jump.canceled += ctx => jumpHeld = false;

        controls.Huff.Toggle.performed += ctx => toggleHeld = true;
        controls.Huff.Toggle.canceled += ctx => toggleHeld = false;

        controls.Huff.Pause.performed += PauseGame;
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private void Update()
    {
        // ROUND-ABOUT Air Drift
        if (inRoundAbout && roundAboutTime > 0)
        {
            float dir = 0f;
            if (wallBouncing)
            {
                dir = (transform.localScale.x > 0) ? DIR_RIGHT : DIR_LEFT;
            }
            else
            {
                dir = analogInput.x;
                if (dir == 0)
                    dir = (transform.localScale.x > 0) ? DIR_RIGHT : DIR_LEFT;
            }

            targetVelocity = new Vector2(roundAboutForce.x * dir, roundAboutForce.y);
        }
    }

    private void FixedUpdate()
    {
        setAnalogInput();

        if (canMove) // if the player can move
        {
            if (huffState == CharacterState.Jumping) // if Huff is Jumping
            {
                if (jumpHeld)
                {
                    holdingJumpTimer += Time.deltaTime;
                    GroundJumpAscend();

                    if (holdingJumpTimer >= maxJumpHeldTime)
                    {
                        huffState = CharacterState.MidAir;
                        holdingJumpTimer = 0f;
                    }
                }
                else
                {
                    huffState = CharacterState.MidAir;
                    holdingJumpTimer = 0f;
                }
            }

            // RUNNING AND AIR DRIFT
            float dir = analogInput.x;
            if (dir != 0)
            {
                if (onGround())
                    Run(dir);
                else
                {
                    if (inRoundAbout)
                        roundAboutAirDrift();
                    else
                        AirDrift(dir);
                }
            }

            // PUFF
            if (inPuff)
                body.linearVelocityY = 0;

            // ROUND ABOUT
            if (inRoundAbout)
            {
                roundAboutCollisionDetection();

                if (roundAboutTime > 0)
                {
                    roundAboutTime -= Time.deltaTime;

                    Vector2 v = Vector2.SmoothDamp(body.linearVelocity, targetVelocity, ref currentVelocity, roundAboutSmoothTime);
                    body.linearVelocity = v;
                }
                else
                {
                    Vector2 v = Vector2.SmoothDamp(body.linearVelocity, new Vector2(targetVelocity.x / 2, body.linearVelocityY), ref currentVelocity, 0.05f);
                    body.linearVelocity = v;
                }
            }
            

        }

        // OnGround Dictated Ground Interactions
        if (onGround())
        {
            voltageCount = 5; // NOTE THIS SHOULD BE TEMPORARY

            body.linearDamping = groundLinDamp; // set the linear dampening to the GROUND linear dampening value

            cancelAllVoltJumps();
        }
        else
            body.linearDamping = airLinDamp; // set the linear dampening to the AIR linear dampening value

        // Animations
        if (anim != null)
        {
            anim.SetFloat("VelocityX", Mathf.Abs(body.linearVelocityX));
            anim.SetBool("OnGround", onGround());
            anim.SetInteger("CurrVoltJump", currentJumpIndex);
        }

        huffState = EvaluateState();
    }

    private bool onGround()
    {
        // EXPLANATION: we cast a box thats a bit under the feet of Huff, extending to exactly the width of the boxCollider, and only detecting the ground layers
        RaycastHit2D raycast = Physics2D.BoxCast(boxCollider.bounds.center, groundDetectBoxSize, 0f, Vector2.down, boxCollider.bounds.extents.y + groundDetectOffsetY, groundLayer);
        return raycast.collider != null;
    }

    private void Run(float direction)
    {
        // Face them in the right direction
        if (direction == DIR_LEFT && transform.localScale.x > 0)
            LookInDirection(DIR_LEFT);
        else if (direction == DIR_RIGHT && transform.localScale.x < 0)
            LookInDirection(DIR_RIGHT);

        float speedDif = groundMaxVel - body.linearVelocityX;
        float movement = direction * speedDif * groundAccelRate;

        body.AddForce(movement * Vector2.right);
    }

    private void AirDrift(float direction)
    {
        float speedDif = airMaxVel - body.linearVelocityX;
        float movement = direction * speedDif * airAccelRate;

        body.AddForce(movement * Vector2.right);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if (!canMove) return; // if the player cannot move, then they can't jump, so return

        if (onGround()) // grounded jump
        {
            GroundJump();
        }
        else { }
    }

    

    private void LookInDirection(int direction)
    {
        transform.localScale = new Vector3(direction * spriteScaleVal, spriteScaleVal, spriteScaleVal);
    }

    private void PauseGame(InputAction.CallbackContext context)
    {
        
    }

    void GroundJump()
    {
        if (anim != null) anim.SetTrigger("Jump");
        huffState = CharacterState.Jumping;

        body.AddForce(new Vector3(0, initialJumpForce * 1000, 0));
    }

    void GroundJumpAscend()
    {
        body.linearVelocity = new Vector2(body.linearVelocityX, body.linearVelocityY += heldJumpForce * Time.deltaTime);
    }

    public void VoltJump()
    {
        if (canVoltJump() && currentJumpIndex >= 0 && currentJumpIndex <= 7)  // assures the player can Volt Jump, and current jump index is valid.
        {
            voltageCount--;
            if (anim != null) anim.SetTrigger("VoltJump");

            switch (currentJumpIndex)
            {
                case PUFF: Puff(true); break;
                case SPIRAL: Spiral(); break;
                case BOUNCE: Bounce(); break;
                case NIBBLE: Nibble(); break;
                case ROUND_ABOUT: RoundAbout(); break;
                case ZAP_LINE: ZapLine(); break;
                case CATCH_N_CHUCK: CatchNChuck(); break;
                case GLIDE: Glide(); break;
            }
        }
        else if (currentJumpIndex == 0) // Pilot Pause but without voltage
        {
            Puff(false);
        }
    }

    public void activateToggleAnimTrigger()
    {
        if (anim != null) anim.SetTrigger("Toggle");
    }

    public void cancelAllVoltJumps()
    {
        if (inPuff)
            cancelPuff();
        if (inRoundAbout)
            cancelRoundAbout();
        if (inSpiral)
            cancelSpiral();
    }

    // NOTE: we need to make a volt version
    void Puff(bool useVoltage)
    {
        /** EXPLANATION
         * The animation for the pause is triggered, and the functionality of y movement being locked
         * is toggled on in the animation calling togglePuffPaused(true), which means the fixed update
         * function maintain x velocity, but makes y = 0.
         */
        
    }

    void beginPuff()
    {
        inPuff = true;
        savedMomentumY = body.linearVelocityY;
    }
    void cancelPuff()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
        inPuff = false;
        body.linearVelocityY = savedMomentumY / 2;
    }

    void RoundAbout()
    {
        if (anim != null) anim.SetTrigger("RoundAbout");
        inRoundAbout = true;

        float dir = analogInput.x;
        if (dir == 0)
            dir = (transform.localScale.x > 0) ? DIR_RIGHT : DIR_LEFT;

        LookInDirection((int) dir);
    }

    void roundAboutCollisionDetection()
    { 
        RaycastHit2D wallRaycast = Physics2D.BoxCast(boxCollider.bounds.center, roundAboutHitboxSize, 0f, Vector2.down, 0f, 7);
        RaycastHit2D[] crackedBlockRaycast = Physics2D.BoxCastAll(boxCollider.bounds.center, roundAboutHitboxSize, 0f, Vector2.down, 0f, 12);

        //if (wallRaycast.collider != null) // collided with wall
        //{
        //    Debug.Log("hit a wall");
        //    RaycastHit2D leftRaycast = Physics2D.Raycast(boxCollider.bounds.center, Vector2.left, boxCollider.bounds.extents.y + 0.05f, 7);
        //    if (leftRaycast.collider != null)
        //        roundAboutWallBounce(DIR_RIGHT); // if we detect a wall right next to us on the left, we can assume we just hit, and we should thus bounce to the right
        //    else
        //        roundAboutWallBounce(DIR_LEFT); // if there was no wall to the left, we can assume the wall was to the right, and thus bounce to the left
        //}

        //foreach (RaycastHit2D crackedBlock in crackedBlockRaycast)
        //{
        //    crackedBlock.collider.gameObject.SetActive(false);
        //    Debug.Log("hit Cracked Block");
        //}
    }

    void roundAboutWallBounce(int dir)
    {
        LookInDirection(dir);
        wallBouncing = true;
        roundAboutTime = roundAboutSmoothTime;
    }

    void roundAboutAirDrift()
    {

    }

    void beginRoundAbout()
    {
        inRoundAbout = true;
    }

    void cancelRoundAbout()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
        inRoundAbout = false;
        wallBouncing = false;

        roundAboutTime = roundAboutSmoothTime;
    }

    void Bounce()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
    }

    void Nibble()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
    }

    void Spiral()
    {
        inSpiral = true;
        if (anim != null) anim.SetTrigger("Spiral");

        body.gravityScale = spiralGravityScale;
    }

    void cancelSpiral()
    {
        inSpiral = false;
        body.gravityScale = defaultGravityScale;
    }

    void ZapLine()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
    }

    void CatchNChuck()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
    }

    void Glide()
    {
        if (anim != null) anim.ResetTrigger("VoltJump");
    }

    public void Die()
    {
        respawnManager.HuffDied();
    }

    public void setJump(int jumpIndex)
    {
        currentJumpIndex = jumpIndex;

        VoltJump();
    }

    void setAnalogInput()
    {
        float x = 0, y = 0;

        if (right) x++;
        if (left) x--;

        if (up) y++;
        if (down) y--;

        analogInput = new Vector2(x, y);
    }

    bool canVoltJump()
    {
        return (voltageCount > 0) && canMove;
    }

    public CharacterState EvaluateState()
    {
        /** NOTE: for any of the states included in this section, make sure the systems in play reset themselves. */ 

        if (huffState == CharacterState.Dialogue 
            || huffState == CharacterState.Jumping)
            return huffState;

        if (onGround())
        {
            // NOTE: there will likely be more states on the ground then this / velocityX won't be the only factor.
            if (Mathf.Abs(body.linearVelocityX) < 0.1f) return CharacterState.Idle;
            else return CharacterState.Running;
        }

        else
        {
            if (inPuff) return CharacterState.Puff;
            else if (inRoundAbout) return CharacterState.RoundAbout;
            else if (inSpiral) return CharacterState.Spiral;
            else return CharacterState.MidAir;
        }

        return CharacterState.Unknown;
    }

    // Doesn't work properly
    public void windAddForce(Vector2 dir)
    {
        body.AddForce(dir);
    }

    private void OnDrawGizmos()
    {
        //Vector2 center = boxCollider.bounds.center + Vector3.down * (boxCollider.bounds.extents.y + groundDetectOffsetY);
        //Vector2 size = new Vector2(boxCollider.bounds.size.x, 0.1f);

        //Gizmos.DrawWireCube(center, size);

        //Collider2D wallRaycast = Physics2D.OverlapBox(boxCollider.bounds.center, new Vector2(0.65f, 0.65f), 0f, 7);

        //Gizmos.DrawWireCube(boxCollider.bounds.center, roundAboutHitboxSize);
    }

}
