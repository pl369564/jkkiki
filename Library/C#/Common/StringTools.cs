using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CToool
{
    public static class StringTools
    {
        /// <summary>
        ///单位秒, 格式00:00
        /// </summary>
        public static string ToTimeString(this int second)
        {
            int mm = second / 60;
            int ss = second % 60;
            return string.Format("{0:D2}:{1:D2}", mm, ss);
        }

        public static int GetDisplayLength(string str, Encoding encoding = null)
        { 
            // 获取默认编码
            if(null == encoding)
                encoding = Encoding.Default;

            // 将字符串转换为字节数组
            byte[] bytes = encoding.GetBytes(str);

            // 计算字节数组的长度
            int byteLength = bytes.Length;

            // 计算字符数
            int charCount = encoding.GetCharCount(bytes);

            // 计算显示长度
            int displayLength = byteLength + (charCount - byteLength) / 2;

            return displayLength;
        }

        public static string CutAtLength(string str,int length,string end, Encoding encoding = null)
        {
            // 获取默认编码
            if (null == encoding)
                encoding = Encoding.Default;

            // 将字符串转换为字节数组
            byte[] bytes = encoding.GetBytes(str);

            int displayLength = CalDisLength(bytes,encoding);

            if (length < displayLength)
            {
                var chars = str.ToCharArray();
                var count = chars.Length;

                while (length < displayLength)
                {
                    count--;
                    if (count == 0)
                        break;
                    bytes = encoding.GetBytes(chars, 0, count);
                    displayLength = CalDisLength(bytes, encoding);
                }
                var endLen = GetDisplayLength(end,encoding);
                count -= endLen;
                if(count <= 0)
                    return string.Empty;
                str = str.Substring(0,count) + end;
            }

            return str;
        }
        private static int CalDisLength(byte[] bytes, Encoding encoding = null)
        {
            if (null == encoding)
                encoding = Encoding.Default;

            // 计算字符数
            var charCount = encoding.GetCharCount(bytes);
            // 计算显示长度
            var len = bytes.Length + (charCount - bytes.Length) / 2;
#if UNITY_EDITOR
            UnityEngine.Debug.Log(len); 
#endif
            return len;
        }
    }
}
