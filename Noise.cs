using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    public static float[] PerlinNoise(int seed, int scaleX, int scaleZ, float amplitude, float scale,  int resolution)
    {
        //generate perlin noise based on seed, scale, dimensions and resolution.
        float[] perlinNoise = 
        new float[(scaleX * resolution + 1) *
        (scaleZ * resolution + 1)];
        System.Random prng = new System.Random(seed);
        int offset = prng.Next(-100000, 100000);
        int i = 0;
        float adjustedScale = scale / resolution + 0.00101f;

        for (int z = 0; z <= scaleZ * resolution; z++)
        {
            for (int x = 0; x <= scaleX * resolution; x++)
            {
                float nx = (float)x / resolution;
                float nz = (float)z / resolution;

                perlinNoise[i] = Mathf.PerlinNoise(nx * adjustedScale + offset, nz * adjustedScale + offset);
                perlinNoise[i] *= amplitude;
                i++;
            }
        }
        return perlinNoise;
        
    }
}
