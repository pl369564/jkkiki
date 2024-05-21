using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CToool
{
    public static class Vector3Expansion
    {
        public static float GetMaxX(this Vector3[] poss)
        {
            float max = poss[0][0];
            for (int i = 1; i < poss.GetLength(0); i++)
            {
                if (max < poss[i][0])
                    max = poss[i][0];
            }
            return max;
        }
        public static Vector3[] MoveOffset(this Vector3[] poss,float x,float y,float z)
        {
            var len = poss.GetLength(0);
            var np = new Vector3[len];
            for (int i = 0; i < len; i++)
            {
                np[i][0] = poss[i][0] + x;
                np[i][1] = poss[i][1] + y;
                np[i][2] = poss[i][2] + z;
            }
            return np;
        }
    } 
}
