using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTransparency : MonoBehaviour
{
    Material mat;

    // Start is called before the first frame update
    void Start()
    {
        mat = GetComponent<MeshRenderer>().material;

        mat.color = new Color(0f, 0f, 0f, 0.1f);
    }
}
