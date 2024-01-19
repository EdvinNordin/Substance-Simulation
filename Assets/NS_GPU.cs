using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NS_GPU : MonoBehaviour
{
    public ComputeShader computeShader;
    private CommandBuffer cmd;
    public RenderTexture velXnext;
    public RenderTexture velYnext;
    public RenderTexture velXprev;
    public RenderTexture velYprev;
    public RenderTexture input;
    public RenderTexture output;

     Texture2D tempTex;
     Texture2D tempTex2;

    private int kernelHandle;

    // Simulation parameters
    static int nx = 20; // Number of grid cells in the x-direction
    static int ny = 20; // Number of grid cells in the y-direction
    
    float[] inputArray;
    float[] inputArray2;

    // Start is called before the first frame update
    void Start()
    {
        inputArray = new float[nx*ny];
        inputArray2 = new float[nx*ny];

        kernelHandle = computeShader.FindKernel("navierStokes");

        tempTex = new Texture2D(nx, ny, TextureFormat.RFloat, false);
        tempTex2 = new Texture2D(nx, ny, TextureFormat.RFloat, false);


        for (int i = 0; i < nx*ny; i++)
        {
            inputArray[i] = 0.0f;
            inputArray2[i] = 0.0f;

            if(i > 230 && i < 235)
            {
                inputArray[i] = 0.9f;
            }      
        }

        
        tempTex.SetPixelData(inputArray, 0, 0);
        tempTex2.SetPixelData(inputArray2, 0, 0);
        
        tempTex.Apply();
        tempTex2.Apply();


        velXnext = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);
        velYnext = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);
        velXprev = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);
        velYprev = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);
        input = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);
        output = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);

        velXnext.enableRandomWrite = true;
        velYnext.enableRandomWrite = true;
        velXprev.enableRandomWrite = true;
        velYprev.enableRandomWrite = true;
        input.enableRandomWrite = true;
        output.enableRandomWrite = true;

        velXnext.Create();
        velYnext.Create();
        velXprev.Create();
        velYprev.Create();
        input.Create();
        output.Create();

        Graphics.Blit(tempTex, velXnext);
        Graphics.Blit(tempTex, velYnext);
        Graphics.Blit(tempTex, velXprev);
        Graphics.Blit(tempTex, velYprev);
        Graphics.Blit(tempTex2, input);
        Graphics.Blit(tempTex, output);

        computeShader.SetTexture(kernelHandle, "velXnext", velXnext);
        computeShader.SetTexture(kernelHandle, "velYnext", velYnext);
        computeShader.SetTexture(kernelHandle, "velXprev", velXprev);
        computeShader.SetTexture(kernelHandle, "velYprev", velYprev);
        computeShader.SetTexture(kernelHandle, "InputTex", input);
        computeShader.SetTexture(kernelHandle, "OutputTex", output);

        computeShader.SetInt("maxWidth", nx);
        computeShader.SetInt("maxHeight", ny);

        
        //computeShader.Dispatch(kernelHandle, nx/10, ny/10, 1);
        //Camera.main.targetTexture = output;
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.Dispatch(kernelHandle, 20, 20, 1);
        //Graphics.Blit(output, rendTex);
        //outputArray.GetData(output);
        //velBuffer.GetData(v);
        //accBuffer.GetData(a);

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        
        meshRenderer.material.mainTexture = output;

        //Debug.Log(sizeof(float));

       /* Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;


        for (int i = 0; i < nx * ny; i++)
        {
            vertices[i].y = outputArray[i];
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();*/
    }

    

}