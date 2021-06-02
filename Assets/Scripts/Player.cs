using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public bool isGrounded;
    public bool isSprinting;

    Transform playerCamera;
    World world;

    public float walkSpeed = 4.5f;
    public float sprintSpeed = 7f;
    public float jumpForce = 5f;
    public float gravity = -9.8f;

    public float playerWidth = 0.15f;

    private float horizontal;
    private float vertical;
    [SerializeField] private float mouseHorizontal;
    [SerializeField] private float mouseVertical;
    private Vector3 velocity;
    [SerializeField] private float verticalMomentum = 0f;
    private bool jumpRequest;

    public Transform highlightBlock;
    public Transform placementBlock;
    public float checkIncrement = 0.1f;
    public float reach = 8f;

    public Text selectedBlockText;
    public byte selectedBlockIndex = 1;

    void Start()
    {
        playerCamera = GetComponentInChildren<Camera>().transform;
        world = GameObject.Find("World").GetComponent<World>();

        Cursor.lockState = CursorLockMode.Locked;

        selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName + " block selected";
    }

    void Update()
    {
        GetPlayerInput();
        PlaceCursorBlocks();
    }

    void FixedUpdate()
    {
        CalculateVelocity();

        if (jumpRequest)
        {
            Jump();
        }

        if (mouseVertical < -90f)
        {
            mouseVertical = -90f;
        }
        else if (mouseVertical > 90f)
        {
            mouseVertical = 90f;
        }

        transform.localRotation = Quaternion.Euler(Vector3.up * mouseHorizontal);
        playerCamera.localRotation = Quaternion.Euler(Vector3.right * -mouseVertical);

        transform.Translate(velocity, Space.World);
    }

    private void Jump()
    {
        verticalMomentum = jumpForce;
        isGrounded = false;
        jumpRequest = false;
    }

    private void CalculateVelocity()
    {
        if (verticalMomentum > gravity)
        {
            verticalMomentum += Time.fixedDeltaTime * gravity;
        }

        if (isSprinting)
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * sprintSpeed;
        }
        else
        {
            velocity = ((transform.forward * vertical) + (transform.right * horizontal)) * Time.fixedDeltaTime * walkSpeed;
        }

        velocity += Vector3.up * verticalMomentum * Time.fixedDeltaTime;

        if ((velocity.z > 0 && Front) || (velocity.z < 0 && Back))
        {
            velocity.z = 0;
        }

        if ((velocity.x > 0 && Right) || (velocity.x < 0 && Left))
        {
            velocity.x = 0;
        }

        if (velocity.y < 0)
        {
            velocity.y = CheckDownSpeed(velocity.y);
        }
        else if (velocity.y > 0)
        {
            velocity.y = CheckUpSpeed(velocity.y);
        }
    }

    private void GetPlayerInput()
    {
        if (Input.GetKeyDown("escape"))
        {
            Cursor.lockState = CursorLockMode.None;
        }

        if (Input.GetButtonDown("Sprint"))
        {
            isSprinting = true;
        }
        
        if (Input.GetButtonUp("Sprint"))
        {
            isSprinting = false;
        }

        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            jumpRequest = true;
        }

        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        mouseHorizontal += Input.GetAxis("Mouse X");
        mouseVertical += Input.GetAxis("Mouse Y");

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0) // if this is not 0, it has been scrolled
        {
            if (scroll > 0)
            {
                selectedBlockIndex++;
            }
            else
            {
                selectedBlockIndex--;
            }

            if (selectedBlockIndex > (byte)(world.blockTypes.Length - 1))
            {
                selectedBlockIndex = 1;
            }

            if (selectedBlockIndex < 1)
            {
                selectedBlockIndex = (byte)(world.blockTypes.Length - 1);
            }

            selectedBlockText.text = world.blockTypes[selectedBlockIndex].blockName + " block selected";
        }

        if (highlightBlock.gameObject.activeSelf)
        {
            // Destroy block
            if (Input.GetMouseButtonDown(0))
            {
                world.GetChunkFromVector3(highlightBlock.position).EditBlock(highlightBlock.position, 0);
            }

            // place
            if (Input.GetMouseButtonDown(1))
            {
                world.GetChunkFromVector3(placementBlock.position).EditBlock(placementBlock.position, selectedBlockIndex);
            }
        }
    }

    private void PlaceCursorBlocks()
    {
        float step = checkIncrement;
        Vector3 lastPos = new Vector3();

        while (step < reach)
        {
            Vector3 pos = playerCamera.position + (playerCamera.forward * step);

            if (world.CheckForBlock(pos))
            {
                highlightBlock.position = pos.FloorToInt();
                placementBlock.position = lastPos;

                highlightBlock.gameObject.SetActive(true);
                placementBlock.gameObject.SetActive(true);

                return;
            }

            lastPos = pos.FloorToInt();

            step += checkIncrement;
        }

        highlightBlock.gameObject.SetActive(false);
        placementBlock.gameObject.SetActive(false);
    }

    private float CheckDownSpeed(float downSpeed)
    {
        Vector3 downCheck1 = new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth);
        Vector3 downCheck2 = new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z - playerWidth);
        Vector3 downCheck3 = new Vector3(transform.position.x + playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth);
        Vector3 downCheck4 = new Vector3(transform.position.x - playerWidth, transform.position.y + downSpeed, transform.position.z + playerWidth);


        if (world.CheckForBlock(downCheck1) || world.CheckForBlock(downCheck2) || world.CheckForBlock(downCheck3) || world.CheckForBlock(downCheck4))
        {
            isGrounded = true;
            return 0;
        }
        else
        {
            isGrounded = false;
            return downSpeed;
        }
    }

    private float CheckUpSpeed(float upSpeed)
    {
        Vector3 upCheck1 = new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth);
        Vector3 upCheck2 = new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z - playerWidth);
        Vector3 upCheck3 = new Vector3(transform.position.x + playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth);
        Vector3 upCheck4 = new Vector3(transform.position.x - playerWidth, transform.position.y + 2f + upSpeed, transform.position.z + playerWidth);

        if (world.CheckForBlock(upCheck1) || world.CheckForBlock(upCheck2) || world.CheckForBlock(upCheck3) || world.CheckForBlock(upCheck4))
        {
            return 0;
        }
        else
        {
            return upSpeed;
        }
    }

    public bool Front
    {
        get
        {
            if (world.CheckForBlock(new Vector3(transform.position.x, transform.position.y, transform.position.z + playerWidth)) ||
                world.CheckForBlock(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z + playerWidth)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool Back
    {
        get
        {
            if (world.CheckForBlock(new Vector3(transform.position.x, transform.position.y, transform.position.z - playerWidth)) ||
                world.CheckForBlock(new Vector3(transform.position.x, transform.position.y + 1f, transform.position.z - playerWidth)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool Left
    {
        get
        {
            if (world.CheckForBlock(new Vector3(transform.position.x - playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForBlock(new Vector3(transform.position.x - playerWidth, transform.position.y + 1f, transform.position.z)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public bool Right
    {
        get
        {
            if (world.CheckForBlock(new Vector3(transform.position.x + playerWidth, transform.position.y, transform.position.z)) ||
                world.CheckForBlock(new Vector3(transform.position.x + playerWidth, transform.position.y + 1f, transform.position.z)))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
