using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NS_GPU : MonoBehaviour
{
    public int TextureWidth = 5;
    public int TextureHeight = 5;

    public ComputeShader navierStokesShader;

    private int advectionKernel;
    private int diffusionKernel;
    private int projectionKernel;
    private int projection2Kernel;
    private int projection3Kernel;
    private int setBoundsXKernel;
    private int setBoundsYKernel;
    private int addValueKernel;
    private int addVelocityKernel;

    private RenderTexture densityTexture;
    private RenderTexture densityPrevTexture;
    private RenderTexture velocityTexture;
    private RenderTexture velocityPrevTexture;
    private RenderTexture velocityTempTexture;
    private RenderTexture pressureTexture;
    private RenderTexture pressurePrevTexture;
    private RenderTexture divergenceTexture;

    Vector2 mousePosition;
    Vector2 previousMousePosition;
    
     Texture2D zeroTex;
     Texture2D startingTex;
    void Start()
    {
        //InitializeTextures();
        mousePosition = Input.mousePosition;
        previousMousePosition = mousePosition;

        advectionKernel = navierStokesShader.FindKernel("Advection");
        diffusionKernel = navierStokesShader.FindKernel("Diffusion");
        projectionKernel = navierStokesShader.FindKernel("Projection");
        projection2Kernel = navierStokesShader.FindKernel("Projection2");
        projection3Kernel = navierStokesShader.FindKernel("Projection3");
        setBoundsXKernel = navierStokesShader.FindKernel("SetBoundsX");
        setBoundsYKernel = navierStokesShader.FindKernel("SetBoundsY");
        addValueKernel = navierStokesShader.FindKernel("AddValue");
        addVelocityKernel = navierStokesShader.FindKernel("AddVelocity");


        densityTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        densityTexture.enableRandomWrite = true;
        densityTexture.Create();

        densityPrevTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat); 
        densityPrevTexture.enableRandomWrite = true;
        densityPrevTexture.Create();

        velocityTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RGFloat);
        velocityTexture.enableRandomWrite = true;
        velocityTexture.Create();

        velocityPrevTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RGFloat); 
        velocityPrevTexture.enableRandomWrite = true;
        velocityPrevTexture.Create();

        velocityTempTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RGFloat);
        velocityTempTexture.enableRandomWrite = true;
        velocityTempTexture.Create();

        pressureTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        pressureTexture.enableRandomWrite = true;
        pressureTexture.Create();

        pressurePrevTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        pressurePrevTexture.enableRandomWrite = true;
        pressurePrevTexture.Create();

        divergenceTexture = new RenderTexture(TextureWidth, TextureHeight, 0, RenderTextureFormat.RFloat);
        divergenceTexture.enableRandomWrite = true;    
        divergenceTexture.Create();

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material.mainTexture = densityTexture;
        
    }

    void Update()
    {
        ChangeTexture();

        Diffuse(velocityPrevTexture, velocityTexture, true);
        Projection();
        Graphics.CopyTexture(velocityPrevTexture, velocityTexture); // Swap output to input
        Advect(velocityPrevTexture, velocityTexture, true);
        Projection();

        Diffuse(densityPrevTexture, densityTexture);
        Graphics.CopyTexture(densityTexture, densityPrevTexture);  // Swap output to input
        Advect(densityPrevTexture, densityTexture);

    }

    void ChangeTexture(){
        
        previousMousePosition = mousePosition;
        mousePosition = Input.mousePosition;

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;


        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(ray, out hit))
            {
                //Debug.Log(GetClosestVertex(hit, triangles));
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider == null || meshCollider.sharedMesh == null)
                {

                }
                else
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int x = vertexHit % TextureHeight;
                    int y = vertexHit / TextureWidth;
                    
                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    navierStokesShader.SetTexture(addValueKernel, "Out", densityTexture);
                    navierStokesShader.Dispatch(addValueKernel, TextureWidth, TextureHeight, 1);
                }
            }
        }
        if (Input.GetMouseButton(1))
        {
            if (Physics.Raycast(ray, out hit))
            {
                float xPos = hit.point.x;
                float yPos = hit.point.y;
                //Debug.Log(GetClosestVertex(hit, triangles));
                MeshCollider meshCollider = hit.collider as MeshCollider;
                
                if (meshCollider == null || meshCollider.sharedMesh == null)
                {

                }
                else
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int x = vertexHit % TextureHeight;
                    int y = vertexHit / TextureWidth;
                    
                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    navierStokesShader.SetFloat("xAmount", mousePosition.x - previousMousePosition.x);
                    navierStokesShader.SetFloat("yAmount", mousePosition.y - previousMousePosition.y);

                    navierStokesShader.SetTexture(addVelocityKernel, "Out", velocityTexture);
                    navierStokesShader.Dispatch(addVelocityKernel, TextureWidth, TextureHeight, 1);
                }
            }
        }
    }

    void Diffuse(RenderTexture inTexture, RenderTexture outTexture, bool setBounds = false)
    {
            navierStokesShader.SetTexture(diffusionKernel, "In", outTexture);
            navierStokesShader.SetTexture(diffusionKernel, "Out", inTexture);
            navierStokesShader.Dispatch(diffusionKernel, TextureWidth, TextureHeight, 1);
            navierStokesShader.SetTexture(diffusionKernel, "In", inTexture);
            navierStokesShader.SetTexture(diffusionKernel, "Out", outTexture);
            navierStokesShader.Dispatch(diffusionKernel,TextureWidth, TextureHeight, 1);
            if (setBounds) SetBounds(inTexture, outTexture);
    }

    void Advect(RenderTexture inTexture, RenderTexture outTexture, bool setBounds = false)
        {
            // Copy velocityInTexture to a temporary buffer so that we do not bind it to both
            // Velocity and XIn buffers at the same time, since that breaks simulation on Windows machines for some reason...
            Graphics.CopyTexture(velocityPrevTexture, velocityTempTexture);
        
            navierStokesShader.SetTexture(advectionKernel, "Velocity", velocityTempTexture);
            navierStokesShader.SetTexture(advectionKernel, "In", inTexture);
            navierStokesShader.SetTexture(advectionKernel, "Out", outTexture);
            navierStokesShader.Dispatch(advectionKernel, TextureWidth, TextureHeight, 1);
            if (setBounds) SetBounds(inTexture, outTexture);
        }

    void Projection()
    {
        // Projection Part 1
        navierStokesShader.SetTexture(projectionKernel, "Pressure", pressureTexture);
        navierStokesShader.SetTexture(projectionKernel, "Divergence", divergenceTexture);
        navierStokesShader.SetTexture(projectionKernel, "Velocity", velocityTexture);
        navierStokesShader.Dispatch(projectionKernel, TextureWidth, TextureHeight, 1);
        SetBounds(velocityPrevTexture, velocityTexture);
        
        // Projection Pt2
        //for (int k = 0; k < 10; k++)
        //{
        navierStokesShader.SetTexture(projection2Kernel, "Divergence", divergenceTexture);
        navierStokesShader.SetTexture(projection2Kernel, "PressurePrev", pressureTexture);
        navierStokesShader.SetTexture(projection2Kernel, "Pressure", pressurePrevTexture);
        navierStokesShader.Dispatch(projection2Kernel, TextureWidth, TextureHeight, 1);

        navierStokesShader.SetTexture(projection2Kernel, "PressurePrev", pressureTexture);
        navierStokesShader.SetTexture(projection2Kernel, "Pressure", pressurePrevTexture);
        navierStokesShader.Dispatch(projection2Kernel, TextureWidth, TextureHeight, 1);
        //}

        // Projection Pt3
        navierStokesShader.SetTexture(projection3Kernel, "Velocity", velocityTexture);
        navierStokesShader.SetTexture(projection3Kernel, "PressurePrev", pressureTexture);
        navierStokesShader.Dispatch(projection3Kernel, TextureWidth, TextureHeight, 1);
        SetBounds(velocityPrevTexture, velocityTexture);
    }

    private void SetBounds(RenderTexture inTexture, RenderTexture outTexture)
    { 
        navierStokesShader.SetTexture(setBoundsXKernel, "In", outTexture);
        navierStokesShader.SetTexture(setBoundsXKernel, "Out", inTexture);
        navierStokesShader.Dispatch(setBoundsXKernel, TextureWidth, 1, 1);
        navierStokesShader.SetTexture(setBoundsYKernel, "In", inTexture);
        navierStokesShader.SetTexture(setBoundsYKernel, "Out", outTexture);
        navierStokesShader.Dispatch(setBoundsYKernel, TextureWidth, 1, 1);
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
    void OnDestroy()
    {
        densityTexture.Release();
        densityPrevTexture.Release();
        velocityTexture.Release();
        velocityPrevTexture.Release();
        velocityTempTexture.Release();
        pressureTexture.Release();
        pressurePrevTexture.Release();
        divergenceTexture.Release();
    }
}
