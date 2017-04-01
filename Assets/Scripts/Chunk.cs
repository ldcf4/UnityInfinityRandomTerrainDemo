using UnityEngine;

public class Chunk
{
    //以下为获取周围Chunk的快捷方法
    public Chunk Left
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x - 1, id_y, out ret);
            return ret;
        }
    }
    public Chunk Right
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x + 1, id_y, out ret);
            return ret;
        }
    }
    public Chunk Up
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x, id_y + 1, out ret);
            return ret;
        }
    }
    public Chunk Down
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x, id_y - 1, out ret);
            return ret;
        }
    }
    public Chunk LeftUp
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x - 1, id_y + 1, out ret);
            return ret;
        }
    }
    public Chunk RightUp
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x + 1, id_y + 1, out ret);
            return ret;
        }
    }
    public Chunk LeftDown
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x - 1, id_y - 1, out ret);
            return ret;
        }
    }
    public Chunk RightDown
    {
        get
        {
            Chunk ret = null;
            ChunkManager.Instence.GetChunk(id_x + 1, id_y - 1, out ret);
            return ret;
        }
    }

    public Rect box;        //块范围
    public Rect box_add;    //添加相邻块的范围
    public Rect box_remove; //移除相邻快的范围
    public int id_x;
    public int id_y;

    private float[,] m_height_map;          //高度图
    private int m_heightmap_len;            //高度图段数，不是高度图的像素点数量

    private Vector3[,] m_mesh_vers;         //预计算 顶点数组
    private Vector3[,] m_mesh_normals;      //预计算 法线数组
    private Vector2[,] m_mesh_uvs;          //预计算 UV数组

    private Vector2 m_mesh_segment_size;    //一段的大小
    private int m_mesh_segment;             //此Chunk总共的段数（单方向）

    private int m_mesh_piece_segment;       //单片网格的段数
    private int m_mesh_piece_num;           //此Chunk有多少片网格（单方向）
    private LodPiece[,] m_piecees;          //片 信息map

    private Material m_chunk_mat;           //此Chunk中所有网格共享此材质
    private GameObject obj;                 //此Chunk的根GameObject

    private HeightMapConfig m_cfg;          //生成高度图的配置
    private Texture2D mask;                 //用于控制地表显示哪张贴图的遮罩
    private Color[] maskcolor;              //遮罩信息，每个通道分别代表一张贴图

    private bool isNormalUpdated=false;     //是否更新过法线

    /// <summary>
    /// 销毁根GameObject和材质
    /// </summary>
    public void DestroyGameObject()
    {
        if (m_piecees!=null)
        {
            var cache_v3 = ChunkManager.Instence.cache_v3;
            var cache_V2 = ChunkManager.Instence.cache_v2;
            var cache_int = ChunkManager.Instence.cache_int;
            for (int i = 0; i < m_mesh_piece_num; i++)
            {
                for (int j = 0; j < m_mesh_piece_num; j++)
                {
                    LodPiece ps = m_piecees[j, i];
                    cache_v3.GiveBack(ps.vers);
                    cache_v3.GiveBack(ps.nors);
                    cache_V2.GiveBack(ps.uvs);
                    cache_int.GiveBack(ps.indexes);
                    ps.vers = null;
                    ps.nors = null;
                    ps.uvs = null;
                    ps.indexes = null;
                    ps.mesh = null;
                }
            }
        }
        if (obj!=null)
        {
            GameObject.Destroy(obj);
            obj = null;
        }
        if (m_chunk_mat!=null)
        {
            Object.Destroy(m_chunk_mat);
            m_chunk_mat = null;
        }
        if (mask!=null)
        {
            Object.Destroy(mask);
            mask = null;
            maskcolor = null;
        }
    }

    /// <summary>
    /// 生成基本高度图，会更具周围Chunk拷贝边界的高度信息
    /// </summary>
    private void GenralBaseHeightMap()
    {
        if (m_height_map==null)
            m_height_map= new float[m_heightmap_len + 1, m_heightmap_len + 1];
        for (int i = 0; i < m_heightmap_len + 1; i++)
        {
            for (int j = 0; j < m_heightmap_len + 1; j++)
            {
                m_height_map[j, i] = -100;
            }
        }
        CopySideMap();
    }

    private void CopySideMap()
    {
        if (Left != null)
        {
            var left_map = Left.m_height_map;
            for (int i = 0; i < m_heightmap_len + 1; i++)
            {
                m_height_map[0, i] = left_map[m_heightmap_len, i];
            }
        }
        if (Right != null)
        {
            var right_map = Right.m_height_map;
            for (int i = 0; i < m_heightmap_len + 1; i++)
            {
                m_height_map[m_heightmap_len, i] = right_map[0, i];
            }
        }
        if (Up != null)
        {
            var up_map = Up.m_height_map;
            for (int i = 0; i < m_heightmap_len + 1; i++)
            {
                m_height_map[i, m_heightmap_len] = up_map[i, 0];
            }
        }
        if (Down != null)
        {
            var down_map = Down.m_height_map;
            for (int i = 0; i < m_heightmap_len + 1; i++)
            {
                m_height_map[i, 0] = down_map[i, m_heightmap_len];
            }
        }
    }

    /// <summary>
    /// 生成高度图和预计算网格信息
    /// </summary>
    /// <param name="config"></param>
    /// <param name="mesh_segment"></param>
    /// <param name="mesh_segment_size"></param>
    /// <param name="mat"></param>
    /// <param name="mesh_piece_segment"></param>
    /// <param name="mesh_piece_num"></param>
    public void GeneralData(HeightMapConfig config,int mesh_segment, Vector2 mesh_segment_size,Material mat, int mesh_piece_segment, int mesh_piece_num)
    {
        m_heightmap_len = 1 << config.Iterations;
        m_cfg = config;
        mask = SurfaceMaskCreator.CreateMask(m_heightmap_len);
        m_chunk_mat = new Material(mat);
        m_chunk_mat.SetTexture("_Mask", mask);
        m_mesh_segment = mesh_segment;
        m_mesh_segment_size = mesh_segment_size;
        m_mesh_piece_segment = mesh_piece_segment;
        m_mesh_piece_num = mesh_piece_num;
    }

    /// <summary>
    /// 耗时计算，再后台线程中执行
    /// </summary>
    public void DoHardWork()
    {
        GenralBaseHeightMap();
        HeightMapFactory.GeneralHeightMapByFractal(m_cfg.Iterations, m_cfg.H, m_cfg.min, m_cfg.max, m_cfg.seed, m_cfg.type, m_height_map);
        var fcfg = ChunkManager.Instence.filter_config;
        HeightMapFilter.FilterFIR(m_height_map, m_heightmap_len + 1, fcfg.times, fcfg.k);
        CopySideMap();
        maskcolor = SurfaceMaskCreator.GeneralMaskData(m_heightmap_len, m_cfg.max, m_height_map, m_cfg.min);
        InitMesh();
        InitPiece();
        ChunkManager.Instence.AddToMap(this);
    }

    /// <summary>
    /// 耗时计算完成后执行
    /// </summary>
    public void HardWorkComplete()
    {
        mask.SetPixels(maskcolor);
        mask.Apply();
        CreatePieces();
    }

    /// <summary>
    /// 预计算网格信息
    /// </summary>
    private void InitMesh()
    {
        int ver_num_each_line = m_mesh_segment + 1;
        if (m_mesh_vers==null)
        {
            m_mesh_vers = new Vector3[ver_num_each_line, ver_num_each_line];
            m_mesh_uvs = new Vector2[ver_num_each_line, ver_num_each_line];
            m_mesh_normals = new Vector3[ver_num_each_line, ver_num_each_line];
        }
        Vector2 one_uv = new Vector2(m_mesh_segment_size.x / box.width, m_mesh_segment_size.y / box.height);
        for (int i = 0; i < ver_num_each_line; i++)
        {
            for (int j = 0; j < ver_num_each_line; j++)
            {
                float posx = m_mesh_segment_size.x * j+box.x;
                float posy = m_mesh_segment_size.y * i+box.y;
                float uu = one_uv.x * j;
                float vv = one_uv.y * i;
                int index_x = Mathf.RoundToInt(m_heightmap_len * uu);
                int index_y = Mathf.RoundToInt(m_heightmap_len * vv);
                float ht = m_height_map[index_x, index_y];
                m_mesh_uvs[j, i] = new Vector2(uu, vv);
                m_mesh_vers[j, i] = new Vector3(posx, ht, posy);
            }
        }
        UpdateNormals();
    }

    /// <summary>
    /// 初始化片的数据
    /// </summary>
    private void InitPiece()
    {
        if (m_piecees == null)
            m_piecees = new LodPiece[m_mesh_piece_num, m_mesh_piece_num];
        Vector2 piecesize = m_mesh_segment_size * m_mesh_piece_segment;
        for (int i = 0; i < m_mesh_piece_num; i++)
        {
            for (int j = 0; j < m_mesh_piece_num; j++)
            {
                LodPiece piece = m_piecees[j, i];
                if (piece == null)
                {
                    piece = new LodPiece();
                    m_piecees[j, i] = piece;
                }
                int offset_x = j * m_mesh_piece_segment;
                int offset_y = i * m_mesh_piece_segment;
                piece.offset_x = offset_x;
                piece.offset_y = offset_y;
                piece.LodLevel = ChunkManager.LOD_LEVEL_MAX;
                piece.box = new Rect(offset_x * m_mesh_segment_size.x + box.x, offset_y * m_mesh_segment_size.y + box.y, piecesize.x, piecesize.y);
            }
        }
        var cache_v3 = ChunkManager.Instence.cache_v3;
        var cache_v2 = ChunkManager.Instence.cache_v2;
        var cache_int = ChunkManager.Instence.cache_int;
        for (int i = 0; i < m_mesh_piece_num; i++)
        {//需要分2次循环执行，否则LookUpSideLodLevel会出错
            for (int j = 0; j < m_mesh_piece_num; j++)
            {
                bool[] isLevelBiger = new bool[] { false, false, false, false };
                LodPiece piece = m_piecees[j, i];
                int each_offset = 1 << piece.LodLevel;
                int segment = m_mesh_piece_segment / each_offset;
                int ver_num = (segment + 1) * (segment + 1);
                Vector3[] vers = cache_v3.GetOneArray(ver_num);
                Vector3[] nors = cache_v3.GetOneArray(ver_num);
                Vector2[] uvs = cache_v2.GetOneArray(ver_num);
                int[] indexes = cache_int.GetOneArray(segment * segment * 6);
                LookUpSideLodLevel(j, i, piece.LodLevel, isLevelBiger);
                UpdatePieceLod(vers, nors, uvs, indexes, segment, piece, isLevelBiger);
                piece.vers = vers;
                piece.nors = nors;
                piece.uvs = uvs;
                piece.indexes = indexes;
                piece.last_lod_level = piece.LodLevel;
                piece.last_is_biger_level = isLevelBiger;
            }
        }
    }

    /// <summary>
    /// 创建所有的片
    /// </summary>
    private void CreatePieces()
    {
        GameObject piece_root = new GameObject("chunk_"+id_x+"_"+id_y);
        obj = piece_root;
        for (int i = 0; i < m_mesh_piece_num; i++)
        {
            for (int j = 0; j < m_mesh_piece_num; j++)
            {
                LodPiece piece = m_piecees[j, i];
                GameObject obj = MeshCreator.DrawMesh(piece.vers, piece.nors, piece.uvs, piece.indexes, m_chunk_mat, "piece_" + j + "_" + i);
                piece.mesh = obj.GetComponent<MeshFilter>().mesh;
                obj.AddComponent<MeshCollider>();
                obj.transform.SetParent(piece_root.transform);
            }
        }
        piece_root.transform.SetParent(ChunkManager.Instence.transform);
    }

    /// <summary>
    /// 刷新单片的网格信息（顶点、UV等）
    /// </summary>
    /// <param name="vers"></param>
    /// <param name="nors"></param>
    /// <param name="uvs"></param>
    /// <param name="indexes"></param>
    /// <param name="segment"></param>
    /// <param name="piece"></param>
    /// <param name="isLevelBiger"></param>
    private void UpdatePieceLod(Vector3[] vers, Vector3[] nors, Vector2[] uvs, int[] indexes, int segment, LodPiece piece, bool[] isLevelBiger)
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

    /// <summary>
    /// 计算三角形索引数组
    /// </summary>
    /// <param name="segment"></param>
    /// <param name="indexes"></param>
    /// <param name="isLevelBiger"></param>
    private void InitPieceIndexes(int segment, int[] indexes, bool[] isLevelBiger)
    {
        int index = 0;
        for (int i = 0; i < segment; i += 2)   //4个格子一起画三角
        {
            for (int j = 0; j < segment; j += 2)
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
                if (isLevelBiger[0] && j == 0)    //左边
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
                if (isLevelBiger[1] && j == segment - 2) //右边
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
                if (isLevelBiger[2] && i == 0) //下
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
                if (isLevelBiger[3] && i == segment - 2) //上
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

    /// <summary>
    /// 划分格子的Lod级别
    /// </summary>
    public void UpdateLodLevel(Vector3 pos)
    {
        var cl = ChunkManager.Instence.CL;
        for (int i = 0; i < m_mesh_piece_num; i++)
        {
            for (int j = 0; j < m_mesh_piece_num; j++)
            {
                LodPiece ps = m_piecees[j, i];
                Vector2 center = ps.box.position + ps.box.size / 2;
                float distence = Vector2.Distance(center, new Vector2(pos.x, pos.z));
                ps.LodLevel = Mathf.FloorToInt((distence / ps.box.width) * cl);
                if(ps.LodLevel>ChunkManager.LOD_LEVEL_MAX)
                {
                    ps.LodLevel = ChunkManager.LOD_LEVEL_MAX;
                }
            }
        }
        
    }

    /// <summary>
    /// 检查所有的片的LOD级别是否符合规范
    /// </summary>
    public void CheckSidesLodLevel()
    {
        for (int i = 0; i < m_mesh_piece_num; i++)
        {
            for (int j = 0; j < m_mesh_piece_num; j++)
            {
                LodPiece ps = m_piecees[j, i];
                CheckSideLodLevel(j, i, ps);
            }
        }
    }

    /// <summary>
    /// 检查周围4个格子的Lod级别是否符合：Lod级别不超过此格子的级别+1。否则，直接强制划分Lod级别，并递归划分
    /// </summary>
    /// <param name="j"></param>
    /// <param name="i"></param>
    /// <param name="ps"></param>
    private void CheckSideLodLevel(int j, int i, LodPiece ps)
    {
        if (j > 0 && m_piecees[j - 1, i].LodLevel - ps.LodLevel > 1)
        {
            m_piecees[j - 1, i].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j - 1, i, m_piecees[j - 1, i]);
        }
        if (j < m_mesh_piece_num - 1 && m_piecees[j + 1, i].LodLevel - ps.LodLevel > 1)
        {
            m_piecees[j + 1, i].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j + 1, i, m_piecees[j + 1, i]);
        }
        if (i > 0 && m_piecees[j, i - 1].LodLevel - ps.LodLevel > 1)
        {
            m_piecees[j, i - 1].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j, i - 1, m_piecees[j, i - 1]);
        }
        if (i < m_mesh_piece_num - 1 && m_piecees[j, i + 1].LodLevel - ps.LodLevel > 1)
        {
            m_piecees[j, i + 1].LodLevel = ps.LodLevel + 1;
            CheckSideLodLevel(j, i + 1, m_piecees[j, i + 1]);
        }
        if (j==0 && Left!=null && Left.m_piecees[m_mesh_piece_num-1,i].LodLevel-ps.LodLevel>1)
        {
            Left.m_piecees[m_mesh_piece_num - 1, i].LodLevel = ps.LodLevel + 1;
            Left.CheckSideLodLevel(m_mesh_piece_num - 1, i, Left.m_piecees[m_mesh_piece_num - 1, i]);
        }
        if (j==m_mesh_piece_num-1 && Right!=null && Right.m_piecees[0,i].LodLevel-ps.LodLevel>1)
        {
            Right.m_piecees[0, i].LodLevel = ps.LodLevel + 1;
            Right.CheckSideLodLevel(0, i, Right.m_piecees[0, i]);
        }
        if (i==0 && Down!=null && Down.m_piecees[j,m_mesh_piece_num-1].LodLevel-ps.LodLevel>1)
        {
            Down.m_piecees[j, m_mesh_piece_num - 1].LodLevel = ps.LodLevel + 1;
            Down.CheckSideLodLevel(j, m_mesh_piece_num - 1, Down.m_piecees[j, m_mesh_piece_num - 1]);
        }
        if (i==m_mesh_piece_num-1 && Up!=null && Up.m_piecees[j,0].LodLevel-ps.LodLevel>1)
        {
            Up.m_piecees[j, 0].LodLevel = ps.LodLevel + 1;
            Up.CheckSideLodLevel(j, 0, Up.m_piecees[j, 0]);
        }
    }

    /// <summary>
    /// 查找周围4个片的LOD是否比此片大
    /// </summary>
    /// <param name="j"></param>
    /// <param name="i"></param>
    /// <param name="LodLevel"></param>
    /// <param name="isLevelBiger"></param>
    private void LookUpSideLodLevel(int j, int i, int LodLevel, bool[] isLevelBiger)
    {
        isLevelBiger[0] = false;
        if (j > 0)
        {
            isLevelBiger[0] = m_piecees[j - 1, i].LodLevel > LodLevel;
        }
        else if (Left!=null)
        {
            isLevelBiger[0] = Left.m_piecees[m_mesh_piece_num - 1, i].LodLevel > LodLevel;
        }
        isLevelBiger[1] = false;
        if (j < m_mesh_piece_num - 1)
        {
            isLevelBiger[1] = m_piecees[j + 1, i].LodLevel > LodLevel;
        }
        else if (Right!=null)
        {
            isLevelBiger[1] = Right.m_piecees[0, i].LodLevel > LodLevel;
        }
        isLevelBiger[2] = false;
        if (i > 0)
        {
            isLevelBiger[2] = m_piecees[j, i - 1].LodLevel > LodLevel;
        }
        else if (Down!=null)
        {
            isLevelBiger[2] = Down.m_piecees[j, m_mesh_piece_num - 1].LodLevel > LodLevel;
        }
        isLevelBiger[3] = false;
        if (i < m_mesh_piece_num - 1)
        {
            isLevelBiger[3] = m_piecees[j, i + 1].LodLevel > LodLevel;
        }
        else if (Up!=null)
        {
            isLevelBiger[3] = Up.m_piecees[j, 0].LodLevel > LodLevel;
        }
    }

    /// <summary>
    /// 更新所有片的网格
    /// </summary>
    public void UpdateLod()
    {
        var m_cache_v3 = ChunkManager.Instence.cache_v3;
        var m_cache_v2 = ChunkManager.Instence.cache_v2;
        var m_cache_int = ChunkManager.Instence.cache_int;
        bool[] isLevelBiger = new bool[4];
        for (int i = 0; i < m_mesh_piece_num; i++)
        {
            for (int j = 0; j < m_mesh_piece_num; j++)
            {
                LodPiece piece = m_piecees[j, i];
                LookUpSideLodLevel(j, i, piece.LodLevel, isLevelBiger);
                if (isNormalUpdated || piece.CheckNeedUpdate(isLevelBiger))
                {
                    piece.SetLastBigLevel(isLevelBiger);
                    int each_offset = 1 << piece.LodLevel;
                    int segment = m_mesh_piece_segment / each_offset;
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
                    piece.UpdateMeshData();
                }
            }
        }
        isNormalUpdated = false;
    }

    /// <summary>
    /// 重新计算此Chunk的所有顶点的法线信息
    /// </summary>
    public void UpdateNormals()
    {
        int ver_num_each_line = m_mesh_segment + 1;
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
        isNormalUpdated = true;
    }

    /// <summary>
    /// 取顶点，可能是相邻Chunk中的点，用于计算法线
    /// </summary>
    /// <param name="index_x"></param>
    /// <param name="index_y"></param>
    /// <returns></returns>
    private Vector3 GetVer(int index_x, int index_y)
    {
        Vector3 ret = new Vector3()
        {
            x = m_mesh_segment_size.x * index_x + box.x,
            z = m_mesh_segment_size.y * index_y + box.y
        };
        float ht = 0;
        int ver_num_each_line = m_mesh_segment + 1;
        if (index_x<0)
        {
            if (index_y < 0)
            {
                if (LeftDown != null)
                    ht = LeftDown.m_mesh_vers[ver_num_each_line - 1 - 1, ver_num_each_line - 1 - 1].y;
                else
                    ht = m_mesh_vers[0, 0].y;
            }
            else if (index_y < ver_num_each_line)
            {
                if (Left != null)
                    ht = Left.m_mesh_vers[ver_num_each_line - 1 - 1, index_y].y;
                else
                    ht = m_mesh_vers[0, index_y].y;
            }
            else
            {
                if (LeftUp != null)
                    ht = LeftUp.m_mesh_vers[ver_num_each_line - 1 - 1, 0 + 1].y;
                else
                    ht = m_mesh_vers[0, ver_num_each_line-1].y;
            }
        }
        else if (index_x < ver_num_each_line)
        {
            if (index_y<0)
            {
                if (Down != null)
                    ht = Down.m_mesh_vers[index_x, ver_num_each_line - 1 - 1].y;
                else
                    ht = m_mesh_vers[index_x, 0].y;
            }
            else if (index_y<ver_num_each_line)
            {
                ht = m_mesh_vers[index_x, index_y].y;
            }
            else
            {
                if (Up != null)
                    ht = Up.m_mesh_vers[index_x, 0 + 1].y;
                else
                    ht = m_mesh_vers[index_x, ver_num_each_line - 1].y;
            }
        }
        else
        {
            if (index_y<0)
            {
                if (RightDown != null)
                    ht = RightDown.m_mesh_vers[0 + 1, ver_num_each_line - 1 - 1].y;
                else
                    ht = m_mesh_vers[ver_num_each_line, 0].y;
            }
            else if (index_y<ver_num_each_line)
            {
                if (Right != null)
                    ht = Right.m_mesh_vers[0 + 1, index_y].y;
                else
                    ht = m_mesh_vers[ver_num_each_line - 1, index_y].y;
            }
            else
            {
                if (RightUp != null)
                    ht = RightUp.m_mesh_vers[0 + 1, 0 + 1].y;
                else
                    ht = m_mesh_vers[ver_num_each_line - 1, ver_num_each_line - 1].y;
            }
        }
        ret.y = ht;
        return ret;
    }
}
