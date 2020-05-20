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
	private ClientWebSocket m_socket;

	private Task<string> m_task;
	private string m_result;

	public bool IsReady => m_socket != null && m_task == null;


	[SerializeField]
	private Text m_sentText;
	[SerializeField]
	private Text m_receivedText;

	// Start is called before the first frame update
	void Start()
	{
		m_cancel = new CancellationTokenSource().Token; // Is this needed?
		Task<ClientWebSocket> create = CreateAndConnect("ws://echo.websocket.org", m_cancel);
		create.ContinueWith(task => m_socket = task.Result);
	}



	private async Task<ClientWebSocket> CreateAndConnect (string uri, CancellationToken cancel)
	{
		ClientWebSocket client = new ClientWebSocket();
		var connect = client.ConnectAsync(new System.Uri("wss://echo.websocket.org"), cancel);
		await connect;
		return client;
	}


	public void DoWebsocketSend ()
	{
		if (!IsReady)
			return;

		string s = $"Hello: {System.Guid.NewGuid().ToString()}";
		m_task = SendAndReceive(m_socket, s, m_cancel);
		m_sentText.text = $"Sent: {s}";
		m_task.ContinueWith(delegate (Task<string> t)
		{
			m_result = t.Result;
			m_task = null;
		});
	}


	void Update()
	{
		if (! string.IsNullOrEmpty(m_result))
		{
			m_receivedText.text = $"Received: {m_result}";
			m_result = null;
		}
	}

	private async Task<string> SendAndReceive (ClientWebSocket socket, string mesg, CancellationToken cancel)
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
	
}
