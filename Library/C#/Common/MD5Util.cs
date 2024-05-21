using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Modules.Communication
{
    public class MD5Util
    {
        public static string GetFileMD5(string path,bool isLower = true)
        {
            using var fs = System.IO.File.OpenRead(path);
            using var cryto = System.Security.Cryptography.MD5.Create();
            var md5bytes = cryto.ComputeHash(fs);
            var format = isLower ? "x2" : "X2";
            return md5bytes.Aggregate(string.Empty, (a, b) => { return a + b.ToString(format); });
        }
        public static string GetFileMD5(byte[] dancedata)
        {
            var md5data = getMD5Data(dancedata);
            return MD5ToString(md5data);
        }

        public static byte[] getMD5Data(byte[] bytes)
        {
            var md5 = new MD5CryptoServiceProvider();
            byte[] md5data = md5.ComputeHash(bytes);
            md5.Clear();

            return md5data;
        }
        public static string MD5ToString(byte[] md5data) 
        {
            return md5data.Aggregate(string.Empty, (a, b) => { return a + b.ToString("x2"); });
            //StringBuilder sb = new StringBuilder();
            //foreach (var b in md5data)
            //{
            //    var item = b.ToString("x2");
            //    //UnityEngine.Debug.Log(item);
            //    sb.Append(item);
            //}
            //return sb.ToString();
        }
    }
}
