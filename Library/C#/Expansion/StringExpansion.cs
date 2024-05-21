using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modules.Communication
{
    public static class StringExpansion
    {
        //public static byte[] ToBytes32(this string str) 
        //{
        //    char[] cs = str.ToCharArray();
        //    byte[] data = new byte[cs.Length*2];
        //    for (int i = 0; i < cs.Length; i++)
        //    {
        //        var raw = BitConverter.GetBytes(cs[i]);
        //        data[2*i] = raw[0];
        //        data[2*i+1] = raw[1];
        //    }
        //    return data;
        //}
        //public static byte[] ToBytes16(this string str)
        //{
        //    char[] cs = str.ToCharArray();
        //    byte[] data = new byte[cs.Length];
        //    for (int i = 0; i < cs.Length; i++)
        //    {
        //        var raw = BitConverter.GetBytes(cs[i]);
        //        data[i] = raw[0];
        //    }
        //    return data;
        //}
        public static string TOString(this byte[] bytes)
        {
            if (bytes == null)
                return "null";
            StringBuilder sb = new StringBuilder();
            if (bytes.Length > 10)
            {
                sb.Append(bytes.Length);
                sb.Append("|");
            }
            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
                sb.Append(" ");
            }
            return sb.ToString();
        }

        public static string TOstring<T>(this T t) where T : struct
        {
            var fields = (typeof(T)).GetFields();
            var obj = t as object;
            StringBuilder sb = new StringBuilder();
            foreach (var item in fields)
            {
                sb.Append(item.Name);
                sb.Append(":");
                if (item.FieldType == typeof(byte[]))
                    sb.Append(Encoding.UTF8.GetString((byte[])item.GetValue(t)).Replace("\0",""));
                    //sb.Append(((byte[])item.GetValue(t)).TOString());
                else
                    sb.Append(item.GetValue(t));
                sb.Append("/");
            }
            return sb.ToString();
        }
    }
}
