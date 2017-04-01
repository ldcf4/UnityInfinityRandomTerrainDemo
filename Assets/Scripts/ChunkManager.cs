using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public const int LOD_LEVEL_MAX = 4;             //最大Lod级别
    public static ChunkManager Instence = null;

    [Header("heightmap")]
    public HeightMapConfig config;                  //高度图生成配置
    [Header("filter")]
    public FIRFilterConfig filter_config;           //过滤器，
    [Header("chunk")]
    public Vector2 ChunkSize=new Vector2(128,128);      //单片Chunk大小
    public Vector2 AddSize=new Vector2(64,64);          //添加周围Chunk的范围
    public Vector2 RemoveSize=new Vector2(160,160);     //移除自身的范围
    public float CL = 1.0f;                             //计算Lod等级的常数
    [Header("mesh")]
    public Material MeshMat;                            //绘制网格所用的材质
    public int MeshPieceSegment = 32;                   //单片网格有多少 段
    public int MeshPieceNum = 8;                        //单个Chunk有多少 片 网格
    [Header("hero")]
    public Transform hero;                              //主角，用于计算LOD等级

    public ArrayCache<Vector3> cache_v3;                //此3项为顶点、UV、三角索引 缓存类
    public ArrayCache<Vector2> cache_v2;
    public ArrayCache<int> cache_int;

    private Vector2 m_MeshSegmentSize;                  //网格最细分时每段的大小
    private int m_TotalMeshSegmentNum;                  //单个Chunk中，总共分多少段网格

    private Dictionary<long, Chunk> chunk_map;          //所有Chunk容器
    private Queue<Chunk> remove_queue;                   //移除队列
    private Queue<Chunk> add_queue;                     //待添加队列
    private Stack<Chunk> cache_chunk;                   //缓存本来应该被释放的Chunk
    private bool needUpdateNormal = false;              //本次是否需要重新计算法线（Normal）
    private WorkThread m_thread_work;                   //后台线程

    /// <summary>
    /// 计算Chunk存储时的key
    /// </summary>
    /// <param name="id_x"></param>
    /// <param name="id_y"></param>
    /// <returns></returns>
    public static long GetKey(int id_x,int id_y)
    {
        ulong ret = (uint)id_x;
        uint temp = (uint)id_y;
        ulong key = (ret << 32) | temp;
        return (long)key;
    }

    private void Awake()
    {
        Instence = this;
        cache_int = new ArrayCache<int>();
        cache_v2 = new ArrayCache<Vector2>();
        cache_v3 = new ArrayCache<Vector3>();
        cache_chunk = new Stack<Chunk>();
        chunk_map = new Dictionary<long, Chunk>();
        remove_queue = new Queue<Chunk>();
        add_queue = new Queue<Chunk>();
        m_TotalMeshSegmentNum = MeshPieceNum * MeshPieceSegment;
        m_MeshSegmentSize = ChunkSize / m_TotalMeshSegmentNum;
        m_thread_work = new WorkThread();
        AddChunk(0,0);
    }

    /// <summary>
    /// 创建Chunk，并将其添加到队列中去
    /// </summary>
    /// <param name="idx"></param>
    /// <param name="idy"></param>
    private void AddChunk(int idx, int idy)
    {
        if (m_thread_work.CheckIsLoading(idx, idy)) return;
        //needUpdateNormal = true;
        Chunk ck = null;
        if (cache_chunk.Count > 0)
            ck = cache_chunk.Pop();
        else
            ck = new Chunk();
        ck.id_x = idx;
        ck.id_y = idy;
        ck.box.x = ck.id_x * ChunkSize.x;
        ck.box.y = ck.id_y * ChunkSize.y;
        ck.box.size = ChunkSize;
        ck.box_add.position = ck.box.position + (ChunkSize - AddSize) / 2;
        ck.box_add.size = AddSize;
        ck.box_remove.position = ck.box.position + (ChunkSize - RemoveSize) / 2;
        ck.box_remove.size = RemoveSize;
        add_queue.Enqueue(ck); //添加到待添加队列中
    }

    /// <summary>
    /// 添加到容器中
    /// </summary>
    /// <param name="ck"></param>
    public void AddToMap(Chunk ck)
    {
        lock (chunk_map)
        {//因为在后台线程中有调用此方法，可能与主线程的foreach冲突
            long key = GetKey(ck.id_x, ck.id_y);
            Debug.Log("add to map key:" + ck.id_x + "_" + ck.id_y);
            chunk_map.Add(key, ck);
        }
    }

    /// <summary>
    /// 从容器中取对应Chunk
    /// </summary>
    /// <param name="id_x"></param>
    /// <param name="id_y"></param>
    /// <param name="ret"></param>
    /// <returns></returns>
    public bool GetChunk(int id_x,int id_y,out Chunk ret)
    {
        long key = GetKey(id_x, id_y);
        return chunk_map.TryGetValue(key, out ret);
    }

    void Update()
    {
        lock (chunk_map)
        {
            Vector3 hero_pos = hero.position;
            UpdateRemove(hero_pos);
            UpdateAdd(hero_pos);
            UpdateLod(hero_pos);
            m_thread_work.OnMainThreadUpdate(ref needUpdateNormal);
        }
    }

    /// <summary>
    /// 检查是否有需要移除的Chunk
    /// </summary>
    /// <param name="hero_pos"></param>
    private void UpdateRemove(Vector3 hero_pos)
    {
        foreach (var temp in chunk_map)
        {
            var ck = temp.Value;
            if (hero_pos.x < ck.box_remove.xMin || hero_pos.x > ck.box_remove.xMax || hero_pos.z < ck.box_remove.yMin || hero_pos.z > ck.box_remove.yMax)
            {
                remove_queue.Enqueue(ck); //将需要移除的Chunk放入移除队列
                ck.DestroyGameObject();
            }
        }
        while (remove_queue.Count>0)//移除不可以放在foreach循环中，所以用此方法从容器中移除
        {
            var ck = remove_queue.Dequeue();
            var key = GetKey(ck.id_x, ck.id_y);
            chunk_map.Remove(key);
            cache_chunk.Push(ck);
        }
    }

    /// <summary>
    /// 检查是否有需要添加的Chunk
    /// </summary>
    /// <param name="hero_pos"></param>
    private void UpdateAdd(Vector3 hero_pos)
    {
        foreach (var temp in chunk_map)
        {
            var ck = temp.Value;
            if (hero_pos.x >= ck.box.xMin && hero_pos.x <= ck.box.xMax
                && hero_pos.z >= ck.box.yMin && hero_pos.z <= ck.box.yMax)
            {
                bool isLeft = hero_pos.x < ck.box_add.xMin;
                bool isRight = hero_pos.x > ck.box_add.xMax;
                bool isUp = hero_pos.z > ck.box_add.yMax;
                bool isDown = hero_pos.z < ck.box_add.yMin;
                if (isLeft && ck.Left==null)
                    AddChunk(ck.id_x - 1, ck.id_y);
                if (isRight && ck.Right==null)
                    AddChunk(ck.id_x + 1, ck.id_y);
                if (isUp && ck.Up==null)
                    AddChunk(ck.id_x, ck.id_y + 1);
                if (isDown && ck.Down==null)
                    AddChunk(ck.id_x, ck.id_y - 1);
                if (isLeft && isUp && ck.LeftUp==null)
                    AddChunk(ck.id_x - 1, ck.id_y + 1);
                if (isLeft && isDown && ck.LeftDown==null)
                    AddChunk(ck.id_x - 1, ck.id_y - 1);
                if (isRight && isUp && ck.RightUp==null)
                    AddChunk(ck.id_x + 1, ck.id_y + 1);
                if (isRight && isDown && ck.RightDown==null)
                    AddChunk(ck.id_x + 1, ck.id_y - 1);
            }
        }
        
        while (add_queue.Count>0) //添加也不可以放到foreach循环中，否则会出错
        {
            var ck = add_queue.Dequeue();
            HeightMapConfig lcfg = config;
            lcfg.seed = config.seed + ck.id_x + ck.id_y;
            //让粗糙度渐变，这个只是测试用，具体可根据实际情况确定
            lcfg.H = config.H + ((Mathf.Sin(ck.id_x)) / 8) + ((Mathf.Cos(ck.id_y)) / 8);
            Debug.Log("Current H:" + lcfg.H);
            ck.GeneralData(lcfg, m_TotalMeshSegmentNum, m_MeshSegmentSize, MeshMat, MeshPieceSegment, MeshPieceNum); //此步需要访问相邻Chunk,当多个Chunk需要添加时，应当保证执行过此步的Chunk都已经存放到容器中了
            m_thread_work.AddWork(ck);
        }
    }

    /// <summary>
    /// 刷新所有的Chunk的Lod网格
    /// </summary>
    /// <param name="hero_pos"></param>
    private void UpdateLod(Vector3 hero_pos)
    {
        foreach (var temp in chunk_map)
        {
            var ck = temp.Value;
            if (needUpdateNormal) //此帧是否需要重新计算法线
            {
                m_thread_work.AddUpdateNormlWork(ck);
            }
            ck.UpdateLodLevel(hero_pos); //划分Lod等级
            ck.CheckSidesLodLevel();     //强制检查LOD等级，使相邻的片的Lod等级差不超过1
            ck.UpdateLod();             //更具LOD更新网格
        }
        needUpdateNormal = false;
    }
}
