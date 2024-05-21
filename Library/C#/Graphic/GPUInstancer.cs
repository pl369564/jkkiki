

using System;
using UnityEngine;
using System.Collections;

namespace CToool
{
    public class GPUInstancer
    {
        public int count { get; private set; }

        public float scale = 1f;

        Mesh mesh;
        Material mat;
        Vector4[] data;
        float[][] rgba;

        GPUInstHelper instHelper = new GPUInstHelper();

        bool isStart = false; 

        public GPUInstancer(Mesh mesh, Material mat,int count)
        {
            this.mesh = mesh;
            this.mat = mat;
            this.count = count;

            InitData(count);
        }
        private void InitData(int count)
        {
            data = new Vector4[count];
            rgba = new float[4][];
            for (int i = 0; i < 4; i++)
            {
                rgba[i] = new float[count];
            }
        }

        public void StartDrawMesh(MonoBehaviour mono)
        {
            isStart = true;
            mono.StartCoroutine(StartDrawMeshCoro());
        }
        public void StopDrawMesh()
        {
            isStart = false;
        }
        IEnumerator StartDrawMeshCoro()
        {
            while (isStart)
            {
                yield return null;
                instHelper.DrawMeshInstanced(mesh, mat, data, rgba,scale);
            }
        }

        public void Update(Vector3[] pos, Color[] color)
        {
            UpdatePosition(pos);
            UpdateColors(color);
        }

        public void UpdatePosition(Vector3[] pos)
        {
            if (pos.Length != data.Length)
            {
                throw new Exception("pos count error ");
            }
            for (int i = 0; i < pos.Length; i++)
            {
                data[i].x = pos[i].x;
                data[i].y = pos[i].y;
                data[i].z = pos[i].z;
                data[i].w = 1;
            }

        }

        public void UpdateColors(Color[] color)
        {
            if (color.Length != count)
            {
                throw new Exception("array count error ");
            }
            for (int i = 0; i < color.Length; i++)
            {
                rgba[0][i] = color[i].r;
                rgba[1][i] = color[i].g;
                rgba[2][i] = color[i].b;
                rgba[3][i] = color[i].a;
            }
        }


    }
}