
using System;
using UnityEngine;

[Serializable]
public struct FIRFilterConfig
{
    public int times;
    [Range(0,1)]
    public float k;
}

public class HeightMapFilter 
{
    public static void FilterFIR(float[,] map,int size,int times,float k)
    {
        for (int t = 0; t < times; t++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (x == 0 )
                        map[x, y] = map[x,y]*k + (1 - k) * map[x, y];
                    else
                        map[x, y] = map[x - 1, y] * k + (1 - k) * map[x, y];
                }
            }
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (y == 0)
                        map[x, y] = map[x, y] * k + (1 - k) * map[x, y];
                    else
                        map[x, y] = map[x, y - 1] * k + (1 - k) * map[x, y];
                }
            }
        }
    }
}
