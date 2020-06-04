using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Net.WebSockets;

public class TestUI : MonoBehaviour
{
	[SerializeField]
	private Test m_test;

	[SerializeField]
	private Toggle m_tls;

	[SerializeField]
	private Button m_connect;

	[SerializeField]
	private Button m_send;

	[SerializeField]
	private Button m_disconnect;

	[SerializeField]
	private Text m_sendText;

	[SerializeField]
	private Text m_recvText;




	// Start is called before the first frame update
	void Start()
	{
		m_tls.SetIsOnWithoutNotify(m_test.IsUsingTls);
		m_tls.onValueChanged.AddListener(b => m_test.IsUsingTls = b);

		// Connect
		m_connect.interactable = true;
		m_connect.onClick.AddListener(delegate ()
		{
			var state = m_test.WebSocketState;
			bool closed = state == WebSocketState.Closed || state == WebSocketState.None;
			if (!closed)
				return;
			Debug.Log("UI requesting to connect.");
			m_test.StartConnecting();
		});


		// Send
		m_send.interactable = false;
		m_send.onClick.AddListener(delegate ()
		{
			if (!m_test.IsReadyToSend)
				return;
			Debug.Log("UI requesting to send.");
			m_test.DoWebsocketSend();
		});

		// Disconnect
		m_disconnect.interactable = false;
		m_disconnect.onClick.AddListener(delegate ()
		{
			Debug.Log("UI requests disconnection.");
			m_test.StartDisconnecting();
		});

		m_test.MessageSent += (s) => m_sendText.text = $"Sent: {s}";
		m_test.MessageReceived += (s) => m_recvText.text = $"Recv: {s}";


		m_test.WebSocketStateChanged += delegate ()
		{
			var s = m_test.WebSocketState;
			Debug.Log($"Socket state becomes {s}.");
			// There are many states, but the logic for the buttons is simple:
			// The connect button is interactable only when disconnected.
			// The other buttons are interactable only when connected.
			// In any other state, no buttons are interactable.
			bool closed = s == WebSocketState.Closed || s == WebSocketState.None;
			bool connected = s == WebSocketState.Open;
			m_connect.interactable = closed;
			m_tls.interactable = closed;
			m_send.interactable = connected;
			m_disconnect.interactable = connected;

			var es = EventSystem.current;
			Button newSelection = closed ? m_connect : connected ? m_send : null;
			if (newSelection != null)
				es.SetSelectedGameObject(newSelection.gameObject);
		};
	}


}
