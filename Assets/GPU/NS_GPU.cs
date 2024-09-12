using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NS_GPU : MonoBehaviour
{
    int planeWidth;
    int planeHeight;

    public ComputeShader navierStokesShader;
    public Shader updateVerticesShader;

    private int diffusionKernel;
    private int advectionKernel;
    private int projection1Kernel;
    private int projection2Kernel;
    private int projection3Kernel;
    private int setBoundsKernel;
    private int TextureSetKernel;
    private int addValueKernel;
    private int addVelocityKernel;
    private int selectChannelKernel;
    private int combineChannelsKernel;

    private RenderTexture densityTexture;
    private RenderTexture densityPrevTexture;
    private RenderTexture tempTexture;

    Vector2 mousePosition;
    Vector2 previousMousePosition;
    Vector2 test;

    Vector2Int resolution;
    Vector3Int threadGroupAmount;
    int xThreadGroups;
    int yThreadGroups;


    public int value = 1000;
    public int velocity = 10;
    public float diffusion = 0.001f;
    public float viscosity = 0.001f;
    public float deltaTime = 0.1f;
    void Start()
    {
        planeWidth = GetComponent<PlaneGenerator>().widthInput;
        planeHeight = GetComponent<PlaneGenerator>().heightInput;

        mousePosition = Input.mousePosition;
        previousMousePosition = mousePosition;

        diffusionKernel = navierStokesShader.FindKernel("Diffusion");
        advectionKernel = navierStokesShader.FindKernel("Advection");
        projection1Kernel = navierStokesShader.FindKernel("Projection1");
        projection2Kernel = navierStokesShader.FindKernel("Projection2");
        projection3Kernel = navierStokesShader.FindKernel("Projection3");
        setBoundsKernel = navierStokesShader.FindKernel("SetBounds");
        TextureSetKernel = navierStokesShader.FindKernel("TextureSet");
        addValueKernel = navierStokesShader.FindKernel("AddValue");
        addVelocityKernel = navierStokesShader.FindKernel("AddVelocity");

        resolution = new Vector2Int(planeWidth, planeHeight);
        threadGroupAmount = new Vector3Int(planeWidth, planeHeight, 1);

        navierStokesShader.GetKernelThreadGroupSizes(0, out uint xThreadGroupSize, out uint yThreadGroupSize, out uint zThreadGroupSize);
        threadGroupAmount = new Vector3Int(Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize), Mathf.CeilToInt(resolution.y / (float)yThreadGroupSize), Mathf.CeilToInt(1 / (float)zThreadGroupSize));

        /*navierStokesShader.GetKernelThreadGroupSizes(setBoundsKernel, out xThreadGroupSize, out yThreadGroupSize, out zThreadGroupSize);
        xThreadGroups = Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize);

        navierStokesShader.GetKernelThreadGroupSizes(TextureSetKernel, out xThreadGroupSize, out yThreadGroupSize, out zThreadGroupSize);
        yThreadGroups = Mathf.CeilToInt(resolution.y / (float)xThreadGroupSize);*/

        navierStokesShader.SetInt("Width", planeWidth);
        navierStokesShader.SetInt("Height", planeHeight);
        navierStokesShader.SetInt("value", value);
        navierStokesShader.SetInt("velocity", velocity);
        navierStokesShader.SetFloat("dt", deltaTime);

        densityTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        densityTexture.enableRandomWrite = true;
        densityTexture.Create();

        densityPrevTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        densityPrevTexture.enableRandomWrite = true;
        densityPrevTexture.Create();

        tempTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        tempTexture.enableRandomWrite = true;
        tempTexture.Create();

        Renderer rend = GetComponent<Renderer>();
        rend.material = new Material(updateVerticesShader);
        rend.material.SetTexture("importTexture", densityTexture);
    }
    void Update()
    {
        ChangeTexture();

        //Velocity Step
        Diffuse(densityPrevTexture, densityTexture, viscosity, 1, true);
        Diffuse(densityPrevTexture, densityTexture, viscosity, 2, true);
        Projection();
        Advect(densityTexture, densityPrevTexture, 1, true);
        Advect(densityTexture, densityPrevTexture, 2, true);
        Swap();
        Projection();

        //Density Step
        Diffuse(densityTexture, densityPrevTexture, diffusion, 0, false);
        Advect(densityPrevTexture, densityTexture, 0, false);
    }

    void Swap()
    {
        RenderTexture tmp = densityPrevTexture; densityPrevTexture = densityTexture; densityTexture = tmp;
    }
    void ChangeTexture()
    {
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
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider != null && meshCollider.sharedMesh != null)
                {
                    int vertexHit = GetClosestVertex(hit, triangles);
                    int x = vertexHit % planeWidth;
                    int y = vertexHit / planeWidth;

                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    navierStokesShader.SetTexture(addValueKernel, "_Out", densityPrevTexture);
                    navierStokesShader.Dispatch(addValueKernel, 1, 1, 1);
                }

            }
        }
        if (Input.GetMouseButton(1))
        {
            if (Physics.Raycast(ray, out hit))
            {
                float xPos = hit.point.x;
                float yPos = hit.point.y;
                MeshCollider meshCollider = hit.collider as MeshCollider;

                if (meshCollider != null && meshCollider.sharedMesh != null)
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int x = vertexHit % planeWidth;
                    int y = vertexHit / planeWidth;

                    float xAmount = mousePosition.x - previousMousePosition.x;
                    float yAmount = mousePosition.y - previousMousePosition.y;

                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    navierStokesShader.SetFloat("xAmount", -yAmount);
                    navierStokesShader.SetFloat("yAmount", xAmount);
                    navierStokesShader.SetTexture(addVelocityKernel, "_Out", densityPrevTexture);
                    navierStokesShader.Dispatch(addVelocityKernel, 1, 1, 1);

                }
            }
        }
        previousMousePosition = mousePosition;
    }

    void Diffuse(RenderTexture inTexture, RenderTexture outTexture, float spread, int indicator, bool setBounds)
    {
        navierStokesShader.SetFloat("spread", spread);
        navierStokesShader.SetInt("indicator", indicator);
        navierStokesShader.SetTexture(diffusionKernel, "_In", inTexture);
        navierStokesShader.SetTexture(diffusionKernel, "_Out", outTexture);
        navierStokesShader.SetTexture(diffusionKernel, "_Temp", tempTexture);
        navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        TextureSet(tempTexture, outTexture);
        if (setBounds) SetBounds(inTexture, outTexture);
    }

    void Advect(RenderTexture inTexture, RenderTexture outTexture, int indicator, bool setBounds = false)
    {
        navierStokesShader.SetInt("indicator", indicator);
        navierStokesShader.SetTexture(advectionKernel, "_In", inTexture);
        navierStokesShader.SetTexture(advectionKernel, "_Out", outTexture);
        navierStokesShader.Dispatch(advectionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        if (setBounds) SetBounds(inTexture, outTexture);
    }

    void Projection()
    {
        // Projection Part 1
        navierStokesShader.SetTexture(projection1Kernel, "_In", densityPrevTexture);
        navierStokesShader.SetTexture(projection1Kernel, "_Out", densityTexture);
        navierStokesShader.SetTexture(projection1Kernel, "_Temp", tempTexture);
        navierStokesShader.Dispatch(projection1Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        TextureSet(tempTexture, densityTexture);
        SetBounds(densityPrevTexture, densityTexture);

        // Projection Pt2
        for (int k = 0; k < 1; k++)
        {
            navierStokesShader.SetTexture(projection2Kernel, "_In", densityPrevTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_Out", densityTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_Temp", tempTexture);
            navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

            TextureSet(tempTexture, densityTexture);
        }

        // Projection Pt3
        navierStokesShader.SetTexture(projection3Kernel, "_In", densityPrevTexture);
        navierStokesShader.SetTexture(projection3Kernel, "_Out", densityTexture);
        navierStokesShader.SetTexture(projection3Kernel, "_Temp", tempTexture);
        navierStokesShader.Dispatch(projection3Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        TextureSet(tempTexture, densityTexture);
        SetBounds(densityPrevTexture, densityTexture);
    }

    private void SetBounds(RenderTexture inTexture, RenderTexture outTexture)
    {
        navierStokesShader.SetTexture(setBoundsKernel, "_In", inTexture);//swap In and Out?
        navierStokesShader.SetTexture(setBoundsKernel, "_Out", outTexture);
        navierStokesShader.Dispatch(setBoundsKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
    }

    private void TextureSet(RenderTexture inTexture, RenderTexture outTexture)
    {
        navierStokesShader.SetTexture(TextureSetKernel, "_In", inTexture);
        navierStokesShader.SetTexture(TextureSetKernel, "_Out", outTexture);
        navierStokesShader.Dispatch(TextureSetKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
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
}
