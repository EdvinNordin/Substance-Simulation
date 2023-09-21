using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class SubstanceController : MonoBehaviour
{
    static int Nx = 10;
    static int Ny = 10;
    float tau = 0.53f;
    int Nt = 3000;

    //lattice speeds and weightes
    static int NL = 9;
    int[] cxs = { 0, 0, 1, 1,  1,  0, -1, -1, -1 };
    int[] cys = { 0, 1, 1, 0, -1, -1, -1,  0,  1 };
    int[] weights = { 4/9, 1/9, 1/36, 1/9, 1/36, 1/9, 1/36, 1/9, 1/36 };

    //initial conditions
    //misoscopic velocities
    float[,,] F = new float [Nx,Ny,NL];

    

    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < Nx; i++){

            for (int j = 0; j < Ny; j++)
            {
                for(int k = 0; k < NL; k++)
                {
                    F[i, j, k] = 1.0f;
                }

            }
        }

        //Debug.Log(F[9, 5, 5]);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
