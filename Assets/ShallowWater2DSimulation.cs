using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShallowWater2DSimulation : MonoBehaviour
{


    // Simulation parameters
    static int nx = 30+1; // Number of grid cells in the x-direction
    static int ny = 30+1; // Number of grid cells in the y-direction
    float dt = 0.001f; // Time step
    float dx = 1f; // Spatial step in the x-direction
    float dy = 1f; // Spatial step in the y-direction
    float g = 9.81f; // Gravity
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
        new_h = new float[nx, ny];
        new_u = new float[nx, ny];
        new_v = new float[nx, ny];
        // Initialize initial conditions (e.g., a water wave in the center)
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                /*float x = (i - nx / 2) * dx;
                float y = (j - ny / 2) * dy;
                float r = Mathf.Sqrt(x * x + y * y);
                if (r < 1.0f)
                {
                    h[i, j] = 1.0f;
                }
                else
                {
                    h[i, j] = 0.5f;
                }*/

                if (i == 15 & j == 15)
                {
                    h[i, j] = 15.0f;
                }
                else
                {
                    h[i, j] = 1.0f;
                }

                u[i, j] = 0.0f;
                v[i, j] = 0.0f;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        float[,] dhdx = GradientX(h, dx);
        float[,] dhdy = GradientY(h, dy);
        float[,] dudx = GradientX(u, dx);
        float[,] dvdy = GradientY(v, dy);
        // Time integration loop (Euler method for simplicity)
        //for (int t = 0; t < 100; t++)
        //{

        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                if (i == nx - 1)
                {
                    // Reflect the wave at the right boundary for h
                    new_h[i, j] = h[i - 1, j];
                    // Reflect velocities for u and v (you can also apply a zero velocity condition)
                    new_u[i, j] = -u[i - 1, j];
                    new_v[i, j] = v[i - 1, j];
                }

                else if (j == ny - 1)
                {
                    // Reflect the wave at the right boundary for h
                    new_h[i, j] = h[i, j - 1];
                    // Reflect velocities for u and v (you can also apply a zero velocity condition)
                    new_u[i, j] = u[i, j - 1];
                    new_v[i, j] = -v[i, j - 1];
                }
                else if (i == 1)
                {
                    // Reflect the wave at the right boundary for h
                    new_h[i, j] = h[1, j];
                    // Reflect velocities for u and v (you can also apply a zero velocity condition)
                    new_u[i, j] = -u[1, j];
                    new_v[i, j] = v[1, j];
                }
                else if (j == 1)
                {
                    // Reflect the wave at the right boundary for h
                    new_h[i, j] = h[i, 1];
                    // Reflect velocities for u and v (you can also apply a zero velocity condition)
                    new_u[i, j] = u[i, 1];
                    new_v[i, j] = -v[i, 1];
                }

                else
                {

                    /* // Calculate new values of h, u, and v
                     new_h[i, j] = h[i, j] - (dt / dx) * (u[i + 1, j] - u[i, j]) - (dt / dy) * (v[i, j + 1] - v[i, j]) - (dt / dx) * (u[i-1, j] - u[i, j]) - (dt / dy) * (v[i, j - 1] - v[i, j]);
                     new_u[i, j] = u[i, j] - (dt / dx) * g * (h[i + 1, j] + h[i - 1, j]);//u[i, j] - (dt / dx) * ((u[i, j] * (u[i + 1, j] - u[i - 1, j]) + 0.5f * (h[i + 1, j] - h[i - 1, j])) / h[i, j]) - (dt / dy) * ((u[i, j] * (v[i, j + 1] - v[i, j - 1]) + 0.5f * (h[i, j + 1] - h[i, j - 1])) / h[i, j]) + g * dt * (h[i, j] - h[i - 1, j]) / dx;
                     new_v[i, j] = v[i, j] - (dt / dy) * g * (h[i, j + 1] + h[i, j - 1]);//* ((u[i + 1, j] * (v[i, j] - v[i - 1, j]) + 0.5f * (h[i + 1, j] - h[i - 1, j])) / h[i, j]) - (dt / dy) * ((v[i, j] * (v[i, j + 1] - v[i, j - 1]) + 0.5f * (h[i, j + 1] - h[i, j - 1])) / h[i, j]) + g * dt * (h[i, j] - h[i, j - 1]) / dy;
                 */
                    h[i, j] -= dt * 0.5f * (dudx[i, j] + dvdy[i, j]);
                    u[i, j] -= dt * (g * dhdx[i, j]);
                    v[i, j] -= dt * (g * dhdy[i, j]);
                }

            }
        }

            // Update arrays
            /*for (int i = 1; i < nx - 1; i++)
            {
                for (int j = 1; j < ny - 1; j++)
                {
                    h[i, j] = new_h[i, j];
                    u[i, j] = new_u[i, j];
                    v[i, j] = new_v[i, j];
                
                }
            }*/
        // Output or visualization code can be added here
        //}
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        for (var i = 1; i < vertices.Length-1; i++)
        {
            int a = i / nx;
            int b = i % nx;
            vertices[i].y = h[a, b];
        }

        mesh.vertices = vertices;
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
                int iNext = (i + 1) % Nx;
                int iPrev = (i - 1) % Nx;
                result[i, j] = ((array[iNext, j] - array[i, j]) - (array[iPrev, j] - array[i, j])) / dx;
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
            for (int j = 1; j < Ny-1; j++)
            {
                int jNext = (j + 1) % Ny;
                int jPrev = (j - 1) % Ny;
                result[i, j] = ((array[i, jNext] - array[i, j]) - (array[i, jPrev] - array[i, j])) / dy;
            }
        }

        return result;
    }
}





