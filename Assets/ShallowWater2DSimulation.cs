using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShallowWater2DSimulation : MonoBehaviour
{


    // Simulation parameters
    static int nx = 30 + 1; // Number of grid cells in the x-direction
    static int ny = 30 + 1; // Number of grid cells in the y-direction
    float dt = 0.005f; // Time step
    float dx = 1f; // Spatial step in the x-direction
    float dy = 1f; // Spatial step in the y-direction
    float g = 9.81f; // Gravity
    float k = 1.0f; // Voscous drag
    float[,] h;
    float[,] u;
    float[,] v;
    float[,] new_h;
    float[,] new_u;
    float[,] new_v;


    // Start is called before the first frame update
    void Start()
    {

        h = new float[nx, ny]; // Water height
        u = new float[nx, ny]; // x-velocity component
        v = new float[nx, ny]; // y-velocity component

        // Initialize initial conditions
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                if (i == 5 & j == 5)
                {
                    h[i, j] = 100.0f;
                }
                else
                {
                    h[i, j] = 0.0f;
                }

                u[i, j] = 0.0f;
                v[i, j] = 0.0f;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Time integration loop (Euler method for simplicity)
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                var info = ReflectionBoundary(i, j);
                h[i, j] = info.Item1;
                u[i, j] = info.Item2;
                v[i, j] = info.Item3;
            }
        }

        // Output or visualization code can be added here
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        for (var i = 0; i < vertices.Length; i++)
        {
            int a = i / nx;
            int b = i % nx;
            vertices[i].y = h[a, b];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }


    (float, float, float) ReflectionBoundary(int i, int j)
    {

        float[,] dhdx = GradientX(h, dx);
        float[,] dhdy = GradientY(h, dy);
        float[,] dudx = GradientX(u, dx);
        float[,] dvdy = GradientY(v, dy);

        if (i == 0 && j == 0)
        {
            h[i, j] = h[i + 1, j + 1];
            u[i, j] = -u[i + 1, j + 1];
            v[i, j] = -v[i + 1, j + 1];
        }
        else if (i == nx && j == 0)
        {
            h[i, j] = h[i - 1, j + 1];
            u[i, j] = -u[i - 1, j + 1];
            v[i, j] = -v[i - 1, j + 1];
        }
        else if (i == 0 && j == ny)
        {
            h[i, j] = h[i + 1, j - 1];
            u[i, j] = -u[i + 1, j - 1];
            v[i, j] = -v[i + 1, j - 1];
        }
        else if (i == nx && j == ny)
        {
            h[i, j] = h[i - 1, j - 1];
            u[i, j] = -u[i - 1, j - 1];
            v[i, j] = -v[i - 1, j - 1];
        }
        else if (i == nx)
        {
            h[i, j] = h[i - 1, j];
            u[i, j] = -u[i - 1, j];
            v[i, j] = v[i - 1, j];
        }
        else if (j == ny)
        {
            h[i, j] = h[i, j - 1];
            u[i, j] = u[i, j - 1];
            v[i, j] = -v[i, j - 1];
        }
        else if (i == 0)
        {
            h[i, j] = h[i + 1, j];
            u[i, j] = -u[i + 1, j];
            v[i, j] = v[i + 1, j];
        }
        else if (j == 0)
        {
            h[i, j] = h[i, j + 1];
            u[i, j] = u[i, j + 1];
            v[i, j] = -v[i, j + 1];
        }
        else
        {

            // Calculate values of h, u, and v                   
            h[i, j] -= dt * (dudx[i, j] + dvdy[i, j]);
            u[i, j] = (-dt - (g * dhdx[i, j]))/ k;
            v[i, j] = (-dt - (g * dhdy[i, j]))/ k;
        }

        var returning = (h[i, j], u[i, j], v[i, j]);

        return returning;
    }

    // Function to calculate the x-gradient of a 2D array
    static float[,] GradientX(float[,] array, float dx)
    {
        int Nx = array.GetLength(0);
        int Ny = array.GetLength(1);
        float[,] result = new float[Nx, Ny];

        for (int i = 1; i < Nx - 1; i++)
        {
            for (int j = 1; j < Ny - 1; j++)
            {
                int iNext = (i + 1);
                int iPrev = (i - 1);
                result[i, j] = (array[iNext, j] - array[iPrev, j]) / (2 * dx);
                //result[i, j] = ((array[iNext, j] - array[i, j]) - (array[iPrev, j] - array[i, j])) / dx;
            }
        }

        return result;
    }

    // Function to calculate the y-gradient of a 2D array
    static float[,] GradientY(float[,] array, float dy)
    {
        int Nx = array.GetLength(0);
        int Ny = array.GetLength(1);
        float[,] result = new float[Nx, Ny];

        for (int i = 1; i < Nx - 1; i++)
        {
            for (int j = 1; j < Ny - 1; j++)
            {
                int jNext = (j + 1);
                int jPrev = (j - 1);
                result[i, j] = (array[i, jNext] - array[i, jPrev]) / (2 * dy);
                //result[i, j] = ((array[i, jNext] - array[i, j]) - (array[i, jPrev] - array[i, j])) / dy;
            }
        }

        return result;
    }

}





