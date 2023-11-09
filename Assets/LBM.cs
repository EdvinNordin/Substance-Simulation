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

    private float[,] ux = new float[nx, ny]; // x-component of velocity
    private float[,] uy = new float[nx, ny]; // y-component of velocity

    private float[,] rho = new float[nx, ny];   // Density

    // D2Q9 and Weights
    private int[] ex = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
    private int[] ey = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    private float[] w = { 4.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 36, 1.0f / 36, 1.0f / 36, 1.0f / 36 };

    // Start is called before the first frame update
    void Start()
    {


        for (int x = 0; x < nx; x++)
        {
            for (int y = 0; y < ny; y++)
            {
                ux[x, y] = 0.0f;
                uy[x, y] = 0.0f;

                for (int k = 0; k < q; k++)
                {

                    f[x, y, k] = 0.0f; // w[k] * rho[x, y];
                    feq[x, y, k] = 0.0f;
                    fnew[x, y, k] = 0.0f
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
                    rho[i, j, k]
                }
            }
        }
                    for (int i = 0; i < ex; i++)
        {
            for (int j = 0; j < ey; j++)
            {
                for (int k = 0; k < q; k++)
                {
                    feq[i, j, k] = w[q] * rho;
                }
            }
        }
    }
}
