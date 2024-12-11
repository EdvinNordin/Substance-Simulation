using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
//https://github.com/matthiasbroske/GPUStableFluids/blob/main/Assets/Scripts/Stable%20Fluids/StableFluids2D.cs
public class NS_GPU : MonoBehaviour
{
    int planeWidth;
    int planeHeight;

    public ComputeShader navierStokesShader;
    public Shader updateVerticesShader;

    private int diffusionDensityKernel;
    private int diffusionVelocityKernel;
    private int advectionDensityKernel;
    private int advectionVelocityKernel;
    private int projection1Kernel;
    private int projection2Kernel;
    private int projection3Kernel;
    private int setBoundsKernel;
    private int TextureSetKernel;
    private int addValueKernel;
    private int addVelocityKernel;
    private int selectChannelKernel;
    private int combineChannelsKernel;

    private RenderTexture stateTexture;
    private RenderTexture prevStateTexture;
    private RenderTexture tempTexture;
    private RenderTexture displayTexture;

    Vector2 mousePosition;
    Vector2 previousMousePosition;
    Vector2 test;

    Vector2Int resolution;
    Vector3Int threadGroupAmount;
    int xThreadGroups;
    int yThreadGroups;


    public float value = 10;
    public float velocity = 10;
    public float diffusion = 0.001f;
    public float viscosity = 0.001f;
    public float deltaTime = 0.1f;
    void Start()
    {
        planeWidth = GetComponent<PlaneGenerator>().widthInput;
        planeHeight = GetComponent<PlaneGenerator>().heightInput;

        mousePosition = Input.mousePosition;
        previousMousePosition = mousePosition;

        diffusionDensityKernel = navierStokesShader.FindKernel("DiffusionDensity");
        diffusionVelocityKernel = navierStokesShader.FindKernel("DiffusionVelocity");
        advectionDensityKernel = navierStokesShader.FindKernel("AdvectionDensity");
        advectionVelocityKernel = navierStokesShader.FindKernel("AdvectionVelocity");
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
        navierStokesShader.SetFloat("value", value);
        navierStokesShader.SetFloat("velocity", velocity);
        navierStokesShader.SetFloat("dt", deltaTime);

        stateTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        stateTexture.enableRandomWrite = true;
        stateTexture.Create();

        prevStateTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        prevStateTexture.enableRandomWrite = true;
        prevStateTexture.Create();

        tempTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        tempTexture.enableRandomWrite = true;
        tempTexture.Create();

        displayTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        displayTexture.enableRandomWrite = true;
        displayTexture.Create();

        Renderer rend = GetComponent<Renderer>();
        rend.material = new Material(updateVerticesShader);
        rend.material.SetTexture("importTexture", displayTexture);

    }

    void Update()
    {

        Swap(ref displayTexture, ref stateTexture);
        ChangeTexture(stateTexture);

        /*
                Texture2D tex = toTexture2D(stateTexture);
                var data = tex.GetRawTextureData<Color>();
                Debug.Log("state = " + data[12].r);*/
        // Run the fluid simulation
        UpdateVelocity();
        UpdateDensity();
        //Diffuse(stateTexture, prevStateTexture, diffusion, diffusionDensityKernel, true);
        //DiffuseV();
        Swap(ref displayTexture, ref stateTexture);
    }

    void UpdateVelocity()
    {
        //Swap(ref prevStateTexture, ref stateTexture);
        //Diffuse(prevStateTexture, stateTexture, viscosity, diffusionVelocityKernel, true);
        Diffuse(prevStateTexture, stateTexture, viscosity, diffusionVelocityKernel, true);
        Swap(ref prevStateTexture, ref tempTexture);
        //Projection();
        Advect(stateTexture, prevStateTexture, advectionVelocityKernel, true);
        Swap(ref stateTexture, ref tempTexture);
        //Projection();
    }

    void UpdateDensity()
    {

        //Swap(ref prevStateTexture, ref stateTexture);
        //Diffuse(prevStateTexture, stateTexture, diffusion, diffusionDensityKernel, false);
        Diffuse(prevStateTexture, stateTexture, diffusion, diffusionDensityKernel, false);
        Swap(ref prevStateTexture, ref tempTexture);

        Advect(stateTexture, prevStateTexture, advectionDensityKernel, false);
        Swap(ref stateTexture, ref tempTexture);
        //Swap(ref prevStateTexture, ref stateTexture);
    }

    void Diffuse(RenderTexture outTexture, RenderTexture inTexture, float spread, int kernel, bool setBounds)
    {
        navierStokesShader.SetFloat("spread", spread);
        //tempTexture = inTexture;
        navierStokesShader.SetTexture(kernel, "_Out", tempTexture);
        navierStokesShader.SetTexture(kernel, "_In", inTexture);
        navierStokesShader.SetTexture(kernel, "_OutTemp", outTexture);
        navierStokesShader.Dispatch(kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

    }


    void Advect(RenderTexture outTexture, RenderTexture inTexture, int kernel, bool setBounds = false)
    {
        //RenderTexture tmp = inTexture;
        navierStokesShader.SetTexture(kernel, "_In", inTexture);
        navierStokesShader.SetTexture(kernel, "_Out", tempTexture);
        navierStokesShader.SetTexture(kernel, "_OutTemp", outTexture);

        navierStokesShader.Dispatch(kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

    }

    void Projection(RenderTexture outTexture, RenderTexture inTexture)
    {
        /*// Projection Part 1
        navierStokesShader.SetTexture(projection1Kernel, "_OutTemp", outTexture);
        navierStokesShader.SetTexture(projection1Kernel, "_In", inTexture);
        navierStokesShader.SetTexture(projection1Kernel, "_Out", tempTexture);
        navierStokesShader.Dispatch(projection1Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        //SetBounds(prevStateTexture);

        // Projection Pt2
        for (int k = 0; k < 1; k++)
        {
            navierStokesShader.SetTexture(projection2Kernel, "_In", inTexture);
            navierStokesShader.SetTexture(projection2Kernel, "_OutTemp", tempTexture);
            navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

            Swap(ref tempTexture, ref inTexture);
            //SetBounds(prevStateTexture);
        }

        // Projection Pt3
        navierStokesShader.SetTexture(projection3Kernel, "_In", inTexture);
        navierStokesShader.SetTexture(projection3Kernel, "_OutTemp", outTexture);
        navierStokesShader.SetTexture(projection3Kernel, "_Out", tempTexture);
        navierStokesShader.Dispatch(projection3Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        Swap(ref tempTexture, ref stateTexture);
        //SetBounds(stateTexture);*/

    }
    /*
        void Diffuse(RenderTexture inTexture, RenderTexture outTexture, float spread, int indicator, bool setBounds)
        {
            navierStokesShader.SetFloat("spread", spread);
            navierStokesShader.SetInt("indicator", indicator);


            navierStokesShader.SetTexture(diffusionKernel, "_In", outTexture);
            navierStokesShader.SetTexture(diffusionKernel, "_Out", inTexture);
            navierStokesShader.SetTexture(diffusionKernel, "_Temp", tempTexture);
            navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
            Swap(ref tempTexture, ref outTexture);

            navierStokesShader.SetTexture(diffusionKernel, "_In", inTexture);
            navierStokesShader.SetTexture(diffusionKernel, "_Out", outTexture);
            navierStokesShader.SetTexture(diffusionKernel, "_Temp", tempTexture);
            navierStokesShader.Dispatch(diffusionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
            Swap(ref tempTexture, ref outTexture);
        }

        void Advect(RenderTexture inTexture, RenderTexture outTexture, int indicator, bool setBounds = false)
        {
            navierStokesShader.SetInt("indicator", indicator);
            navierStokesShader.SetTexture(advectionKernel, "_In", inTexture);
            navierStokesShader.SetTexture(advectionKernel, "_Out", outTexture);
            navierStokesShader.SetTexture(advectionKernel, "_Temp", tempTexture);
            navierStokesShader.Dispatch(advectionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
            //Swap(ref tempTexture, ref outTexture);
        }

        void Projection()
        {
            // Projection Part 1
            navierStokesShader.SetTexture(projection1Kernel, "_In", prevStateTexture);
            navierStokesShader.SetTexture(projection1Kernel, "_Out", stateTexture);
            navierStokesShader.SetTexture(projection1Kernel, "_Temp", tempTexture);
            navierStokesShader.Dispatch(projection1Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

            Swap(ref tempTexture, ref prevStateTexture);
            SetBounds(prevStateTexture);

            // Projection Pt2
            for (int k = 0; k < 1; k++)
            {
                navierStokesShader.SetTexture(projection2Kernel, "_In", prevStateTexture);
                navierStokesShader.SetTexture(projection2Kernel, "_Temp", tempTexture);
                navierStokesShader.Dispatch(projection2Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

                Swap(ref tempTexture, ref prevStateTexture);
                SetBounds(prevStateTexture);
            }

            // Projection Pt3
            navierStokesShader.SetTexture(projection3Kernel, "_In", prevStateTexture);
            navierStokesShader.SetTexture(projection3Kernel, "_Out", stateTexture);
            navierStokesShader.SetTexture(projection3Kernel, "_Temp", tempTexture);
            navierStokesShader.Dispatch(projection3Kernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

            Swap(ref tempTexture, ref stateTexture);
            SetBounds(stateTexture);
        }*/
    Texture2D toTexture2D(RenderTexture rTex)
    {
        Texture2D tex = new Texture2D(planeWidth, planeHeight, TextureFormat.RGBAFloat, false);
        // ReadPixels looks at the active RenderTexture.
        RenderTexture.active = rTex;
        tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
        tex.Apply();
        return tex;
    }
    private void SetBounds(RenderTexture texture)
    {
        navierStokesShader.SetTexture(setBoundsKernel, "_Out", texture);
        navierStokesShader.Dispatch(setBoundsKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
    }

    private void TextureSet(RenderTexture inTexture, RenderTexture outTexture)
    {
        navierStokesShader.SetTexture(TextureSetKernel, "_In", inTexture);
        navierStokesShader.SetTexture(TextureSetKernel, "_Out", outTexture);
        navierStokesShader.Dispatch(TextureSetKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
    }

    void Swap(ref RenderTexture In, ref RenderTexture Out)
    {
        RenderTexture tmp = In; In = Out; Out = tmp;
    }

    void ChangeTexture(RenderTexture texture)
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
                    navierStokesShader.SetTexture(addValueKernel, "_Out", texture);
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
                    navierStokesShader.SetTexture(addVelocityKernel, "_Out", texture);
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
}
