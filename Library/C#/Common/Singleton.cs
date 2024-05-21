using UnityEngine;

public class Singleton<T> where T:new ()
{
    // 单例实例
    private static T instance;

    // 锁class用的对象，线程安全用
    private static object syncRoot = new object();

    protected Singleton() { }

    public static T Instance
    {
        get
        {
            // 一次判空。如果这次判断不为空，就不必再执行加锁了，避免加锁的消耗
            if (instance == null)
            {
                lock (syncRoot)
                {
                    // 二次判空。作为临界值进行判空，这次判空是必要的。
                    if (instance == null)
                    {
                        instance = new T();
                    }
                }
            }
            return instance;
        }
    }
}
