using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Points : MonoBehaviour
{

    List<GameObject> pointObjects;

    public GameObject prefab;
    public Material waterMaterialRef;
    public bool debug;
    public int N;
    public int iter;
    public float dt;
    public float diffusion;
    public float viscosity;

    Fluid fluid;
    Color defaultColor;
    Vector3 mousePos, Pos = new Vector3(0,0,0);
    Vector3 point = new Vector3();
    float xPos, yPos;
    float density = 0;
    float prevX, prevY, randX, randY, temp = 0f;
    GameObject Triangle;
    // Start is called before the first frame update
    void Start()
    {
        Camera.main.orthographicSize = N/2;
        Camera.main.transform.position = new Vector3(0, -0.5f, -100);
        Camera.main.orthographic = false;
        Camera.main.fieldOfView = 54.0f;
        Camera.main.farClipPlane = 110.0f;

        pointObjects = new List<GameObject>();

        //dt, diffusion (mixing), viscosity
        fluid = new Fluid(dt, diffusion, viscosity, iter, N);

        fluid.addDensity(N / 2, N / 2, 700, N);
        if (debug)
        {
            prefab.GetComponent<SpriteRenderer>().enabled = false;
            prefab.transform.GetChild(0).GetComponent<SpriteRenderer>().enabled = true;
        }


        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {
                temp = Mathf.PerlinNoise(i * 10f / (N + 1f), j * 10f / (N + 1f)) * 2 - 1;
                //Debug.Log(temp);
                fluid.addDensity(i, j, 10*temp, N);
                
                

                pointObjects.Add(Instantiate(prefab, new Vector3(i-N / 2, j-N / 2, 0), Quaternion.identity));

                

            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        xPos = Mathf.Lerp(-N, N, Mathf.InverseLerp(0, 1100, Input.mousePosition.x)) + N / 2;
        yPos = Mathf.Lerp(-N / 2, N / 2, Mathf.InverseLerp(0, 510, Input.mousePosition.y)) + N / 2;

        //Debug.Log("!"xPos);
        point = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, Camera.main.nearClipPlane));
        xPos = point.x;
        yPos = point.y;
        //Debug.Log(xPos);


        Color color = Color.white;

        if (Input.GetMouseButton(1))
        {
            fluid.addVelocity((int)yPos, (int)xPos, 10 * (yPos - prevY), 10 * (xPos - prevX), N);
            Debug.Log(xPos);
        }

        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N; j++)
            {

                if (Input.GetMouseButton(0))
                {
                    fluid.addDensity((int)yPos, (int)xPos, 1.001f, N);
                    Debug.Log((int)yPos);
                }

                Triangle = pointObjects[j + i * N].transform.GetChild(0).gameObject;
                Triangle.transform.eulerAngles = new Vector3(0,0,fluid.getVelocity(j,i)*180f/3.14f);
                density = fluid.getDensity(j + i * N)/100f;

                Pos = pointObjects[j + i * N].transform.position;
                pointObjects[j + i * N].transform.position = new Vector3(Pos.x, Pos.y, -density);
                color = new Color(density, density, density) * waterMaterialRef.color + defaultColor;
                pointObjects[j + i * N].GetComponent<SpriteRenderer>().material.color = color;
            }
        }
        prevX = xPos;
        prevY = yPos;
        fluid.step();
    }

}