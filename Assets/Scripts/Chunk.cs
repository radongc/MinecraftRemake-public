using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk
{
    public ChunkCoord coord;

    private GameObject chunkObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    //private MeshCollider meshCollider;

    int vertexIndex = 0;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<int> transparentTriangles = new List<int>();
    Material[] materials = new Material[2];
    List<Vector2> uvs = new List<Vector2>();

    byte[,,] blockMap = new byte[BlockData.ChunkWidth, BlockData.ChunkHeight, BlockData.ChunkWidth]; // bool array to be able to check whether each block space is occupied by a block

    private bool _isActive;

    public bool isBlockMapPopulated;

    World world;

    public Chunk (ChunkCoord _coord, World _world, bool generateOnLoad)
    {
        coord = _coord;
        world = _world;
        IsActive = true;

        if (generateOnLoad)
        {
            Init();
        }
    }

    public void Init()
    {
        chunkObject = new GameObject();
        meshFilter = chunkObject.AddComponent<MeshFilter>();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();

        materials[0] = world.material;
        materials[1] = world.transparentMaterial;

        meshRenderer.materials = materials;

        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position = new Vector3(coord.x * BlockData.ChunkWidth, 0f, coord.z * BlockData.ChunkWidth);
        chunkObject.name = "Chunk: " + coord.x + ", " + coord.z;
        chunkObject.tag = "GameChunk";

        PopulateBlockMap();
        UpdateChunk();
    }

    void PopulateBlockMap() // mark each coordinate in world space that will be occupied by chunk blocks as occupied
    {
        for (int y = 0; y < BlockData.ChunkHeight; y++) // iterate thru y axis
        {
            for (int x = 0; x < BlockData.ChunkWidth; x++) // iterate thru x axis
            {
                for (int z = 0; z < BlockData.ChunkWidth; z++) // iterate thru z axis
                {
                    blockMap[x, y, z] = world.GetBlock(new Vector3(x, y, z) + Position);
                }
            }
        }

        isBlockMapPopulated = true;
    }

    void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
        transparentTriangles.Clear();
        uvs.Clear();
        vertexIndex = 0;
    }

    void UpdateChunk() // populate the chunk mesh for each position in world space previously marked for occupation
    {
        ClearMeshData();

        for (int y = 0; y < BlockData.ChunkHeight; y++)
        {
            for (int x = 0; x < BlockData.ChunkWidth; x++)
            {
                for (int z = 0; z < BlockData.ChunkWidth; z++)
                {
                    if (world.blockTypes[blockMap[x, y, z]].isSolid)
                    {
                        UpdateMeshData(new Vector3(x, y, z));
                    }
                }
            }
        }
        CreateMesh();
    }

    public void TimedChunkUpdate()
    {
        for (int y = 0; y < BlockData.ChunkHeight; y++)
        {
            for (int x = 0; x < BlockData.ChunkWidth; x++)
            {
                for (int z = 0; z < BlockData.ChunkWidth; z++)
                {
                    byte currentBlock = blockMap[x, y, z];

                    if (world.blockTypes[currentBlock].affectedByGravity)
                    {
                        if (!world.blockTypes[blockMap[x, y - 1, z]].isSolid)
                        {
                            blockMap[x, y, z] = 0;
                            blockMap[x, y - 1, z] = currentBlock;
                            UpdateSurroundingBlocks(x, y, z);
                        }
                    }
                }
            }
        }

        UpdateChunk();
    }

    public bool IsActive
    {
        get
        {
            return _isActive;
        }
        set
        {
            _isActive = value;

            if (chunkObject != null)
            {
                chunkObject.SetActive(value);
            }
        }
    }

    public Vector3 Position
    {
        get
        {
            return chunkObject.transform.position;
        }
    }

    bool IsBlockInChunk(int x, int y, int z)
    {
        if (x < 0 || x > BlockData.ChunkWidth - 1 || y < 0 || y > BlockData.ChunkHeight - 1 || z < 0 || z > BlockData.ChunkWidth - 1)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    public byte GetBlockFromGlobalVector3(Vector3 pos)
    {
        int xPos = Mathf.FloorToInt(pos.x);
        int yPos = Mathf.FloorToInt(pos.y);
        int zPos = Mathf.FloorToInt(pos.z);

        xPos -= Mathf.FloorToInt(Position.x);
        zPos -= Mathf.FloorToInt(Position.z);

        return blockMap[xPos, yPos, zPos];
    }

    public void EditBlock(Vector3 pos, byte newBlockID)
    {
        int xPos = Mathf.FloorToInt(pos.x);
        int yPos = Mathf.FloorToInt(pos.y);
        int zPos = Mathf.FloorToInt(pos.z);

        xPos -= Mathf.FloorToInt(Position.x);
        zPos -= Mathf.FloorToInt(Position.z);

        if ((newBlockID == 0 && world.blockTypes[blockMap[xPos, yPos, zPos]].isBreakable) || newBlockID != 0)
        {
            blockMap[xPos, yPos, zPos] = newBlockID;

            UpdateSurroundingBlocks(xPos, yPos, zPos);

            UpdateChunk();

            Debug.Log("Updating " + xPos + ", " + yPos + ", " + zPos);
        }
        else if (newBlockID == 0 && !world.blockTypes[blockMap[xPos, yPos, zPos]].isBreakable)
        {
            Debug.Log("Tried to break unbreakable block: " + xPos + ", " + yPos + ", " + zPos);
        }
    }

    void UpdateSurroundingBlocks(int x, int y, int z)
    {
        Vector3 changedBlock = new Vector3(x, y, z);

        for (int p = 0; p < 6; p++)
        {
            Vector3 currentBlock = changedBlock + BlockData.faceCheckOffsets[p];

            if (!IsBlockInChunk((int)currentBlock.x, (int)currentBlock.y, (int)currentBlock.z))
            {
                world.GetChunkFromVector3(currentBlock + Position).UpdateChunk();
            }
        }
    }

    bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x); // floor each position checked so it's perfectly even
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        // if any of the coordinates are outside of the chunk (for now) they are unoccupied
        if (!IsBlockInChunk(x, y, z))
        {
            return world.CheckBlockTransparency(pos + Position);
        }

        return world.blockTypes[blockMap[x, y, z]].isTransparent; // otherwise, return whether or not the position is occupied
    }

    void UpdateMeshData(Vector3 pos)
    {
        byte blockID = blockMap[(int)pos.x, (int)pos.y, (int)pos.z];
        bool isTransparent = world.blockTypes[blockID].isTransparent;

        // p = face index, i = vert index on face. ex. p= 1 i =5 is the 6th vertex on the 2nd (Front in this case) face
        for (int p = 0; p < 6; p++) // iterate thru faces of block
        {
            if (CheckVoxel(pos + BlockData.faceCheckOffsets[p])) // while adding mesh data for each face (each iteration of p), make sure there is no block adjacent to current face
            {
                // the reason for no 2nd loop here is essentially so some vertices do not get added multiple times
                vertices.Add(pos + BlockData.vertices[BlockData.triangles[p, 0]]);
                vertices.Add(pos + BlockData.vertices[BlockData.triangles[p, 1]]);
                vertices.Add(pos + BlockData.vertices[BlockData.triangles[p, 2]]);
                vertices.Add(pos + BlockData.vertices[BlockData.triangles[p, 3]]);
                
                AddTexture(world.blockTypes[blockID].GetTextureID(p));

                if (!isTransparent)
                {
                    triangles.Add(vertexIndex);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 2);
                    triangles.Add(vertexIndex + 1);
                    triangles.Add(vertexIndex + 3);
                }
                else
                {
                    transparentTriangles.Add(vertexIndex);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 2);
                    transparentTriangles.Add(vertexIndex + 1);
                    transparentTriangles.Add(vertexIndex + 3);
                }

                vertexIndex += 4;
            }
        }
    }

    void CreateMesh() // build the mesh
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();

        mesh.subMeshCount = 2;
        mesh.SetTriangles(triangles, 0);
        mesh.SetTriangles(transparentTriangles, 1);

        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals();
        mesh.Optimize();

        meshFilter.mesh = mesh;
    }

    void AddTexture (int textureID)
    {
        float y = textureID / BlockData.TextureAtlasSizeInBlocks;
        float x = textureID - (y * BlockData.TextureAtlasSizeInBlocks);

        x *= BlockData.NormalizedBlockTextureSize;
        y *= BlockData.NormalizedBlockTextureSize;

        y = 1f - y - BlockData.NormalizedBlockTextureSize;

        uvs.Add(new Vector2(x, y));
        uvs.Add(new Vector2(x, y + BlockData.NormalizedBlockTextureSize));
        uvs.Add(new Vector2(x + BlockData.NormalizedBlockTextureSize, y));
        uvs.Add(new Vector2(x + BlockData.NormalizedBlockTextureSize, y + BlockData.NormalizedBlockTextureSize));
    }
}

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int _x, int _z)
    {
        x = _x;
        z = _z;
    }

    public ChunkCoord(Vector3 pos)
    {

        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / BlockData.ChunkWidth;
        z = zCheck / BlockData.ChunkWidth;

    }

    public bool Equals (ChunkCoord other)
    {
        if (other == null)
        {
            return false;
        }
        else if (other.x == x && other.z == z)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public override string ToString()
    {
        return x + ", " + z;
    }
}