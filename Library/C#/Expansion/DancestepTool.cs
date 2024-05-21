using com.hgfly.uavutils;
using CToool;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DancestepTool
{
    public DancestepTool()
    {

    }

    /// <summary>
    /// 变加速直线运动//xxx
    /// </summary>
    private RawDancestep MoveStraight(Vector3[] start, Vector3[] goal, FlyParams inst)
    {
        var accXY_max  = inst.ax_max;
        var vxy_max    = inst.vxy_max;
        var az_max     = inst.az_max;
        var maxVz      = inst.vz_max;
        var frame_rate = inst.frame_rate;

        int count = start.Length;
        RawDancestep rawDancestep = new RawDancestep(start);

        for (int i = 0; i < count; i++)
        {
            Vector3 startPoint = start[i];
            Vector3 goalPoint = goal[i];
            float totalTime;
            //float totalDist = Vector3.Distance(startPoint, goalPoint);

            var dev = startPoint - goalPoint;
            float dist_XY = Mathf.Sqrt(Mathf.Pow(dev.x, 2) + Mathf.Pow(dev.z, 2));
            float dist_Z = Mathf.Abs(dev.y);

            // 计算在XY平面上的加速度和减速度所需时间和距离
            float t_accXY = vxy_max / accXY_max;
            float dist_accXY = 0.5f * accXY_max * t_accXY * t_accXY;

            // 判断是否需要加速 并 计算在XY平面上的匀速时间和距离
            float dist_velXY;
            float t_velXY;
            if (dist_accXY * 2 >= dist_XY)
            {
                // 起点直接到达终点，没有匀速过程
                t_accXY = Mathf.Sqrt(dist_XY / accXY_max);
                t_velXY = 0f;
                dist_accXY = dist_XY;
                totalTime = t_accXY * 2;
            }
            else
            {
                // 需要进行加速、匀速和减速过程
                dist_velXY = dist_XY - 2 * dist_accXY;
                t_velXY = dist_velXY / vxy_max;
                totalTime = t_accXY + t_velXY + t_accXY;
            }

            // 计算在Z轴上的加速度和减速度所需时间和距离
            float t_accZ = maxVz / az_max;
            float dist_accZ = 0.5f * az_max * t_accZ * t_accZ;

            // 判断是否需要加速 并 计算在Z轴上的匀速时间和距离
            float dist_velZ;
            float t_velZ;
            if (dist_accZ * 2 >= dist_Z)
            {
                // 起点直接到达终点，没有匀速过程
                t_accZ = Mathf.Sqrt(dist_Z / az_max);
                t_velZ = 0f;
                dist_accZ = dist_Z;
            }
            else
            {
                // 需要进行加速、匀速和减速过程
                dist_velZ = dist_Z - 2 * dist_accZ;
                t_velZ = dist_velZ / maxVz;
            }

            // 计算总时间
            totalTime = Mathf.Max(totalTime, t_accZ + t_velZ + t_accZ);

            // 计算帧间隔时间
            float deltaTime = 1 / frame_rate;

            // 计算帧数
            int frameCount = Mathf.CeilToInt(totalTime * frame_rate);

            // 初始化路径数组
            var path = new Vector3[frameCount + 1];

            // X轴和Y轴上的位置
            float posX = startPoint.x;
            float posY = startPoint.z;
            // Z轴上的位置
            float posZ = startPoint.y;

            float t = deltaTime;

            float accX_max = Mathf.Sqrt(accXY_max);
            float accY_max = Mathf.Sqrt(accXY_max);
            float vx_max = Mathf.Sqrt(vxy_max);
            float vy_max = Mathf.Sqrt(vxy_max);

            // 计算每一帧的位置
            for (int j = 0; j < frameCount; j++)
            {
                // 判断当前时间所处的阶段，并根据阶段计算XY轴上的位置
                if (t < t_accXY)
                {
                    posX += 0.5f * accX_max * t * t * Mathf.Sign(dev.x);
                    posY += 0.5f * accY_max * t * t * Mathf.Sign(dev.z);
                }
                else if (t < t_accXY + t_velXY)
                {
                    posX += vx_max * t * Mathf.Sign(dev.x);
                    posY += vy_max * t * Mathf.Sign(dev.x);
                }
                else if (t < t_accXY + t_velXY + t_accXY)
                {
                    var nt = t - t_accXY - t_velXY;
                    //posX += 0.5f * accX_max * t * t * Mathf.Sign(dev.x);
                    //posY += 0.5f * accY_max * t * t * Mathf.Sign(dev.z);
                    posX -= 0.5f * accXY_max * (totalTime - t) * (totalTime - t) * Mathf.Sign(goalPoint.x - startPoint.x);
                    posY = goalPoint[2] - 0.5f * accXY_max * (totalTime - t) * (totalTime - t) * Mathf.Sign(goalPoint[2] - startPoint[2]);
                }

                // 判断当前时间所处的阶段，并根据阶段计算Z轴上的位置
                if (t < t_accZ)
                {
                    posZ = startPoint[1] + 0.5f * az_max * t * t * Mathf.Sign(goalPoint[1] - startPoint[1]);
                }
                else if (t < t_accZ + t_velZ)
                {
                    posZ = startPoint[1] + dist_accZ * Mathf.Sign(goalPoint[1] - startPoint[1]) + maxVz * (t - t_accZ) * Mathf.Sign(goalPoint[1] - startPoint[1]);
                }
                else
                {
                    posZ = goalPoint[1] - 0.5f * az_max * (totalTime - t) * (totalTime - t) * Mathf.Sign(goalPoint[1] - startPoint[1]);
                }

                // 更新路径数组中每一帧的位置
                path[j] = new Vector3(posX, posZ, posY);
            }
            path[frameCount] = goal[i];
            rawDancestep.data[i].AddRange(path);
        }

        return rawDancestep;
    }

    internal (double[,], double[,], double[,]) TakeOff2(Vector3[] start, float dist_Z, float az_max, float maxVz, float frame_rate)
    {
        float totalTime;

        // 计算在Z轴上的加速度和减速度所需时间和距离
        float t_accZ = maxVz / az_max;
        float dist_accZ = 0.5f * az_max * t_accZ * t_accZ;

        // 判断是否需要加速 并 计算在Z轴上的匀速时间和距离
        float dist_velZ = 0;
        float t_velZ;
        if (dist_accZ * 2 >= dist_Z)
        {
            // 起点直接到达终点，没有匀速过程
            t_accZ = Mathf.Sqrt(dist_Z / az_max);
            t_velZ = 0f;
            dist_accZ = dist_Z / 2;
        }
        else
        {
            // 需要进行加速、匀速和减速过程
            dist_velZ = dist_Z - 2 * dist_accZ;
            t_velZ = dist_velZ / maxVz;
        }

        // 计算总时间
        totalTime = t_accZ + t_velZ + t_accZ;

        // 计算帧间隔时间
        float deltaTime = 1 / frame_rate;

        // 计算帧数
        int frameCount = Mathf.CeilToInt(totalTime * frame_rate);

        int count = start.Length;

        // 初始化路径数组
        double[,] x = new double[count,frameCount + 1];
        double[,] y = new double[count,frameCount + 1];
        double[,] z = new double[count, frameCount + 1];
        float t = 0;

        // 计算每一帧的位置
        for (int j = 0; j < frameCount; j++)
        {
            t += deltaTime;

            float offset = 0;

            // 判断当前时间所处的阶段，并根据阶段计算Z轴上的位置
            if (t < t_accZ)
            {
                offset = 0.5f * az_max * t * t;
            }
            else if (t < t_accZ + t_velZ)
            {
                offset = dist_accZ + maxVz * (t - t_accZ);
            }
            else
            {
                offset = dist_Z - 0.5f * az_max * (totalTime - t) * (totalTime - t);
            }

            for (int i = 0; i < count; i++)
            {
                x[i, j] = start[i].x;
                y[i, j] = start[i].z;
                z[i, j] = start[i].y + offset;
            }
        }
        for (int i = 0; i < count; i++)
        {
            // X轴和Y轴上的位置
            // Z轴上的位置
            x[i, frameCount] = start[i].x;
            y[i, frameCount] = start[i].z;
            z[i, frameCount] = start[i].y + dist_Z;
        }
        return (x, y, z);
    }

    internal (double[,], double[,], double[,]) TakeOff(Vector3[] start, float dist_Z, FlyParams inst)
    {
        var maxVz = inst.vz_max;
        var az_max = inst.az_max;
        var az_min = -inst.az_min;

        var frame_rate = inst.frame_rate;

        // 计算在Z轴上的加速度和减速度所需时间和距离
        float t_accZ1 = maxVz / az_max;
        float dist_accZ1 = 0.5f * az_max * t_accZ1 * t_accZ1;

        float t_accZ2 = maxVz / az_min;
        float dist_accZ2 = 0.5f * az_min * t_accZ2 * t_accZ2;

        // 判断是否需要加速 并 计算在Z轴上的匀速时间和距离
        float dist_velZ = 0;
        float t_velZ;
        if (dist_accZ1 + dist_accZ2 >= dist_Z)
        {
            maxVz = Mathf.Sqrt(2 * dist_Z * az_min * az_max / (az_max + az_min));
            //没有匀速过程
            t_accZ1 = maxVz/az_max;
            t_accZ2 = maxVz/az_min;
            t_velZ = 0f;
            dist_accZ1 = 0.5f * maxVz * t_accZ1;
            //dist_accZ2 = 0.5f * maxVz * t_accZ2;
        }
        else
        {
            // 需要进行加速、匀速和减速过程
            dist_velZ = dist_Z - dist_accZ1 - dist_accZ2;
            t_velZ = dist_velZ / maxVz;
        }

        // 计算总时间
        float totalTime = t_accZ1 + t_velZ + t_accZ2;

        // 计算帧间隔时间
        float deltaTime = 1 / frame_rate;

        // 计算帧数
        int frameCount = Mathf.CeilToInt(totalTime * frame_rate+0.1f);

        int count = start.Length;

        // 初始化路径数组
        double[,] x = new double[count, frameCount + 1];
        double[,] y = new double[count, frameCount + 1];
        double[,] z = new double[count, frameCount + 1];
        float t = 0;

        // 计算每一帧的位置
        for (int j = 0; j < frameCount; j++)
        {
            t += deltaTime;

            float offset;

            // 判断当前时间所处的阶段，并根据阶段计算Z轴上的位置
            if (t <= t_accZ1)
            {
                offset = 0.5f * az_max * t * t;
            }
            else if (t <= t_accZ1 + t_velZ)
            {
                offset = dist_accZ1 + maxVz * (t - t_accZ1);
            }
            else
            {
                var nt = t - t_accZ1 - t_velZ;
                if (nt > t_accZ2)
                    nt = t_accZ2;
                offset = dist_accZ1 + dist_velZ + (maxVz - 0.5f * az_min * nt) * nt;
            }

            for (int i = 0; i < count; i++)
            {
                x[i, j] = start[i].x;
                y[i, j] = start[i].z;
                z[i, j] = start[i].y + offset;
            }
        }
        for (int i = 0; i < count; i++)
        {
            // X轴和Y轴上的位置
            // Z轴上的位置
            x[i, frameCount] = start[i].x;
            y[i, frameCount] = start[i].z;
            z[i, frameCount] = start[i].y + dist_Z;
        }
        Debug.Log($"t = {t},totalTime = {totalTime},lz1 = {z[0, frameCount - 1]}/{z[0, frameCount]}");
        return (x, y, z);
    }

    internal (double[,], double[,], double[,]) Land(Vector3[] start, FlyParams inst)
    {
        var maxVz = -inst.vz_min;
        var az_max = -inst.az_min; 
        var az_min = inst.az_max;

        var frame_rate = inst.frame_rate;

        // 计算帧间隔时间
        float deltaTime = 1 / frame_rate;

        int count = start.Length;

        float[][] pathlist = new float[count][]; 

        for (int i = 0; i < count; i++)
        {
            var dist_Z = start[i].y;

            // 计算在Z轴上的加速度和减速度所需时间和距离
            float t_accZ1 = maxVz / az_max;
            float dist_accZ1 = 0.5f * az_max * t_accZ1 * t_accZ1;

            float t_accZ2 = maxVz / az_min;
            float dist_accZ2 = 0.5f * az_min * t_accZ2 * t_accZ2;

            // 判断是否需要加速 并 计算在Z轴上的匀速时间和距离
            float dist_velZ = 0;
            float t_velZ;
            if (dist_accZ1 + dist_accZ2 >= dist_Z)
            {
                maxVz = Mathf.Sqrt(2 * dist_Z * az_min * az_max / (az_max + az_min));
                //没有匀速过程
                t_accZ1 = maxVz / az_max;
                t_accZ2 = maxVz / az_min;
                t_velZ = 0f;
                dist_accZ1 = 0.5f * maxVz * t_accZ1;
                //dist_accZ2 = 0.5f * maxVz * t_accZ2;
            }
            else
            {
                // 需要进行加速、匀速和减速过程
                dist_velZ = dist_Z - dist_accZ1 - dist_accZ2;
                t_velZ = dist_velZ / maxVz;
            }

            // 计算总时间
            float totalTime = t_accZ1 + t_velZ + t_accZ2;

            // 计算帧数
            int fc = Mathf.CeilToInt(totalTime * frame_rate + 0.1f);
            pathlist[i] = new float[fc];

            float t = 0;
            // 计算每一帧的位置
            for (int j = 0; j < fc; j++)
            {
                t += deltaTime;

                // 判断当前时间所处的阶段，并根据阶段计算Z轴上的位置
                if (t <= t_accZ1)
                {
                    pathlist[i][j] = 0.5f * az_max * t * t;
                }
                else if (t <= t_accZ1 + t_velZ)
                {
                    pathlist[i][j] = dist_accZ1 + maxVz * (t - t_accZ1);
                }
                else
                {
                    var nt = t - t_accZ1 - t_velZ;
                    if (nt > t_accZ2)
                        nt = t_accZ2;
                    pathlist[i][j] = dist_accZ1 + dist_velZ + (maxVz - 0.5f * az_min * nt) * nt;
                }
            }
        }
        int frameCount = GetMaxLength(pathlist);

        // 初始化路径数组
        double[,] x = new double[count, frameCount];
        double[,] y = new double[count, frameCount];
        double[,] z = new double[count, frameCount];

        for (int i = 0; i < count; i++)
        {
            var pos = start[i];
            float offset = 0;
            for (int j = 0; j < frameCount; j++)
            {
                if (pathlist[i].Length > j)
                {
                    offset = pathlist[i][j];
                }
                x[i, j] = pos.x;
                y[i, j] = pos.z;
                z[i, j] = pos.y - offset;
            }
        }

        return (x, y, z);
    }

    private int GetMaxLength(float[][] pathlist)
    {
        var length = pathlist[0].Length;
        if (pathlist.Length > 1)
            for (int i = 1; i < pathlist.Length; i++)
            {
                if (pathlist[i].Length > length)
                    length = pathlist[i].Length;
            }
        return length;
    }

    internal Vector3[][] MoveOffset(Vector3[] start, FlyParams inst, float x, float y, float z)
    {
        var vxy = inst.vxy_max;
        var vz = inst.vz_max;
        var frame_rate = inst.frame_rate;

        var dis_xy = Mathf.Sqrt(x * x + y * y);
        var fr_xy = frame_rate * dis_xy / vxy;
        var fr_z = frame_rate * z / vz;

        int fr = (int)(fr_xy > fr_z ? fr_xy : fr_z);
        fr++;

        x /= fr;
        y /= fr;
        z /= fr;

        int count = start.Length;
        Vector3[][] path = new Vector3[count][];
        for (int i = 0; i < count; i++)
        {
            path[i] = new Vector3[fr];
            for (int j = 0; j < fr; j++)
            {
                path[i][j].x = start[i].x + x * j;
                path[i][j].y = start[i].y + y * j;
                path[i][j].z = start[i].z + z * j;
            }
        }
        return path;
    }

    internal Vector3[][] MoveCaptionHorzl(Vector3[] start, FlyParams inst)
    {
        var vx = inst.vx_max;
        var vy = inst.vz_max;
        var ax = inst.ax_max;
        var ay = inst.ay_max;
        var frame_rate = inst.frame_rate;
        int count = start.Length;

        float dist = start.GetMaxX();

        var s_fr = frame_rate * dist / vx;
        s_fr++;
        var sx = -dist / s_fr;
        s_fr++;

        var dax = ax / frame_rate;
        var day = vy * 2 / frame_rate;

        Vector3[][] path = new Vector3[count][];
        for (int i = 0; i < count; i++)
        {
            path[i] = new Vector3[(int)s_fr];
            var tx = start[i].x + sx;
            var tz = start[i].y;
            var ty = start[i].z;
            var tv = vx;
            for (int j = 1; j < s_fr; j++)
            {
                path[i][j].x = tx;
                path[i][j].y = tz;
                path[i][j].z = ty;
                if (tx > 0)
                {
                    tx += sx;
                }
                else
                {
                    if (tv > -vx)
                    {
                        tv -= dax;
                        tx += (sx * (tv / vx));
                        ty += day;
                    }
                }
            }
        }
        return path;
    }

    public List<List<int>> InitSegmentQueues(int count, int row, int inv)
    {
        inv++;
        int queueCount = inv * inv;
        List<List<int>> queues = new List<List<int>>();
        //queues.Fill(queueCount);
        for (int i = 0; i < count; i++)
        {
            var indexRow = i % row;
            var IndexCol = i / row;

            var remainR = indexRow % inv;
            var remainC = IndexCol % inv;

            var index = remainC * inv + remainR;

            if (queues.Count <= index)
            {
                queues.Add(new List<int>());
            }
            queues[index].Add(i);
        }
        return queues;
    }

}
