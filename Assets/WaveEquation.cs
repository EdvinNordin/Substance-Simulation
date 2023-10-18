using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveEquation : MonoBehaviour
{
    public ComputeShader computeShader;

    public RenderTexture renderTexture;

    // Simulation parameters
    static int nx = 30 + 1; // Number of grid cells in the x-direction
    static int ny = 30 + 1; // Number of grid cells in the y-direction
    public float dt = 0.001f; // Time step
    float g = 9.81f; // Gravity
    float c = 0.0f; // Speed of sound in the medium
    float s = 1.0f; // Step size
    public float k = 0.0005f; // Viscosity
    public float d = 3.0f; // Mean depth to the ocean floor
    float[,] h;
    float[,] a;
    float[,] v;


    // Start is called before the first frame update
    void Start()
    {
        c = Mathf.Sqrt(g * d);
        h = new float[nx, ny]; // Water height
        a = new float[nx, ny]; // x-velocity component
        v = new float[nx, ny]; // y-velocity component



        // Initialize initial conditions
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                /*if (i > 5 && i < 12 && j > 5 && j < 12)
                {
                    h[i, j] = -5.0f;
                }*/
                if (i == 8 && j == 13)
                {
                    h[i, j] = 0.0f;
                }                
                else
                {
                    h[i, j] = 0.0f;
                }
                a[i, j] = 0.0f;
                v[i, j] = 0.0f;

            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if(Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(ray, out hit))
                {
                //Debug.Log(GetClosestVertex(hit, triangles));
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider == null || meshCollider.sharedMesh == null)
                {
                    Debug.Log("nope");

                }
                else
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int a = vertexHit / nx;
                    int b = vertexHit % nx;
                    h[a, b] += 2.0f; 
                }
            }
        }
        

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny ; j++)
            {
                if (i == 0 && j == 0)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i + 1, j]  + h[i, j + 1] - (2.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (i == nx - 1 && j == 0)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i - 1, j] + h[i, j + 1] - (2.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (i == 0 && j == ny - 1)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i + 1, j] + h[i, j - 1] - (2.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (i == nx - 1 && j == ny - 1)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i - 1, j] + h[i, j - 1] - (2.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (i == nx - 1)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i - 1, j] + h[i, j - 1] + h[i, j + 1] - (3.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (j == ny - 1)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i - 1, j] + h[i + 1, j] + h[i, j - 1] - (3.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (i == 0)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i + 1, j] + h[i, j - 1] + h[i, j + 1] - (3.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else if (j == 0)
                {
                    a[i, j] = (c * c) / (s * s) * (h[i - 1, j] + h[i + 1, j]  + h[i, j + 1] - (3.0f * h[i, j])) - k * v[i, j];
                    v[i, j] = v[i, j] + dt * a[i, j];
                }
                else
                {
                    a[i, j] = (c * c) / (s * s) * (h[i - 1, j] + h[i + 1, j] + h[i, j - 1] + h[i, j + 1] - (4.0f * h[i, j])) - k*v[i,j];
                    v[i, j] = v[i, j] + dt * a[i, j];

                }
            }
        }

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                /*if (i == nx - 1)
                {
                    h[i, j] = 0.0f;
                }
                else if (j == ny - 1)
                {
                    h[i, j] = 0.0f;
                }
                else if (i == 0)
                {
                    h[i, j] = 0.0f; ;
                }
                else if (j == 0)
                {
                    h[i, j] = 0.0f;
                }
                else
                {*/
                h[i, j] = (h[i, j] + dt * v[i, j]);
            //}
            }
        }

        // Output or visualization code can be added here
        for (var i = 0; i < vertices.Length; i++)
        {
            int a = i / nx;
            int b = i % nx;
            vertices[i].y = h[a, b];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    public static int GetClosestVertex(RaycastHit aHit, int[] aTriangles)
    {
        var b = aHit.barycentricCoordinate;
        int index = aHit.triangleIndex * 3;
        if (aTriangles == null || index < 0 || index + 2 >= aTriangles.Length)
            return -1;

        if (b.x > b.y)
        {
            if (b.x > b.z)
                return aTriangles[index]; // x
            else
                return aTriangles[index + 2]; // z
        }
        else if (b.y > b.z)
            return aTriangles[index + 1]; // y
        else
            return aTriangles[index + 2]; // z
    }

}
