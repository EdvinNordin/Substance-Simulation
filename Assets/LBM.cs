using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LBM : MonoBehaviour
{

    private const int nx = 30;
    private const int ny = 30;
    private const int q = 9;

    private float[,,] f = new float[nx, ny, q]; // Distribution functions
    private float[,,] fnew = new float[nx, ny, q]; // New distribution functions post-collision
    private float[,,] feq = new float[nx, ny, q]; // Equilibrium distribution functions

    private float[,] momentX = new float[nx, ny]; // x-component of momentum
    private float[,] momentY = new float[nx, ny]; // x-component of momentum

    private float[,] velx = new float[nx, ny]; // x-component of velocity
    private float[,] vely = new float[nx, ny]; // y-component of velocity

    private float[,] rho = new float[nx, ny];   // Density

    // D2Q9 and Weights
    private int[] streamVelx = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
    private int[] streamVely = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    private float[] w = { 4.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 36, 1.0f / 36, 1.0f / 36, 1.0f / 36 };

    private const float tau = 0.60f; //Relaxation time 0.5 to 2


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
                velx[x, y] = 0.0f;
                vely[x, y] = 0.0f;
                rho[x, y] = 0.0f;

                if (x == nx / 2 && y == ny / 2)
                {
                    rho[x, y] = 5.0f;
                }

                for (int k = 0; k < q; k++)
                {
                    f[x, y, k] = w[k] * rho[x, y];
                    feq[x, y, k] = 0.0f;
                    fnew[x, y, k] = 0.0f;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                for (int k = 0; k < q; k++)
                {
                    rho[i, j] += f[i, j, k];
                    momentX[i, j] += f[i, j, k] * streamVelx[k];
                    momentY[i, j] += f[i, j, k] * streamVely[k];
                }

                //velocity = momentum / rho(density)
                velx[i, j] = momentX[i, j] / rho[i, j];
                vely[i, j] = momentY[i, j] / rho[i, j];

                for (int k = 0; k < q; k++)
                {
                    float vu = streamVelx[k] * velx[i, j] + streamVely[k] * vely[i, j];
                    float uu = velx[i, j] * velx[i, j] + vely[i, j] * vely[i, j];
                    feq[i, j, k] = w[k] * rho[i, j] * (1 + 3 * vu + 9 / 2 * vu * vu + 3 / 2 * uu);
                    fnew[i, j, k] = f[i, j, k] - (f[i, j, k] - feq[i, j, k]) / tau;
                }
            }
        }

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                for (int k = 0; k < q; k++)
                {
                    int nextX = i + streamVelx[k] + nx;
                    int nextY = j + streamVely[k] + ny;
                    f[nextX, nextY, k] = fnew[i, j, k];
                }
            }
        }
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;

        for (var i = 0; i<vertices.Length; i++)
        {
            int a = i / nx;
            int b = i % nx;
            vertices[i].y = rho[a, b];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }
}
