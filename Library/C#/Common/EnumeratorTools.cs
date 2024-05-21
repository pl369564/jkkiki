using System;
using System.Collections.Generic;
using System.Linq;

namespace CToool
{
    public static class EnumeratorTools
    {
        public static T Get<T>(this List<T> list,int index)
        { 
            if(index>=0&&index<list.Count)
                return list[index];
            return default;
        }
        public static T Last<T>(this List<T> list)
        {
           return list[list.Count - 1];
        }
        public static T max<T>(this List<T> enumerator)
        {
            return enumerator.Max();
        }

        public static void WriteTO(this byte[] data, byte[] buffer, ref int offset)
        {
            Array.Copy(data, 0, buffer, offset, data.Length);
            offset += data.Length;
            if (offset < data.Length)
            {
                throw new Exception("Offset OverMaxValue");
            }
        }

        #region [填充]

        public static Array Init<T>(Array ts) where T : new()
        {
            ts.Initialize();
            return ts;
        }
        public static T[] Init<T>(this T[] ts) where T : new()
        {
            for (int i = 0; i < ts.Length; i++)
            {
                ts[i] = new T();
            }
            return ts;
        }
        public static T[] Fill<T>(this T[] ts, T fillValue)
        {
            for (int i = 0; i < ts.Length; i++)
            {
                ts[i] = fillValue;
            }
            return ts;
        }
        public static List<T> Fill<T>(this List<T> ts, T fillValue, int count)
        {
            if (ts == null)
                ts = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                if (i < ts.Count)
                    ts[i] = fillValue;
                else
                    ts.Add(fillValue);
            }
            return ts;
        }

        public static List<T> Fill<T>(this List<T> ts, int count) where T : new()
        {
            if (ts == null)
                ts = new List<T>();
            for (int i = 0; i < count; i++)
            {
                if (i < ts.Count)
                    ts[i] = new T();
                else
                    ts.Add(new T());
            }
            return ts;
        }
        #endregion

        public static T[] CClone<T>(this T[] origin)
        {
            T[] nary = new T[origin.Length];
            Array.Copy(nary,origin,origin.Length);
            return nary;
        }

    }
}
