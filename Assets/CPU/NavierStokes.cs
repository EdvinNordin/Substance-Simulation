using System.Collections;
using System.Collections.Generic;
using UnityEditor.Build;
using UnityEngine;

public class NavierStokes : MonoBehaviour
{


    public int nx = 10; // Number of grid cells in the x-direction
    public int ny = 10; // Number of grid cells in the y-direction
    public Shader updateVerticesShader;

    public int iter = 1; //iterations of calculation of the whole thing?
    //int size;

    public float dt = 0.01f;
    public float visc = 0.1f;
    public float diffusion = 0.1f;

    float[,] source;
    float[,] density;

    float[,] Vx;
    float[,] Vy;

    float[,] Vx0; //Vx before
    float[,] Vy0; //Vy before

    float xPos;
    float yPos;

    float prevX;
    float prevY;
    public bool debug;
    List<GameObject> pointObjects;
    public GameObject prefab;
    private RenderTexture densityTexture;
    private Texture2D densityTexture2D;

    // Start is called before the first frame update
    void Start()
    {

        if (debug)
        {
            pointObjects = new List<GameObject>();
            //pointObjects.Add(Instantiate(prefab, new Vector3(1 / 2, 0, 1 / 2), Quaternion.identity));


            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    Vector3 Pos = new Vector3(i, 0, j);
                    //Debug.Log(i + " " + j);
                    var obj = Instantiate(prefab);
                    obj.transform.position = Pos;
                    obj.name = (i * nx + j).ToString();
                    pointObjects.Add(obj);
                }
            }
            /*prefab.GetComponent<SpriteRenderer>().enabled = false;
            prefab.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;*/
        }

        prevX = 0.0f;
        prevY = 0.0f;

        source = new float[nx, ny];
        density = new float[nx, ny];

        Vx = new float[nx, ny];
        Vy = new float[nx, ny];

        Vx0 = new float[nx, ny];
        Vy0 = new float[nx, ny];

        for (int i = 0; i < nx; i++)

        {
            for (int j = 0; j < ny; j++)
            {
                source[i, j] = 0f;
                density[i, j] = 0f;//Random.value;

                Vx[i, j] = 0.0f;
                Vy[i, j] = 0.0f;

                Vx0[i, j] = 0f;
                Vy0[i, j] = 0f;
            }
        }
        densityTexture2D = new Texture2D(nx, ny, TextureFormat.RFloat, false);
        densityTexture = new RenderTexture(nx, ny, 0, RenderTextureFormat.RFloat);
        densityTexture.enableRandomWrite = true;
        densityTexture.Create();

        Renderer rend = GetComponent<Renderer>();
        rend.material = new Material(updateVerticesShader);
        //rend.material.mainTexture = densityTexture;
        rend.material.SetTexture("importTexture", densityTexture);
    }


    // Update is called once per frame

    void Update()
    {
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                float value = density[i, j];
                densityTexture2D.SetPixel(i, j, new Color(value, value, value, 1.0f));
            }
        }
        densityTexture2D.Apply();

        // Copy the Texture2D to the RenderTexture
        Graphics.Blit(densityTexture2D, densityTexture);
        /*for (int i = 0; i < nx; i++)

        {
            for (int j = 0; j < ny; j++)
            {

                Vx[i, j] += Random.Range(-1f,1f);
                Vy[i, j] += Random.Range(-1f, 1f);

            }
        }*/
        xPos = -Input.mousePosition.y;
        yPos = Input.mousePosition.x;

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;






        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        //Debug.Log(triangles.Length);
        if (Input.GetMouseButton(0))
        {
            if (Physics.Raycast(ray, out hit))
            {
                //Debug.Log(GetClosestVertex(hit, triangles));
                MeshCollider meshCollider = hit.collider as MeshCollider;
                if (meshCollider != null && meshCollider.sharedMesh != null)
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int a = vertexHit / nx;
                    int b = vertexHit % nx;
                    source[a, b] += 30.0f;
                }
            }
        }
        float xAmount;// = xPos - prevX;
        float yAmount;// = yPos - prevY;
        if (Input.GetMouseButton(1))
        {
            if (Physics.Raycast(ray, out hit))
            {
                xPos = hit.point.x;
                yPos = hit.point.y;
                //Debug.Log(GetClosestVertex(hit, triangles));
                MeshCollider meshCollider = hit.collider as MeshCollider;

                if (meshCollider != null && meshCollider.sharedMesh != null)
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int a = vertexHit / nx;
                    int b = vertexHit % nx;



                    float sigma = 2f;
                    float mult = 1f;
                    int radius = 3;
                    if (a > radius && a < nx - radius && b > radius && b < ny - radius)
                    {
                        for (int i = 0; i < radius; i++)
                        {
                            for (int j = 0; j < radius; j++)
                            {

                                //xAmount = mult * (Mathf.Exp(-((i - 0) * (i - 0)) / (2 * sigma * sigma)) - 0.5f);
                                //yAmount = mult * (Mathf.Exp(-((j - 0) * (j - 0)) / (2 * sigma * sigma)) - 0.5f);
                                xAmount = Mathf.Atan(i - 1);
                                yAmount = Mathf.Atan(j - 1);
                                Vx0[a + i - 1, b + j - 1] += xAmount;
                                Vy0[a + i - 1, b + j - 1] += yAmount;
                                //Debug.Log("MIDDLE" + (a) + " " + (b));
                                //Debug.Log((a + i - 1) + ", " + (b + j - 1) + ": " + xAmount + ", " + yAmount);
                            }
                        }
                    }
                }
            }
        }




        //mixing and spreading velocities out. 1: left and right edge, 2: top and bottom edge
        diffuse(1, Vx0, Vx, visc);
        diffuse(2, Vy0, Vy, visc);

        //sets the boxes to equilibrium, has to be incompressible
        project(Vx0, Vy0, Vx, Vy);

        //moves velocity from velocity. 1: left and right edge, 2: top and bottom edge
        advect(1, Vx, Vx0, Vx0, Vy0);
        advect(2, Vy, Vy0, Vx0, Vy0);

        //sets the boxes to equilibrium, has to be incompressible
        project(Vx, Vy, Vx0, Vy0);




        //mixing and spreading out. 0: not att edge
        diffuse(0, source, density, diffusion);
        //diffuse(0, density, source, diffusion);

        //moves dye from velocity. 0: not att edge
        advect(0, density, source, Vx, Vy);


        /*
                for (int y = 0; y < ny; y++)
                {
                    for (int x = 0; x < nx; x++)
                    {
                        float value = density[x, y];
                        Color color = new Color(1, 1, 1, 1);
                        testTexture.SetPixel(x, y, Color.white);// + new Color(value, value, value, 0));
                    }
                }

                testTexture.Apply();

                Graphics.Blit(testTexture, densityTexture);
        */
        //Debug.Log(testTexture.GetPixel(0, 0));
        float[,] velocityMagnitude = calculateVelocityMagnitudeForVisualization(Vx, Vy);
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                vertices[i * nx + j].y = velocityMagnitude[i, j];// * density[i, j];
                mesh.vertices = vertices;
                mesh.RecalculateNormals();
            }
        }

        prevX = xPos;
        prevY = yPos;

        /*Vx = density;
        density = source;
        source = Vx;*/



        /*
                if (debug)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;

                    if (Input.GetMouseButton(0))
                    {
                        if (Physics.Raycast(ray, out hit))
                        {
                            //Debug.Log(GetClosestVertex(hit, triangles));
                            MeshCollider meshCollider = hit.collider as MeshCollider;
                            if (meshCollider != null && meshCollider.sharedMesh != null)
                            {
                                int test = 0;
                                string name = meshCollider.gameObject.transform.parent.ToString();
                                int nameLength = name.Length;
                                name = name.Replace(" (UnityEngine.Transform)", "");
                                bool suceed = int.TryParse(name, out test);
                                if (suceed)
                                {
                                    int a = test / nx;
                                    int b = test % nx;
                                    density[a, b] += 3.0f;
                                }
                            }
                        }
                    }
                    /*if (Input.GetMouseButton(1))
                    {
                        if (Physics.Raycast(ray, out hit))
                        {
                            //Debug.Log(GetClosestVertex(hit, triangles));
                            MeshCollider meshCollider = hit.collider as MeshCollider;
                            if (meshCollider != null && meshCollider.sharedMesh != null)
                            {
                                int test = 0;
                                string name = meshCollider.gameObject.transform.parent.ToString();
                                int nameLength = name.Length;
                                name = name.Replace(" (UnityEngine.Transform)", "");
                                bool suceed = int.TryParse(name, out test);
                                if(suceed){
                                    int a = test / nx;
                                    int b = test % nx;
                                    Vx[a,b] = xPos-prevX;
                                    Vy[a,b] = yPos-prevY;
                                }
                            }
                        }
                    }*/
        //}
        /*
        Vector3 Pos;
        for (var i = 0; i < nx; i++)
        {
            for (var j = 0; j < ny; j++)
            {
                Pos = pointObjects[j + i * nx].transform.position;
                pointObjects[j + i * nx].transform.position = new Vector3(Pos.x, density[i, j], Pos.z);
                GameObject Triangle = pointObjects[j + i * nx].transform.GetChild(0).gameObject;
                Triangle.transform.eulerAngles = new Vector3(0, getVelocity(i, j) * 180f / 3.14f, 0);
            }
        }
    */

    }


    public void addDensity(int x, int y, float amount)
    {
        density[x, y] += amount;
    }

    public void addVelocity(int x, int y, float amountX, float amountY)
    {
        Vx[x, y] += amountX;
        Vy[x, y] += amountY;
    }

    //mixing and spreading out
    public void diffuse(int bound, float[,] x, float[,] x0, float spread)
    {
        float a = dt * spread * (nx - 2) * (ny - 2);
        lin_solve(bound, x, x0, a, 1 + 4 * a);
    }

    //sets the boxes to equilibrium, incompressible
    public void project(float[,] velocX, float[,] velocY, float[,] p1, float[,] div1)
    {
        float[,] p = new float[nx, ny];
        float[,] div = new float[nx, ny];
        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                div[i, j] = -0.5f *
                    (velocX[i + 1, j] -
                      velocX[i - 1, j] +
                      velocY[i, j + 1] -
                      velocY[i, j - 1]) / (nx + ny / 2);
                p[i, j] = 0;
            }
        }

        set_bnd(0, div);
        set_bnd(0, p);
        lin_solve(0, p, div, 1, 4);

        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                velocX[i, j] -= 0.5f * (p[i + 1, j] - p[i - 1, j]) * nx;
                velocY[i, j] -= 0.5f * (p[i, j + 1] - p[i, j - 1]) * ny;
            }
        }

        set_bnd(1, velocX);
        set_bnd(2, velocY);

    }

    //move the stuff
    public void advect(int bound, float[,] d, float[,] d0, float[,] velocX, float[,] velocY)
    {
        float i0, i1, j0, j1;

        float dtx = dt * (nx - 2);
        float dty = dt * (ny - 2);

        float s0, s1, t0, t1;
        float tmp1, tmp2, x, y;

        float nxFloat = nx - 2;
        float nyFloat = ny - 2;
        float ifloat, jfloat;
        int i, j;

        for (i = 1, ifloat = 1; i < nx - 1; i++, ifloat++)
        {
            for (j = 1, jfloat = 1; j < ny - 1; j++, jfloat++)
            {
                tmp1 = dtx * velocX[i, j];
                tmp2 = dty * velocY[i, j];

                //x, y is the backtracked positions
                x = ifloat - tmp1;
                y = jfloat - tmp2;


                if (x < 0.5) x = 0.5f;
                if (x > nxFloat + 0.5) x = nxFloat + 0.5f;
                i0 = Mathf.Floor(x);
                i1 = i0 + 1.0f;

                if (y < 0.5) y = 0.5f;
                if (y > nyFloat + 0.5) y = nyFloat + 0.5f;
                j0 = Mathf.Floor(y);
                j1 = j0 + 1.0f;

                s1 = x - i0;
                s0 = 1.0f - s1;
                t1 = y - j0;
                t0 = 1.0f - t1;

                int i0i = (int)i0;
                int i1i = (int)i1;
                int j0i = (int)j0;
                int j1i = (int)j1;

                d[i, j] =
                  s0 * (t0 * d0[i0i, j0i] + t1 * d0[i0i, j1i]) +
                  s1 * (t0 * d0[i1i, j0i] + t1 * d0[i1i, j1i]);

                //if (velocX[i, j] > 1) Debug.Log(d[i, j]);
            }
        }

        set_bnd(bound, d);
    }

    public void lin_solve(int bound, float[,] x, float[,] x0, float a, float c)
    {
        float cRecip = 1.0f / c;
        for (int t = 0; t < iter; t++)
        {
            for (int j = 1; j < nx - 1; j++)
            {
                for (int i = 1; i < ny - 1; i++)
                {
                    x[i, j] =
                      (x0[i, j] + a *
                          (x[i + 1, j] +
                            x[i - 1, j] +
                            x[i, j + 1] +
                            x[i, j - 1])) *
                      cRecip;
                }
            }
            set_bnd(bound, x);
        }
    }

    //set bounding box
    public void set_bnd(int bound, float[,] x)
    {
        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                x[i, 0] = bound == 2 ? -x[i, 1] : x[i, 1]; // if bound = 2 is true x = -x else x = x 
                x[i, ny - 1] = bound == 2 ? -x[i, ny - 2] : x[i, ny - 2];

            }
        }
        for (int i = 1; i < nx - 1; i++)
        {
            for (int j = 1; j < ny - 1; j++)
            {
                x[0, j] = bound == 1 ? -x[1, j] : x[1, j];
                x[nx - 1, j] = bound == 1 ? -x[nx - 2, j] : x[nx - 2, j];

            }
        }

        x[0, 0] = 1;//-0.5f * (x[1, 0] + x[0, 1]);
        x[0, ny - 1] = 1;//-0.5f * (x[1, ny - 1] + x[0, ny - 2]);
        x[nx - 1, 0] = 1;//-0.5f * (x[nx - 2, 0] + x[nx - 1, 1]);
        x[nx - 1, ny - 1] = 1;//-0.5f * (x[nx - 2, ny - 1] + x[nx - 1, ny - 2]);
    }

    public float getVelocity(int i, int j)
    {
        float x = Vx[i, j];
        float y = Vy[i, j];
        float dir = Mathf.Atan(y / x);

        if (x == 0)
        {
            dir = Mathf.Atan(y / (x + 0.00000001f));
        }

        return dir;
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

    // Method to calculate and normalize velocity magnitude for visualization
    public float[,] calculateVelocityMagnitudeForVisualization(float[,] velocX, float[,] velocY)
    {
        int nx = velocX.GetLength(0);
        int ny = velocX.GetLength(1);
        float[,] velocityMagnitude = new float[nx, ny];
        float maxMagnitude = 0.0f;

        // Calculate velocity magnitude
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                float magnitude = Mathf.Sqrt(velocX[i, j] * velocX[i, j] + velocY[i, j] * velocY[i, j]);
                velocityMagnitude[i, j] = magnitude;
                if (magnitude > maxMagnitude)
                {
                    maxMagnitude = magnitude;
                }
            }
        }

        // Normalize magnitudes to [0, 1]
        if (maxMagnitude > 0)
        {
            for (int i = 0; i < nx; i++)
            {
                for (int j = 0; j < ny; j++)
                {
                    velocityMagnitude[i, j] /= maxMagnitude;
                }
            }
        }

        return velocityMagnitude;
    }
}
