using System.Diagnostics;
using UnityEngine;

public class SurfaceMaskCreator
{
    public static Color[] GeneralMaskData(int segment, float MaxHeight, float[,] m_ver_ht, float min)
    {
        Color[] maskcolors = new Color[(segment + 1) * (segment + 1)];
        float total_dis = MaxHeight - min;
        float each_height = total_dis / 5;
        float[] optimal = new float[4];
        float opt, low, high, percent;
        Color[] carray = new Color[4] { new Color(1, 0, 0, 0), new Color(0, 1, 0, 0), new Color(0, 0, 1, 0), new Color(0, 0, 0, 1) };
        for (int i = 0; i < 4; i++)
        {
            optimal[i] = (i + 1) * each_height + min;
        }
        for (int i = 0; i < segment + 1; i++)
        {
            for (int j = 0; j < segment + 1; j++)
            {
                float ht = m_ver_ht[j, i];
                int key = i * (segment + 1) + j;
                if (ht <= each_height + min)
                {
                    maskcolors[key] = new Color(1, 0, 0, 0);
                }
                else if (ht >= (MaxHeight - each_height))
                {
                    maskcolors[key] = new Color(0, 0, 0, 1);
                }
                else
                {
                    maskcolors[key] = new Color(0, 0, 0, 0);
                    for (int k = 0; k < 4; k++)
                    {
                        opt = optimal[k];
                        low = opt - each_height;
                        high = opt + each_height;
                        if (ht >= low && ht < opt)
                        {
                            percent = (ht - low) / each_height;
                            //maskcolors[key] += (carray[k] * percent);
                            ColorAdd(ref maskcolors[key], ref carray[k], percent);
                        }
                        else if (ht >= opt && ht < high)
                        {
                            percent = (high - ht) / each_height;
                            //maskcolors[key] += (carray[k] * percent);
                            ColorAdd(ref maskcolors[key], ref carray[k], percent);
                        }
                    }
                }
            }
        }
        return maskcolors;
    }
    public static Texture2D CreateMask(int segment)
    {
        var mask = new Texture2D(segment + 1, segment + 1, TextureFormat.ARGB32, true)
        {
            wrapMode = TextureWrapMode.Clamp
        };
        return mask;
    }

    /// <summary>
    /// ret += (add * per)   性能优化写法
    /// </summary>
    /// <param name="ret">最终被加的颜色</param>
    /// <param name="add">用于加的基本色</param>
    /// <param name="per">百分比</param>
    private static void ColorAdd(ref Color ret,ref Color add,float per)
    {
        ret.a += (add.a * per);
        ret.r += (add.r * per);
        ret.g += (add.g * per);
        ret.b += (add.b * per);
    }

}

