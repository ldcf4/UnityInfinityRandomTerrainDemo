using UnityEngine;
using System.Collections;
using System.Threading;
using System.Collections.Generic;

public class WorkThread
{
    private Thread m_thread;
    private Queue<Chunk> m_work_queue;
    private Queue<Chunk> m_complete_queue;
    private bool isRun = false;
    private Dictionary<long, Chunk> m_loading_map;

    private Thread m_normal_thread;
    private Queue<Chunk> m_normal_queue;
    private bool isNormalRun = false;

    public WorkThread()
    {
        m_work_queue = new Queue<Chunk>();
        m_complete_queue = new Queue<Chunk>();
        m_loading_map = new Dictionary<long, Chunk>();
        m_thread = new Thread(BackgroundWork);

        m_normal_queue = new Queue<Chunk>();
        m_normal_thread = new Thread(OnUpdateNormal);
    }

    private void BackgroundWork()
    {
        while (true)
        {
            while (m_work_queue.Count > 0)
            {
                var ck = m_work_queue.Dequeue();
                ck.DoHardWork();
                m_complete_queue.Enqueue(ck);
            }
            Thread.Sleep(100);
        }
    }

    public void AddWork(Chunk ck)
    {
        //ck.DoHardWork();
        //ck.HardWorkComplete();

        var key = ChunkManager.GetKey(ck.id_x, ck.id_y);
        m_loading_map.Add(key, ck);
        m_work_queue.Enqueue(ck);
        if (!isRun)
        {
            isRun = true;
            m_thread.Start();
        }
    }
    public void OnMainThreadUpdate(ref bool needUpdateNormal)
    {
        while (m_complete_queue.Count>0)
        {
            var ck = m_complete_queue.Dequeue();
            ck.HardWorkComplete();
            var key = ChunkManager.GetKey(ck.id_x, ck.id_y);
            m_loading_map.Remove(key);
            needUpdateNormal = true;
        }
    }

    public bool CheckIsLoading(int idx,int idy)
    {
        var key = ChunkManager.GetKey(idx, idy);
        return m_loading_map.ContainsKey(key);
    }
    
    private void OnUpdateNormal()
    {
        while (true)
        {
            while (m_normal_queue.Count>0)
            {
                var ck = m_normal_queue.Dequeue();
                ck.UpdateNormals();
            }
            Thread.Sleep(10);
        }
    }

    public void AddUpdateNormlWork(Chunk ck)
    {
        //ck.UpdateNormals();

        m_normal_queue.Enqueue(ck);
        if (!isNormalRun)
        {
            isNormalRun = true;
            m_normal_thread.Start();
        }
    }

    public void Stop()
    {
        m_thread.Abort();
        m_normal_thread.Abort();
    }
}
