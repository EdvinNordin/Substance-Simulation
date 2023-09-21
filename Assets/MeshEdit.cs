using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshEdit : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        /*for (var i = 0; i < vertices.Length; i++)
        {
            vertices[i] += normals[i] * Mathf.Sin(Time.time);
        }*/

        vertices[2] = new Vector3(1,1,1) * Mathf.Sin(Time.time);

        mesh.vertices = vertices;
    }
}
