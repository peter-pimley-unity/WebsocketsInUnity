using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net.WebSockets;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.UI;

public class Test : MonoBehaviour
{

	private CancellationToken m_cancel;

	private ClientWebSocket m_socket = null;


	public event System.Action<string> MessageSent, MessageReceived;


	// Replies are placed in here by the worker thread,
	// and collected and processed by the main thread.
	private List<string> m_replies = new List<string>();


	public bool IsWaitingForReply { get; private set; }

	public bool IsReadyToSend => WebSocketState == WebSocketState.Open && !IsWaitingForReply;

	private bool m_tls = true;
	public bool IsUsingTls {
		get {
			return m_tls;
		}
		set {
			var state = WebSocketState;
			if (state != WebSocketState.None)
				throw new System.Exception(
					$"Can only set when state is None.  Currently {state}.");
			m_tls = value;
		}
	}


	// We poll the state on the main thread every frame, looking for changes.
	private WebSocketState m_lastState = WebSocketState.None;
	public WebSocketState WebSocketState => m_socket != null ? m_socket.State : WebSocketState.None;

	public System.Action WebSocketStateChanged; // Sends the old and the new states.

	private void Awake()
	{
		m_socket = new ClientWebSocket();
	}

	private void Update()
	{
		PollSocket();
		NotifyReplies();
	}

	private void PollSocket ()
	{
		WebSocketState newState = m_socket != null ? m_socket.State : WebSocketState.None;
		bool change = newState != m_lastState;
		m_lastState = newState;
		if (change)
			if (WebSocketStateChanged != null)
				WebSocketStateChanged();
	}

	private void NotifyReplies()
	{
		lock (m_replies)
		{
			if (m_replies.Count == 0)
				return;
			foreach (string s in m_replies)
				if (MessageReceived != null)
					MessageReceived(s);
			m_replies.Clear();
		}

	}



	public void StartConnecting()
	{
		Debug.Log("Connecting.");
		var state = WebSocketState;
		bool closed = state == WebSocketState.Closed || state == WebSocketState.None;
		if (!closed)
		{
			Debug.LogWarning($"StartConnecting called when not fully closed (state was {state}.");
			return;
		}

		string protocol = IsUsingTls ? "wss" : "ws";
		string url = $"{protocol}://echo.websocket.org";
		Debug.Log($"Connecting to {url}");
		m_socket = new ClientWebSocket();
		m_socket.ConnectAsync(new System.Uri(url), m_cancel);
	}


	public void DoWebsocketSend()
	{
		Debug.Log("Sending");
		bool ready = WebSocketState == WebSocketState.Open && !IsWaitingForReply;
		if (!ready)
		{
			Debug.LogWarning($"Ignoring request to send when state is {WebSocketState} and IsWaitingForReply={IsWaitingForReply}.");
			return;
		}

		string s = $"Hello: {System.Guid.NewGuid().ToString()}";

		if (MessageSent != null)
			MessageSent(s);

		IsWaitingForReply = true;
		Task<string> sendReceive = SendAndReceive(m_socket, s, m_cancel);

		sendReceive.ContinueWith(delegate (Task<string> t)
		{
			IsWaitingForReply = false;
			string reply = t.Result;
			lock (m_replies)
				m_replies.Add(reply);
		});
	}



	private static async Task<string> SendAndReceive (ClientWebSocket socket, string mesg, CancellationToken cancel)
	{
		System.Text.Encoding utf8 = System.Text.Encoding.UTF8;
		byte[] bytes = utf8.GetBytes(mesg);
		await socket.SendAsync(new System.ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancel);

		byte[] recvBuf = new byte[4096];

		WebSocketReceiveResult res = await socket.ReceiveAsync(new System.ArraySegment<byte>(recvBuf), cancel);

		bool ok = res.EndOfMessage && res.MessageType == WebSocketMessageType.Text;
		if (!ok)
			throw new System.Exception("Unexpected reply from remote server.");
		string s = utf8.GetString(recvBuf, 0, res.Count);
		return s;
	}
	

	public void StartDisconnecting ()
	{
		if (!IsReadyToSend)
		{
			Debug.LogWarning("Asked to disconnect while busy");
			return;
		}
		m_socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", m_cancel)
			.ContinueWith(t => m_socket = null);
	}
}
