using UnityEngine;
using System.Collections;

/// <summary>
/// 转换成火山，把某个高度以上的倒置
/// </summary>
public class HeightMapVolcano 
{

    public static void ToVolcano(float[,] map,int size,float volcano_height)
    {
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (map[j,i]>volcano_height)
                {
                    map[j, i] = volcano_height - (map[j, i] - volcano_height);
                }
            }
        }
    }
}
