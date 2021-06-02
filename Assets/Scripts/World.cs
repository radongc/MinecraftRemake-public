using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    [HideInInspector] public string gameVersion;

    [HideInInspector] public int seed;
    public BiomeAttributes biome;

    public Transform player;
    public Vector3 spawnPosition;

    public PhysicMaterial physicalMaterial;

    public Material material;
    public Material transparentMaterial;

    public BlockType[] blockTypes;

    Chunk[,] chunks = new Chunk[BlockData.WorldSizeInChunks, BlockData.WorldSizeInChunks];

    List<ChunkCoord> activeChunks = new List<ChunkCoord>();
    ChunkCoord playerChunkCoord;
    ChunkCoord playerLastChunkCoord;

    List<ChunkCoord> chunkGenQueue = new List<ChunkCoord>();
    private bool generatingChunks;

    private float timePerChunkUpdate = 0.5f;
    private float chunkUpdateTimer = 0f;

    void Awake()
    {
        gameVersion = "0.12b";

        seed = Random.Range(-10000, 10000);
    }

    void Start()
    {
        Random.InitState(seed); // seed the map

        spawnPosition = new Vector3((BlockData.WorldSizeInChunks * BlockData.ChunkWidth) / 2f, BlockData.ChunkHeight - 60f, (BlockData.WorldSizeInChunks * BlockData.ChunkWidth) / 2f);
        GenerateWorld();

        playerLastChunkCoord = GetChunkCoordFromVector3(player.position);
    }

    void Update()
    {
        playerChunkCoord = GetChunkCoordFromVector3(player.position);

        chunkUpdateTimer += Time.deltaTime;

        if (!playerChunkCoord.Equals(playerLastChunkCoord))
        {
            CheckViewDistance();
        }

        if (chunkGenQueue.Count > 0 && !generatingChunks)
        {
            StartCoroutine("GenerateChunks");
        }

        if (chunkUpdateTimer >= timePerChunkUpdate)
        {
            chunkUpdateTimer = 0f;

            StartCoroutine("UpdateChunks");
        }
    }

    void GenerateWorld()
    {
        for (int x = (BlockData.WorldSizeInChunks / 2) - BlockData.ViewDistanceInChunks; x < (BlockData.WorldSizeInChunks / 2) + BlockData.ViewDistanceInChunks; x++)
        {
            for (int z = (BlockData.WorldSizeInChunks / 2) - BlockData.ViewDistanceInChunks; z < (BlockData.WorldSizeInChunks / 2) + BlockData.ViewDistanceInChunks; z++)
            {
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);
                activeChunks.Add(new ChunkCoord(x, z));
            }
        }

        player.position = spawnPosition;
    }

    IEnumerator GenerateChunks()
    {
        generatingChunks = true;

        while (chunkGenQueue.Count > 0)
        {
            chunks[chunkGenQueue[0].x, chunkGenQueue[0].z].Init();
            chunkGenQueue.RemoveAt(0);
            yield return null;
        }

        generatingChunks = false;
    }

    IEnumerator UpdateChunks()
    {
        ChunkCoord playerChunk = GetChunkCoordFromVector3(player.position);
        
        /*ChunkCoord[] surroundingChunks = new ChunkCoord[] // doing surrounding chunks causes very noticeable lag, need a new way to do this
        {
            new ChunkCoord(playerChunk.x, playerChunk.z - 1), // left
            new ChunkCoord(playerChunk.x, playerChunk.z + 1), // right
            new ChunkCoord(playerChunk.x - 1, playerChunk.z), // front
            new ChunkCoord(playerChunk.x + 1, playerChunk.z), // back
        };*/

        chunks[playerChunk.x, playerChunk.z].TimedChunkUpdate();

        /*foreach(ChunkCoord c in surroundingChunks)
        {
            chunks[c.x, c.z].TimedChunkUpdate();
        }*/

        yield return null;
    }

    public ChunkCoord GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / BlockData.ChunkWidth);
        int z = Mathf.FloorToInt(pos.z / BlockData.ChunkWidth);
        
        return new ChunkCoord(x, z);
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        ChunkCoord coord = GetChunkCoordFromVector3(pos);

        return chunks[coord.x, coord.z];
    }

    public bool CheckForBlock(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > BlockData.ChunkHeight)
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isBlockMapPopulated)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetBlockFromGlobalVector3(pos)].isSolid;
        }

        return blockTypes[GetBlock(pos)].isSolid;
    }

    public bool CheckBlockTransparency(Vector3 pos)
    {
        ChunkCoord thisChunk = new ChunkCoord(pos);

        if (!IsChunkInWorld(thisChunk) || pos.y < 0 || pos.y > BlockData.ChunkHeight)
        {
            return false;
        }

        if (chunks[thisChunk.x, thisChunk.z] != null && chunks[thisChunk.x, thisChunk.z].isBlockMapPopulated)
        {
            return blockTypes[chunks[thisChunk.x, thisChunk.z].GetBlockFromGlobalVector3(pos)].isTransparent;
        }

        return blockTypes[GetBlock(pos)].isTransparent;
    }

    void CheckViewDistance()
    {
        ChunkCoord coord = GetChunkCoordFromVector3(player.position);
        playerLastChunkCoord = playerChunkCoord;

        List<ChunkCoord> previouslyActiveChunks = new List<ChunkCoord>(activeChunks);

        for (int x = coord.x - BlockData.ViewDistanceInChunks; x < coord.x + BlockData.ViewDistanceInChunks; x++)
        {
            for (int z = coord.z - BlockData.ViewDistanceInChunks; z < coord.z + BlockData.ViewDistanceInChunks; z++)
            {
                if (IsChunkInWorld(new ChunkCoord(x, z)))
                {
                    if (chunks[x, z] == null)
                    {
                        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, false);
                        chunkGenQueue.Add(new ChunkCoord(x, z));
                    }
                    else if (!chunks[x, z].IsActive)
                    {
                        chunks[x, z].IsActive = true;
                    }

                    activeChunks.Add(new ChunkCoord(x, z));
                }

                for (int i = previouslyActiveChunks.Count - 1; i > -1; i--)
                {
                    if (previouslyActiveChunks[i].Equals(new ChunkCoord(x, z)))
                    {
                        previouslyActiveChunks.RemoveAt(i);
                    }
                }
            }
        }

        foreach(ChunkCoord c in previouslyActiveChunks)
        {
            chunks[c.x, c.z].IsActive = false;
        }
    }

    public byte GetBlock(Vector3 pos)
    {
        int yPos = Mathf.FloorToInt(pos.y);

        /* Immutable Pass (Means absolute, this section will never change)*/

        // outside world is always air
        if (!IsBlockInWorld(pos))
        {
            return 0;
        }

        // absolute bottom (y 0) is always bedrock
        if (yPos == 0)
        {
            return 1;
        }

        /* Basic Terrain Pass */

        int terrainHeight = Mathf.FloorToInt(biome.terrainHeight * Noise.Get2DPerlin(new Vector2(pos.x, pos.z), 0, biome.terrainScale, seed)) + biome.solidGroundHeight;
        byte blockValue = 0;

        if (yPos == terrainHeight)
        {
            blockValue = biome.topLevelBlockID; // grass
        }
        else if (yPos < terrainHeight && yPos > terrainHeight - 4)
        {
            blockValue = biome.lowerLevelBlockID; // dirt
        }
        else if (yPos > terrainHeight)
        {
            return 0; // air
        }
        else
        {
            blockValue = biome.undergroundBlockID; // stone
        }

        /* SECOND PASS */

        if (blockValue == biome.undergroundBlockID || blockValue == biome.lowerLevelBlockID || blockValue == biome.topLevelBlockID)
        {
            foreach (Lode lode in biome.lodes)
            {
                if (yPos > lode.minHeight && yPos < lode.maxHeight)
                {
                    if (Noise.Get3DPerlin(pos, lode.noiseOffset, lode.scale, lode.threshold, seed))
                    {
                        if (!lode.restrictToStone)
                        {
                            blockValue = lode.blockID;
                        }
                        else if (lode.restrictToStone)
                        {
                            if (blockValue == 2)
                            {
                                blockValue = lode.blockID;
                            }
                            else
                            {
                                return blockValue;
                            }
                        }
                    }
                }
            }
        }
        
        return blockValue;

    }

    bool IsChunkInWorld(ChunkCoord coord)
    {
        if (coord.x > 0 && coord.x < BlockData.WorldSizeInChunks - 1 && coord.z > 0 && coord.z < BlockData.WorldSizeInChunks - 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    bool IsBlockInWorld(Vector3 pos)
    {
        if (pos.x >= 0 && pos.x < BlockData.WorldSizeInBlocks && pos.y >= 0 && pos.y < BlockData.ChunkHeight && pos.z >= 0 && pos.z < BlockData.WorldSizeInBlocks)
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}

[System.Serializable]
public class BlockType
{
    public string blockName;

    public bool isSolid;

    public bool affectedByGravity;

    public bool isBreakable;
    public bool isTransparent;

    [Header("Texture Values")]
    public int backFaceTexture;
    public int frontFaceTexture;
    public int topFaceTexture;
    public int bottomFaceTexture;
    public int leftFaceTexture;
    public int rightFaceTexture;

    // Order of block faces: Back, Front, Top, Bottom, Left, Right

    public int GetTextureID(int faceIndex)
    {
        switch(faceIndex)
        {
            case 0:
                return backFaceTexture;
            case 1:
                return frontFaceTexture;
            case 2:
                return topFaceTexture;
            case 3:
                return bottomFaceTexture;
            case 4:
                return leftFaceTexture;
            case 5:
                return rightFaceTexture;
            default:
                Debug.Log("Invalid face index");
                return 0;
        }
    }
}
