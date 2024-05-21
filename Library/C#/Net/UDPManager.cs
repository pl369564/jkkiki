using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Xml;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Threading;

namespace Modules.Communication
{
    public class UDPManager
    {
        #region [基础功能]

        private static List<UdpClient> permanentClient = new List<UdpClient>();

        //private static Dictionary<int,UdpClient> sendClients = new Dictionary<int,UdpClient>();
        private static UdpClient sendUdp;

        /// <summary>
        /// 发送数据
        /// </summary>
        //private static UdpClient sendUdp;
        private static IPEndPoint sendIpEnd;
        private static IPAddress sendCurrentIPAddress;
        private static bool should_reset_sendudp;
        private static void SocketSend(byte[] sendData, IPAddress UDPClientAddRess, int port, bool isdebug = true)
        {
			try {
				//if (!sendClients.TryGetValue(port,out UdpClient sendUdp))
				//{
				//    sendUdp = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
				//    sendUdp.EnableBroadcast = true;
				//    sendClients.Add(port,sendUdp);
				//}

				if (sendUdp == null | should_reset_sendudp) {
					sendUdp = new UdpClient (new IPEndPoint (IPAddress.Any, 0));
					sendUdp.EnableBroadcast = true;
                    should_reset_sendudp = false;
				}

				if (sendCurrentIPAddress == null || !sendCurrentIPAddress.Equals (UDPClientAddRess)) {
					sendIpEnd = new IPEndPoint (UDPClientAddRess, port);
					sendCurrentIPAddress = UDPClientAddRess;
				}

				sendUdp.Send (sendData, sendData.Length, sendIpEnd);
				if (isdebug && sendData.Length > 6)
					Debug.LogWarning ($"SocketSend:msgid={sendData[5]}/{StringExpansion.ToString (sendData)},IpEndPoint:{sendIpEnd}");
			} catch (Exception e) {
				Debug.LogError ($"{UDPClientAddRess}/{port}:{StringExpansion.ToString (sendData)}");
				Debug.LogError (e);
                should_reset_sendudp = true;
			}
		}

        /// <summary>
        /// 接收
        /// </summary>
        private static UdpClient recvUdp;
        private static IPEndPoint recvIPEnd;
        private static IPAddress recvCurrentIPAddress;
        private static void SocketReceve(Action<byte[]> CallBack, IPAddress UDPClientAddRess, int port)
        {

            Debug.Log($"SocketReceve in {UDPClientAddRess}:{port}");
            Task.Run(() =>
            {
                //UdpClient udp = new UdpClient(iPEnd);
                if (recvUdp == null)
                {
                    recvIPEnd = new IPEndPoint(UDPClientAddRess, port);
                    recvUdp = new UdpClient(recvIPEnd);
                    recvCurrentIPAddress = UDPClientAddRess;
                }else if(!recvCurrentIPAddress.Equals(UDPClientAddRess))
                {
                    recvIPEnd = new IPEndPoint(UDPClientAddRess, port);
                    recvUdp = new UdpClient(recvIPEnd);
                    recvCurrentIPAddress = UDPClientAddRess;
                }

                var recvData = new byte[1024];
                if (recvUdp != null)
                {
                    recvData = recvUdp.Receive(ref recvIPEnd);
                    if (recvData.Length > 0)
                        CallBack(recvData);
                }
            });
        }

        private static UdpClient SocketRecevePermanent(CAction<byte[], IPEndPoint> CallBack, IPAddress UDPClientAddRess, int port)
        {
#if UNITY_STANDALONE || UNITY_EDITOR
            var iPEnd = new IPEndPoint(UDPClientAddRess, port);
            Debug.Log($"SocketReceve in {UDPClientAddRess}:{port}");
            UdpClient udp = new UdpClient(iPEnd);
#else
            IPEndPoint iPEnd = null;
            UdpClient udp = new UdpClient (port);
            Debug.Log ($"SocketReceve in {udp.Client.LocalEndPoint}");
#endif

			permanentClient.Add(udp);
            Task.Run(() =>
            {
                byte[] cacheBytes = null;
                while (udp?.Client!=null)
                {
                    var recvData = udp.Receive(ref iPEnd);
                    if (recvData.Length > 0)
                    {
                        if (cacheBytes != null && cacheBytes.Length != 0)
                        {
                            Debug.Log($"存在缓存上次处理剩下的数据:ipep = {iPEnd},l = {cacheBytes.Length}");

                            var data = new byte[cacheBytes.Length + recvData.Length];
                            Array.Copy(cacheBytes, 0, data, 0, cacheBytes.Length);
                            Array.Copy(recvData, 0, data, cacheBytes.Length, recvData.Length);

                            recvData = data;
                        }
                        var ndata = CallBack(recvData, iPEnd);
                        if (ndata != null && ndata[0] != 254)
                        {
							Debug.LogError("解析出错" + StringExpansion.ToString (ndata));
                            cacheBytes = null;
                        }
                        else
                        {
                            cacheBytes = ndata;
                        }
                    }
                }
				udp?.Close ();
				udp?.Dispose ();
				permanentClient?.Remove(udp);
                Debug.Log($"StopReceve in {UDPClientAddRess}:{port}");
            });
            return udp;
        }

        #endregion

        #region 公开接口
        public static void StopAllClients() 
        {
            //foreach (var sendUdp in sendClients.Values)
            //{
            //    sendUdp.Dispose();
            //}
            //sendClients.Clear();

            recvUdp?.Close();
            foreach (var item in permanentClient)
            {
                item.Dispose();
            }
            permanentClient.Clear();
        }

        /// <summary>
        /// 广播mavlink协议
        /// </summary>
        public static void SocketBroadCast<T>(T obj, int port = 8888,bool islog = true) where T : struct
        {
            var ipBytes = CommunicationMgr.GetLocalIPAddres().GetAddressBytes();
            ipBytes[3] = 255;
            SendMLStructure(obj,new IPAddress(ipBytes), CommunicationMgr.GetIPAddressEnd(), port,islog);
            //Instance.SocketSend2(GenerateMAVLinkPacket(obj), IPAddress.Broadcast, port);
        }
        /// <summary>
        /// 发送mavlink协议
        /// </summary>
        public static void SendMLStructure<T>(T obj, IPAddress iPAddress,byte ipend, int port, bool islog = true) where T : struct
        {
            SocketSend(MavlinkCUtil.GenerateMAVLinkPacket(obj, ipend, islog), iPAddress, port, islog);
        }
        /// <summary>
        /// 接收一次消息
        /// </summary>
        public static void ReceveData(Action<byte[]> CallBack, string UDPClientAddRes, int port)
        {
            IPAddress ipad;
            if (string.IsNullOrEmpty(UDPClientAddRes))
                ipad = CommunicationMgr.GetLocalIPAddres();
            else
                ipad = IPAddress.Parse(UDPClientAddRes);
            SocketReceve(CallBack, ipad, port);
        }
        /// <summary>
        /// 一直接收消息
        /// </summary>
        public static UdpClient ReceveDataPermanent(CAction<byte[],IPEndPoint> CallBack, string UDPClientAddRes, int port)
        {
            IPAddress ipad;
            if (string.IsNullOrEmpty(UDPClientAddRes))
                ipad = CommunicationMgr.GetLocalIPAddres();
            else
                ipad = IPAddress.Parse(UDPClientAddRes);
            return SocketRecevePermanent(CallBack, ipad, port);
        }

        public static void GroupCast(MAVLink.mavlink_plane_command_t msg, int port, List<IPAddress> iPAddresses, bool isLog)
        {
            var ipend = CommunicationMgr.GetIPAddressEnd();
            foreach (var ip in iPAddresses)
            {
                SendMLStructure(msg,ip,ipend,port,isLog);
            }
        }

        #endregion
    }

}
