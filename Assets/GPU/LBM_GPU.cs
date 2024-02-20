using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LBM_GPU : MonoBehaviour
{
    int planeWidth;
    int planeHeight;

    public ComputeShader LBMShader;
    public Shader updateVerticesShader;

    private int collisionKernel;
    private int streamingKernel;
    private int addValueKernel;
    private int addVelocityKernel;

    private RenderTexture fTexture;
    private RenderTexture fnewTexture;
    private RenderTexture feqTexture;
    private RenderTexture rhoTexture;    
    private RenderTexture inputRhoTexture;
    private RenderTexture velTexture;
    private RenderTexture latticeTexture;

    float[] lattice = { 0.0f, 1.0f, 0.0f, -1.0f, 0.0f, 1.0f, -1.0f, -1.0f, 1.0f,  
                                 0.0f, 0.0f, 1.0f, 0.0f, -1.0f, 1.0f, 1.0f, -1.0f, -1.0f, 
                                 4.0f / 9.0f, 1.0f / 9.0f, 1.0f / 9.0f, 1.0f / 9.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 36.0f, 1.0f / 36.0f, 1.0f / 36.0f };
    //private int[] latticeDirY = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    //private float[] latticeWeight = { 4.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 9, 1.0f / 36, 1.0f / 36, 1.0f / 36, 1.0f / 36 };
    Texture2D latticeTemp;

    Vector2 mousePosition;
    Vector2 previousMousePosition;
    Vector2 test;
    
    Vector2Int resolution;
    Vector3Int threadGroupAmount;
    int xThreadGroups;
    int yThreadGroups;

    // Start is called before the first frame update
    void Start()
    {
        planeWidth = GetComponent<PlaneGenerator>().widthInput;
        planeHeight = GetComponent<PlaneGenerator>().heightInput;
        
        mousePosition = Input.mousePosition;
        previousMousePosition = mousePosition;

        collisionKernel = LBMShader.FindKernel("Collision");
        streamingKernel = LBMShader.FindKernel("Streaming");
        addValueKernel = LBMShader.FindKernel("AddValue");
        addVelocityKernel = LBMShader.FindKernel("AddVelocity");

        resolution = new Vector2Int(planeWidth, planeHeight);
        threadGroupAmount = new Vector3Int(planeWidth, planeHeight, 1);

        LBMShader.GetKernelThreadGroupSizes(collisionKernel, out uint xThreadGroupSize, out uint yThreadGroupSize, out uint zThreadGroupSize);
        threadGroupAmount = new Vector3Int(Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize),  Mathf.CeilToInt(resolution.y / (float)yThreadGroupSize), Mathf.CeilToInt(1 / (float)zThreadGroupSize));

        LBMShader.SetInt("width", planeWidth);
        LBMShader.SetInt("height", planeHeight);

        fTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        fTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        fTexture.volumeDepth = 9;
        fTexture.enableRandomWrite = true;
        fTexture.Create();

        fnewTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        fnewTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        fnewTexture.volumeDepth = 9;
        fnewTexture.enableRandomWrite = true;
        fnewTexture.Create();

        feqTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        feqTexture.dimension = UnityEngine.Rendering.TextureDimension.Tex3D;
        feqTexture.volumeDepth = 9;
        feqTexture.enableRandomWrite = true;
        feqTexture.Create();

        
        
        

        latticeTemp = new Texture2D(9, 3, TextureFormat.RFloat, false);
        latticeTemp.SetPixelData(lattice, 0);
        //latticeTexture.Apply();
        latticeTexture = new RenderTexture(9, 3, 0, RenderTextureFormat.RFloat);
        
        latticeTexture.enableRandomWrite = true;
        Graphics.Blit(latticeTemp, latticeTexture);
        latticeTexture.Create();

        rhoTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        rhoTexture.enableRandomWrite = true;
        rhoTexture.Create();

        inputRhoTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat);
        inputRhoTexture.enableRandomWrite = true;
        inputRhoTexture.Create();

        velTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RGFloat);
        velTexture.enableRandomWrite = true;
        velTexture.Create();
    }

    // Update is called once per frame
    void Update()
    {
        ChangeTexture();

        LBMShader.SetTexture(collisionKernel, "fTexture", fTexture);
        LBMShader.SetTexture(collisionKernel, "fnewTexture", fnewTexture);
        LBMShader.SetTexture(collisionKernel, "feqTexture", feqTexture);
        LBMShader.SetTexture(collisionKernel, "rhoTexture", rhoTexture);
        LBMShader.SetTexture(collisionKernel, "inputRhoTexture", inputRhoTexture);
        LBMShader.SetTexture(collisionKernel, "velTexture", velTexture);
        LBMShader.SetTexture(collisionKernel, "latticeTexture", latticeTexture);
        LBMShader.Dispatch(collisionKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);

        LBMShader.SetTexture(streamingKernel, "fTexture", fTexture);
        LBMShader.SetTexture(streamingKernel, "fnewTexture", fnewTexture);
        LBMShader.SetTexture(streamingKernel, "latticeTexture", latticeTexture);
        LBMShader.Dispatch(streamingKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);


        //update the vertices based on the density texture
        Renderer rend = GetComponent<Renderer> ();
        rend.material = new Material(updateVerticesShader);
        //rend.material.mainTexture = densityTexture;
        rend.material.SetTexture("importTexture", rhoTexture);
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
                if (meshCollider != null && meshCollider.sharedMesh != null)
                {
                    int vertexHit = GetClosestVertex(hit, triangles);
                    int x = vertexHit % planeWidth;
                    int y = vertexHit / planeWidth;
                    
                    LBMShader.SetFloat("hitPosX", x);
                    LBMShader.SetFloat("hitPosY", y);
                    LBMShader.SetTexture(addValueKernel, "inputRhoTexture", rhoTexture);
                    LBMShader.Dispatch(addValueKernel, threadGroupAmount.x, threadGroupAmount.y, threadGroupAmount.z);
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
                
                if (meshCollider != null && meshCollider.sharedMesh != null)
                {
                    int vertexHit = GetClosestVertex(hit, triangles);
                    int x = vertexHit % planeWidth;
                    int y = vertexHit / planeWidth;

                    float xAmount = mousePosition.x - previousMousePosition.x;
                    float yAmount = mousePosition.y - previousMousePosition.y;
                    //Debug.Log(mousePosition.y - previousMousePosition.y);
                    LBMShader.SetFloat("hitPosX", x);
                    LBMShader.SetFloat("hitPosY", y);
                    LBMShader.SetFloat("xAmount", xAmount);
                    LBMShader.SetFloat("yAmount", yAmount);

                    LBMShader.SetTexture(addVelocityKernel, "velTexture", velTexture);
                    LBMShader.Dispatch(addVelocityKernel, 1, 1, 1);

                }
            }
        }
        //Debug.Log((mousePosition.x - previousMousePosition.x) + " " + (mousePosition.y - previousMousePosition.y));
    
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
