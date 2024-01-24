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

     Texture2D zeroTex;
     Texture2D startingTex;

    private int kernelHandle;

    // Simulation parameters
    static int nx = 20; // Number of grid cells in the x-direction
    static int ny = 20; // Number of grid cells in the y-direction
    
    float[] zeroArray;
    float[] startingArray;

    // Start is called before the first frame update
    void Start()
    {
        zeroArray = new float[nx*ny];
        startingArray = new float[nx*ny];

        kernelHandle = computeShader.FindKernel("navierStokes");

        for (int i = 0; i < nx*ny; i++)
        {
            zeroArray[i] = 0.001f;
            startingArray[i] = 0.001f;

            if(i > 225 && i < 235)
            {
                startingArray[i] = 0.9f;
            }      
        }

        zeroTex = new Texture2D(nx, ny, TextureFormat.RFloat, false);
        startingTex = new Texture2D(nx, ny, TextureFormat.RFloat, false);
        
        zeroTex.SetPixelData(zeroArray, 0, 0);
        startingTex.SetPixelData(startingArray, 0, 0);
        
        zeroTex.Apply();
        startingTex.Apply();


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

        /*velXnext.Create();
        velYnext.Create();
        velXprev.Create();
        velYprev.Create();
        input.Create();
        output.Create();*/

        Graphics.Blit(zeroTex, velXnext);
        Graphics.Blit(zeroTex, velYnext);
        Graphics.Blit(zeroTex, velXprev);
        Graphics.Blit(zeroTex, velYprev);
        Graphics.Blit(zeroTex, input);
        Graphics.Blit(startingTex, output);

        computeShader.SetTexture(kernelHandle, "velXnext", velXnext);
        computeShader.SetTexture(kernelHandle, "velYnext", velYnext);
        computeShader.SetTexture(kernelHandle, "velXprev", velXprev);
        computeShader.SetTexture(kernelHandle, "velYprev", velYprev);
        computeShader.SetTexture(kernelHandle, "InputTex", input);
        computeShader.SetTexture(kernelHandle, "OutputTex", output);

        /*computeShader.SetInt("maxWidth", nx-1);
        computeShader.SetInt("maxHeight", ny-1);
*/
        
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = velXnext;
        //computeShader.Dispatch(kernelHandle, 20, 20, 1);
        //Camera.main.targetTexture = output;
    }

    // Update is called once per frame
    void Update()
    {
        
        //Debug.Break();
        computeShader.Dispatch(kernelHandle, 20, 20, 1);
        //Graphics.Blit(output, rendTex);
        //outputArray.GetData(output);
        //velBuffer.GetData(v);
        //accBuffer.GetData(a);

        

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