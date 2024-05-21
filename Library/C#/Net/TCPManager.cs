using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;
using System.Threading.Tasks;

namespace Modules.Communication
{
    public delegate byte[] CAction<in T1, in T2>(T1 arg1, T2 arg2);
    public class TCPManager {
        public static TCPManager _instance;
        public static TCPManager Instance {
            get {
                if (_instance == null)
                    _instance = new TCPManager ();
                return _instance;
            }
        }

        private static readonly Dictionary<TcpClient, Thread> m_ClientsDict = new Dictionary<TcpClient, Thread> ();

        public static void SocketSend(byte[] sendData, TcpClient tcpClient, bool isDebug)
        {
            try {
                var stream = tcpClient.GetStream ();
                stream.Write (sendData, 0, sendData.Length);
                if (isDebug)
                    Debug.LogWarning ($"SocketSend:msgid={sendData[5]}/{StringExpansion.ToString (sendData)}\n{tcpClient.Client.LocalEndPoint}");
            } catch (Exception e) {
                throw (e);
            }
        }

        internal static void ReceveDataPermanent(CAction<byte[], EndPoint> CallBack, TcpClient client, int port, Action onDisConnect)
        {
            var remoteEndPoint = client.Client.RemoteEndPoint;
            Debug.Log ($"TCP SocketReceve in {remoteEndPoint}");
            var task = new Thread (() =>
            {
                byte[] bytes = new byte[1024];
                byte[] cacheBytes = null;
                int length = 0;
                try {
                    client.NoDelay = true;
                    while (client != null && m_ClientsDict.ContainsKey (client) && client.IsOnline ()) {
                        try {
                            client.ReceiveTimeout = 5000;
                            length = client.Client.Receive (bytes);
                        } catch (Exception e) {
                            Debug.Log ($"断开连接:{e}");
                            break;
                        }

                        if (length == 0)
                            break;

                        byte[] data;
                        if (cacheBytes != null && cacheBytes.Length != 0) {
                            Debug.Log ($"存在缓存上次处理剩下的数据:ipep = {remoteEndPoint},l = {cacheBytes.Length}");

                            data = new byte[cacheBytes.Length + length];
                            Array.Copy (cacheBytes, 0, data, 0, cacheBytes.Length);
                            Array.Copy (bytes, 0, data, cacheBytes.Length, length);
                        } else {
                            data = new byte[length];
                            Array.Copy (bytes, data, data.Length);
                        }

                        var ndata = CallBack (data, remoteEndPoint);
                        if (ndata != null && ndata[0] != 254) {
                            Debug.LogError ("解析出错" + StringExpansion.ToString (ndata));
                            cacheBytes = null;
                        } else {
                            cacheBytes = ndata;
                        }
                    }
                } catch (Exception e) {
                    Debug.LogError ($"SocketReceve exception:{e}");
                } finally {
                    //if (length != 0)
                    //    Debug.Log ($"DisConnect at :{StringExpansion.ToString (bytes)}/{length}/{(client != null ? (client.Client != null ? 0 : 1) : 2)}");
                    client?.Dispose ();
                    onDisConnect ();
                }
            });
            m_ClientsDict.Add (client, task);
            task.Start ();
        }

        internal static void StopReceve(TcpClient client)
        {
            if (client == null)
                return;
            try {
                //client.Client?.Dispose();
                //client.Dispose();
                if (m_ClientsDict.TryGetValue (client, out Thread task)) {
                    m_ClientsDict.Remove (client);
                    //if(task.IsAlive)
                    //    task.Abort();
                }
            } catch (Exception e) {
                //if (!(e is ThreadAbortException))
                Debug.LogError (e);
            }
        }
    }

    public static class TcpClientEx {
        public static bool IsOnline(this TcpClient tcpClient)
        {
            if (tcpClient.Client == null)
                return false;

            try {
                Socket socket = tcpClient.Client;
                bool offline = (socket.Poll (1000, SelectMode.SelectRead) && socket.Available == 0) || !socket.Connected;
                return !offline;
            } catch {
                return false;
            }
        }
    }
}
