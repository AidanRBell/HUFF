using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEditor.Profiling;
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

    const int DIR_LEFT = -1, DIR_RIGHT = 1;
    float spriteScaleVal;
    
    // Ground Detection
    [SerializeField] private Vector2 groundDetectBoxSize;
    [SerializeField] private Vector3 castOffsetX;
    [SerializeField] private float castDistance;
    [SerializeField] private LayerMask groundLayer;

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
    [SerializeField] private Vector2 roundAboutForce;

    // Spiral
    bool inSpiral = false;
    [SerializeField] private float spiralGravityScale;


    // Debugging
    float triggerTestTime = 0f;


    private void Start()
    {
        body = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        boxCollider = GetComponent<BoxCollider2D>();

        spriteScaleVal = transform.localScale.x;
        defaultGravityScale = body.gravityScale;
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
        //Debug.Log("Current jump = " + currentJumpIndex + ", v count = " + voltageCount);

        //Debug.Log(huffState.ToString());

        
    }

    private void FixedUpdate()
    {
        setXYInputs();

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

            float dir = analogInput.x;
            
            if (dir != 0)
            {
                if (onGround())
                    Run(dir);
                else
                    AirDrift(dir);
            }

            if (inPuff)
                body.linearVelocityY = 0;

        }

        if (onGround())
        {
            voltageCount = 1; // NOTE THIS SHOULD BE TEMPORARY
            inPuff = false;

            body.linearDamping = groundLinDamp; // set the linear dampening to the GROUND linear dampening value

            if (inSpiral)
            {
                toggleSpiralOff();
            }
        }
        else
            body.linearDamping = airLinDamp; // set the linear dampening to the AIR linear dampening value

        // Animations
        anim.SetFloat("VelocityX", Mathf.Abs(body.linearVelocityX));
        anim.SetBool("OnGround", onGround());
        anim.SetInteger("CurrVoltJump", currentJumpIndex);

        huffState = EvaluateState();
    }

    private bool onGround()
    {
        if (Physics2D.BoxCast(transform.position + castOffsetX, groundDetectBoxSize, 0, -transform.up, castDistance, groundLayer))
            return true;
        return false;
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
        else
        {
            
        }
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
        anim.SetTrigger("Jump");
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
            anim.SetTrigger("VoltJump");

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
        anim.SetTrigger("Toggle");
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

    void togglePuffOn()
    {
        inPuff = true;
        savedMomentumY = body.linearVelocityY;
    }
    void togglePuffOff()
    {
        anim.ResetTrigger("VoltJump");
        inPuff = false;
        body.linearVelocityY = savedMomentumY / 2;
    }

    void RoundAbout()
    {
        anim.SetTrigger("RoundAbout");
        inRoundAbout = true;

        body.AddForce(roundAboutForce);
    }

    void toggleRoundAboutOn()
    {
        inRoundAbout = true;
    }

    void toggleRoundAboutOff()
    {
        anim.ResetTrigger("VoltJump");
        inRoundAbout = false;
    }

    void Bounce()
    {
        anim.ResetTrigger("VoltJump");
    }

    void Nibble()
    {
        anim.ResetTrigger("VoltJump");
    }

    void Spiral()
    {
        inSpiral = true;
        anim.SetTrigger("Spiral");

        body.gravityScale = spiralGravityScale;
    }

    void toggleSpiralOff()
    {
        inSpiral = false;
        body.gravityScale = defaultGravityScale;
    }

    void ZapLine()
    {
        anim.ResetTrigger("VoltJump");
    }

    void CatchNChuck()
    {
        anim.ResetTrigger("VoltJump");
    }

    void Glide()
    {
        anim.ResetTrigger("VoltJump");
    }

    public void Die()
    {

    }

    public void setJump(int jumpIndex)
    {
        currentJumpIndex = jumpIndex;

        VoltJump();
    }

    void setXYInputs()
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

    void transitionJump(int jumpIndex)
    {
        togglePuffOff();
        toggleSpiralOff();
        
    }

    public void bodyAddForce(Vector2 dir)
    {
        body.AddForce(dir);
    }

    /** For drawing the Ground Detection Box */
    //private void OnDrawGizmos()
    //{
    //    Gizmos.DrawWireCube((transform.position + castOffsetX) - transform.up * castDistance, groundDetectBoxSize);
    //}

}
