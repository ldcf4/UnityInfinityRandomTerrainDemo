using UnityEngine;
using System.Collections;

public class LodPiece
{
    public Mesh mesh;               //此片的网格
    public int LodLevel;            //LOD等级
    public int offset_x;            //偏移的 段 数
    public int offset_y;            //同上
    public Rect box;                //此片的范围
    public Vector3[] vers;          //此片的顶点数组
    public Vector3[] nors;          //法线数组
    public Vector2[] uvs;           //uv数组
    public int[] indexes;           //三角形顶点索引数组

    public int last_lod_level;          //上次LOD等级
    public bool[] last_is_biger_level;  //上次周围的片Lod等级是否大于此片的数组

    /// <summary>
    /// 判断是否需要更新
    /// </summary>
    /// <param name="new_biger_level"></param>
    /// <returns></returns>
    public bool CheckNeedUpdate(bool[] new_biger_level)
    {
        if (mesh == null) return false;
        if (LodLevel != last_lod_level)
        {
            last_lod_level = LodLevel;
            return true;
        }
        for (int i = 0; i < 4; i++)
        {
            if (new_biger_level[i] != last_is_biger_level[i])
            {
                last_lod_level = LodLevel;                
                return true;
            }
        }
        return false;
    }

    public void SetLastBigLevel(bool[] new_biger_level)
    {
        for (int i = 0; i < 4; i++)
        {
            last_is_biger_level[i] = new_biger_level[i];
        }
    }

    public void UpdateMeshData()
    {
        if (mesh!=null)
        {
            mesh.Clear();
            mesh.vertices = vers;
            mesh.uv = uvs;
            mesh.triangles = indexes;
            mesh.normals = nors;
        }
    }
}
