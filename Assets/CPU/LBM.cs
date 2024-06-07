using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LBM : MonoBehaviour
{

    private const int nx = 100;
    private const int ny = 100;
    private const int q = 9;

    private float[,,] f = new float[nx, ny, q]; // Distribution functions
    private float[,,] fnew = new float[nx, ny, q]; // New distribution functions post-collision
    private float[,,] feq = new float[nx, ny, q]; // Equilibrium distribution functions

    private float[,] momentX = new float[nx, ny]; // x-component of momentum
    private float[,] momentY = new float[nx, ny]; // x-component of momentum
    private float[,] inputVelX = new float[nx, ny]; // x-component of momentum
    private float[,] inputVelY = new float[nx, ny]; // x-component of momentum

    private float[,] velX = new float[nx, ny]; // x-component of velocity
    private float[,] velY = new float[nx, ny]; // y-component of velocity

    private float[,] rho = new float[nx, ny];   // Density
    private float[,] inputRho = new float[nx, ny];   // Density

    // D2Q9 and Weights
    //private int[] latticeDirX = { 0, 1, 0, -1, 0, 1, -1, -1, 1 };
    private int[] latticeDirX = { -1, 0, 1, -1, 0, 1, -1, 0, 1 };
    //private int[] latticeDirY = { 0, 0, 1, 0, -1, 1, 1, -1, -1 };
    private int[] latticeDirY = { 1, 1, 1, 0, 0, 0, -1, -1, -1 };
    private float[] latticeWeight = { 1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 4.0f / 9.0f, 1.0f / 9.0f, 1.0f / 36.0f, 1.0f / 9.0f, 1.0f / 36.0f  };

    public const float tau = 1.0f; //Relaxation time 0.5 to 2

    List<GameObject> pointObjects;

    public GameObject prefab;

    float xPos;
    float yPos;

    float prevX;
    float prevY;

    // Start is called before the first frame update
    void Start()
    {
         /*pointObjects = new List<GameObject>();

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                GameObject newPoint = Instantiate(prefab, new Vector3(i - nx / 2, 0, j - ny / 2), Quaternion.identity);
                pointObjects.Add(newPoint);
                newPoint.name = "Point:" + i + "," + j;
            }
        }*/

        prevX = 0.0f;
        prevY = 0.0f;

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                velX[i, j] = 0.0f;
                velY[i, j] = 0.0f;
                rho[i, j] = 0.0f;
                inputRho[i, j] = 0.0f;
                inputVelX[i, j] = 0.0f;
                inputVelY[i, j] = 0.0f;

                /*if(i == nx/2 && j == ny/2){
                    inputRho[i, j] = 2.0f;
                }*/

                

            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        //collsion step
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {

                float rhoTmp = 0;
                float mXTmp = 0;
                float mYTmp = 0;

                for (int k = 0; k < q; k++)
                {
                    rhoTmp += f[i, j, k];
                    mXTmp += f[i, j, k] * latticeDirX[k];
                    mYTmp += f[i, j, k] * latticeDirY[k];
                }


                if(rhoTmp < 0.0001f)
                {
                    rhoTmp = 0.0001f;
                }

                //velocity = momentum / rho(density)
                rho[i, j] = rhoTmp+inputRho[i,j];
                velX[i, j] = mXTmp / rhoTmp;// + inputVelX[i,j];
                velY[i, j] = mYTmp / rhoTmp;// + inputVelY[i,j];
                
                //velX[i, j] += inputVelX[i, j];
                //velY[i, j] += inputVelY[i, j];

                inputRho[i,j] = 0.0f;
                float distance = Mathf.Sqrt(velX[i, j] * velX[i, j] + velY[i, j] * velY[i, j]);
                

                if(distance > 1.0f){
                   
                    //velX[i, j] = velX[i, j] / distance;
                    //velY[i, j] = velY[i, j] / distance;               

                }
                
                float uu = velX[i, j] * velX[i, j] + velY[i, j] * velY[i, j];
                for (int k = 0; k < q; k++)
                {
                    float vu = latticeDirX[k] * velX[i, j] + latticeDirY[k] * velY[i, j];

                    feq[i, j, k] = latticeWeight[k] * rho[i, j] * (1 + 3 * vu + 9 / 2 * vu * vu - 3 / 2 * uu);
                    fnew[i, j, k] = f[i, j, k] -(f[i, j, k] - feq[i, j, k]) / tau;
                    
                }
            }
        }


        // Streaming step
        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                /*for (int k = 0; k < q; k++)
                {
                    int nextX = i + latticeDirX[k];
                    int nextY = j + latticeDirY[k];

                    // If the next cell is inside the domain, move the particle to the next cell
                    if(nextX >= 0 && nextX < nx && nextY >= 0 && nextY < ny)
                    {
                        f[nextX, nextY, k] = fnew[i, j, k];
                    }
                    // If the next cell is outside the domain, apply the outflow boundary condition
                    else
                    {
                        f[i, j, k] = 0f;//fnew[i, j, k];
                    }
                }*/
                

                for (int k = 0; k < 9; ++k)
                {
                    int test = Mathf.Abs((int)Mathf.Clamp((fnew[i, j, k]*1000), -5, 5));
                    if(test > 1){
                        //Debug.Log(i + ", " + j);
                    }
                    int nextX = i + latticeDirX[k] * test;
                    int nextY = j + latticeDirY[k] * test;
                    
                    
                    if (nextX < 0 && nextY < 0){ // top left
                           f[0, 0, k] = fnew[i,j,k];
                        
                    }
                    else if (nextX < 0 && nextY >= ny){ //bottom left
                        
                            f[0, ny-1, k] = fnew[i,j,k];
                        
                    }
                    else if (nextX >= nx && nextY < 0){ // top right
                           f[nx-1, 0, k] = fnew[i,j,k];
                        
                    }
                    else if (nextX >= nx && nextY >= ny){ // bottom right
                           f[nx-1, ny-1, k] = fnew[i,j,k];
                        
                    }
                    
                    else if(nextX < 0)
                    {

                        
                        f[0, nextY, k] = fnew[i,j,k];
                        
                        
                    }   
                    else if(nextX >= nx)
                    {
                        
                            //f[i,j,k-2] += fnew[i,j,k];
                            //f[i, j, k] = 0;
                            
                            f[nx-1, nextY, k] = fnew[i,j,k];
                        
                        
                    }
                    else if(nextY < 0)
                    {
                         //f[i,j,k+6] += fnew[i,j,k];
                            //f[i, j, k] = 0;
                            f[nextX, 0, k] = fnew[i,j,k];
                        
                        
                    }
                    else if(nextY >= ny)
                    {
                        
                            //f[i,j,k-6] += fnew[i,j,k];
                            //f[i, j, k] = 0;
                            f[nextX, ny-1, k] = fnew[i,j,k];
                        
                        
                    }
                    else
                    {
                        f[nextX, nextY, k] = fnew[i, j, k];
                    }
                }
            }
        }

        Mesh mesh = GetComponent<MeshFilter>().mesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
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

                    //string name = hit.transform.gameObject.name;
                    //int pointHit = int.Parse(name.Substring(name.Length - 3, name.Length - 2));
                    int vertexHit = GetClosestVertex(hit, triangles);
                    //pointObjects[0].transform.position = hit.point;
                    int a = vertexHit / nx;
                    int b = vertexHit % nx;
                    inputRho[a, b] += 0.001f;
                }
            }
        }

        /*if (Input.GetMouseButton(1))
        {
            if (Physics.Raycast(ray, out hit))
            {
                xPos = hit.point.x;
                yPos = hit.point.y;
                //Debug.Log(GetClosestVertex(hit, triangles));
                MeshCollider meshCollider = hit.collider as MeshCollider;

                if (meshCollider == null || meshCollider.sharedMesh == null)
                {
                    Debug.Log("nope");

                }
                else
                {

                    int vertexHit = GetClosestVertex(hit, triangles);
                    int a = vertexHit / nx;
                    int b = vertexHit % nx;
                    inputVelX[a, b] += 0.10f * (xPos - prevX);
                    inputVelY[a, b] += 0.10f * (yPos - prevY);
                }
            }
        }*/


        for (var i = 0; i<nx*ny; i++)//vertices.Length; i++)
        {
            int a = i / nx;
            int b = i % nx;
            float temp = 0.0f;
            
            temp = rho[a, b];
            
            /*pointObjects[i].transform.eulerAngles = new Vector3(0,getVelocity(a,b)*180f/3.14f,0);
            
            Vector3 pos = pointObjects[i].transform.position;
            pointObjects[i].transform.position = new Vector3 (pos.x, temp, pos.z);
            // = new Vector3(0, temp, 0);*/
            vertices[i].y = temp*100;
        }

        //Debug.Log(CalculateReynoldsNumber(nx));
        //Debug.Log(CalculateCFL());

        mesh.vertices = vertices;
        mesh.RecalculateNormals();

        prevX = xPos;
        prevY = yPos;
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

    public float getVelocity(int i, int j)
    {
        float x = velX[i,j];
        float y = velY[i, j];
        float dir = Mathf.Atan(y / x);

        if (x == 0)
        {
            dir = Mathf.Atan(y / (x + 0.00000001f));
        }


        return dir;
    }
    public float CalculateAverageVelocity()
    {
        float totalVelocity = 0.0f;
        int count = 0;

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                float velocityMagnitude = Mathf.Sqrt(velX[i, j] * velX[i, j] + velY[i, j] * velY[i, j]);
                totalVelocity += velocityMagnitude;
                count++;
            }
        }

        return totalVelocity / count;
    }

    public float CalculateReynoldsNumber(float characteristicLength)
    {
        float totalVelocity = 0.0f;
        int count = 0;

        for (int i = 0; i < nx; i++)
        {
            for (int j = 0; j < ny; j++)
            {
                float velocityMagnitude = Mathf.Sqrt(velX[i, j] * velX[i, j] + velY[i, j] * velY[i, j]);
                totalVelocity += velocityMagnitude;
                count++;
            }
        }
        float characteristicVelocity = totalVelocity / count;

        float nu = (2 * tau - 1) / 6.0f;
        float reynoldsNumber = (characteristicVelocity * characteristicLength) / nu;
        return reynoldsNumber;
}

public float CalculateCFL()
{
    float maxVelocity = 0.0f;

    // Find the maximum velocity in the domain
    for (int i = 0; i < nx; i++)
    {
        for (int j = 0; j < ny; j++)
        {
            float velocityMagnitude = Mathf.Sqrt(velX[i, j] * velX[i, j] + velY[i, j] * velY[i, j]);
            if (velocityMagnitude > maxVelocity)
            {
                maxVelocity = velocityMagnitude;
            }
        }
    }

    float dt = 1.0f;

    float dx = 1.0f;

    float CFL = maxVelocity * dt / dx;

    return CFL;
}
}


                    /* if(nextX < 0 && nextY < 0){
                        if(k == 0) {
                            //f[i,j,6] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 1){
                            //f[i,j,7] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 2){
                            //f[i,j,6] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 5){
                            //f[i,j,3] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 8){
                            //f[i,j,6] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        f[i, j, k]= fnew[i, j, k];
                    }
                    else if(nextX >= nx && nextY < 0){
                        if(k == 2) {
                            //f[i,j,6] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 1){
                            //f[i,j,5] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 0){
                            //f[i,j,6] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 3){
                            //f[i,j,8] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 5){
                            //f[i,j,6] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        f[i, j, k]= fnew[i, j, k];
                    }
                    else if(nextX < 0 && nextY >= ny){
                        if(k == 0) {
                            //f[i,j,2] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 3){
                            //f[i,j,5] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 6){
                            //f[i,j,2] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 7){
                            //f[i,j,1] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 8){
                            //f[i,j,2] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        f[i, j, k]= fnew[i, j, k];
                    }
                    else if(nextX >= nx && nextY >= ny){
                        if(k == 2) {
                            //f[i,j,0] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 3){
                            //f[i,j,1] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 8){
                            //f[i,j,0] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 7){
                            //f[i,j,1] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        else if(k == 0){
                            //f[i,j,2] += fnew[i,j,k];
                            f[i, j, k] = 0;
                        }
                        f[i, j, k]= fnew[i, j, k];
                    } */