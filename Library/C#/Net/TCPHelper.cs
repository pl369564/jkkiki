using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace Modules.Communication
{
    public delegate byte[] CAction<in T1, in T2>(T1 arg1, T2 arg2);
    public delegate bool VerifyAction<in T>(T arg);
    public class TCPHelper
    {
        private static Dictionary<TcpClient, Thread> m_ClientsDict = new Dictionary<TcpClient, Thread>();

        public static void SocketSend(byte[] sendData, TcpClient tcpClient, bool isDebug)
        {
            try
            {
                var stream = tcpClient.GetStream();
                stream.Write(sendData, 0, sendData.Length);
                if (isDebug) Debug.LogWarning($"SocketSend:msgid={sendData[9]:X2}-{sendData[10]:X2}/{sendData.TOString()}");
            }
            catch (Exception e)
            {
                throw(e);
            }
        }

        internal static void ReceveDataPermanent(CAction<byte[], EndPoint> CallBack, VerifyAction<byte[]> Verify, TcpClient client,Action onDisConnect)
        {
            var rep = client.Client.RemoteEndPoint;
            Debug.Log($"TCP SocketReceve in {rep}");
            var task = new Thread(() =>
            {
                byte[] bytes = new byte[2048];
                byte[] cacheBytes = null;
                int len = 0;
                try
                {
                    client.NoDelay = true;
                    while (m_ClientsDict.ContainsKey(client) && client?.Client!=null&& client.IsOnline())
                    {
                        try
                        {
                            //client.ReceiveTimeout = 0;
                            len = client.Client.Receive(bytes);
                        }
                        catch (Exception e)
                        {
                            Debug.Log($"断开连接:{e}");
                            break;
                        }
                        if (len == 0) 
                        {
                            break;
                        }
                        byte[] data;

                        if (cacheBytes != null && cacheBytes.Length != 0)
                        {
                            Debug.Log($"存在缓存上次处理剩下的数据:ipep = {rep},l = {cacheBytes.Length}");
                            
                            data = new byte[cacheBytes.Length + len];
                            Array.Copy(cacheBytes, 0, data, 0, cacheBytes.Length);
                            Array.Copy(bytes, 0, data, cacheBytes.Length, len);
                        }
                        else 
                        {
                            data = new byte[len];
                            Array.Copy(bytes, data, data.Length);
                        }
                        var ndata = CallBack(data, rep);
                        if (ndata != null && !Verify(ndata))
                        {
                            Debug.LogError("解析出错"+ ndata.TOString());
                            cacheBytes = null;
                        }
                        else
                        {
                            cacheBytes = ndata;
                        }
                    }
                    
                }
                catch (Exception e)
                {
                        Debug.LogError($":{e}");
                }
                finally
                {
                    if(len!=0)
                        Debug.Log($"DisConnect at :{bytes.TOString()}/{len}/{(client != null ? (client.Client != null ? 0 : 1) : 2)}");
                    if (client != null) 
                    {
                        client.Dispose();
                    }
                    onDisConnect();
                }
            });
            m_ClientsDict.Add(client, task);
            task.Start();
        }

        internal static void StopReceve(TcpClient client) 
        {
            if (client == null)
                return;
            try
            {
                //client.Client?.Dispose();
                //client.Dispose();
                if (m_ClientsDict.TryGetValue(client, out Thread task))
                {
                    m_ClientsDict.Remove(client);
                    //if(task.IsAlive)
                    //    task.Abort();
                }
            }
            catch (Exception e)
            {
                //if (!(e is ThreadAbortException))
                    Debug.LogError(e);
            }
        }
    }
    public static class TcpClientEx
    {
        public static bool IsOnline(this TcpClient c)
        {
            if (c == null||c.Client == null|| !c.Client.Connected)
                return false;
            var ispoll = c.Client.Poll(1000, SelectMode.SelectRead);
            return !((ispoll && (c.Client.Available == 0)) || !c.Client.Connected);
        }
    }
}
