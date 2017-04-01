using System.Collections.Generic;
using UnityEngine;

public class TerrainDyRectCreator : MonoBehaviour
{
    private const int LOD_LEVEL_MAX= 4;

    [Header("Heightmap")]
    public HeightMapConfig config;
    [Header("LineHeightmap")]
    public LineMapConfig config_line;
    [Header("ParticleDeposition")]
    public ParticleDepositionConfig config_particle;
    [Header("Filter")]
    public FIRFilterConfig fir_config;

    [Header("Mesh")]
    public int PieceMechSegment = 16;    //每篇网格多少段
    public int PieceNum = 4;            //多少篇网格
    public Material PieceMat;

    private int MeshSegment = 64;       //世界总共分多少网格片段

    private Vector2 m_mesh_segment_size;
    private Vector3[,] m_mesh_vers;
    private Vector3[,] m_mesh_normals;
    private Vector2[,] m_mesh_uvs;
    private int ver_num_each_line;

    private int m_heightmap_len = 0;
    private float[,] m_heightmap;

    private LodPiece[,] m_piecees;
    private System.Diagnostics.Stopwatch m_watch;
    private string log_msg = string.Empty;

    private ArrayCache<Vector3> m_cache_v3;
    private ArrayCache<Vector2> m_cache_v2;
    private ArrayCache<int> m_cache_int;

    private Vector2 FixedWorldSize = new Vector2(128, 128);

    public Transform hero;

    private void Awake()
    {
        m_cache_v3 = new ArrayCache<Vector3>();
        m_cache_v2 = new ArrayCache<Vector2>();
        m_cache_int = new ArrayCache<int>();

        float max = 0;

        m_watch = new System.Diagnostics.Stopwatch();
        m_heightmap_len = 1 << config.Iterations;
        m_heightmap = HeightMapFactory.CreateHeightMapByFractal(config.Iterations, config.H, config.min, config.max, config.seed, config.type);
        max = config.max;
        HeightMapFilter.FilterFIR(m_heightmap, m_heightmap_len + 1, fir_config.times, fir_config.k);

        //m_heightmap_len = config_line.size -1;
        //m_heightmap = HeightMapFuzzyLandscaping.CreateHeightMap(config_line.size, config_line.seed, config_line.max,config_line.smooth_step);

        //m_heightmap_len = config_line.size - 1;
        //m_heightmap = HeightMapFuzzyLandscaping.CreateMapByLine(config_line.size, config_line.seed, config_line.max, config_line.line_num,config_line.smooth_step);

        //m_heightmap_len = config_particle.size - 1;
        //m_heightmap = HeightMapParticleDeposition.CreateMap(config_particle.size, config_particle.seed, config_particle.deposition_time, config_particle.deposition_deap, config_particle.deposition_size,config_particle.each_h,out max);
        //HeightMapVolcano.ToVolcano(m_heightmap, m_heightmap_len + 1, max * 0.5f);
        //HeightMapFilter.FilterFIR(m_heightmap, m_heightmap_len + 1, fir_config.times, fir_config.k);


        InitMeshMap();
        var mask = SurfaceMaskCreator.CreateMask(m_heightmap_len);
        var maskcolor = SurfaceMaskCreator.GeneralMaskData(m_heightmap_len, max, m_heightmap, 0);
        mask.SetPixels(maskcolor);
        mask.Apply();
        PieceMat.SetTexture("_Mask", mask);
    }

    private void InitMeshMap()
    {
        MeshSegment = PieceMechSegment * PieceNum;
        m_mesh_segment_size = FixedWorldSize / MeshSegment;
        ver_num_each_line = MeshSegment + 1;
        m_mesh_vers = new Vector3[ver_num_each_line, ver_num_each_line];
        m_mesh_uvs = new Vector2[ver_num_each_line, ver_num_each_line];
        m_mesh_normals = new Vector3[ver_num_each_line, ver_num_each_line];
        Vector2 one_uv = new Vector2(m_mesh_segment_size.x / FixedWorldSize.x, m_mesh_segment_size.y / FixedWorldSize.y);
        for (int i = 0; i < ver_num_each_line; i++)
        {
            for (int j = 0; j < ver_num_each_line; j++)
            {
                float posx = m_mesh_segment_size.x * j;
                float posy = m_mesh_segment_size.y * i;
                float uu = one_uv.x * j;
                float vv = one_uv.y * i;
                int index_x = Mathf.RoundToInt(m_heightmap_len * uu);
                int index_y = Mathf.RoundToInt(m_heightmap_len * vv);
                float ht = m_heightmap[index_x, index_y];
                m_mesh_uvs[j, i] = new Vector2(uu, vv);
                m_mesh_vers[j, i] = new Vector3(posx, ht, posy);
            }
        }
        for (int i = 0; i < ver_num_each_line; i++)
        {
            for (int j = 0; j < ver_num_each_line; j++)
            {
                Vector3 p0 = m_mesh_vers[j, i];
                Vector3 p1 = GetVer(j + 1, i);
                Vector3 p2 = GetVer(j, i - 1);
                Vector3 p3 = GetVer(j - 1, i);
                Vector3 p4 = GetVer(j, i + 1);

                Vector3 v01 = p1 - p0;
                Vector3 v02 = p2 - p0;
                Vector3 v03 = p3 - p0;
                Vector3 v04 = p4 - p0;
                v01.Normalize();
                v02.Normalize();
                v03.Normalize();
                v04.Normalize();

                Vector3 n1 = Vector3.Cross(v04, v01);
                Vector3 n2 = Vector3.Cross(v01, v02);
                Vector3 n3 = Vector3.Cross(v02, v03);
                Vector3 n4 = Vector3.Cross(v03, v04);

                Vector3 nor = n1 + n2 + n3 + n4;
                nor.Normalize();
                m_mesh_normals[j, i] = nor;
            }
        }
        Debug.LogFormat("ver num {0}", ver_num_each_line* ver_num_each_line);
        int size_of_vector3 = 12;
        int size_of_vector2 = 8;
        long total_total = (size_of_vector3+ size_of_vector3+ size_of_vector2)*ver_num_each_line*ver_num_each_line;
        Debug.LogFormat("total(ver+uv+normal) cache size:{0}Byte==={1}MB", total_total, total_total / 1024f / 1024f);
    }

    private Vector3 GetVer(int index_x,int index_y)
    {
        float posx = m_mesh_segment_size.x * index_x;
        float posy = m_mesh_segment_size.y * index_y;
        if (index_x < 0) index_x = 0;
        if (index_x >= ver_num_each_line) index_x = ver_num_each_line-1;
        if (index_y < 0) index_y = 0;
        if (index_y >= ver_num_each_line) index_y = ver_num_each_line-1;
        float ht = m_mesh_vers[index_x, index_y].y;
        return new Vector3(posx,ht,posy);
    }

    private void InitPiece()
    {//test
        GameObject piece_root = new GameObject("piece_root");
        m_piecees = new LodPiece[PieceNum, PieceNum];
        for (int i = 0; i < PieceNum; i++)
        {
            for (int j = 0; j < PieceNum; j++)
            {
                int offset_x = j * PieceMechSegment;
                int offset_y = i * PieceMechSegment;
                LodPiece piece = new LodPiece()
                {
                    offset_x = offset_x,
                    offset_y = offset_y,
                    LodLevel = LOD_LEVEL_MAX,
                    box = new Rect(offset_x * m_mesh_segment_size.x, offset_y * m_mesh_segment_size.y, PieceMechSegment * m_mesh_segment_size.x, PieceMechSegment * m_mesh_segment_size.y)
                };
                m_piecees[j, i] = piece;
                GameObject obj = CreatePiece(piece, "piece_" + j + "_" + i);
                obj.transform.SetParent(piece_root.transform);
            }
        }
    }

    private GameObject CreatePiece(LodPiece piece,string name)
    {
        int each_offset = 1 << piece.LodLevel;
        int segment = PieceMechSegment / each_offset;
        int ver_num = (segment + 1) * (segment + 1);
        bool[] isLevelBiger = new bool[] { false, false, false, false };
        Vector3[] vers = new Vector3[ver_num];
        Vector3[] nors = new Vector3[ver_num];
        Vector2[] uvs = new Vector2[ver_num];
        int[] indexes = new int[segment * segment * 6];
        UpdatePieceLod(vers, nors, uvs, indexes, segment, piece, isLevelBiger);
        GameObject obj = MeshCreator.DrawMesh(vers, nors, uvs, indexes, PieceMat, name);
        piece.mesh = obj.GetComponent<MeshFilter>().mesh;
        piece.vers = vers;
        piece.nors = nors;
        piece.uvs = uvs;
        piece.indexes = indexes;
        piece.last_lod_level = piece.LodLevel;
        piece.last_is_biger_level = isLevelBiger;
        obj.AddComponent<MeshCollider>();
        return obj;
    }

    private void UpdatePieceLod(Vector3[] vers,Vector3[] nors,Vector2[] uvs, int[] indexes,int segment,LodPiece piece, bool[] isLevelBiger)
    {
        int each_offset = 1 << piece.LodLevel;
        for (int i = 0; i < segment + 1; i++)
        {
            for (int j = 0; j < segment + 1; j++)
            {
                int index_x = piece.offset_x + each_offset * j;
                int index_y = piece.offset_y + each_offset * i;
                int index = i * (segment + 1) + j;
                vers[index] = m_mesh_vers[index_x, index_y];
                nors[index] = m_mesh_normals[index_x, index_y];
                uvs[index] = m_mesh_uvs[index_x, index_y];
            }
        }
        InitPieceIndexes(segment, indexes, isLevelBiger);
    }

    private void InitPieceIndexes(int segment,int[] indexes, bool[] isLevelBiger)
    {
        int index = 0;
        for (int i = 0; i < segment; i+=2)   //4个格子一起画三角
        {
            for (int j = 0; j < segment; j+=2)
            {
                int p00 = i * (segment + 1) + j;
                int p10 = p00 + 1;
                int p20 = p00 + 2;
                int p01 = (i + 1) * (segment + 1) + j;
                int p11 = p01 + 1;
                int p21 = p01 + 2;
                int p02 = (i + 2) * (segment + 1) + j;
                int p12 = p02 + 1;
                int p22 = p02 + 2;
                if (isLevelBiger[0] && j==0)    //左边
                {
                    indexes[index] = p00;
                    indexes[index + 1] = p02;
                    indexes[index + 2] = p11;
                    index += 3;
                }
                else
                {
                    indexes[index] = p00;
                    indexes[index + 1] = p01;
                    indexes[index + 2] = p11;

                    indexes[index + 3] = p01;
                    indexes[index + 4] = p02;
                    indexes[index + 5] = p11;
                    index += 6;
                }
                if (isLevelBiger[1] && j==segment-2) //右边
                {
                    indexes[index] = p11;
                    indexes[index + 1] = p22;
                    indexes[index + 2] = p20;
                    index += 3;
                }
                else
                {
                    indexes[index] = p11;
                    indexes[index + 1] = p21;
                    indexes[index + 2] = p20;

                    indexes[index + 3] = p11;
                    indexes[index + 4] = p22;
                    indexes[index + 5] = p21;
                    index += 6;
                }
                if (isLevelBiger[2]&&i==0) //下
                {
                    indexes[index] = p00;
                    indexes[index + 1] = p11;
                    indexes[index + 2] = p20;
                    index += 3;
                }
                else
                {
                    indexes[index] = p00;
                    indexes[index + 1] = p11;
                    indexes[index + 2] = p10;

                    indexes[index + 3] = p10;
                    indexes[index + 4] = p11;
                    indexes[index + 5] = p20;
                    index += 6;
                }
                if (isLevelBiger[3]&&i==segment-2) //上
                {
                    indexes[index] = p02;
                    indexes[index + 1] = p22;
                    indexes[index + 2] = p11;
                    index += 3;
                }
                else
                {
                    indexes[index] = p02;
                    indexes[index + 1] = p12;
                    indexes[index + 2] = p11;

                    indexes[index + 3] = p12;
                    indexes[index + 4] = p22;
                    indexes[index + 5] = p11;
                    index += 6;
                }
                
            }
        }
        for (int i = index; i < indexes.Length; i++)
        {
            indexes[i] = 0;
        }
    }

    // Use this for initialization
    void Start()
    {
        InitPiece();
    }

    /// <summary>
    /// 划分格子的Lod级别
    /// </summary>
    private void UpdateLodLevel()
    {
        var cam = Camera.main;
        float CL = 0.6f;
        Vector3 pos = hero.position;
        for (int i = 0; i < PieceNum; i++)
        {
            for (int j = 0; j < PieceNum; j++)
            {
                LodPiece ps = m_piecees[j, i];
                Vector2 center = ps.box.position + ps.box.size / 2;
                float distence = Vector2.Distance(center, new Vector2(pos.x, pos.z));
                ps.LodLevel = Mathf.FloorToInt((distence / ps.box.width) * CL);
                if (ps.LodLevel > LOD_LEVEL_MAX)
                {
                    ps.LodLevel = LOD_LEVEL_MAX;
                }
            }
        }
        for (int i = 0; i < PieceNum; i++)
        {
            for (int j = 0; j < PieceNum; j++)
            {
                LodPiece ps = m_piecees[j, i];
                CheckSideLodLevel(j, i, ps);
            }
        }
    }

    /// <summary>
    /// 检查周围4个格子的Lod级别是否符合：Lod级别不超过此格子的级别+1，这个条件。否则，直接强制划分Lod级别，并递归划分
    /// </summary>
    /// <param name="j"></param>
    /// <param name="i"></param>
    /// <param name="ps"></param>
    private void CheckSideLodLevel(int j,int i,LodPiece ps)
    {
        if (j>0 && m_piecees[j-1,i].LodLevel-ps.LodLevel>1)
        {
            m_piecees[j - 1, i].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j - 1, i, m_piecees[j - 1, i]);
        }
        if (j<PieceNum-1 && m_piecees[j+1,i].LodLevel-ps.LodLevel>1)
        {
            m_piecees[j + 1, i].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j + 1, i, m_piecees[j + 1, i]);
        }
        if (i>0 && m_piecees[j,i-1].LodLevel-ps.LodLevel>1)
        {
            m_piecees[j, i - 1].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j, i - 1, m_piecees[j, i - 1]);
        }
        if (i<PieceNum-1 && m_piecees[j,i+1].LodLevel-ps.LodLevel>1)
        {
            m_piecees[j, i + 1].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j, i + 1, m_piecees[j, i + 1]);
        }
    }

    private void LookUpSideLodLevel(int j,int i,int LodLevel,bool [] isLevelBiger)
    {
        isLevelBiger[0] = false;
        if (j>0)
        {
            isLevelBiger[0] = m_piecees[j - 1, i].LodLevel > LodLevel;
        }
        isLevelBiger[1] = false;
        if (j<PieceNum-1)
        {
            isLevelBiger[1] = m_piecees[j + 1, i].LodLevel > LodLevel;
        }
        isLevelBiger[2] = false;
        if (i>0)
        {
            isLevelBiger[2] = m_piecees[j, i - 1].LodLevel > LodLevel;
        }
        isLevelBiger[3] = false;
        if (i<PieceNum-1)
        {
            isLevelBiger[3] = m_piecees[j, i + 1].LodLevel > LodLevel;
        }
    }

    private void UpdateLod()
    {
        bool[] isLevelBiger = new bool[4];
        for (int i = 0; i < PieceNum; i++)
        {
            for (int j = 0; j < PieceNum; j++)
            {
                LodPiece piece = m_piecees[j, i];
                LookUpSideLodLevel(j, i, piece.LodLevel, isLevelBiger);
                if (piece.CheckNeedUpdate(isLevelBiger))
                {
                    int each_offset = 1 << piece.LodLevel;
                    int segment = PieceMechSegment / each_offset;
                    int ver_num = (segment + 1) * (segment + 1);
                    if (ver_num != piece.vers.Length)
                    {
                        m_cache_v3.GiveBack(piece.vers);
                        piece.vers = m_cache_v3.GetOneArray(ver_num);
                        m_cache_v3.GiveBack(piece.nors);
                        piece.nors = m_cache_v3.GetOneArray(ver_num);
                        m_cache_v2.GiveBack(piece.uvs);
                        piece.uvs = m_cache_v2.GetOneArray(ver_num);
                        m_cache_int.GiveBack(piece.indexes);
                        piece.indexes = m_cache_int.GetOneArray(segment * segment * 6);
                    }
                    UpdatePieceLod(piece.vers, piece.nors, piece.uvs, piece.indexes, segment, piece, isLevelBiger);
                    var mesh = piece.mesh;
                    mesh.Clear();
                    mesh.vertices = piece.vers;
                    mesh.uv = piece.uvs;
                    mesh.triangles = piece.indexes;
                    mesh.normals = piece.nors;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_watch.Reset();
        m_watch.Start();
        UpdateLodLevel();
        m_watch.Stop();
        long lod_level_t = m_watch.ElapsedMilliseconds;
        m_watch.Reset();
        m_watch.Start();
        UpdateLod();
        m_watch.Stop();
        long lod_t = m_watch.ElapsedMilliseconds;
        //log_msg=string.Format("update level t:{0}, lod mesh t:{1}\nint num:{2}!\nv3 num:{3}!\nv2 num:{4}!", lod_level_t, lod_t,m_cache_int.GetDebugInfo(),m_cache_v3.GetDebugInfo(),m_cache_v2.GetDebugInfo());
        log_msg = string.Format("update level t:{0}, lod mesh t:{1}", lod_level_t, lod_t);
    }

    private void OnGUI()
    {
        int w = Screen.width, h = Screen.height;

        GUIStyle style = new GUIStyle();

        Rect rect = new Rect(0, 60, w, h * 2 / 100);
        style.alignment = TextAnchor.UpperLeft;
        style.fontSize = 24;
        style.normal.textColor = new Color(0.0f, 0.0f, 0.5f, 1.0f);
        GUI.Label(rect, log_msg, style);
    }
}
