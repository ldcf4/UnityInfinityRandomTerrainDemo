
using System;

[Serializable]
public struct LineMapConfig
{
    public int size;
    public int seed;
    public int max;
    public int line_num;
    public int smooth_step;
}

public class HeightMapFuzzyLandscaping
{
    public static float[,] CreateHeightMap(int size,int seed,float max,int step)
    {
        float[,] map = new float[size, size];
        Random ran = new Random(seed);
        
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                map[j, i] = (float)ran.NextDouble() * max;
            }
        }

        //JamesMcNeillSmooth(map, size);
        Smooth(map, size, step);

        return map;
    }
    
    public static float[,] CreateMapByLine(int size ,int seed ,float max,int line_num,int smooth_step)
    {
        float[,] map = new float[size, size];
        Random ran = new Random(seed);

        for (int line_index = 0; line_index < line_num; line_index++)
        {
            float x_start = ran.Next(size - 1);
            float y_start = ran.Next(size - 1);
            double angle = ran.NextDouble() * Math.PI * 2;
            float x_diff = (float)Math.Sin(angle);
            float y_diff = (float)Math.Cos(angle);
            float curt_value = (float)ran.NextDouble() * max;
            do
            {
                map[(int)x_start, (int)y_start] = curt_value;
                x_start = x_start + x_diff;
                y_start = y_start + y_diff;
                if (x_diff*x_diff<y_diff*y_diff)
                {
                    curt_value = (float)Math.Sin(x_start) * max + max;
                }
                else
                {
                    curt_value = (float)Math.Sin(y_start) * max + max;
                }
            } while ((y_start<size)&&(y_start>0f)
                &&(x_start<size)&&(x_start>0f));
        }

        //JamesMcNeillSmooth(map, size);
        Smooth(map, size, smooth_step);

        return map;
    }

    public static void JamesMcNeillSmooth(float[,] map,int width)
    {
        //int row_offset = 0;
        int row_offset = width;

        for (int square_size = width; square_size > 1; square_size /= 2)
        {
            for (int x1 = row_offset; x1 < width; x1+=square_size)
            {
                for (int y1 = row_offset; y1 < width; y1+= square_size)
                {
                    //Calculate the four corner offsets
                    int x2 = ((x1 + square_size) % width);
                    int y2 = ((y1 + square_size) % width);

                    float i1 = map[x1, y1];
                    float i2 = map[x2, y1];
                    float i3 = map[x1, y2];
                    float i4 = map[x2, y2];

                    //weighted averages , 权重平均值
                    float p1 = ((i1 * 9) + (i2 * 3) + (i3 * 3) + (i4)) / 16;
                    float p2 = ((i1 * 3) + (i2 * 9) + (i3) + (i4 * 3)) / 16;
                    float p3 = ((i1 * 3) + (i2) + (i3 * 9) + (i4 * 3)) / 16;
                    float p4 = ((i1) + (i2 * 3) + (i3 * 3) + (i4 * 9)) / 16;

                    //Calcuate the center points of each quadrant
                    int x3 = ((x1 + square_size / 4) % width);
                    int y3 = ((y1 + square_size / 4) % width);
                    x2 = ((x3 + square_size/2) % width);
                    y2 = ((y3 + square_size/2) % width);

                    //set the points to the averages calcuated above
                    //设置上面计算得的平均值
                    map[x3, y3] = p1;
                    map[x2, y3] = p2;
                    map[x3, y2] = p3;
                    map[x2, y2] = p4;
                }
            }
            row_offset = square_size / 4;
        }
    }

    public static void Smooth(float[,] map,int size,int step)
    {
        for (int y = 0; y < size; y+=step)
        {
            for (int x = 0; x < size; x+=step)
            {
                float total = 0;
                for (int y_local = y; y_local < y+step ; y_local++)
                {
                    for (int x_local = x; x_local < x+step; x_local++)
                    {
                        total += map[x_local, y_local];
                    }
                }
                float avg = total / (step * step);
                for (int y_local = y; y_local < y+step; y_local++)
                {
                    for (int x_local = x; x_local < x+step; x_local++)
                    {
                        map[x_local, y_local] = avg;
                    }
                }
            }
        }
    }
}
