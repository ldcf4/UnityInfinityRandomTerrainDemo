using System.Collections.Generic;

public class ArrayCache<T>
{
    private Dictionary<int, Stack<T[]>> m_v3_cache = new Dictionary<int, Stack<T[]>>();
    public T[] GetOneArray(int num)
    {
        Stack<T[]> target_arrays = null;
        if (m_v3_cache.TryGetValue(num, out target_arrays))
        {
            if (target_arrays.Count > 0)
            {
                return target_arrays.Pop();
            }
        }
        return new T[num];
    }

    public void GiveBack(T[] array)
    {
        int num = array.Length;
        Stack<T[]> target_array = null;
        if (!m_v3_cache.TryGetValue(num, out target_array))
        {
            target_array = new Stack<T[]>();
            m_v3_cache.Add(num, target_array);
        }
        target_array.Push(array);
    }

    public string GetDebugInfo()
    {
        string ret = "";
        foreach (var item in m_v3_cache)
        {
            ret += string.Format("({0}:{1}); ", item.Key, item.Value.Count);
        }
        return ret;
    }
}