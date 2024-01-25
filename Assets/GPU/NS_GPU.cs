using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NS_GPU : MonoBehaviour
{
    int planeWidth;
    int planeHeight;

    public ComputeShader navierStokesShader;
    public Shader updateVerticesShader;

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
    Vector2 test;
    
    Vector2Int resolution;
    Vector3Int threadGroupAmount;
    int xThreadGroups;
    int yThreadGroups;

    public float diffusion = 0.01f;
    public float viscosity = 0.000000001f;
    void Start()
    {
        planeWidth = GetComponent<PlaneGenerator>().widthInput;
        planeHeight = GetComponent<PlaneGenerator>().heightInput;
        
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
        
        resolution = new Vector2Int(planeWidth, planeHeight);
        threadGroupAmount = new Vector3Int(planeWidth, planeHeight, 1);

        navierStokesShader.GetKernelThreadGroupSizes(advectionKernel, out uint xThreadGroupSize, out uint yThreadGroupSize, out uint zThreadGroupSize);
        threadGroupAmount = new Vector3Int(Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize),  Mathf.CeilToInt(resolution.y / (float)yThreadGroupSize), Mathf.CeilToInt(1 / (float)zThreadGroupSize));

        navierStokesShader.GetKernelThreadGroupSizes(setBoundsXKernel, out xThreadGroupSize, out yThreadGroupSize, out zThreadGroupSize);
        xThreadGroups = Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize);

        navierStokesShader.GetKernelThreadGroupSizes(setBoundsYKernel, out xThreadGroupSize, out yThreadGroupSize, out zThreadGroupSize);
        yThreadGroups = Mathf.CeilToInt(resolution.y / (float)xThreadGroupSize);

        navierStokesShader.SetInt("Width", planeWidth);
        navierStokesShader.SetInt("Height", planeHeight);


        densityTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        densityTexture.enableRandomWrite = true;
        densityTexture.Create();

        densityPrevTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat); 
        densityPrevTexture.enableRandomWrite = true;
        densityPrevTexture.Create();

        velocityTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RGFloat);
        velocityTexture.enableRandomWrite = true;
        velocityTexture.Create();

        velocityPrevTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RGFloat); 
        velocityPrevTexture.enableRandomWrite = true;
        velocityPrevTexture.Create();

        velocityTempTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RGFloat);
        velocityTempTexture.enableRandomWrite = true;
        velocityTempTexture.Create();

        pressureTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        pressureTexture.enableRandomWrite = true;
        pressureTexture.Create();

        pressurePrevTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        pressurePrevTexture.enableRandomWrite = true;
        pressurePrevTexture.Create();

        divergenceTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        divergenceTexture.enableRandomWrite = true;    
        divergenceTexture.Create();

        //MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        //meshRenderer.material.mainTexture = densityTexture;
        
    }

    void Update()
    {
        ChangeTexture();

        Diffuse(velocityPrevTexture, velocityTexture, true, viscosity);
        Projection();
        Graphics.CopyTexture(velocityPrevTexture, velocityTexture); // Swap output to input
        Advect(velocityPrevTexture, velocityTexture, true);
        Projection();

        Diffuse(densityPrevTexture, densityTexture, false, diffusion);
        Graphics.CopyTexture(densityTexture, densityPrevTexture);  // Swap output to input
        Advect(densityPrevTexture, densityTexture);

        //update the vertices based on the density texture
        Renderer rend = GetComponent<Renderer> ();
        rend.material = new Material(updateVerticesShader);
        rend.material.mainTexture = densityTexture;
        rend.material.SetTexture("_MainTex", densityTexture);

    }

    void ChangeTexture(){
        
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
                    int x = vertexHit % planeWidth;
                    int y = vertexHit / planeWidth;
                    
                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    navierStokesShader.SetTexture(addValueKernel, "Out", densityTexture);
                    navierStokesShader.Dispatch(addValueKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
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
                    int x = vertexHit % planeWidth;
                    int y = vertexHit / planeWidth;
                    
                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    navierStokesShader.SetFloat("xAmount", mousePosition.x - previousMousePosition.x);
                    navierStokesShader.SetFloat("yAmount", mousePosition.y - previousMousePosition.y);

                    navierStokesShader.SetTexture(addVelocityKernel, "Out", velocityTexture);
                    navierStokesShader.Dispatch(addVelocityKernel, 1, 1, 1);
                }
            }
        }
        
        previousMousePosition = mousePosition;
        //Debug.Log((mousePosition.x - previousMousePosition.x) + " " + (mousePosition.y - previousMousePosition.y));
    }

    void Diffuse(RenderTexture inTexture, RenderTexture outTexture, bool setBounds, float spread)
    {
        navierStokesShader.SetFloat("spread", spread);
        navierStokesShader.SetTexture(diffusionKernel, "In", outTexture);
        navierStokesShader.SetTexture(diffusionKernel, "Out", inTexture);
        navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
        navierStokesShader.SetTexture(diffusionKernel, "In", inTexture);
        navierStokesShader.SetTexture(diffusionKernel, "Out", outTexture);
        navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
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
            navierStokesShader.Dispatch(advectionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
            if (setBounds) SetBounds(inTexture, outTexture);
        }

    void Projection()
    {
        // Projection Part 1
        navierStokesShader.SetTexture(projectionKernel, "Pressure", pressureTexture);
        navierStokesShader.SetTexture(projectionKernel, "Divergence", divergenceTexture);
        navierStokesShader.SetTexture(projectionKernel, "Velocity", velocityTexture);
        navierStokesShader.Dispatch(projectionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
        SetBounds(velocityPrevTexture, velocityTexture);
        
        // Projection Pt2
        //for (int k = 0; k < 10; k++)
        //{
        navierStokesShader.SetTexture(projection2Kernel, "Divergence", divergenceTexture);
        navierStokesShader.SetTexture(projection2Kernel, "PressurePrev", pressureTexture);
        navierStokesShader.SetTexture(projection2Kernel, "Pressure", pressurePrevTexture);
        navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        navierStokesShader.SetTexture(projection2Kernel, "PressurePrev", pressureTexture);
        navierStokesShader.SetTexture(projection2Kernel, "Pressure", pressurePrevTexture);
        navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
        //}

        // Projection Pt3
        navierStokesShader.SetTexture(projection3Kernel, "Velocity", velocityTexture);
        navierStokesShader.SetTexture(projection3Kernel, "PressurePrev", pressureTexture);
        navierStokesShader.Dispatch(projection3Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
        SetBounds(velocityPrevTexture, velocityTexture);
    }

    private void SetBounds(RenderTexture inTexture, RenderTexture outTexture)
    { 
        navierStokesShader.SetTexture(setBoundsXKernel, "In", outTexture);
        navierStokesShader.SetTexture(setBoundsXKernel, "Out", inTexture);
        navierStokesShader.Dispatch(setBoundsXKernel, 5, 1, 1);
        navierStokesShader.SetTexture(setBoundsYKernel, "In", inTexture);
        navierStokesShader.SetTexture(setBoundsYKernel, "Out", outTexture);
        navierStokesShader.Dispatch(setBoundsYKernel, 5, 1, 1);
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
        /*densityTexture.Release();
        densityPrevTexture.Release();
        velocityTexture.Release();
        velocityPrevTexture.Release();
        velocityTempTexture.Release();
        pressureTexture.Release();
        pressurePrevTexture.Release();
        divergenceTexture.Release();*/
    }
}
