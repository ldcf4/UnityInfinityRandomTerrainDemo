
using UnityEngine;
using UnityEngine.Rendering;

class MeshCreator
{
    public static GameObject DrawMesh(Vector3[] vers, Vector2[] uvs, int[] tris, Material mat, string objname)
    {
        var plane = new GameObject(objname);
        Mesh mesh = plane.AddComponent<MeshFilter>().mesh;
        var renderer = plane.AddComponent<MeshRenderer>();
        renderer.material = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        //给mesh 赋值
        mesh.Clear();
        mesh.vertices = vers;
        mesh.uv = uvs;
        mesh.triangles = tris;
        //重置法线
        mesh.RecalculateNormals();
        //重置范围
        mesh.RecalculateBounds();
        return plane;
    }

    public static GameObject DrawMesh(Vector3[] vers, Vector3[] normals, Vector2[] uvs, int[] tris, Material mat, string objname)
    {
        var plane = new GameObject(objname);
        Mesh mesh = plane.AddComponent<MeshFilter>().mesh;
        var renderer = plane.AddComponent<MeshRenderer>();
        renderer.material = mat;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        //给mesh 赋值
        mesh.Clear();
        mesh.vertices = vers;
        mesh.uv = uvs;
        mesh.triangles = tris;
        //重置法线
        mesh.normals = normals;
        //重置范围
        mesh.RecalculateBounds();
        return plane;
    }
}

