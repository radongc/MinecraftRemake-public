using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Vector3ExtensionMethods
{
    /// <summary>
    /// Floors the entire Vector3 to an integer. Useful for voxel-based games, where world block positon values are never in-between any two whole numbers.
    /// </summary>
    /// <param name="vector"></param>
    /// <returns></returns>
    public static Vector3 FloorToInt(this Vector3 vector)
    {
        int xPos = Mathf.FloorToInt(vector.x);
        int yPos = Mathf.FloorToInt(vector.y);
        int zPos = Mathf.FloorToInt(vector.z);

        return new Vector3(xPos, yPos, zPos);
    }

    public static float FloorToInt(this float num)
    {
        int floorNum = Mathf.FloorToInt(num);

        return floorNum;
    }
}
