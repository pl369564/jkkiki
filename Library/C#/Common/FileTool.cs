using System;
using System.Collections.Generic;
using System.IO;

namespace CToool
{
    public class FileTool
    {
        public class DoublePos
        {
            public static void SaveData(double[,] data, string name)
            {
                byte[] bys = new byte[data.Length * sizeof(double)];
                Buffer.BlockCopy(data, 0, bys, 0, bys.Length);
                File.WriteAllBytes(name, bys);
                UnityEngine.Debug.Log($"Save {name} Succseed");
            }
            public static double[,] ReadDoubleAry(string name)
            {
                var bytes = File.ReadAllBytes(name);
                var droneNum = bytes.Length / sizeof(double) / 3;
                var dan = new double[droneNum, 3];
                Buffer.BlockCopy(bytes, 0, dan, 0, bytes.Length);
                return dan;
            }

        }


    }
}
