using System.Collections;
using UnityEngine;
using Modules.Communication;
using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.IO;
using static MAVLink;
using UnityEngine.Networking;
using System.Text;
using GameMain;
using CCommon;
using UnityEngine.Events;

public class CommunicationMgr
{
    public const int PORT_SEND = 8085;
    public const int PORT_RECEIVE_IP = 8668;
    public const int PORT_TCP_CONNECT = 8888;

    public static byte IP_TAIL = 0;

    #region [获取本地IP]

    public static bool AP_MODE = true;

    public static bool CheckNetworkAdapter()
    {
        AP_MODE = !NetUtil.FindRouteDrone();
        //Debug.Log($"APMODE:{AP_MODE}");
        return AP_MODE;
    }

    public static IPAddress GetLocalIPAddres()
    {
        CheckNetworkAdapter();
        var ipad = NetUtil.GetIPAddres(AP_MODE);
        if(ipad!=null)
            IP_TAIL = ipad.GetAddressBytes()[3];
        return ipad;
    }

    public static byte[] GetLocalIP()
    {
        return GetLocalIPAddres()?.GetAddressBytes();
    }
    public static string GetLocalIP_Str()
    {
        return GetLocalIPAddres().ToString();
    }
    public static byte GetIPAddressEnd()
    {
        //return IP_TAIL;
        byte[] bytes = GetLocalIP ();
        return bytes == null || bytes.Length <= 0 ? byte.MaxValue : bytes[3];
    }

    #endregion

    public class MavLink
    {
        public static byte token;

        #region [FORMATION_CMD]
        /// <summary>
        /// 进入加速度计校准
        /// </summary>
        /// <param name="type">0=广播,1=点对点</param>
        public static mavlink_formation_cmd_t GetFormationCmd_ENTER_CALACC(ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.ENTER_CALACC, start_id: start_id, end_id: end_id, type: type);
        }

        /// <summary>
        /// 退出加速度计校准
        /// </summary>
        /// <param name="type">0=广播,1=点对点</param>
        public static mavlink_formation_cmd_t GetFormationCmd_EXIT_CALACC(ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.EXIT_CALACC, start_id: start_id, end_id: end_id, type: type);
        }

        /// <summary>
        /// 一键起飞
        /// </summary>
        /// <param name="high">起飞高度</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="rgbl">rgb+灯光模式</param>
        /// <param name="type">0=广播,1=点对点</param>
        public static mavlink_formation_cmd_t GetFormationCmd_OneKeyTakeoff(short high, FormationCmdMode mode, int rgbl, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.ONE_KEY_TAKEOFF, param1: high, param3: (short)mode, param4: rgbl, start_id: start_id, end_id: end_id, type: type);
        }

        /// <summary>
        /// 准备
        /// </summary>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_PREPARE(FormationCmdMode mode, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.PREPARE, param3: (short)mode, start_id: start_id, end_id: end_id, type: type);
        }

        /// <summary>
        /// 起飞
        /// </summary>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Takeoff(FormationCmdMode mode, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.TAKEOFF, param3: (short)mode, start_id: start_id, end_id: end_id, type: type, ack: 1);
        }

        /// <summary>
        /// 降落
        /// </summary>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_LAND(FormationCmdMode mode, ushort start_id = ushort.MinValue, ushort end_id = ushort.MaxValue, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.LAND, param3: (short)mode, type: type, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 悬停
        /// </summary>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_HOLD(short seconds, FormationCmdMode mode, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.HOLD, type: type, start_id: start_id, end_id: end_id, param1: seconds, param3: (short)mode);
        }

        /// <summary>
        /// 解锁
        /// </summary>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_ARM(FormationCmdMode mode, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd(MAV_FORMATION_CMD.ARM, param3: (short)mode, type: type, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 上锁
        /// </summary>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_DISARM(FormationCmdMode mode, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd(MAV_FORMATION_CMD.DISARM, param3: (short)mode, type: type, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 开启低电量显示
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_SHOWON_WARNING_BATT(ushort start_id, ushort end_id)
		{
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SHOWON_WARNING_BATT, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 关闭低电量显示
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_SHOWOFF_WARNING_BATT(ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SHOWOFF_WARNING_BATT, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 上下飞(上正下负)
        /// </summary>
        /// <param name="speed">移动速度</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="distance">移动距离</param>
        public static mavlink_formation_cmd_t GetFormationCmd_UP_DOWN(short speed, FormationCmdMode mode, short distance, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd(MAV_FORMATION_CMD.UP_DOWN, type: type, start_id: start_id, end_id: end_id, param1: speed, param3: (short)mode, param4: rgbl, z: distance);
        }
        /// <summary>
        /// 前后飞(前正后负)
        /// </summary>
        /// <param name="speed">移动速度</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="distance">移动距离</param>
        public static mavlink_formation_cmd_t GetFormationCmd_FORWARD_BACK(short speed, FormationCmdMode mode, short distance, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.FORWARD_BACK, type: type, start_id: start_id, end_id: end_id, param1: speed, param3: (short)mode, param4: rgbl, x: distance);
        }
        /// <summary>
        /// 左右飞(左正右负)
        /// </summary>
        /// <param name="speed">移动速度</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="distance">移动距离</param>
        public static mavlink_formation_cmd_t GetFormationCmd_LEFT_RIGHT(short speed, FormationCmdMode mode, short distance, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.LEFT_RIGHT, type: type, start_id: start_id, end_id: end_id, param1: speed, param3: (short)mode, param4: rgbl, y: distance);
        }

        /// <summary>
        /// 左右旋转(左正右负)
        /// </summary>
        /// <param name="speed">移动速度</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="angle">移动角度</param>
        public static mavlink_formation_cmd_t GetFormationCmd_RotateL_R(short speed, FormationCmdMode mode, short angle, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.YAW_ANGLE, type: type, start_id: start_id, end_id: end_id, param1: speed, param3: (short)mode, param4: rgbl, yaw: angle);
        }

        /// <summary>
        /// 弹跳模式v
        /// </summary>
        /// <param name="time">弹跳次数</param>
        /// <param name="distance">上下跳距离</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="rgbl">RGB灯</param>
        /// <returns></returns>
        public static mavlink_formation_cmd_t GetFormationCmd_Bounce(short time, short distance, FormationCmdMode mode, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.BOUNCE, type: type, start_id: start_id, end_id: end_id, param1: time, z: distance, param3: (short)mode, param4: rgbl);
        }

        /// <summary>
        /// 以机头前方设定距离为半径,环绕飞行
        /// </summary>
        /// <param name="radius">环绕半径（厘米）（正：逆时针 负：顺时针）</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Circle(short radius, FormationCmdMode mode, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.CIRCLE, type: type, start_id: start_id, end_id: end_id, param1: radius, param3: (short)mode, param4: rgbl);
        }

        /// <summary>
        /// 自转一定圈数
        /// </summary>
        /// <param name="loops">自转圈数（正：逆时针 负：顺时针）</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Yaw_Turn(short loops, FormationCmdMode mode, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.YAW_TURN, type: type, start_id: start_id, end_id: end_id, param1: loops, param3: (short)mode, param4: rgbl);
        }

        /// <summary>
        /// 翻滚
        /// </summary>
        /// <param name="motion">翻滚方向</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Filp(DIRECTION_OF_MOTION motion, FormationCmdMode mode, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.FLIP, type: type, start_id: start_id, end_id: end_id, param1: (short)motion, param3: (short)mode, param4: rgbl);
        }

        /// <summary>
        /// 直线飞行
        /// </summary>
        /// <param name="speed">飞行速度（0.01米每秒）（正：逆时针 负：顺时针）</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Straight_Flight(short speed, FormationCmdMode mode, short x, short y, short z, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.STRAIGHT_FLIGHT, type: type, start_id: start_id, end_id: end_id, param1: speed, param3: (short)mode, param4: rgbl, x: x, y: z, z: y);
        }

        /// <summary>
        /// 曲线飞行
        /// </summary>
        /// <param name="speed">飞行速度（0.01米每秒）（正：逆时针 负：顺时针）</param>
        /// <param name="curveMode">曲线飞行模式</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Curve_Flight(short speed, short curveMode, FormationCmdMode mode, short x, short y, short z, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.CURVE_FLIGHT, type: type, start_id: start_id, end_id: end_id, param1: speed, param2: curveMode, param3: (short)mode, param4: rgbl, x: x, y: z, z: y);
        }

        /// <summary>
        /// 以当前机头前方设定距离为半径,垂直画圆
        /// </summary>
        /// <param name="radius">半径（厘米）（正：向上 负：向下）</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Vertical_Circle(short radius, FormationCmdMode mode, ushort start_id, ushort end_id, int rgbl = 0, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.VERTICAL_CIRCLE, type: type, start_id: start_id, end_id: end_id, param1: radius, param3: (short)mode, param4: rgbl);
        }

        /// <summary>
        /// 开启避障功能, 即开启红外避障传感器
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_Enable_Avoidance(ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.ENABLE_AVOIDANCE, type: type, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 关闭避障功能, 即关闭红外避障传感器
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_Disable_Avoidance(ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.DISABLE_AVOIDANCE, type: type, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 设置避障逻辑
        /// </summary>
        /// <param name="avoidanceValue">0字节：某方有障碍，1字节：往某方运动</param>
        /// <param name="x">前后飞行距离。单位: 厘米</param>
        /// <param name="y">左右飞行距离。单位: 厘米</param>
        /// <param name="z">上下飞行距离。单位: 厘米</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Set_Avoidance(short avoidanceValue, FormationCmdMode mode, short x, short y, short z, ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SET_AVOIDANCE, type: type, start_id: start_id, end_id: end_id, param1: avoidanceValue, param3: (short)mode, x: x, y: y, z: z);
        }

        /// <summary>
        /// 设置灯光
        /// </summary>
        /// <param name="seconds">持续时间</param>
        /// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        /// <param name="r">红</param>
        /// <param name="g">绿</param>
        /// <param name="b">蓝</param>
        /// <param name="status">灯光模式</param>
        public static mavlink_formation_cmd_t GetFormationCmd_Set_RGB(short seconds, FormationCmdMode mode, byte r, byte g, byte b, MAV_RGB_STATUS status, ushort start_id, ushort end_id, byte type = 0)
        {
            int rgbl = 0;
            byte rgbStatus = (byte)status;
            rgbl += r;
            rgbl += (g << 8);
            rgbl += (b << 16);
            rgbl += (rgbStatus << 24);
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SET_RGB, type: type, start_id: start_id, end_id: end_id, param1: seconds, param3: (short)mode, param4: rgbl);
        }

        /// <summary>
        /// 取消设置灯光
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_Cancel_RGB(ushort start_id, ushort end_id, byte type = 0)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.CANSEL_RGB, type: type, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 设置飞行速度
        /// </summary>
        /// <param name="speed">速度。1: 低，2: 中，3: 高</param>
        public static mavlink_formation_cmd_t GetFormationCmd_SET_VELOCITY(byte speed, ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SET_VELOCITY, start_id: start_id, end_id: end_id, param1: speed);
        }

        /// <summary>
        /// 设置航向角灵敏度
        /// </summary>
        /// <param name="sensitivity">灵敏度。1: 低，2: 中，3: 高</param>
        public static mavlink_formation_cmd_t GetFormationCmd_SET_YAWRATE(byte sensitivity, ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SET_YAWRATE, start_id: start_id, end_id: end_id, param1: sensitivity);
        }

        /// <summary>
        /// 开灯
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_ENABLE_LED(ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.ENABLE_LED, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 关灯
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_DISABLE_LED(ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.DISABLE_LED, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 设置灯光亮度
        /// </summary>
        /// <param name="brightness">灯光亮度。1: 低，2: 中，3: 高</param>
        public static mavlink_formation_cmd_t GetFormationCmd_SET_RGB_BRIGHTNESS(byte brightness, ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SET_RGB_BRIGHTNESS, start_id: start_id, end_id: end_id, param1: brightness);
        }

        /// <summary>
        /// 打开低电降落
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_ENABLE_BATTERY_FS(ushort start_id, ushort end_id)
		{
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.ENABLE_BATTERY_FS, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 关闭低电降落
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_DISABLE_BATTERY_FS(ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.DISABLE_BATTERY_FS, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 设置飞机参数/同步飞机数据
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_SET_PARAMETER(int param3, int param4, ushort start_id, ushort end_id)
        {
            return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.SET_PARAMETER, param3: param3, param4: param4, start_id: start_id, end_id: end_id);
        }

        ///// <summary>
        ///// 进入巡线模式
        ///// </summary>
        ///// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        //public static mavlink_formation_cmd_t GetFormationCmd_Enter_Line_Walking(FormationCmdMode mode, ushort start_id, ushort end_id)
        //{
        //    return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.ENTER_FOLLOW_LINE, param3: (short)mode, start_id: start_id, end_id: end_id);
        //}

        ///// <summary>
        ///// 退出巡线模式
        ///// </summary>
        ///// <param name="mode">飞控命令应用模式。1: 应用于单机驾驶、多人对战、AR模式, 2: 应用于单机编程、编队编程、拖拽编程</param>
        //public static mavlink_formation_cmd_t GetFormationCmd_Exit_Line_Walking(FormationCmdMode mode, ushort start_id, ushort end_id)
        //{
        //    return GetMLS_Formation_Cmd (MAV_FORMATION_CMD.EXIT_FOLLOW_LINE, param3: (short)mode, start_id: start_id, end_id: end_id);
        //}

        /// <summary>
        /// 时间同步
        /// </summary>
        public static mavlink_system_time_t GetFormationCmd_TIME_SYNC(ulong time_unix_usec, uint time_boot_ms, ushort start_id, ushort end_id)
        {
            return GetMavlinkSystemTime_Cmd (MAV_FORMATION_CMD.TIME_SYNC, time_unix_usec, time_boot_ms, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 辅助舞步(暂时都填0)
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_AUX_SETUP(ushort start_id, ushort end_id, short x = 0, short y = 0, short z = 0)
        {
            return GetMLS_Formation_Cmd(MAV_FORMATION_CMD.AUX_SETUP, x: x, y: y, z: z, start_id: start_id, end_id: end_id);
        }

        /// <summary>
        /// 修改ID
        /// </summary>
        public static mavlink_formation_cmd_t GetFormationCmd_CHANGE_ID(ushort id, short newId)
        {
            return GetMLS_Formation_Cmd(MAV_FORMATION_CMD.CHANGE_ID, param1:newId, start_id: id, end_id: id);
        }


        public static mavlink_formation_cmd_t GetMLS_Formation_Cmd(MAV_FORMATION_CMD cmd, short param1 = 0, short param2 = 0, int param3 = 0, int param4 = 0
        , short x = 0, short y = 0, short z = 0, short yaw = 0, ushort start_id = 0, ushort end_id = 0, byte ack = 1, byte type = 0)
        {
            Debug.LogWarning($"MavLink_Formation_Cmd|cmd={cmd},param={param1}|{param2}|{param3}|{param4},xyz={x}|{y}|{z},yaw={yaw},start/endId={start_id}/{end_id}");
            mavlink_formation_cmd_t structure = new mavlink_formation_cmd_t();
            structure.cmd = (byte)cmd;
            structure.param1 = param1;
            structure.param2 = param2;
            structure.param3 = param3;
            structure.param4 = param4;
            structure.x = x;
            structure.y = y;
            structure.z = z;
            structure.yaw = yaw;
            structure.start_id = start_id;
            structure.end_id = end_id;
            structure.ack = ack;
            structure.type = type;
            structure.token = token++ ;
            return structure;
        }

        public static mavlink_system_time_t GetMavlinkSystemTime_Cmd(MAV_FORMATION_CMD cmd, ulong time_unix_usec, uint time_boot_ms, ushort start_id = 0, ushort end_id = 0)
        {
            Debug.LogWarning ($"MavLink_SystemTime_Cmd|cmd = {cmd},time_unix_usec={time_unix_usec},,start/endId = {start_id}/{end_id}");
            return new mavlink_system_time_t (time_unix_usec, time_boot_ms);
        }

        /// <summary>
        /// 获取文件信息
        /// </summary>
        /// <param name="fileSize">文件大小</param>
        /// <param name="danceInfo">舞步信息</param>
        /// <param name="danceBorder">舞步边界</param>
        /// <param name="planeId">对应飞机编号</param>
        /// <param name="fileType">0-升级内核文件 1-升级系统文件 2-舞步文件</param>
        /// <param name="versionBytes">舞步版本</param>
        /// <param name="md5Bytes">文件md5码</param>
        /// <param name="fileNameBytes">文件名</param>
        public static mavlink_file_heads_t FileHeads(uint fileSize, float[] danceInfo, float[] danceBorder, ushort planeId, byte fileType, byte[] versionBytes, byte[] md5Bytes, byte[] fileNameBytes)
        {
            Debug.LogWarning ($"MavLink_File_Heads|planeId={planeId},fileSize={fileSize},fileType={fileType}");
            mavlink_file_heads_t file_heads_t = new mavlink_file_heads_t (0, fileSize, danceInfo, danceBorder, planeId, fileType, versionBytes, md5Bytes, fileNameBytes);
            return file_heads_t;
        }
        #endregion

        #region [Plane_Command]
        public enum MAV_PLANE_CMD : byte {
            /// <summary>格式化。type==0: 照片, type==1: 视频, type==2: 舞步文件, type==3: 固件</summary>
            MAV_PLANE_CMD_STORAGE_FORMAT = 1,
            /// <summary>容量</summary>
            MAV_PLANE_CMD_STORAGE_CAPACITY = 2,
            /// <summary>UDP广播 ack 第一位 type 第二位 data 第三位 reserve第四位</summary>
            MAV_PLANE_CMD_WIFI_BROADCAST = 3,
            /// <summary>WIFI 模式切换。type==0: 2.4G热点, type==1: 5G热点, type==2: 路由模式</summary>
            MAV_PLANE_CMD_WIFI_MODE = 4,
            /// <summary>拍照。type==0: 拍照</summary>
			MAV_PLANE_CMD_TAKE_PHOTO = 5,
            /// <summary>录像。type==0: 开始录像, type==1: 停止录像</summary>
			MAV_PLANE_CMD_TAKE_VIDEO = 6,
            /// <summary>激光</summary>
			MAV_PLANE_CMD_LASER = 7,
            /// <summary>主摄俯仰</summary>
			MAV_PLANE_CMD_PITCH = 8,
            /// <summary>图传开关</summary>
			MAV_PLANE_CMD_SWITH_RTP = 9,
            /// <summary>下视二维码开关</summary>
			MAV_PLANE_CMD_SWITH_QR = 10,
            /// <summary>巡线检测开关</summary>
			MAV_PLANE_CMD_SWITH_LINE = 11,
            /// <summary>固件升级</summary>
			MAV_PLANE_CMD_UPDATE = 12,
            /// <summary>check adb file</summary>
			MAV_PLANE_CMD_ADB_CHECK = 13,
            /// <summary>adb update</summary>
			MAV_PLANE_CMD_ADB_UPDATE = 14,
            /// <summary>断开连接</summary>
			MAV_PLANE_CMD_DISCONNECT = 15,
            /// <summary>adb check main camera, take one picture</summary>
			MAV_PLANE_CMD_ADB_MAINCAM_CHECK = 16,
            /// <summary>adb check sub camera, take one picture</summary>
            MAV_PLANE_CMD_ADB_SUBCAM_CHECK = 17,
            /// <summary>设置wifi账号密码</summary>
            MAV_PLANE_CMD_SET_WIFI = 18,
            /// <summary>bt open or close</summary>
			MAV_PLANE_CMD_BT_BLE_SET = 19,
            /// <summary>获取扫描到的路由列表。type==0: sta模式默认连接路由信息, type==1: 所有可连接路由列表</summary>
			MAV_PLANE_CMD_GET_ROUTE_INFO = 20,
            /// <summary>获取版本</summary>
            MAV_PLANE_CMD_VERSION = 21,
            /// <summary>选择录像分辨率720p/1080p，默认720p</summary>
            MAV_PLANE_CMD_SELECT_VIDEO_RESOLUTION = 22,
            /// <summary>抗闪屏设置</summary>
            MAV_PLANE_CMD_RESIST_SREEN_FLICKER = 23,
            /// <summary>系统重启和关机</summary>
            MAV_PLANE_CMD_SHUTDOWN_REBOOT = 24,
            /// <summary>同步系统时间</summary>
            MAV_PLANE_CMD_SYNC_TIME = 25,
        }

		/// <summary>
		/// 切换2.4Gwifi
		/// </summary>
		public static mavlink_plane_command_t Plane_Cmd_SwitchWifiTo24G(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_WIFI_MODE, 0, plane_id);
        }
        /// <summary>
        /// 切换5Gwifi
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_SwitchWifiTo5G(ushort plane_id) 
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_WIFI_MODE,1, plane_id);
        }
        /// <summary>
        /// 切换AP模式
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_SwitchWifiToAP(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_WIFI_MODE, 2, plane_id);
        }

        /// <summary>
        /// 获取扫描到的路由列表。
        /// </summary>
        /// <param name="type">type==0: sta模式默认连接路由信息, type==1: 所有可连接路由列表</param>
        public static mavlink_plane_command_t Plane_Cmd_GET_ROUTE_INFO(byte type, ushort plane_id)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_GET_ROUTE_INFO, type, plane_id);
        }

		///// <summary>
		///// 获取系统版本
		///// </summary>
		//public static mavlink_plane_command_t Plane_Cmd_VERSION(ushort plane_id)
		//{
		//	return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_VERSION, 0, plane_id);
		//}

		/// <summary>
		///  bt切换wifi。
		/// </summary>
		/// <param name="type">type==0: 关闭, type==1: 开启</param>
		public static mavlink_plane_command_t Plane_Cmd_BT_BLE_SET(byte type, ushort plane_id)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_BT_BLE_SET, type, plane_id);
        }

        /// <summary>
        /// 照相
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_TAKE_PHOTO(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_TAKE_PHOTO,0,plane_id);
        }

        /// <summary>
        /// 开始录像
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_VIDEO_BeginRecord(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_TAKE_VIDEO, 0, plane_id);
        }

        /// <summary>
        /// 结束录像
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_VIDEO_EndRecord(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_TAKE_VIDEO, 1, plane_id);
        }

        /// <summary>
        /// 开启激光接收
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_OpenReserveLaser(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_LASER,2, plane_id);
        }

        /// <summary>
        /// 关闭激光接收
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_CloseReserveLaser(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_LASER, 3, plane_id);
        }

        /// <summary>
        /// 单发激光
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_SingleLaser(ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_LASER, 0, plane_id);
        }

        /// <summary>
        /// 一直连发无弹量
        /// </summary>
        /// <param name="plane_id">飞机编号</param>
        /// <param name="frequency">发射频率，范围1-14次/秒</param>
        public static mavlink_plane_command_t Plane_Cmd_MultiLaser(ushort plane_id, byte frequency)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_LASER, 4, plane_id, data: frequency);
        }

        /// <summary>
        /// 连发固定弹量激光
        /// </summary>
        /// <param name="plane_id">飞机编号</param>
        /// <param name="frequency">发射频率，范围1-14次/秒</param>
        /// <param name="loops">发射数量，范围1-255次</param>
        /// <param name="ack">0: 不回复消息，1: 回复消息</param>
        public static mavlink_plane_command_t Plane_Cmd_MultiLoopLaser(ushort plane_id, byte frequency, byte loops, byte ack = 1)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_LASER, 1, plane_id, data: frequency, reserve: loops, ack: ack);
        }

        /// <summary>
        /// 关闭发射激光
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_StopLaser(ushort plane_id, byte ack = 1)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_LASER, 5, plane_id, ack: ack);
        }

        /// <summary>
        /// 俯仰角变化
        /// </summary>
        /// <param name="plane_id">飞机编号</param>
        /// <param name="type">转动方向. 0-单机驾驶、多人对战界面中飞机摄像头上, 1-单机驾驶、多人对战界面中飞机摄像头下, 2和3算法控制，4-校准，5-单机编队编程积木上，6-单机编队编程积木下</param>
        /// <param name="angle">转动角度</param>
        /// <returns></returns>
        public static mavlink_plane_command_t Plane_Cmd_Pitch(ushort plane_id, byte type, ushort angle)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_PITCH, type, plane_id, data: angle);
        }

        /// <summary>
        /// 开启二维码
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_SWITH_QR(bool isopen, ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_SWITH_QR, (byte)(isopen ? 0 : 1), plane_id);
        }

        /// <summary>
        /// 图传
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_Swith_RTP(bool isopen,ushort plane_id)
        {
            return Get_Plane_Command(MAV_PLANE_CMD.MAV_PLANE_CMD_SWITH_RTP, (byte)(isopen ? 0 : 1), plane_id);
        }

        /// <summary>
        /// 固件升级
        /// </summary>
        public static mavlink_plane_command_t Plane_Cmd_Update(ushort plane_id)
		{
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_UPDATE, 0, plane_id);
        }

        /// <summary>
        /// 设置录像和RTP分辨率
        /// </summary>
		/// <param name="type">0: 720p, 1: 1080p, 2: 360P</param>
        /// <param name="plane_id">飞行器自身编号ID</param>
        public static mavlink_plane_command_t Plane_Cmd_SelectVideoResolution(byte type, ushort plane_id)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_SELECT_VIDEO_RESOLUTION, type, plane_id);
        }

        /// <summary>
        /// 抗闪屏设置
        /// </summary>
        /// <param name="type">0: 25fps(50Hz), 1: 30fps(60Hz)</param>
        /// <param name="plane_id">飞行器自身编号ID</param>
        public static mavlink_plane_command_t Plane_Cmd_ResistScreenFlicker(byte type, ushort plane_id)
        {
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_RESIST_SREEN_FLICKER, type, plane_id);
        }

        /// <summary>
        /// 系统重启和关机
        /// </summary>
        /// <param name="type">操作类型。0: 重启，1: 关机</param>
        /// <param name="plane_id">飞行器自身编号ID</param>
        public static mavlink_plane_command_t Plane_Cmd_SHUTDOWN_REBOOT(byte type, ushort plane_id)
		{
            return Get_Plane_Command (MAV_PLANE_CMD.MAV_PLANE_CMD_SHUTDOWN_REBOOT, type, plane_id);
        }

        public static mavlink_plane_command_t Get_Plane_Command(MAV_PLANE_CMD cmd, byte type, ushort plane_id, byte ack = 1, byte reserve = 0, ushort data = 0, ulong utc = 0)
        {
            Debug.LogWarning($"MavLink_Plane_Command|cmd = {cmd},type = {type},data = {data},reserve = {reserve},plane_id = {plane_id},token = {token}");
            mavlink_plane_command_t structure = new mavlink_plane_command_t();
            structure.cmd = (byte)cmd;
            structure.type = type;
            structure.utc = utc;
            structure.data = data;
            structure.plane_id = plane_id;
            structure.ack = ack;
            structure.reserve = reserve;
            structure.token = token++;
            return structure;
        }

        #endregion

        #region [Manual_Control]

        public static mavlink_manual_control_t Get_Manual_Control(short x = 0, short y = 0, short z = 0, short r = 0, ushort buttons = 0, byte target = 0)
        {
            var cmd = new mavlink_manual_control_t(x, y, z, r, buttons, target);
            return cmd;
        }

        #endregion

        /// <summary>
        /// wifi修改名称密码 type:0=2.4G,1=5G,2=路由模式
        /// </summary>
        public static mavlink_wifi_sets_t Get_SetWifi(ushort plane_id,byte type, string wifiname, string psw, byte ack = 1)
        {
            return new mavlink_wifi_sets_t(token++,plane_id,type, ack, Encoding.UTF8.GetBytes(wifiname), Encoding.UTF8.GetBytes(psw));
        }

        /// <summary>
        /// 颜色追踪
        /// </summary>
        public static mavlink_color_track_target_t ColorTrackTarget(byte fun_id, uint data_w, uint data_h, byte[] data, uint rect_x, uint rect_y, uint rect_w, uint rect_h)
        {
            Debug.LogWarning ($"MavLink_Color_Track_Target_Command|fun_id={fun_id},data_w={data_w},data_h={data_h},rect_x={rect_x},rect_y={rect_y},rect_w={rect_w},rect_h={rect_h}");
            return new mavlink_color_track_target_t (data_w, data_h, rect_x, rect_y, rect_w, rect_h, fun_id, data);
        }

        /// <summary>
        /// 巡线飞行
        /// </summary>
        /// <param name="fun_id">0:向前巡线，无视路口；1：向前巡线，遇到路口结束巡线；2：在路口悬停；3：在轨道上悬停； 10：退出巡线； 现在只有向前巡线</param>
        /// <param name="dist">距离，mm</param>
        /// <param name="tv">巡线时间，ms</param>
        /// <param name="way_color">巡线颜色色域，0-黑色 255-白色</param>
        public static mavlink_line_walking_t LineWalking(uint fun_id, uint dist, uint tv, uint way_color)
        {
            Debug.LogWarning ($"MavLink_Line_Walking_Command|fun_id={fun_id},dist={dist},tv={tv},way_color={way_color}");
            mavlink_line_walking_t line_walking = new mavlink_line_walking_t
			{
				fun_id = fun_id,
				dist = dist,
				tv = tv,
				way_color = way_color
			};
			return line_walking;
        }

        /// <summary>
        /// 二维码识别对齐
        /// </summary>
        /// <param name="duration">任务持续时间/超时时间</param>
        /// <param name="radius">0: close，1: open explore；当激活探索时，飞机在找不到二维码，就在一个小的范围内运动扫描寻找二维码</param>
        /// <param name="qrSize">告诉算法二维码的物理大小，用于计算飞机和二维码的距离，默认大小0.2m；给0表示数据无效，直接使用算法默认值；</param>
        /// <param name="runRate">二维码检测的运行帧率，根据现阶段cpu资源占用情况，最大帧率不超过5Hz，给0表示使用算法默认，大于5按照5处理，为保证安全，算法不采纳run_rate，先由算法自行决定；</param>
        /// <param name="qrId">1: mode=2||mode=12时，识别qr_id的二维码；2: 识别到二维码id，外部可根据qr_id执行相应的动作</param>
        /// <param name="mode">mode:
        /// 0、结束任务
        /// 1、开启下视摄像头二维码对齐并识别二维码
        /// 2、下视摄像头识别二维码ID【0，9】但不对齐二维码（识别到就退出）
        /// 11、开启主设二维码对齐并识别二维码
        /// 12、主设识别二维码ID【0，9】但不对齐二维码（识别到就退出）</param>
        /// <param name="backgroundGrayscale"></param>
        /// <returns></returns>
        public static mavlink_qrrecognite_deal_t QRRecogniteDeal(double duration, float radius, float qrSize, int runRate, int qrId, byte mode, sbyte backgroundGrayscale)
        {
            Debug.LogWarning ($"mavlink_qrrecognite_deal_Command|time_duration={duration},search_radius={radius},qr_size={qrSize},run_rate={runRate},qr_id={qrId},mode={mode},qr_background_grayscale={backgroundGrayscale}");
            mavlink_qrrecognite_deal_t deal = new mavlink_qrrecognite_deal_t
            {
                time_duration = duration,
                search_radius = radius,
                qr_size = qrSize,
                run_rate = runRate,
                qr_id = qrId,
                mode = mode,
                qr_background_grayscale = backgroundGrayscale
            };
            return deal;
        }

        /// <summary>
        /// 颜色识别
        /// </summary>
        /// <param name="mode">1: 开始,跑一帧</param>
        public static mavlink_colorrecog_t ColorRecognite(byte mode)
        {
            Debug.LogWarning ($"mavlink_colorrecog_Command|mode={mode}");
            return new mavlink_colorrecog_t () { mode = mode };
        }

        /// <summary>
        /// 识别标签。
        /// </summary>
        /// <param name="mode">0-9: 识别0-9的数字标签；
        /// 10: 识别左箭头；
        /// 11: 识别右箭头；
        /// 12: 识别上箭头；
        /// 13: 识别下箭头；
        /// 20: 结束任务。触发识别后识别过程持续4s，如果识别成功就立马结束。</param>
        public static mavlink_camera_t MarkRecognite(byte mode)
        {
            Debug.LogWarning ($"mavlink_camera_Command|mode={mode},type=0");
            return new mavlink_camera_t () { mode = mode, type = 0 };
        }

        /// <summary>
        /// 主摄追踪二维码n秒/主摄识别二维码
        /// </summary>
		/// <param name="mode">0-9: 识别0-9的二维码, 20: 结束任务</param>
        /// <param name="type">当识别二维码时，type==1表示持续识别，type==2表示片段跟踪（4s）</param>
        /// <param name="duration">追踪时间，duration=0时表示识别二维码，duration!=0时表示追踪二维码n秒</param>
        public static mavlink_camera_t TrackingQR(byte mode, byte type, ushort duration)
        {
            Debug.LogWarning ($"mavlink_camera_Command|mode={mode},type={type},time_duration={duration}");
            return new mavlink_camera_t () { mode = mode, type = type, time_duration = duration };
        }

        /// <summary>
        /// 添加Mavlink监听
        /// </summary>
        public static UnityAction<MAVLinkMessage, EndPoint> AddMLMsgListener<T>(Action<T, EndPoint> action, bool isMainThread = false) where T : struct
        {
            uint id = MavlinkCUtil.GetMavlinkId<T>();
            UnityAction<MAVLinkMessage, EndPoint> unityAction = (MAVLinkMessage msg, EndPoint iPEnd) =>
            {
                try
                {
                    T obj = msg.ToStructure<T>();
                    action(obj, iPEnd);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            };
            MavlinkMessageMgr.Instance.AddMLListener(id, unityAction, isMainThread);
            return unityAction;
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="isMainThread"></param>
        public static void RemoveListener<T>(UnityAction<MAVLinkMessage, EndPoint> action, bool isMainThread = false) where T : struct
        {
            uint id = MavlinkCUtil.GetMavlinkId<T>();

            MavlinkMessageMgr.Instance.RemoveListener(id,action, isMainThread);
        }

        /// <summary>
        /// 编队状态
        /// </summary>
        public enum MAV_FORMATION_STATUS
        {
            MAV_FORMATION_STATUS_ACTIVATED = 1, /* 0x01  | *///飞机激活
            MAV_FORMATION_STATUS_RANGE_SAFE = 2, /* 0x02  | *///安全范围内
            MAV_FORMATION_STATUS_TAKEOFF_ALLOW = 4, /* 0x04  | *///起飞许可
            MAV_FORMATION_STATUS_ARM = 8, /* 0x08  | *///已解锁
            MAV_FORMATION_STATUS_LAND = 16, /* 0x10  | *///降落中
            MAV_FORMATION_STATUS_EDUCATION = 32, /* 0x20  | *///教育版本
            MAV_FORMATION_STATUS_UWB = 64, /* 0x40  | *///UWB正常
            MAV_FORMATION_STATUS_DANCE = 128, /* 0x80  | *///舞步进行中
            MAV_FORMATION_STATUS_ENUM_END = 0x81, /*  | *///
        }

        /// <summary>
        /// 传感器状态
        /// </summary>
        public enum MAV_SYS_SENSOR
        {
            MAV_SYS_STATUS_SENSOR_3D_GYRO = 1, /* 0x01 3D gyro 陀螺仪| */
            MAV_SYS_STATUS_SENSOR_3D_ACCEL = 2, /* 0x02 3D accelerometer 加速度计| */
            MAV_SYS_STATUS_SENSOR_3D_MAG = 4, /* 0x04 3D magnetometer 磁力计| */
            MAV_SYS_STATUS_SENSOR_ABSOLUTE_PRESSURE = 8, /* 0x08 absolute pressure 绝对压强| */
            MAV_SYS_STATUS_SENSOR_GPS = 16, /* 0x10 GPS | */
            MAV_SYS_STATUS_SENSOR_OPTICAL_FLOW = 32, /* 0x20 optical flow 光流| */
            MAV_SYS_STATUS_SENSOR_Z_ALTITUDE_CONTROL = 64, /* 0x40 z/altitude control 高度控制| */
            MAV_SYS_STATUS_SENSOR_XY_POSITION_CONTROL = 128, /* 0x80 x/y position control 位置控制| */
            MAV_SYS_STATUS_SENSOR_MOTOR_OUTPUTS = 256, /* 0x100 motor outputs / control 电机输出| */
            MAV_SYS_STATUS_SENSOR_RC_RECEIVER = 512, /* 0x200 rc receiver | 遥控接收机*/
            MAV_SYS_STATUS_AHRS = 1024, /* 0x400 AHRS subsystem health 姿态航向系统运行状况| */
            MAV_SYS_STATUS_TERRAIN = 2048, /* 0x800 Terrain subsystem health | */
            MAV_SYS_STATUS_BATTERY = 4096, /* 0x1000 battery */
            MAV_SYS_STATUS_SENSOR_3D_GYRO_CALIBRATED = 8192, /* 0x2000 3D_GYRO 陀螺仪校准 */
            MAV_SYS_STATUS_SENSOR_3D_ACCEL_CALIBRATED = 16384, /* 0x4000 3D_ACCEL 加速度计校准*/
            MAV_SYS_STATUS_SENSOR_3D_MAG_CALIBRATED = 32768, /* 0x8000 3D_MAG 磁力计校准*/
            MAV_SYS_STATUS_APP_CONTROL = 65536, /* 0x10000 app control */
            MAV_SYS_STATUS_SENSOR_ENUM_END = 0x20000001, /*  | */
        }

        /// <summary>
        /// 传感器校准状态
        /// </summary>
        public enum MAV_CLIBRATION_STATUS
        {
            MAV_CLIBRATION_STATUS_IDLE = 1, /* 0x01  | *///不在校准中
            MAV_CLIBRATION_STATUS_ON = 2, /* 0x02  | *///校准中
            MAV_CLIBRATION_STATUS_SUCCESS = 4, /* 0x04  | *///校准成功
            MAV_CLIBRATION_STATUS_FAILED = 8, /* 0x08  | *///校准失败
            MAV_CLIBRATION_STATUS_END = 0x81, /*  | */
        }

    }

    public class UDP
    {
        /// <summary>
        /// 广播mavlink协议
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="structure"></param>
        public static void BroadCast_MLStructure<T>(T structure, int port = PORT_SEND, bool islog = true) where T:struct
        {
            UDPManager.SocketBroadCast(structure, port,islog);
        }

        /// <summary>
        /// 点对点发送
        /// </summary>
        public static void Send_MLStructure<T>(T structure, IPAddress iPAddress, int port = PORT_SEND, bool isLog = true) where T : struct
        {
            UDPManager.SendMLStructure(structure, iPAddress, GetIPAddressEnd(), port, isLog);
        }

        public static void StopAllPermanent() 
        {
            UDPManager.StopAllClients();
        }

        /// <summary>
        /// 开始监听端口并转换byte[]数据为mavlink
        /// </summary>
        /// <param name="UDPClientAddRes"></param>
        /// <param name="port"></param>
        public static void StartReceveMavlinkMsg(string UDPClientAddRes = "0.0.0.0", int port = PORT_RECEIVE_IP)
        {
            try {
                UDPManager.ReceveDataPermanent (MavlinkMessageMgr.Instance.ReceveMavlinkMsgBytes, UDPClientAddRes, port);
            } catch (Exception e) { Debug.LogError (e); }
        }

		public static void RemoveAllMLMsgListener<T>(bool isMainThread = false) 
        {
            uint id = MavlinkCUtil.GetMavlinkId<T>();
            MavlinkMessageMgr.Instance.RemoveAllListener(id, isMainThread);
        }
        public static void RemoveMLMsgListener<T>(UnityAction<MAVLinkMessage, EndPoint> action,bool isMainThread)
        {
            uint id = MavlinkCUtil.GetMavlinkId<T>();
            MavlinkMessageMgr.Instance.RemoveListener(id, action, isMainThread);
        }

        /// <summary>
        /// 广播ip
        /// </summary>
        public static void BroadCastIp(int port,byte[] ipBytes)
        {
            var msg = new mavlink_plane_command_t(0, MavLink.token++,ipBytes[2], 0, 3, ipBytes[0], ipBytes[1], ipBytes[3]);
            BroadCast_MLStructure(msg, port,false);
        }
        public static void GroupCastIp(int port,List<IPAddress> iPAddresses, byte[] ipBytes)
        {
            var msg = new mavlink_plane_command_t(0, MavLink.token++, ipBytes[2], 0, 3, ipBytes[0], ipBytes[1], ipBytes[3]);
            UDPManager.GroupCast(msg, port, iPAddresses, false);
        }
    }

    public class TCP 
    {
        public static void SendFile(string filePath,byte[] ip, int port)
        {
            Debug.Log($"dp:{filePath}|plane.ip = {ip}");
            try
            {
                var client = Connect(new IPAddress(ip), port);
                if (!client.Connected)
                {
                    Debug.LogError($"{ip}:TCP连接失败");
                    return;
                }
                if (File.Exists(filePath))
                {
                    byte[] bye = File.ReadAllBytes(filePath);
                    client.GetStream().Write(bye, 0, bye.Length);
                }
                else
                {
                    Debug.LogError($"{filePath}不存在");
                }
                client.Close();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
        }

        internal static TcpClient Connect(IPAddress iPAddress,int port)
        {
            var client = new TcpClient();
            client.Connect(iPAddress,port);
            return client;
        }
        internal static TcpClient ConnectToDevice(IPAddress iPAddress, int port, AsyncCallback asyncCallback)
        {
            var client = new TcpClient();
            IAsyncResult result = client.BeginConnect(iPAddress, port, asyncCallback, client);
			////Wait here until the callback processes the connection.
			//result.AsyncWaitHandle.WaitOne ();
			return client;
        }
        internal static void SendMavlinkMsg<T>(T structure, TcpClient tcpClient, bool isDebug = true)where T:struct
        {
            try
            {
                TCPManager.SocketSend(MavlinkCUtil.GenerateMAVLinkPacket(structure, GetIPAddressEnd(), isDebug), tcpClient, isDebug);

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e);
            } 
        
        }

        internal static void SendData(byte[] sendData,TcpClient tcpClient,bool isDebug = true) 
        {
            TCPManager.SocketSend(sendData,tcpClient,isDebug);
        }
        internal static void StartReceveMavlinkMsg(TcpClient tcpClient,int port, Action onDisConnect)
        {
            TCPManager.ReceveDataPermanent(
                MavlinkMessageMgr.Instance.ReceveMavlinkMsgBytes
                , tcpClient
                , port,onDisConnect);
        }

        internal static void StopReceve(TcpClient tcpClient)
        {
            TCPManager.StopReceve(tcpClient);
        }
    }

    public class HTTP 
    {
        private const string TEST_Head = "http://testdeploy-api.hg-fly.net";
        private const string Head = "https://ds-api.hg-fly.net";


        public static string GetAthleticsModeList(bool istest) 
        {
            return "/game/sign/brawl/modeList";
            //string p = "/sign/brawl/modeList";
            //if (istest)
            //    return TEST_Head + p;
            //else
            //    return Head + p;
        }
 
        public static string GetSaveGameData(bool istest)
        {
            string p = "/game/sign/brawl/saveGameData";
            if (istest)
                return TEST_Head + p;
            else
                return Head + p;
        }

        public static string LANServerIP = "";
        public static int LANServerPort = 8282;

        public static string GetAddRoom()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/add";
        }
        public static string GetEditRoom()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/edit";
        }
        public static string GetDeleteRoom()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/delete";
        }
        public static string GetRoomList()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/list";
        }
        public static string GetRoomDetail()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/detail";
        }
        public static string GetRoomAdjust()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/adjust";
        }
        public static string GetRandomAssign()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/randomAssign";
        }
        public static string GetAddPlayerInfo()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/addPlayerInfo";
        }
        public static string GetPlayerlist()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/getPlayers";
        }
        public static string GetEditDroneInfo()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/editDroneInfo";
        }
        public static string GetKickPlayer()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/kick";
        }
        public static string GetBeginGame()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/begin";
        }
        public static string GetBeHit()
        {
            return $"http://{LANServerIP}:{LANServerPort}/game/room/beHit";
        }

        public static IEnumerator GetHttpJson(string httpPath,Action<string> jAction)
        {
            UnityWebRequest uwr = UnityWebRequest.Get(httpPath);
            yield return uwr.SendWebRequest();
            //Debug.Log($"isError:{uwr.error}|path = {httpPath}|data:{uwr.downloadHandler.data.Length}");
            if (uwr.result == UnityWebRequest.Result.Success)
            {
                var text = uwr.downloadHandler.text;
                jAction(text);
            }
        }

        #region [请求飞机照片视频]

        public static string PictureJsonAdress(string planeip)
        {
            return $"http://{planeip}:12346/http.cgi?media_type=0&page=1&pagecount=20";
        }
        public static string VideoJsonAdress(string planeip)
        {
            return $"http://{planeip}:12346/http.cgi?media_type=1&page=1&pagecount=20";
        }
        public static string DeletePictureAddress(string planeip, string filename)
        {
            return $"http://{planeip}:12346/http.cgi?del_type=0&filename={filename}";
        }
        public static string DeleteVideoAddress(string planeip, string filename)
        {
            return $"http://{planeip}:12346/http.cgi?del_type=1&filename={filename}";
        } 
        #endregion


    }

    public class WebSocket 
    {
        public static void SendAddDroneMsg() 
        {
            SendWebMessage(0,2000);
        }
        public static void SendSelectDroneMsg(int id)
        {
            SendWebMessage(0, 2001, id);
        }
        public static void SendDeleteDroneMsg(int id)
        {
            SendWebMessage(0, 2003, id);
        }
        public static void SendCopyDroneMsg(int id)
        {
            SendWebMessage(0, 2004, id);
        }

        public static void SendWebMessage(int ClassType,int FuncType, object returnValue = null)
        {
            Debug.Log($"{ClassType}|{FuncType}|{returnValue}");
            FunctionReturnData functionReturnData = new FunctionReturnData
            {
                ClassType = ClassType,
                FuncType = FuncType,
                FuncReturnValue = returnValue?.ToString()
            };
            string message = LitJson.JsonMapper.ToJson(functionReturnData);

            WebSocketHandle.Instance.SendMessage(message.ToString());
        }

    }

    public class MavLinkKeyListener<K, V> where V : struct
    {
        Dictionary<K, Action<V, EndPoint>> m_CallbackDict;
        public delegate K ParseKey(V obj);
        ParseKey m_ParseKeyAction;
        readonly bool m_IsOnce;
        bool m_IsMainThread;
        UnityAction<MAVLinkMessage, EndPoint> m_UnityAction;
        public MavLinkKeyListener(ParseKey toGetkey,bool isOnce = false,bool isMainThread = false)
        {
            m_CallbackDict = new Dictionary<K, Action<V, EndPoint>>();
            m_UnityAction = MavLink.AddMLMsgListener<V>(OnMavListen, isMainThread);
            m_ParseKeyAction = toGetkey;
            this.m_IsOnce = isOnce;
            this.m_IsMainThread = isMainThread;
        }
        public void Listen(K key, Action<V> callback)
        {
            if (m_CallbackDict.TryGetValue(key, out Action<V, EndPoint> action2))
            {
                Debug.Log("有重复监听,将覆盖:" + key);
            }
            m_CallbackDict[key] = (v,e)=> { callback(v); };
        }
        public void Listen(K key, Action<V, EndPoint> callback)
        {
            if (m_CallbackDict.TryGetValue(key, out Action<V, EndPoint> action2))
            {
                Debug.Log("有重复监听,将覆盖:" + key);
            }
            m_CallbackDict[key] = callback;
        }
        public void RemoveListen(K key)
        {
            if (m_CallbackDict.TryGetValue(key, out Action<V, EndPoint> action2))
            {
                m_CallbackDict.Remove(key);
            }
        }
        private void OnMavListen(V obj,EndPoint iPEnd)
        {
            var k = m_ParseKeyAction(obj);
            if (m_CallbackDict.TryGetValue(k, out Action<V, EndPoint> action2))
            {
                if (m_IsOnce)
                    m_CallbackDict.Remove(k);
                action2(obj,iPEnd);
            }
        }
        public bool ContainsKey(K key) 
        {
            return m_CallbackDict.ContainsKey(key);
        }

        internal void Dispose()
        {
            m_CallbackDict.Clear();
            m_CallbackDict = null;
            UDP.RemoveMLMsgListener<V>(m_UnityAction, m_IsMainThread);
            m_UnityAction = null;
        }
    }
}

