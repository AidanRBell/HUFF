using System.Xml;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VoltJumpToggle : MonoBehaviour
{
    public bool canToggle = true;
    public int newestJumpIndex = -1;
    private int selectedJump;

    const int PILOT = 0, RODEO = 1, BELL = 2, NULL = 3,
        SHARD = 4, ZZZ = 5, COG = 6, GRIM = 7;

    private bool toggleButtonHeld, up, down, left, right;

    private Vector2 analogInput;

    [SerializeField] private float slowerSpeed = 0.3f;
    [SerializeField] private float notSelectedBrightness = 0.6f, disabledBrightness = 0.3f;

    PlayerControls controls;

    [SerializeField] HuffMovement huffMovement;
    [SerializeField] GameObject panel;

    [SerializeField] Image[] jumpIcons;

    private void OnEnable()
    {
        analogInput = Vector2.zero;

        controls = new PlayerControls();
        controls.Enable();

        controls.Huff.Toggle.performed += TogglePressed;
        controls.Huff.Toggle.canceled += ToggleReleased;

        controls.Huff.Up.performed += ctx => up = true;
        controls.Huff.Up.canceled += ctx => up = false;

        controls.Huff.Down.performed += ctx => down = true;
        controls.Huff.Down.canceled += ctx => down = false;

        controls.Huff.Left.performed += ctx => left = true;
        controls.Huff.Left.canceled += ctx => left = false;

        controls.Huff.Right.performed += ctx => right = true;
        controls.Huff.Right.canceled += ctx => right = false;

        setJumpIconStatuses();

        panel.SetActive(false);
    }

    private void Awake()
    {
        if (newestJumpIndex < 0) // invalid input (or none at all)
            newestJumpIndex = 0;
        else if (newestJumpIndex > 7) // player has unlocked all the jumps
            newestJumpIndex = 7;

        selectedJump = huffMovement.currentJumpIndex;
    }


    private void OnDisable()
    {
        controls.Huff.Toggle.performed -= TogglePressed;
        controls.Huff.Toggle.canceled -= ToggleReleased;

        controls.Huff.Up.performed -= ctx => up = true;
        controls.Huff.Up.canceled -= ctx => up = false;

        controls.Huff.Down.performed -= ctx => down = true;
        controls.Huff.Down.canceled -= ctx => down = false;

        controls.Huff.Left.performed -= ctx => left = true;
        controls.Huff.Left.canceled -= ctx => left = false;

        controls.Huff.Right.performed -= ctx => right = true;
        controls.Huff.Right.canceled -= ctx => right = false;

        controls.Disable();

        huffMovement.setJump(selectedJump);
    }

    private void Update()
    {

        if (toggleButtonHeld)
        {
            setXYInputs();
            int jump = requestedJumpIndex();

            if (jump > newestJumpIndex)
                jump = PILOT;

            if (jump > -1) // an direction is being held
            {
                for (int i = 0; i <= newestJumpIndex; i++)
                {
                    if (i == jump)
                    {
                        selectedJump = jump;
                        setJumpIconAsSelected(i);
                    }
                    else
                        setJumpIconAsNotSelected(i);
                }
            }
        }


    }

    private int requestedJumpIndex()
    {
        if (analogInput.x == 0 && analogInput.y == 0) return -1; // no input
        if (analogInput.x == -1 && analogInput.y == 1) return PILOT; // up left
        if (analogInput.x == 0 && analogInput.y == 1) return RODEO; // up
        if (analogInput.x == 1 && analogInput.y == 1) return BELL; // up right
        if (analogInput.x == 1 && analogInput.y == 0) return NULL; // right
        if (analogInput.x == 1 && analogInput.y == -1) return SHARD; // down right
        if (analogInput.x == 0 && analogInput.y == -1) return ZZZ; // down
        if (analogInput.x == -1 && analogInput.y == -1) return COG; // down left
        if (analogInput.x == -1 && analogInput.y == 0) return GRIM; // left

        return 0;
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

    private void TogglePressed(InputAction.CallbackContext context)
    {
        toggleButtonHeld = true;

        if (canToggle)
        {
            huffMovement.canMove = false;
            huffMovement.activateToggleAnimTrigger();

            panel.SetActive(true);
            Time.timeScale = slowerSpeed;
        }
    }

    private void ToggleReleased(InputAction.CallbackContext context)
    {
        toggleButtonHeld = false;
        turnOffToggle();

        huffMovement.setJump(selectedJump);
    }

    public void turnOffToggle()
    {
        huffMovement.canMove = true;
        panel.SetActive(false);
        Time.timeScale = 1f;
    }

    static public void PRINT(string s)
    {
        Debug.Log(s);
    }

    bool validJump(int index)
    {
        return index <= newestJumpIndex && index >= 0;
    }

    void setJumpIconStatuses()
    {
        setJumpIconAsSelected(PILOT);

        for (int i = 1; i <= newestJumpIndex; i++)
            setJumpIconAsNotSelected(i);

        for (int i = newestJumpIndex + 1; i < 8; i++)
            setJumpIconAsDisabled(i);

    }

    void setJumpIconAsSelected(int index)
    {
        jumpIcons[index].color = Color.white;
    }

    void setJumpIconAsNotSelected(int index)
    {
        jumpIcons[index].color = new Color(notSelectedBrightness, notSelectedBrightness, notSelectedBrightness, 1);
        // possibly change the opacity
    }

    void setJumpIconAsDisabled(int index)
    {
        jumpIcons[index].color = new Color(disabledBrightness, disabledBrightness, disabledBrightness, 1);
        // possibly change the opacity
    }
}
