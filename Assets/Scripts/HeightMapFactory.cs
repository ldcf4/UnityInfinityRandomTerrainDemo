using System;
using UnityEngine;

public enum TerrainType
{
    Normal, //普通
    Basin,  //盆地
    Hill    //山
}

[Serializable]
public struct HeightMapConfig
{
    [Range(5,14)]
    public int Iterations;
    [Range(0.1f,1f)]
    public float H;
    public float min;
    public float max;
    public int seed;
    public TerrainType type;
}

/**
    随机分形地形生成
    http://www.cnblogs.com/lookof/archive/2009/03/18/1415259.html
    ************/

class HeightMapFactory
{
    /// <summary>
    /// 使用分形算法(菱形-四边形)计算出高度图
    /// </summary>
    /// <param name="Iterations">迭代次数</param>
    /// <param name="H">粗糙度常数</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="seed">随机数种子</param>
    /// <param name="type">类型</param>
    public static float[,] CreateHeightMapByFractal(int Iterations, float H, float min, float max, int seed, TerrainType type)
    {
        int Segment = 1 << Iterations;
        float[,] heightmap = new float[Segment + 1, Segment + 1];
        for (int i = 0; i < Segment + 1; i++)
        {
            for (int j = 0; j < Segment + 1; j++)
            {
                heightmap[j, i] = -100;
            }
        }
        GeneralHeightMapByFractal(Iterations, H, min, max, seed, type, heightmap);
        return heightmap;
    }

    /// <summary>
    /// 使用分形算法计算出高度图
    /// </summary>
    /// <param name="Iterations">迭代次数</param>
    /// <param name="H">粗糙度常数</param>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    /// <param name="seed">随机数种子</param>
    /// <param name="type">类型</param>
    public static void GeneralHeightMapByFractal(int Iterations, float H, float min, float max,int seed,TerrainType type, float[,] heightmap)
    {
        int Segment = 1 << Iterations;
        float random_range = (max - min);
        var rand = new System.Random(seed);
        float centerh = min + random_range * 0.5f;
        float rd = 0;
        if (type==TerrainType.Normal) //普通
        {
            heightmap[0, 0] = rd + centerh;
            heightmap[Segment, 0] = rd + centerh;
            heightmap[0, Segment] = rd + centerh;
            heightmap[Segment, Segment] = rd + centerh;
            heightmap[Segment / 2, Segment / 2] = rd + centerh;
        }
        else if (type == TerrainType.Basin) //盆地
        {
            heightmap[0, 0] = max;
            heightmap[Segment, 0] = max;
            heightmap[0, Segment] = max;
            heightmap[Segment, Segment] = max;
            heightmap[Segment / 2, Segment / 2] = min;
            heightmap[0, Segment / 2] = max;
            heightmap[Segment, Segment / 2] = max;
            heightmap[Segment / 2, 0] = max;
            heightmap[Segment / 2, Segment] = max;
        }
        else if (type == TerrainType.Hill ) //山
        {
            heightmap[0, 0] = min;
            heightmap[Segment, 0] = min;
            heightmap[0, Segment] = min;
            heightmap[Segment, Segment] = min;
            heightmap[Segment / 2, Segment / 2] = max;
        }

        float random_factor = Mathf.Pow(2, -H);
        for (int i = 1; i <= Iterations; i++)
        {
            int curt_segment = Segment / (1 << (i - 1));
            int rectnum = 1 << (i - 1);
            random_range = random_range * random_factor;
            for (int iterI = 0; iterI < rectnum; iterI++)
            {
                for (int iterJ = 0; iterJ < rectnum; iterJ++)
                {
                    int basex = iterJ * curt_segment;
                    int basey = iterI * curt_segment;
                    int centerx = basex + curt_segment / 2;
                    int centery = basey + curt_segment / 2;
                    float leftdown = heightmap[basex, basey];
                    float rightdown = heightmap[basex + curt_segment, basey];
                    float lefttop = heightmap[basex, basey+curt_segment];
                    float rightop = heightmap[basex + curt_segment, basey + curt_segment];
                    float centerht = GetHeight(leftdown, rightdown, lefttop, rightop, random_range,rand);
                    SetHeight(centerx, centery, heightmap, centerht);//矩形中点
                }
            }

            for (int iterI = 0; iterI < rectnum; iterI++)
            {
                for (int iterJ = 0; iterJ < rectnum; iterJ++)
                {
                    int basex = iterJ * curt_segment;
                    int basey = iterI * curt_segment;
                    int centerx = basex + curt_segment / 2;
                    int centery = basey + curt_segment / 2;
                    float leftdown = heightmap[basex, basey];
                    float rightdown = heightmap[basex + curt_segment, basey];
                    float lefttop = heightmap[basex, basey + curt_segment];
                    float rightop = heightmap[basex + curt_segment, basey + curt_segment];
                    float center = heightmap[centerx, centery];

                    int left = centerx - curt_segment;
                    if (left < 0)
                    {
                        left += Segment;
                    }
                    int right = centerx + curt_segment;
                    if (right>=Segment)
                    {
                        right -= Segment;
                    }
                    int down = centery - curt_segment;
                    if (down < 0)
                    {
                        down += Segment;
                    }
                    int top = centery + curt_segment;
                    if (top>=Segment)
                    {
                        top -= Segment;
                    }
                    //下面为中点的四个相邻点
                    float temp0 = GetHeight(heightmap[left, centery], lefttop, center, leftdown, random_range,rand);
                    SetHeight(basex, centery, heightmap, temp0);
                    if (iterI == rectnum - 1)
                    {//只有最后一横才需要加上它上面的点
                        float temp1 = GetHeight(lefttop, heightmap[centerx, top], rightop, center, random_range,rand);
                        SetHeight(centerx, basey + curt_segment, heightmap, temp1);
                    }
                    if (iterJ==rectnum-1)
                    {//只有最后一纵才需要加上它右边的点
                        float temp2 = GetHeight(center, rightop, heightmap[right, centery], rightdown, random_range,rand);
                        SetHeight(basex + curt_segment, centery, heightmap, temp2);
                    }
                    float temp3 = GetHeight(leftdown, center, rightdown, heightmap[centerx, down], random_range,rand);
                    SetHeight(centerx, basey, heightmap, temp3);
                }
            }
        }
    }

    private static void SetHeight(int idx,int idy,float[,] heightmap,float targetht)
    {
        //heightmap[idx, idy] = targetht;
        if (heightmap[idx, idy] < -50f)
        {
            heightmap[idx, idy] = targetht;
        }
    }

    //分形（4个相邻点的平均值+一个随机值）
    private static float GetHeight(float p0, float p1, float p2, float p3, float random_range,System.Random rand)
    {
        float rd = ((float)rand.NextDouble() - 0.5f) * 2 * random_range;
        return ((p0 + p1 + p2 + p3) / 4.0f + rd);
    }

}
