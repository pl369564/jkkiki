using System;
using System.Collections.Generic;
using UnityEngine;

namespace CToool
{
    public class GPUInstHelper
    {
        public GPUInstHelper(string r = "_testRed", string g = "_testGreen", string b = "_testBlue", string a = "_testAlpha")
        {
            paramsNames = new string[] { r, g, b, a };
            matrix = new Matrix4x4[1023];
            for (int i = 0; i < 1023; i++)
            {
                matrix[i] = Matrix4x4.identity;
            }
        }
        string[] paramsNames;

        Matrix4x4[] matrix = new Matrix4x4[1023];
        MaterialPropertyBlock mpb = new MaterialPropertyBlock();

        public void DrawMeshInstanced(Mesh mesh, Material mat, Vector4[] data, IEnumerable<float[]> rgbw = null,float scale = 1f)
        {
            if (data==null || data.Length == 0)
                return;
            int index = 0;
            int count = data.Length;
            while (count - index > 1023)
            {
                DrawMeshInstanced(mesh, mat, data,rgbw, scale, 1023, index);
                index += 1023;
            }
            DrawMeshInstanced(mesh, mat, data, rgbw, scale, count - index, index);
        }

        private void DrawMeshInstanced(Mesh mesh, Material mat, Vector4[] data, IEnumerable<float[]> rgbw, float scale, int count, int index)
        {
            //Matrix4x4[] matrix = new Matrix4x4[count];

            System.Threading.Tasks.Parallel.For(0, count, (int i) =>
            {
                int j = index + i;
                //matrix[i] = Matrix4x4.identity;   ///   set default identity
                //设置位置
                matrix[i].SetColumn(3, data[j]);  /// 4th colummn: set   position

                //设置缩放
                matrix[i].m00 = scale;
                matrix[i].m11 = scale;
                matrix[i].m22 = scale;

            });
            //var mpb = new MaterialPropertyBlock();
            if (rgbw != null)
            {
                var enumerator = rgbw.GetEnumerator();
                for (int i = 0; i < 4; i++)
                {
                    if (enumerator.MoveNext())
                    {
                        mpb.SetFloatArray(paramsNames[i], enumerator.Current);

                    }
                }
            }
            Graphics.DrawMeshInstanced(mesh, 0, mat, matrix, count, mpb);
        }

        public void DrawMeshInstanced(Mesh mesh, Material mat, List<Vector4> data, List<float>[] rgbw = null, float scale = 1f)
        {
            if (data == null || data.Count == 0)
                return;
            int index = 0;
            int count = data.Count;
            while (count - index > 1023)
            {
                DrawMeshInstanced(mesh, mat, data, rgbw, scale, 1023, index);
                index += 1023;
            }
            DrawMeshInstanced(mesh, mat, data, rgbw, scale, count - index, index);
        }
        private void DrawMeshInstanced(Mesh mesh, Material mat, List<Vector4> data, List<float>[] rgbw, float scale, int count, int index)
        {
            //Matrix4x4[] matrix = new Matrix4x4[count];

            System.Threading.Tasks.Parallel.For(0, count, (int i) =>
            {
                int j = index + i;
                //matrix[i] = Matrix4x4.identity;   ///   set default identity
                //设置位置
                matrix[i].SetColumn(3, data[j]);  /// 4th colummn: set   position

                //设置缩放
                matrix[i].m00 = scale;
                matrix[i].m11 = scale;
                matrix[i].m22 = scale;

            });
            //var mpb = new MaterialPropertyBlock();
            if (rgbw != null)
            {
                mpb.SetFloatArray(paramsNames[0], rgbw[0]);
                mpb.SetFloatArray(paramsNames[1], rgbw[1]);
                mpb.SetFloatArray(paramsNames[2], rgbw[2]);
                mpb.SetFloatArray(paramsNames[3], rgbw[3]);
            }
            Graphics.DrawMeshInstanced(mesh, 0, mat, matrix, count, mpb);
        }

    }
}
