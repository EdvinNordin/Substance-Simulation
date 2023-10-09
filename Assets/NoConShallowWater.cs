using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoConShallowWater : MonoBehaviour
{


    // Simulation parameters
    static int nx = 30 + 1; // Number of grid cells in the x-direction
    static int ny = 30 + 1; // Number of grid cells in the y-direction
    float dt = 0.001f; // Time step
    float dx = 1.0f; // Spatial step in the x-direction
    float dy = 1.0f; // Spatial step in the y-direction
    float g = 9.81f; // Gravity
    float k = 0.5f; // Viscous drag
    float[,] h;
    float[,] newh;
    float[,] newu;
    float[,] newv;
    float[,] u;
    float[,] v;
    float[,] hu;
    float[,] hv;


    // Start is called before the first frame update
    void Start()
    {

        h = new float[nx, ny]; // Water height
        newh = new float[nx, ny];
        newu = new float[nx, ny];
        newv = new float[nx, ny];
        u = new float[nx, ny]; // x-velocity component
        v = new float[nx, ny]; // y-velocity component
        hu = new float[nx, ny];
        hv = new float[nx, ny];

        // Initialize initial conditions
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                if (i > 8 && i < 12 && j > 8 && j < 12)
                {
                    h[i, j] = 15.5f;
                }
                else
                {
                    h[i, j] = 15.01f;
                }
                u[i, j] = 0.0f;
                v[i, j] = 0.0f;

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        float maxU = 0.0f;
        float maxV = 0.0f;
        float c = 0.0f;

        //RungeKutta4();
        ExplicitEuler();


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

    void computeRHS(float[,] h, float[,] u, float[,] v, out float[,] h_rhs, out float[,] u_rhs, out float[,] v_rhs)
    {
        // Initialize h_rhs, u_rhs, and v_rhs arrays here
        h_rhs = new float[nx, ny];
        u_rhs = new float[nx, ny];
        v_rhs = new float[nx, ny];

        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                /*h_rhs[i, j] = ((h[i + 1, j] * u[i + 1, j] - h[i - 1, j] * u[i - 1, j]) / (2.0f * dx)
                              + (h[i, j + 1] * v[i, j + 1] - h[i, j - 1] * v[i, j - 1]) / (2.0f * dy));

                u_rhs[i, j] = (u[i, j] * (u[i + 1, j] - u[i - 1, j]) / (2.0f * dx)
                              + v[i, j] * (u[i + 1, j] - u[i - 1, j]) / (2.0f * dy)
                              + g * (h[i + 1, j] - h[i - 1, j]) / (2.0f * dx));

                v_rhs[i, j] = (u[i, j] * (v[i, j + 1] - v[i, j - 1]) / (2.0f * dx)
                              + v[i, j] * (v[i, j + 1] - v[i, j - 1]) / (2.0f * dy)
                              + g * (h[i, j + 1] - h[i, j - 1]) / (2.0f * dy));*/

                if (u[i, j] > 0)
                {
                    h_rhs[i, j] = -(h[i, j] * u[i, j] - h[i - 1, j] * u[i - 1, j]) / (dx);
                }
                else
                {
                    h_rhs[i, j] = -(h[i + 1, j] * u[i + 1, j] - h[i, j] * u[i, j]) / (dx);
                }
                if (v[i, j] > 0)
                {
                    h_rhs[i, j] -= (h[i, j] * v[i, j] - h[i, j - 1] * v[i, j - 1]) / (dy);
                }
                else
                {
                    h_rhs[i, j] -= (h[i, j + 1] * v[i, j + 1] - h[i, j] * v[i, j]) / (dy);
                }

                if (u[i, j] > 0)
                {
                    u_rhs[i, j] = -(u[i, j] * (u[i, j] - u[i - 1, j]) / (dx)
                              + v[i, j] * (u[i, j] - u[i - 1, j]) / (dy)
                              + g * (h[i + 1, j] - h[i - 1, j]) / (2.0f * dx));
                }
                else
                {
                    u_rhs[i, j] = -(u[i, j] * (u[i + 1, j] - u[i, j]) / (dx)
                              + v[i, j] * (u[i + 1, j] - u[i, j]) / ( dy)
                              + g * (h[i + 1, j] - h[i - 1, j]) / (2.0f * dx));
                }
                u_rhs[i, j] += k * ((u[i + 1, j] - 2 * u[i, j] + u[i - 1, j]) / (dx * dx)
                   + (u[i, j + 1] - 2 * u[i, j] + u[i, j - 1]) / (dy * dy));

                if (v[i, j] > 0)
                {
                    v_rhs[i, j] = -(u[i, j] * (v[i, j] - v[i, j - 1]) / (dx)
                                  + v[i, j] * (v[i,j] - v[i, j - 1]) / ( dy)
                                  + g * (h[i, j + 1] - h[i, j - 1]) / (2.0f * dy));
                }
                else
                {
                    v_rhs[i, j] = -(u[i, j] * (v[i, j + 1] - v[i, j]) / (dx)
                                  + v[i, j] * (v[i, j + 1] - v[i, j]) / (dy)
                                  + g * (h[i, j + 1] - h[i, j - 1]) / (2.0f * dy));
                }
                v_rhs[i, j] += k * ((v[i + 1, j] - 2 * v[i, j] + v[i - 1, j]) / (dx * dx)
                   + (v[i, j + 1] - 2 * v[i, j] + v[i, j - 1]) / (dy * dy));



            }
        }

    }

    void RungeKutta4()
    {

        float maxU = 0.0f;
        float maxV = 0.0f;
        float c = 0.0f;

        float[,] h1 = new float[nx, ny];
        float[,] u1 = new float[nx, ny];
        float[,] v1 = new float[nx, ny];

        float[,] h2 = new float[nx, ny];
        float[,] u2 = new float[nx, ny];
        float[,] v2 = new float[nx, ny];

        float[,] h3 = new float[nx, ny];
        float[,] u3 = new float[nx, ny];
        float[,] v3 = new float[nx, ny];

        float[,] h4 = new float[nx, ny];
        float[,] u4 = new float[nx, ny];
        float[,] v4 = new float[nx, ny];
        // ... similarly for h2, u2, v2, h3, u3, v3, h4, u4, v4

        float[,] h_temp = new float[nx, ny];
        float[,] u_temp = new float[nx, ny];
        float[,] v_temp = new float[nx, ny];


        // Initialization of these arrays would be necessary.

        // 1st evaluation
        computeRHS(h, u, v, out h1, out u1, out v1);

        // 2nd evaluation
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                h_temp[i, j] = h[i, j] + 0.5f * dt * h1[i, j];
                u_temp[i, j] = u[i, j] + 0.5f * dt * u1[i, j];
                v_temp[i, j] = v[i, j] + 0.5f * dt * v1[i, j];
            }
        }
        computeRHS(h_temp, u_temp, v_temp, out h2, out u2, out v2);

        // 3rd evaluation
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                h_temp[i, j] = h[i, j] + 0.5f * dt * h2[i, j];
                u_temp[i, j] = u[i, j] + 0.5f * dt * u2[i, j];
                v_temp[i, j] = v[i, j] + 0.5f * dt * v2[i, j];
            }
        }
        computeRHS(h_temp, u_temp, v_temp, out h3, out u3, out v3);

        // 4th evaluation
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                h_temp[i, j] = h[i, j] + dt * h3[i, j];
                u_temp[i, j] = u[i, j] + dt * u3[i, j];
                v_temp[i, j] = v[i, j] + dt * v3[i, j];
            }
        }
        computeRHS(h_temp, u_temp, v_temp, out h4, out u4, out v4);

        // Combine evaluations for new values
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                if (i == nx - 1)
                {
                    h[i, j] = h[i - 1, j];
                    u[i, j] = u[i - 1, j];
                    v[i, j] = v[i - 1, j];
                }
                else if (j == ny - 1)
                {
                    h[i, j] = h[i, j - 1];
                    u[i, j] = u[i, j - 1];
                    v[i, j] = v[i, j - 1];
                }
                else if (i == 0)
                {
                    h[i, j] = h[i + 1, j];
                    u[i, j] = u[i + 1, j];
                    v[i, j] = v[i + 1, j];
                }
                else if (j == 0)
                {
                    h[i, j] = h[i, j + 1];
                    u[i, j] = u[i, j + 1];
                    v[i, j] = v[i, j + 1];
                }
                else
                {
                    h[i, j] += dt / 6.0f * (h1[i, j] + 2.0f * h2[i, j] + 2.0f * h3[i, j] + h4[i, j]);
                    u[i, j] += dt / 6.0f * (u1[i, j] + 2.0f * u2[i, j] + 2.0f * u3[i, j] + u4[i, j]);
                    v[i, j] += dt / 6.0f * (v1[i, j] + 2.0f * v2[i, j] + 2.0f * v3[i, j] + v4[i, j]);
                }

                maxU = Mathf.Max(maxU, Mathf.Abs(u[i, j]));
                maxV = Mathf.Max(maxV, Mathf.Abs(v[i, j]));
            }
        }
        c = (maxU * dt / dx) + (maxV * dt / dy);
        if (c >= 1.0f)
        {
            Debug.Log(c);
            Debug.Log(maxU);
            Debug.Log(maxV);

            //Debug.Break();
        }
    }

    void ExplicitEuler()
    {

        float maxU = 0.0f;
        float maxV = 0.0f;
        float c = 0.0f;

        // Time integration loop (Euler method for simplicity)
        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                if (i == nx - 1)
                {
                    newh[i, j] = newh[i - 1, j];
                    newu[i, j] = newu[i - 1, j];
                    newv[i, j] = newv[i - 1, j];
                }
                else if (j == ny - 1)
                {
                    newh[i, j] = newh[i, j - 1];
                    newu[i, j] = newu[i, j - 1];
                    newv[i, j] = newv[i, j - 1];
                }
                else if (i == 0)
                {
                    newh[i, j] = newh[i + 1, j];
                    newu[i, j] = newu[i + 1, j];
                    newv[i, j] = newv[i + 1, j];
                }
                else if (j == 0)
                {
                    newh[i, j] = newh[i, j + 1];
                    newu[i, j] = newu[i, j + 1];
                    newv[i, j] = newv[i, j + 1];
                }
                else
                {
                    if (h[i,j] <= 0.0f)
                    {
                        h[i, j] = 0.001f;
                    }
                    newh[i, j] = h[i, j] - dt * (gradientI(u, dx, i, j) + gradientJ(v, dy, i, j));

                    newu[i, j] = u[i, j] - dt * (g * h[i, j] * gradientI(h, dx, i, j) +
                                (u[i, j] / h[i, j]) * gradientI(u, dx, i, j) +
                                (v[i, j] / h[i, j]) * gradientJ(u, dy, i, j));

                    newv[i, j] = v[i, j] - dt * (g * h[i, j] * gradientJ(h, dy, i, j) +
                          (u[i, j] / h[i, j]) * gradientI(v, dx, i, j) +
                          (v[i, j] / h[i, j]) * gradientJ(v, dy, i, j));

                    /*newh[i, j] = h[i, j] - dt * ((h[i + 1, j] * u[i + 1, j] - h[i - 1, j] * u[i - 1, j]) / (2.0f * dx) + (h[i, j + 1] * v[i, j + 1] - h[i, j - 1] * v[i, j - 1]) / (2.0f * dy));
                    newu[i, j] = u[i, j] - dt * (u[i, j] * (u[i + 1, j] - u[i - 1, j]) / (2.0f * dx) + v[i, j] * (u[i + 1, j] - u[i - 1, j]) / (2.0f * dy) + g * (h[i + 1, j] - h[i - 1, j]) / (2.0f * dx));
                    newv[i, j] = v[i, j] - dt * (u[i, j] * (v[i, j + 1] - v[i, j - 1]) / (2.0f * dx) + v[i, j] * (v[i, j + 1] - v[i, j - 1]) / (2.0f * dy) + g * (h[i, j + 1] - h[i, j - 1]) / (2.0f * dy));
                    */
                }

                maxU = Mathf.Max(maxU, Mathf.Abs(u[i, j]));
                maxV = Mathf.Max(maxV, Mathf.Abs(v[i, j]));

            }
        }

        c = (maxU * dt / dx) + (maxV * dt / dy);
        if (c >= 1.0f)
        {
            Debug.Log(c);
            Debug.Log(maxU);
            Debug.Log(maxV);

            Debug.Break();
        }

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                h[i, j] = newh[i, j];
                u[i, j] = newu[i, j];
                v[i, j] = newv[i, j];
            }
        }
    }

    float gradientI(float[,] array, float delta, int i, int j)
    {
        return (array[i + 1, j] - array[i - 1, j]) / (2.0f * delta);
    }

    float gradientJ(float[,] array, float delta, int i, int j)
    {
        return (array[i, j + 1] - array[i, j - 1]) / (2.0f * delta);
    }
}