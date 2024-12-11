using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LBM_GPU : MonoBehaviour
{
    int planeWidth;
    int planeHeight;

    public ComputeShader LBMShader;
    public Shader updateVerticesShader;
    public float tau = 1.0f;
    public float dt = 0.1f;

    private int densityKernel;
    private int collisionKernel;
    private int streamingKernel;
    private int addValueKernel;
    private int addVelocityKernel;

    private RenderTexture fTexture;
    private RenderTexture fnewTexture;
    private RenderTexture feqTexture;
    private RenderTexture rhoTexture;
    private RenderTexture inRhoTexture;
    private RenderTexture velTexture;
    private RenderTexture tempVelTexture;
    private RenderTexture latticeTexture;
    private RenderTexture densityTexture;
    private RenderTexture densityPrevTexture;
    private RenderTexture tempTexture;

    float[] lattice = { -1.0f, 0.0f, 1.0f, -1.0f, 0.0f, 1.0f, -1.0f, 0.0f, 1.0f,
                        1.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, -1.0f, -1.0f, -1.0f,
                        1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 4.0f / 9.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f };

    //NW N NE E SE S SW W C 
    /*float[] lattice = { -1.0f, 0.0f, 1.0f, 1.0f, 1.0f, 0.0f, -1.0f, -1.0f, 0.0f,  
                        1.0f, 1.0f, 1.0f, 0.0f, -1.0f, -1.0f, -1.0f, 0.0f, 0.0f,  
                        1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 4.0f / 9.0f };
    */
    Texture2D latticeTemp;

    Vector2 mousePosition;
    Vector2 previousMousePosition;

    Vector2Int resolution;
    Vector3Int threadGroupAmount;

    // Start is called before the first frame update
    void Start()
    {
        planeWidth = GetComponent<PlaneGenerator>().widthInput;
        planeHeight = GetComponent<PlaneGenerator>().heightInput;

        mousePosition = Input.mousePosition;
        previousMousePosition = mousePosition;

        densityKernel = LBMShader.FindKernel("Density");
        collisionKernel = LBMShader.FindKernel("Collision");
        streamingKernel = LBMShader.FindKernel("Streaming");
        addValueKernel = LBMShader.FindKernel("AddValue");
        addVelocityKernel = LBMShader.FindKernel("AddVelocity");

        resolution = new Vector2Int(planeWidth, planeHeight);
        threadGroupAmount = new Vector3Int(planeWidth, planeHeight, 1);

        LBMShader.GetKernelThreadGroupSizes(collisionKernel, out uint xThreadGroupSize, out uint yThreadGroupSize, out uint zThreadGroupSize);
        threadGroupAmount = new Vector3Int(Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize), Mathf.CeilToInt(resolution.y / (float)yThreadGroupSize), Mathf.CeilToInt(1 / (float)zThreadGroupSize));

        LBMShader.SetInt("width", planeWidth);
        LBMShader.SetInt("height", planeHeight);
        LBMShader.SetFloat("tau", tau);
        LBMShader.SetFloat("dt", dt);

        densityTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        densityTexture.enableRandomWrite = true;
        densityTexture.Create();

        densityPrevTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        densityPrevTexture.enableRandomWrite = true;
        densityPrevTexture.Create();

        tempTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.ARGBFloat);
        tempTexture.enableRandomWrite = true;
        tempTexture.Create();


        latticeTemp = new Texture2D(9, 3, TextureFormat.RFloat, false);
        latticeTemp.SetPixelData(lattice, 0);
        latticeTemp.Apply();
        latticeTexture = new RenderTexture(9, 3, 0, RenderTextureFormat.RFloat) { enableRandomWrite = true };
        Graphics.Blit(latticeTemp, latticeTexture);
        latticeTexture.Create();

        LBMShader.SetTexture(addValueKernel, "inRhoTexture", inRhoTexture);
        LBMShader.SetTexture(addVelocityKernel, "tempVelTexture", tempVelTexture);

        LBMShader.SetTexture(densityKernel, "fTexture", fTexture);
        LBMShader.SetTexture(densityKernel, "latticeTexture", latticeTexture);
        LBMShader.SetTexture(densityKernel, "velTexture", velTexture);
        LBMShader.SetTexture(densityKernel, "tempVelTexture", tempVelTexture);
        LBMShader.SetTexture(densityKernel, "rhoTexture", rhoTexture);
        LBMShader.SetTexture(densityKernel, "inRhoTexture", inRhoTexture);


        LBMShader.SetTexture(collisionKernel, "fTexture", fTexture);
        LBMShader.SetTexture(collisionKernel, "fnewTexture", fnewTexture);
        LBMShader.SetTexture(collisionKernel, "feqTexture", feqTexture);
        LBMShader.SetTexture(collisionKernel, "rhoTexture", rhoTexture);
        LBMShader.SetTexture(collisionKernel, "velTexture", velTexture);
        LBMShader.SetTexture(collisionKernel, "latticeTexture", latticeTexture);


        LBMShader.SetTexture(streamingKernel, "fTexture", fTexture);
        LBMShader.SetTexture(streamingKernel, "fnewTexture", fnewTexture);
        LBMShader.SetTexture(streamingKernel, "latticeTexture", latticeTexture);
    }

    // Update is called once per frame
    void Update()
    {
        ChangeTexture();

        float time = Time.time;
        LBMShader.SetFloat("time", time);

        LBMShader.Dispatch(densityKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        LBMShader.Dispatch(collisionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        LBMShader.Dispatch(streamingKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);


        //update the vertices based on the density texture
        Renderer rend = GetComponent<Renderer>();
        rend.material = new Material(updateVerticesShader);
        rend.material.SetTexture("importTexture", rhoTexture);
        //rend.material.mainTexture = densityTexture;
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

                    LBMShader.SetFloat("hitPosX", x);
                    LBMShader.SetFloat("hitPosY", y);


                    LBMShader.Dispatch(addValueKernel, 1, 1, 1);
                }

            }
        }
        if (Input.GetKeyDown(KeyCode.M))//(Input.GetMouseButton(1))
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

                    LBMShader.SetFloat("hitPosX", x);
                    LBMShader.SetFloat("hitPosY", y);
                    LBMShader.SetFloat("xAmount", -yAmount);
                    LBMShader.SetFloat("yAmount", xAmount);

                    LBMShader.Dispatch(addVelocityKernel, 1, 1, 1);

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
