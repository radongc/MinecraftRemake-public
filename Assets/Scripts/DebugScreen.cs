using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugScreen : MonoBehaviour
{
    bool isActive;

    GameObject debugParent;
    World world;

    // game values
    private float currentFPS;
    private ChunkCoord currentChunk;

    private float playerXPos;
    private float playerYPos;
    private float playerZPos;

    // UI labels
    [SerializeField] private Text gameVersionText;
    [SerializeField] private Text currentFPSText;
    [SerializeField] private Text playerXPosText;
    [SerializeField] private Text playerYPosText;
    [SerializeField] private Text playerZPosText;
    [SerializeField] private Text seedText;
    [SerializeField] private Text chunkText;

    void Start()
    {
        debugParent = GameObject.Find("DebugScreen");
        world = GameObject.Find("World").GetComponent<World>();

        isActive = true;

        InitStaticGameValues();
    }

    void InitStaticGameValues() // these values only need to be updated once
    {
        // game values

        // Game version
        gameVersionText.text = world.gameVersion;

        // Seed
        seedText.text = world.seed.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        GetInput();
        UpdateDebugScreen();

        if (isActive)
        {
            if (!debugParent.activeSelf)
            {
                debugParent.SetActive(true);
            }
        }

        if (!isActive)
        {
            if (debugParent.activeSelf)
            {
                debugParent.SetActive(false);
            }
        }
    }

    void GetInput()
    {
        if (Input.GetButtonDown("DisplayDebug"))
        {
            isActive = !isActive;
        }
    }

    void UpdateDebugScreen() // these values need to be updated constantly
    {
        if (isActive)
        {
            // FPS
            currentFPS = 1.0f / Time.deltaTime;

            currentFPSText.text = currentFPS.FloorToInt().ToString();

            // Player pos
            playerXPos = world.player.position.x;
            playerYPos = world.player.position.y;
            playerZPos = world.player.position.z;

            playerXPosText.text = playerXPos.ToString();
            playerYPosText.text = playerYPos.ToString();
            playerZPosText.text = playerZPos.ToString();

            // Chunk
            currentChunk = world.GetChunkCoordFromVector3(world.player.position);

            chunkText.text = currentChunk.x + ", " + currentChunk.z;
        }
    }
}
