using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConSWE : MonoBehaviour
{


    // Simulation parameters
    static int nx = 30 + 1; // Number of grid cells in the x-direction
    static int ny = 30 + 1; // Number of grid cells in the y-direction
    float dt = 0.0001f; // Time step
    float dx = 1.0f; // Spatial step in the x-direction
    float dy = 1.0f; // Spatial step in the y-direction
    float g = 9.81f; // Gravity
    float k = 1.0f; // Viscous drag
    float[,] h;
    float[,] u;
    float[,] v;
    float[,] hu;
    float[,] hv;
    float[,] newh;
    float[,] newu;
    float[,] newv;


    // Start is called before the first frame update
    void Start()
    {

        h = new float[nx, ny]; // Water height
        u = new float[nx, ny]; // x-velocity component
        v = new float[nx, ny]; // y-velocity component
        hu = new float[nx, ny]; // y-velocity componen
        hv = new float[nx, ny]; // y-velocity componen
        newh = new float[nx, ny]; // y-velocity componen
        newu = new float[nx, ny]; // y-velocity componen
        newv = new float[nx, ny]; // y-velocity componen

        float[,] k1_h = new float[nx, ny];
        float[,] k1_hu = new float[nx, ny];
        float[,] k1_hv = new float[nx, ny];

        // Initialize initial conditions
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {

                if (i == 15 && j == 15)
                {
                    h[i, j] = 1.0f;
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
        float c = 0.0f; 
        

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {


                // Time integration loop (Euler method for simplicity)

                /*if (i == 0 && j == 0)
                {
                    h[i, j] = h[i + 1, j + 1];
                    u[i, j] = -u[i + 1, j + 1];
                    v[i, j] = -v[i + 1, j + 1];
                }
                else if (i == nx - 1 && j == 0)
                {
                    h[i, j] = h[i - 1, j + 1];
                    u[i, j] = -u[i - 1, j + 1];
                    v[i, j] = -v[i - 1, j + 1];
                }
                else if (i == 0 && j == ny - 1)
                {
                    h[i, j] = h[i + 1, j - 1];
                    u[i, j] = -u[i + 1, j - 1];
                    v[i, j] = -v[i + 1, j - 1];
                }
                else if (i == nx - 1 && j == ny - 1)
                {
                    h[i, j] = h[i - 1, j - 1];
                    u[i, j] = -u[i - 1, j - 1];
                    v[i, j] = -v[i - 1, j - 1];
                }
                else */
                if (i == nx - 1)
                {
                    h[i, j] = h[i - 1, j];
                    u[i, j] = -u[i - 1, j];
                    v[i, j] = v[i - 1, j];
                }
                else if (j == ny - 1)
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

                    // F fluxes
                    float F_h = h[i,j] * u[i, j];
                    float F_hu = h[i, j] * u[i, j] * u[i, j] + 0.5f * g * h[i, j] * h[i, j];
                    float F_hv = h[i, j] * u[i, j] * v[i, j];

                    float FL_h = h[i-1, j] * u[i - 1, j];
                    float FL_hu = h[i - 1, j] * u[i - 1, j] * u[i - 1, j] + 0.5f * g * h[i - 1, j] * h[i - 1, j];
                    float FL_hv = h[i - 1, j] * u[i - 1, j] * v[i - 1, j];

                    float FR_h = h[i + 1, j] * u[i + 1, j];
                    float FR_hu = h[i + 1, j] * h[i + 1, j] * u[i + 1, j] + 0.5f * g * h[i + 1, j] * h[i + 1, j];
                    float FR_hv = h[i + 1, j] * u[i + 1, j] * v[i + 1, j];//

                    // G fluxes
                    float G_h = h[i, j] * v[i, j];
                    float G_hu = h[i, j] * v[i, j] * u[i, j];
                    float G_hv = v[i, j] * h[i, j] * v[i, j] + 0.5f * g * h[i, j] * h[i, j];

                    float GL_h = h[i, j - 1] * v[i, j - 1];
                    float GL_hu = h[i, j - 1] * v[i, j - 1] * u[i, j - 1];
                    float GL_hv = v[i, j - 1] * h[i, j - 1] * v[i, j - 1] + 0.5f * g * h[i, j - 1] * h[i, j - 1];

                    float GR_h = h[i, j + 1] * v[i, j + 1];
                    float GR_hu = h[i, j + 1] * v[i, j + 1] * u[i, j + 1];//
                    float GR_hv = h[i, j + 1] * h[i, j + 1] * v[i, j + 1] + 0.5f * g * h[i, j + 1] * h[i, j + 1];

                    //euler method
                    
                    newh[i, j] = h[i, j] - dt * k * (((FR_h - F_h) / dx) + ((FL_h - F_h) / dx) + ((GR_h - G_h) / dy) + ((GL_h - G_h) / dy));

                    newu[i, j] = u[i, j] - dt * k * (((FR_hu - F_hu) / dx) + ((FL_hu - F_hu) / dx) + ((GR_hu - G_hu) / dy) + ((GL_hu - G_hu) / dy));

                    newv[i, j] = v[i, j] - dt * k * (((FR_hv - F_hv) / dx) + ((FL_hv - F_hv) / dx) + ((GR_hv - G_hv) / dy) + ((GL_hv - G_hv) / dy));
           

                }

            }
        }


        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                u[i, j] = newu[i, j];
                v[i, j] = newv[i, j];

                h[i, j] = newh[i, j];
            }
        }

                float CFLx = dt * c / dx;
        float CFLy = dt * c / dy;

        //Debug.Log("x: " + CFLx);
        //Debug.Log("y: " + CFLy);
        if (CFLx > 0.95f && CFLy > 0.95f)
        {
            Debug.Break();
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


    void ReflectionBoundary(int i, int j)
    {

         /*h[i, j] = h[i, j] - dt * (u[i + 1, j] - u[i, j] / dx) - (v[i, j + 1] + v[i, j] / dx);
            u[i, j] = u[i, j] - dt * g * (h[i + 1, j] - h[i, j] / dx);
            v[i, j] = v[i, j] - dt * g * (h[i, j + 1] - h[i, j] / dy);

            hu[i, j] = hu[i, j] + dt * ((FR_hu - FL_hu) / dx) + dt * ((GR_hu - GL_hu) / dy);
            hv[i, j] = hv[i, j] + dt * ((FR_hv - FL_hv) / dx) + dt * ((GR_hv - GL_hv) / dy);


            if (hu[i, j] > 1.0f)
            {
                hu[i, j] = 1.0f;
            }
            if (hu[i, j] < -1.0f)
            {
                hu[i, j] = -1.0f;
            }
            if (hv[i, j] > 1.0f)
            {
                hv[i, j] = 1.0f;
            }
            if (hv[i, j] < -1.0f)
            {
                hv[i, j] = -1.0f;
            


            u[i, j] = hu[i, j] / h[i, j];
            v[i, j] = hv[i, j] / h[i, j];}*/
            

        
    }
}