using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CToool
{
    public static class UnityTool
    {
        public static void SetEnable(this Behaviour bh, bool enabled)
        {
            bh.enabled = enabled;
        }
        public static void SetActive(this Component c, bool active)
        {
            c.gameObject.SetActive(active);
        }

        internal static Vector3 ToVector3(double v1, double v2, double v3)
        {
            return new Vector3((float)v1, (float)v2, (float)v3);
        }
        public static void Add(ref this Vector3 vector, float x, float y, float z)
        {
            vector.x += x;
            vector.y += y;
            vector.z += z;
        }
    }
}
