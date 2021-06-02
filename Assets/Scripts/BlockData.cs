using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockData
{
    public static readonly int ChunkHeight = 256; // how tall is the chunk
    public static readonly int ChunkWidth = 16; // how wide is it? (x and z)
    public static readonly int WorldSizeInChunks = 100;

    public static int WorldSizeInBlocks
    {
        get
        {
            return WorldSizeInChunks * ChunkWidth;
        }
    }

    public static readonly int ViewDistanceInChunks = 5;

    public static readonly int TextureAtlasSizeInBlocks = 4; // how many block per row in texture atlas (textures png file)
    public static float NormalizedBlockTextureSize
    {
        get
        {
            return 1f / (float)TextureAtlasSizeInBlocks;
        }
    }

    public static readonly Vector3[] vertices = new Vector3[] // the vertices of the prototypical block (essentially coordinates of where each vertex lies on a 3d graph
    {
        new Vector3(0.0f, 0.0f, 0.0f), // 0
        new Vector3(1.0f, 0.0f, 0.0f), // 1
        new Vector3(1.0f, 1.0f, 0.0f), // 2
        new Vector3(0.0f, 1.0f, 0.0f), // 3
        new Vector3(0.0f, 0.0f, 1.0f), // 4
        new Vector3(1.0f, 0.0f, 1.0f), // 5
        new Vector3(1.0f, 1.0f, 1.0f), // 6
        new Vector3(0.0f, 1.0f, 1.0f) // 7
    };

    public static readonly Vector3[] faceCheckOffsets = new Vector3[] // offset for areas of space next to each block face. ex. checking faceCheckOffsets[0] checks the coordinates of the block behind the current block
    {
        new Vector3(0.0f, 0.0f, -1.0f), // Look behind
        new Vector3(0.0f, 0.0f, 1.0f), // Look in front
        new Vector3(0.0f, 1.0f, 0.0f), // Look above
        new Vector3(0.0f, -1.0f, 0.0f), // Look below
        new Vector3(-1.0f, 0.0f, 0.0f), // Look left
        new Vector3(1.0f, 0.0f, 0.0f) // Look right
    };

    public static readonly int[,] triangles = new int[,] // triangles of each block. 6 faces, 4 tris each. (4 becomes 6 in Chunk.cs; the middle 2 are removed because they repeat and create unnecessary verts) must go in clockwise order; if counterclockwise, face will show up facing opposite of desired direction (for example, look at bottom face, which is counter-clockwise). need to look at a block visually with vertices numbered to fully understand
    {
        // Order of block faces: Back, Front, Top, Bottom, Left, Right
        { 0, 3, 1, 2 }, // Back face
        { 5, 6, 4, 7 }, // Front face
        { 3, 7, 2, 6 }, // Top face
        { 1, 5, 0, 4 }, // Bottom face
        { 4, 7, 0, 3 }, // Left face
        { 1, 2, 5, 6 } // Right face
    };

    public static readonly Vector2[] uvs = new Vector2[]
    {
        new Vector2(0.0f, 0.0f),
        new Vector2(0.0f, 1.0f),
        new Vector2(1.0f, 0.0f),
        new Vector2(1.0f, 1.0f)
    };
}
