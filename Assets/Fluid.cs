using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Fluid
{
    int N;
    int iter;
    int size;

    float dt;
    float diff;
    float visc;

    float[] s;
    float[] density;

    float[] Vx;
    float[] Vy;

    float[] Vx0; //Vx before
    float[] Vy0; //Vy before

    public Fluid(float dt, float diffusion, float viscosity,int iter, int N)
    {
        this.N = N;
        this.iter = iter;
        this.size = N;

        this.dt = dt;
        this.diff = diffusion;
        this.visc = viscosity;

        this.s = new float[N * N];
        this.density = new float[N * N];

        this.Vx = new float[N * N];
        this.Vy = new float[N * N];

        this.Vx0 = new float[N * N];
        this.Vy0 = new float[N * N];

        for(int i = 0; i < N * N; i++)
        {
            s[i] = 0f;
            density[i] = 0f;

            Vx[i] = 0f;
            Vy[i] = 0f;

            Vx0[i] = 0f;
            Vy0[i] = 0f;
        }

    }

    public void step()
    {
        int iter = this.iter;
        int N = this.size;
        float visc = this.visc;
        float diff = this.diff;
        float dt = this.dt;
        float[] Vx = this.Vx;
        float[] Vy = this.Vy;
        float[] Vx0 = this.Vx0;
        float[] Vy0 = this.Vy0;
        float[] s = this.s;
        float[] density = this.density;

        
        //mixing and spreading out. 1: left and right edge, 2: top and bottom edge
        diffuse(1, Vx0, Vx, visc, dt, iter, N);
        diffuse(2, Vy0, Vy, visc, dt, iter, N);

        //sets the boxes to equilibrium, has to be incompressible
        project(Vx0, Vy0, Vx, Vy, iter, N);

        //moves dye from velocity. 1: left and right edge, 2: top and bottom edge
        advect(1, Vx, Vx0, Vx0, Vy0, dt, N);
        advect(2, Vy, Vy0, Vx0, Vy0, dt, N);

        //sets the boxes to equilibrium, has to be incompressible
        project(Vx, Vy, Vx0, Vy0, iter, N);

        //mixing and spreading out. 0: not att edge
        diffuse(0, s, density, diff, dt, iter, N);

        //moves dye from velocity. 0: not att edge
        advect(0, density, s, Vx, Vy, dt, N);


    }

    public int IX(int x, int y, int N)
    {
        x = Mathf.Clamp(x, 0, N - 1);
        y = Mathf.Clamp(y, 0, N - 1);
        return x + y * N;
    }

    public void addDensity(int x, int y, float amount, int N)
    {
        int index = IX(x, y, N);
        this.density[index] += amount;
    }

    public void addVelocity(int x, int y, float amountX, float amountY, int N)
    {
        int index = IX(x, y, N);

        this.Vx[index] += amountX;
        this.Vy[index] += amountY;
    }

    //mixing and spreading out
    public void diffuse (int b, float[] x, float[] x0, float diff, float dt, int iter, int N)
    {
        float a = dt * diff * (N - 2) * (N - 2);
        lin_solve(b, x, x0, a, 1 + 4 * a, iter, N);
    }

    //linear solve
    public void lin_solve(int b, float[] x,float[] x0,float a,float c, int iter, int N)
    {
        float cRecip = 1.0f / c;
        for (int t = 0; t < iter; t++)
        {
            for (int j = 1; j < N - 1; j++)
            {
                for (int i = 1; i < N - 1; i++)
                {                      
                    x[IX(i, j, N)] =
                      (x0[IX(i, j, N)] + a *
                          (x[IX(i + 1, j, N)] +
                            x[IX(i - 1, j, N)] +
                            x[IX(i, j + 1, N)] +
                            x[IX(i, j - 1, N)])) *
                      cRecip;
                }
            }
            set_bnd(b, x, N);
        }
    }

    //sets the boxes to equilibrium, incompressible
    public void project(float[] velocX, float[] velocY, float[] p, float[] div, int iter, int N) {
        for (int j = 1; j < N - 1; j++) {
            for (int i = 1; i < N - 1; i++) {
                div[IX(i, j, N)] =  (-0.5f *
                    (velocX[IX(i + 1, j, N)] -
                      velocX[IX(i - 1, j, N)] +
                      velocY[IX(i, j + 1, N)] -
                      velocY[IX(i, j - 1, N)])) /N;
                p[IX(i, j, N)] = 0;
            }
        }

        set_bnd(0, div, N);
        set_bnd(0, p, N);
        lin_solve(0, p, div, 1, 4, iter, N);

        for (int j = 1; j < N - 1; j++)
        {
            for (int i = 1; i < N - 1; i++)
            {
                velocX[IX(i, j, N)] -= 0.5f * (p[IX(i + 1, j, N)] - p[IX(i - 1, j, N)]) * N;
                velocY[IX(i, j, N)] -= 0.5f * (p[IX(i, j + 1, N)] - p[IX(i, j - 1, N)]) * N;
            }
        }

        set_bnd(1, velocX, N);
        set_bnd(2, velocY, N);

    }

    //move the stuff
    public void advect(int b, float[] d, float[] d0, float[] velocX,float[] velocY, float dt, int N)
    {
        float i0, i1, j0, j1;

        float dtx = dt * (N - 2);
        float dty = dt * (N - 2);

        float s0, s1, t0, t1;
        float tmp1, tmp2, x, y;

        float Nfloat = N - 2;
        float ifloat, jfloat;
        int i, j;

        for (j = 1, jfloat = 1; j < N - 1; j++, jfloat++)
        {
            for (i = 1, ifloat = 1; i < N - 1; i++, ifloat++)
            {
                tmp1 = dtx * velocX[IX(i, j, N)];
                tmp2 = dty * velocY[IX(i, j, N)];
                x = ifloat - tmp1;
                y = jfloat - tmp2;

                if (x < 0.5) x = 0.5f;
                if (x > Nfloat + 0.5) x = Nfloat + 0.5f;
                i0 = Mathf.Floor(x);
                i1 = i0 + 1.0f;
                if (y < 0.5) y = 0.5f;
                if (y > Nfloat + 0.5) y = Nfloat + 0.5f;
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

                d[IX(i, j, N)] =
                  s0 * (t0 * d0[IX(i0i, j0i, N)] + t1 * d0[IX(i0i, j1i, N)]) +
                  s1 * (t0 * d0[IX(i1i, j0i, N)] + t1 * d0[IX(i1i, j1i, N)]);
            }
        }

        set_bnd(b, d, N);
    }

    //set bounding box
    public void set_bnd(int b, float[] x, int N)
    {
        for (int i = 1; i < N-1; i++)
        {

            x[IX(i, 0, N)] = b == 2 ? -x[IX(i, 1, N)] : x[IX(i, 1, N)];
            x[IX(i, N - 1, N)] = b == 2 ? -x[IX(i, N - 2, N)] : x[IX(i, N - 2, N)];

        }
        for (int j = 1; j < N-1; j++)
        {
            x[IX(0, j, N)] = b == 1 ? -x[IX(1, j, N)] : x[IX(1, j, N)];
            x[IX(N - 1, j, N)] = b == 1 ? -x[IX(N - 2, j, N)] : x[IX(N - 2, j, N)];
        }

        x[IX(0, 0, N)] = 0.5f * (x[IX(1, 0, N)] + x[IX(0, 1, N)]);
        x[IX(0, N - 1, N)] = 0.5f * (x[IX(1, N - 1, N)] + x[IX(0, N - 2, N)]);
        x[IX(N - 1, 0, N)] = 0.5f * (x[IX(N - 2, 0, N)] + x[IX(N - 1, 1, N)]);
        x[IX(N - 1, N - 1, N)] = 0.5f * (x[IX(N - 2, N - 1, N)] + x[IX(N - 1, N - 2, N)]);
       //x[IX(0, N - 1, N)] = 1000f; //y,x,N
    }

    public float getDensity(int i)
    {
        return this.density[i];
    }

    public float fadeDensity(int i, float amount)
    {
        this.density[i] -= amount;
        return this.density[i];
    }
    public float getVelocity(int i, int j)
    {
        float x = this.Vx[IX(i,j,N)];
        float y = this.Vy[IX(i, j, N)];
        float dir = Mathf.Atan(y / x);

        if (x == 0)
        {
            dir = Mathf.Atan(y / (x + 0.00000001f));
        }


        return dir;
    }
}