using System;
using System.Collections;

[Serializable]
public struct ParticleDepositionConfig
{
    public int size;
    public int seed;
    public int deposition_time;
    public int deposition_deap;
    public int deposition_size;
    public float each_h;
}

/// <summary>
/// 粒子沉积
/// </summary>
public class HeightMapParticleDeposition 
{

    public static float[,] CreateMap(int size,int seed,int deposition_time,int deposition_deap,int deposition_size, float each_h,out float max)
    {
        float[,] ret = new float[size, size];
        Random rand = new Random(seed);
        for (int i = 0; i < deposition_time; i++)
        {
            int posx = rand.Next(size - 1);
            int posy = rand.Next(size - 1);
            for (int j = 0; j < deposition_deap; j++)
            {
                double angle = rand.NextDouble() * Math.PI * 2;

                int offset = rand.Next(deposition_size);
                int offsetx = (int)(Math.Sin(angle) * offset);
                int offsety = (int)(Math.Cos(angle) * offset);
                int tempx = posx;
                int tempy = posy;
                if (posx + offsetx >= 0 && posx + offsetx < size) tempx += offsetx;
                if (posy + offsety >= 0 && posy + offsety < size) tempy += offsety;
                Deposition(ret, size, tempx, tempy, each_h);
            }
        }
        max = 0;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (ret[j,i]>max)
                {
                    max = ret[j, i];
                }
            }
        }
        return ret;
    }

    public static void Deposition(float[,] map,int size,int pos_x,int pos_y,float each_h)
    {
        float target_h = map[pos_x, pos_y];
        if (pos_x>0 && map[pos_x-1,pos_y]+each_h<=target_h)
        {
            Deposition(map, size, pos_x - 1, pos_y, each_h);
        }
        else if (pos_y<size-1 && map[pos_x,pos_y+1]+each_h<=target_h)
        {
            Deposition(map, size, pos_x, pos_y+1, each_h);
        }
        else if (pos_x<size-1 && map[pos_x+1,pos_y]+each_h<=target_h)
        {
            Deposition(map, size, pos_x+1, pos_y, each_h);
        }
        else if (pos_y>0 && map[pos_x,pos_y-1]+each_h<=target_h)
        {
            Deposition(map, size, pos_x, pos_y - 1, each_h);
        }
        else if (pos_x>0&&pos_y<size-1&&map[pos_x-1,pos_y+1]+each_h<=target_h)
        {
            Deposition(map, size, pos_x-1, pos_y + 1, each_h);
        }
        else if (pos_x<size-1 && pos_y<size-1 && map[pos_x+1,pos_y+1]+each_h  <= target_h)
        {
            Deposition(map, size, pos_x+1, pos_y + 1, each_h);
        }
        else if (pos_x<size-1 && pos_y>0 && map[pos_x+1,pos_y-1]+each_h  <= target_h)
        {
            Deposition(map, size, pos_x+1, pos_y - 1, each_h);
        }
        else if (pos_x>0 && pos_y>0 && map[pos_x-1,pos_y-1]+each_h  <= target_h)
        {
            Deposition(map, size, pos_x-1, pos_y - 1, each_h);
        }
        else
        {
            map[pos_x, pos_y] += each_h;
        }
    }

}
