using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
//https://github.com/matthiasbroske/GPUStableFluids/blob/main/Assets/Scripts/Stable%20Fluids/StableFluids2D.cs
public class TEST : MonoBehaviour
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

    /*private RenderTexture densityTexture;
    private RenderTexture densityPrevTexture;
    private RenderTexture tempTexture;*/

    private RenderTexture displayTexture;
    private RenderTexture densityTexture;
    private RenderTexture densityPrevTexture;
    private RenderTexture velocityTexture;
    private RenderTexture velocityPrevTexture;
    private RenderTexture tempTexture;
    private RenderTexture pressureTexture;
    private RenderTexture pressurePrevTexture;
    private RenderTexture divergenceTexture;
    private RenderTexture _prevState;
    private RenderTexture _State;

    Vector2 mousePosition;
    Vector2 previousMousePosition;
    Vector2 test;

    Vector2Int resolution;
    Vector3Int threadGroupAmount;
    int xThreadGroups;
    int yThreadGroups;


    public int value = 10;
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

        navierStokesShader.SetInt("Width", planeWidth);
        navierStokesShader.SetInt("Height", planeHeight);
        navierStokesShader.SetInt("value", value);
        navierStokesShader.SetInt("velocity", velocity);
        navierStokesShader.SetFloat("dt", deltaTime);

        /*densityTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        densityTexture.enableRandomWrite = true;
        densityTexture.Create();

        densityPrevTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        densityPrevTexture.enableRandomWrite = true;
        densityPrevTexture.Create();

        tempTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        tempTexture.enableRandomWrite = true;
        tempTexture.Create();*/

        displayTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 4);
        densityTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 4);
        densityPrevTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 4);
        velocityTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 2);
        velocityPrevTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 2);
        tempTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 2);
        pressureTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 1);
        divergenceTexture = AllocateRWLinearRT(resolution.x, resolution.y, 0, 1);
        _prevState = AllocateRWLinearRT(resolution.x, resolution.y, 0, 4);
        _State = AllocateRWLinearRT(resolution.x, resolution.y, 0, 4);


        Renderer rend = GetComponent<Renderer>();
        rend.material = new Material(updateVerticesShader);
        rend.material.SetTexture("importTexture", displayTexture);

    }

    void Update()
    {

        ChangeTexture();
        // Run the fluid simulation
        UpdateVelocity();
        UpdateDensity();


        // Copy the final density texture to the display texture
        Graphics.CopyTexture(densityTexture, displayTexture);
    }

    void UpdateVelocity()
    {
        Diffuse(velocityPrevTexture, velocityTexture, viscosity, true);
        //Projection();
        Advect(velocityTexture, velocityPrevTexture, true);
        //Projection();
    }

    void UpdateDensity()
    {
        Diffuse(densityPrevTexture, densityTexture, diffusion, false);
        Advect(densityTexture, densityPrevTexture);
    }
    void Diffuse(RenderTexture outTexture, RenderTexture inTexture, float spread, bool setBounds)
    {
        /*navierStokesShader.SetFloat("spread", spread);

        navierStokesShader.SetTexture(diffusionKernel, "_In", outTexture);
        navierStokesShader.SetTexture(diffusionKernel, "_Out", inTexture);
        navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        navierStokesShader.SetTexture(diffusionKernel, "_In", inTexture);
        navierStokesShader.SetTexture(diffusionKernel, "_Out", outTexture);
        navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
*/
        navierStokesShader.SetFloat("spread", spread);

        for (int i = 0; i < 2; i++)
        {
            navierStokesShader.SetTexture(diffusionKernel, "_In", outTexture);
            navierStokesShader.SetTexture(diffusionKernel, "_Out", inTexture);
            navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

            // Swap textures for the next iteration
            RenderTexture temp = outTexture;
            outTexture = inTexture;
            inTexture = temp;
        }
    }
    void Advect(RenderTexture outTexture, RenderTexture inTexture, bool setBounds = false)
    {

        navierStokesShader.SetTexture(advectionKernel, "_Velocity", velocityTexture);

        navierStokesShader.SetTexture(advectionKernel, "_In", inTexture);
        navierStokesShader.SetTexture(advectionKernel, "_Out", outTexture);

        navierStokesShader.Dispatch(advectionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

    }

    void Projection()
    {
        navierStokesShader.SetTexture(projection1Kernel, "_Pressure", pressureTexture);
        navierStokesShader.SetTexture(projection1Kernel, "_Divergence", divergenceTexture);
        navierStokesShader.SetTexture(projection1Kernel, "_Velocity", velocityTexture);
        navierStokesShader.Dispatch(projection1Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
        //SetBounds(velocityPrevTexture, velocityTexture);

        // Projection Pt2
        for (int k = 0; k < 10; k++)
        {
            /*navierStokesShader.SetTexture(projection2Kernel, "_Divergence", divergenceTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_PressureIn", pressureTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_Pressure", pressurePrevTexture);
            navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
            navierStokesShader.SetTexture(projection2Kernel, "_PressureIn", pressurePrevTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_Pressure", pressureTexture);
            navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);*/

            navierStokesShader.SetTexture(projection2Kernel, "_Divergence", divergenceTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_Pressure", pressureTexture);
            navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);


        }

        // Projection Pt3
        navierStokesShader.SetTexture(projection3Kernel, "_Velocity", velocityTexture);
        navierStokesShader.SetTexture(projection3Kernel, "_Pressure", pressureTexture);
        navierStokesShader.Dispatch(projection3Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
        //SetBounds(velocityPrevTexture, velocityTexture);
    }


    void SetBounds(RenderTexture texture)
    {
        navierStokesShader.SetTexture(setBoundsKernel, "_Out", texture);
        navierStokesShader.Dispatch(setBoundsKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
    }

    void TextureSet(RenderTexture inTexture, RenderTexture outTexture)
    {
        navierStokesShader.SetTexture(TextureSetKernel, "_In", inTexture);
        navierStokesShader.SetTexture(TextureSetKernel, "_Out", outTexture);
        navierStokesShader.Dispatch(TextureSetKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
    }

    void Swap(ref RenderTexture In, ref RenderTexture Out)
    {
        RenderTexture tmp = In; In = Out; Out = tmp;
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

                    //Debug.Log("X: " + xAmount + " Y: " + yAmount);

                    navierStokesShader.SetFloat("hitPosX", x);
                    navierStokesShader.SetFloat("hitPosY", y);
                    /* navierStokesShader.SetFloat("xAmount", -yAmount);
                    navierStokesShader.SetFloat("yAmount", xAmount); */
                    navierStokesShader.SetFloat("xAmount", -yAmount);
                    navierStokesShader.SetFloat("yAmount", xAmount);
                    navierStokesShader.SetTexture(addVelocityKernel, "_Out", velocityPrevTexture);
                    navierStokesShader.Dispatch(addVelocityKernel, 1, 1, 1);

                }

            }
        }
        previousMousePosition = mousePosition;
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

    public static RenderTexture AllocateRWLinearRT(int width, int height, int depth = 0, int componentCount = 4, TextureWrapMode wrapMode = TextureWrapMode.Clamp, FilterMode filterMode = FilterMode.Bilinear)
    {
        // Determine format from component count
        RenderTextureFormat format;
        switch (componentCount)
        {
            case 1:
                format = RenderTextureFormat.RFloat;
                break;
            case 2:
                format = RenderTextureFormat.RGFloat;
                break;
            default:  // Unfortunately no RGBFloat, so default to float4 ARGBFloat for both 3 and 4 component counts
                format = RenderTextureFormat.ARGBFloat;
                break;
        }

        // Construct the render texture using given parameters
        RenderTexture texture = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
        // If 3D render texture
        if (depth > 0)
        {
            texture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
            texture.volumeDepth = depth;
        }
        texture.wrapMode = wrapMode;
        texture.filterMode = filterMode;
        texture.enableRandomWrite = true;
        texture.Create();

        return texture;
    }
}
