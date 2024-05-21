using System;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class WebSocketHandle : MonoBehaviour {
	
	public static WebSocketHandle Instance;
	//int[] ports = new int[] { 4649, 8080, 1251, 1502, 1061, 1702, 2160, 2509, 3021 };
	private WebSocketServer m_WebSocketServer = null;
	/// <summary> 收到Scratch积木数据的回调 </summary>
	public Action<string> MessageCallback { get; set; }

	private void Awake()
	{
		Instance = this;
		ListenOnPort (4649);
	}

	private void OnDestroy()
	{
		m_WebSocketServer?.Stop ();
		m_WebSocketServer = null;
	}

	private bool ListenOnPort(int port)
	{
		m_WebSocketServer = new WebSocketServer ($"ws://0.0.0.0:{port}");
		m_WebSocketServer.AddWebSocketService<CallUnity> ("/CallUnity", (callUnity) => { callUnity.MessageCallback = MessageCallback; });
		m_WebSocketServer.Start ();
		if (m_WebSocketServer.IsListening) {
			Debug.Log ($"Listening on port {m_WebSocketServer.Port}, and providing WebSocket services:");
			foreach (var path in m_WebSocketServer.WebSocketServices.Paths)
				Debug.Log ($"- {path}");
			return true;
		}
		return false;
	}

	public void SendMessage(string message, string path = "/CallUnity")
	{
		if (m_WebSocketServer.IsListening && m_WebSocketServer.WebSocketServices[path] != null)
			m_WebSocketServer.WebSocketServices[path].Sessions.Broadcast (message);
	}

	private class CallUnity : WebSocketBehavior {

		/// <summary> 收到Scratch积木数据的回调 </summary>
		public Action<string> MessageCallback { get; set; }

		protected override void OnOpen()
		{
			Debug.Log ("CallUnity:OnOpen");
		}
		protected override void OnClose(CloseEventArgs e)
		{
			base.OnClose (e);
		}
		protected override void OnError(ErrorEventArgs e)
		{
			base.OnError (e);
		}

		protected override void OnMessage(MessageEventArgs e)
		{
			//Debug.Log (e.Data);
			//var obj = LitJson.JsonMapper.ToObject<FunctionData>(e.Data);
			//Debug.Log(obj.FuncType);
			//Newtonsoft.Json.Linq.JObject job = new Newtonsoft.Json.Linq.JObject();
			//job.Add("FuncType",obj.FuncType);
			//Instance.SendMessage(job.ToString());

			if (string.IsNullOrEmpty (e.Data)) {
				Debug.LogError ($"Scratch发来的消息为空! {DateTime.Now:HH:mm:ss}");
				return;
			}
			MessageCallback?.Invoke (e.Data);
			//Debug.Log ($"Scratch发来的消息:{Message}! {DateTime.Now:HH:mm:ss:ffff}");
		}
	}
}