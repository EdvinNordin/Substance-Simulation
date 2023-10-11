using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script that generates a plane with width and depth.
/// </summary>
public class PlaneGenerator : MonoBehaviour
{
    // Reference to the mesh filter.
    private MeshFilter meshFilter;

    // Width of our quad.
    [SerializeField]
    [Range(1, 100)]
    private int width = 2;

    // Depth of our plane.
    [SerializeField]
    [Range(1, 100)]
    private int depth = 2;

    /// <summary>
    /// Unity method called on first frame.
    /// </summary>
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GeneratePlane();

    }

    /// <summary>
    /// Method which generates plane.
    /// </summary>
    private void GeneratePlane()
    {
        // Creating a mesh object.
        Mesh mesh = new Mesh();

        // Defining vertices.
        Vector3[] vertices = new Vector3[(width + 1) * (depth + 1)];

        int i = 0;
        for (int d = 0; d <= depth; d++)
        {
            for (int w = 0; w <= width; w++)
            {
                vertices[i] = new Vector3(w, 0, d) - new Vector3(width / 2f, 0, depth / 2f);
                i++;
            }
        }

        // Defining triangles.
        int[] triangles = new int[width * depth * 2 * 3]; // 2 - polygon per quad, 3 - corners per polygon

        for (int d = 0; d < depth; d++)
        {
            for (int w = 0; w < width; w++)
            {
                // quad triangles index.
                int ti = (d * (width) + w) * 6; // 6 - polygons per quad * corners per polygon

                // First tringle
                triangles[ti] = (d * (width + 1)) + w;
                triangles[ti + 1] = ((d + 1) * (width + 1)) + w;
                triangles[ti + 2] = ((d + 1) * (width + 1)) + w + 1;

                // Second triangle
                triangles[ti + 3] = (d * (width + 1)) + w;
                triangles[ti + 4] = ((d + 1) * (width + 1)) + w + 1;
                triangles[ti + 5] = (d * (width + 1)) + w + 1;
            }
        }

        // Defining UV.
        Vector2[] uv = new Vector2[(width + 1) * (depth + 1)];

        i = 0;
        for (int d = 0; d <= depth; d++)
        {
            for (int w = 0; w <= width; w++)
            {
                uv[i] = new Vector2(w / (float)width, d / (float)depth);
                i++;
            }
        }

        // Assigning vertices, triangles and UV to the mesh.
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;

        // Assigning mesh to mesh filter to display it.
        meshFilter.mesh = mesh;


        MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = mesh;
    }
}
