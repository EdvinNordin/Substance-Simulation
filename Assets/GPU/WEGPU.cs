using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WEGPU : MonoBehaviour
{
    public ComputeShader computeShader;

    public ComputeBuffer heightBuffer;
    public ComputeBuffer velBuffer;
    public ComputeBuffer accBuffer;

    private int kernelHandle;

    // Simulation parameters
    static int nx = 20; // Number of grid cells in the x-direction
    static int ny = 20; // Number of grid cells in the y-direction
    float[] h;
    float[] a;
    float[] v;

    // Start is called before the first frame update
    void Start()
    {
        h = new float[nx*ny];
        v = new float[nx*ny];
        a = new float[nx*ny];

        kernelHandle = computeShader.FindKernel("CSMain");

        for (int i = 0; i < nx*ny; i++)
        {
            h[i] = 0.0f;
            v[i] = 0.0f;
            a[i] = 0.0f;   

            if(i == 247)
            {
                h[i] = 1.0f;
            }         
        }


        heightBuffer = new ComputeBuffer(nx * ny, sizeof(float));
        velBuffer = new ComputeBuffer(nx * ny, sizeof(float));
        accBuffer = new ComputeBuffer(nx * ny, sizeof(float));

        heightBuffer.SetData(h);
        velBuffer.SetData(v);
        accBuffer.SetData(a);

        computeShader.SetBuffer(kernelHandle, "heightBuffer", heightBuffer);
        computeShader.SetBuffer(kernelHandle, "velBuffer", velBuffer);
        computeShader.SetBuffer(kernelHandle, "accBuffer", accBuffer);

    }

    // Update is called once per frame
    void Update()
    {
        computeShader.Dispatch(kernelHandle, nx/10, ny/10, 1);

        heightBuffer.GetData(h);
        //velBuffer.GetData(v);
        //accBuffer.GetData(a);

        //Debug.Log(sizeof(float));

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;


        for (int i = 0; i < nx * ny; i++)
        {
            vertices[i].y = h[i];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void OnDisable()
    {
        heightBuffer.Release();
        velBuffer.Release();
        accBuffer.Release();
    }

}
