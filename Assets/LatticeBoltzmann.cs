using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LatticeBoltzmann : MonoBehaviour
{
    private const int nx = 30;
    private const int ny = 30;
    private const int ndir = 9;

    private float[,,] f = new float[nx, ny, ndir]; // Distribution functions
    private float[,,] feq = new float[nx, ny, ndir]; // Equilibrium distribution functions
    private float[,,] fnew = new float[nx, ny, ndir]; // New distribution functions post-collision
    private float[,] rho = new float[nx, ny];   // Density
    private float[,] ux = new float[nx, ny]; // x-component of velocity
    private float[,] uy = new float[nx, ny]; // y-component of velocity

    // D2Q9 and Weights
    private int[] ex = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
    private int[] ey = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    private float[] w = { 4.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 36, 1.0f / 36, 1.0f / 36, 1.0f / 36 };

    private const float tau = 0.60f; //Relaxation time

    float xPos;
    float yPos;

    float prevX;
    float prevY;


    // Start is called before the first frame update
    void Start()
    {
        prevX = 0.0f;
        prevY = 0.0f;

        for (int x = 0; x < nx; x++)
        {
            for (int y = 0; y < ny; y++)
            {
                if(x == 8 && y == 13 )
                {
                    for (int q = 0; q < 9; q++)
                    {
                        f[x, y, q] = 3.0f;
                    }
                }
                else
                {
                    for (int q = 0; q < 9; q++)
                    {
                        f[x, y, q] = 0.0f;
                    }
                    ux[x, y] = 0.0f;
                    uy[x, y] = 0.0f;

                }

                for (int k = 0; k < ndir; k++)
                {
                    f[x, y, k] = w[k] * rho[x, y];
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
       

        for (int x = 0; x < nx; x++)
        {
            for (int y = 0; y < ny; y++)
            {
                // Compute macroscopic variables
                float rhoTmp = 0;
                float uxTmp = 0;
                float uyTmp = 0;

                for (int k = 0; k < ndir; k++)
                {
                    rhoTmp += f[x, y, k];
                    uxTmp += f[x, y, k] * ex[k];
                    uyTmp += f[x, y, k] * ey[k];
                }

                rho[x, y] = rhoTmp;
                ux[x, y] = uxTmp / rhoTmp;
                uy[x, y] = uyTmp / rhoTmp;

                // Compute equilibrium distribution
                for (int k = 0; k < ndir; k++)
                {
                    float cu = ex[k] * ux[x, y] + ey[k] * uy[x, y];
                    float usqr = ux[x, y] * ux[x, y] + uy[x, y] * uy[x, y];
                    feq[x, y, k] = rho[x, y] * w[k] * (1 + 3 * cu + 9 * cu * cu / 2.0f - 3 * usqr / 2.0f);
                    fnew[x, y, k] = f[x, y, k] - (f[x, y, k] - feq[x, y, k]) / tau;
                }
            }
        }

        // Streaming step
        for (int x = 0; x < nx; x++)
        {
            for (int y = 0; y < ny; y++)
            {
                for (int k = 0; k < ndir; k++)
                {
                    int x_next = (x + ex[k] + nx) % nx;
                    int y_next = (y + ey[k] + ny) % ny;
                    f[x_next, y_next, k] = fnew[x, y, k];
                }
            }
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Input.GetMouseButton(0))
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
                    rho[a, b] += 1.0f;
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            if (Physics.Raycast(ray, out hit))
            {
                xPos = hit.point.x;
                yPos = hit.point.y;
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
                    ux[a, b] += 10.0f * (xPos - prevX);
                    uy[a, b] += 10.0f * (yPos - prevY);
                }
            }
        }

        for (var i = 0; i < vertices.Length; i++)
        {
            int a = i / nx;
            int b = i % nx;
            vertices[i].y = rho[a, b];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();


        prevX = xPos;
        prevY = yPos;
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
