using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WEGPU : MonoBehaviour
{
    public ComputeShader computeShader;

    private RenderTexture heightTexture;
    private RenderTexture velTexture;
    private RenderTexture accTexture;

    public Shader updateVerticesShader;
    public float dt = 0.01f;
    public float c = 10.0f;
    private int WaveEquationKernel;
    private int AddValueKernel;

    int planeWidth;
    int planeHeight;
    Vector2Int resolution;
    Vector3Int threadGroupAmount;

    // Start is called before the first frame update
    void Start()
    {
        /*h = new float[nx*ny];
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
        accBuffer.SetData(a);*/
        
        planeWidth = GetComponent<PlaneGenerator>().widthInput;
        planeHeight = GetComponent<PlaneGenerator>().heightInput;

        heightTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        heightTexture.Create();

        velTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        velTexture.Create();
        
        accTexture = new RenderTexture(planeWidth, planeHeight, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true
        };
        accTexture.Create();

        
        WaveEquationKernel = computeShader.FindKernel("WaveEquation");
        AddValueKernel = computeShader.FindKernel("AddValue");

        resolution = new Vector2Int(planeWidth, planeHeight);
        threadGroupAmount = new Vector3Int(planeWidth, planeHeight, 1);

        computeShader.GetKernelThreadGroupSizes(WaveEquationKernel, out uint xThreadGroupSize, out uint yThreadGroupSize, out uint zThreadGroupSize);
        threadGroupAmount = new Vector3Int(Mathf.CeilToInt(resolution.x / (float)xThreadGroupSize),  Mathf.CeilToInt(resolution.y / (float)yThreadGroupSize), Mathf.CeilToInt(1 / (float)zThreadGroupSize));


        computeShader.SetTexture(WaveEquationKernel, "heightTexture", heightTexture);
        computeShader.SetTexture(WaveEquationKernel, "velTexture", velTexture);
        computeShader.SetTexture(WaveEquationKernel, "accTexture", accTexture);

        
        computeShader.SetTexture(AddValueKernel, "heightTexture", heightTexture);

        computeShader.SetInt("planeWidth", planeWidth);
        computeShader.SetInt("planeHeight", planeHeight);
        computeShader.SetFloat("c", c);
        computeShader.SetFloat("dt", dt);

    }

    // Update is called once per frame
    void Update()
    {
        computeShader.Dispatch(WaveEquationKernel, planeWidth/10, planeHeight/10, 1);

        Renderer rend = GetComponent<Renderer> ();
        rend.material = new Material(updateVerticesShader);
        rend.material.SetTexture("importTexture", heightTexture);

       Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
       Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
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
                    
                    computeShader.SetInt("hitPosX", x);
                    computeShader.SetInt("hitPosY", y);

                    computeShader.Dispatch(AddValueKernel, 1, 1, 1);
                }
                
            }
        }
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
