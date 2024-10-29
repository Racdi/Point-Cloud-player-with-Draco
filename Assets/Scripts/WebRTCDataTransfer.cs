using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using WebSocketSharp;

namespace WebRTCTutorial
{
    public delegate void MessageHandler(string message);

    public class WebRTCDataTransfer : MonoBehaviour
    {
        [SerializeField]
        private string url = $"ws://10.1.2.159:8080";
        private WebSocket _ws;
        
        private readonly ConcurrentQueue<string> _receivedMessages = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<string> _receivedErrors = new ConcurrentQueue<string>();

        public event MessageHandler MessageReceived;

        public void SendWebSocketMessage(string message) => _ws.Send(message);
        public void Connect()
        {
            _ws = new WebSocket(url);
            // Subscribe to events
            _ws.OnMessage += OnMessage;
            _ws.OnError += OnError;
            // Connect
            _ws.Connect(); 

        }
        protected void Update()
        {
            // Process received errors on the main thread - Unity functions can only be called from the main thread
            while (_receivedErrors.TryDequeue(out var error))
            {
                Debug.LogError("WS error: " + error);
            }
            while (_receivedMessages.TryDequeue(out var message))
            {
                Debug.Log("WS Message Received: " + message);
                MessageReceived?.Invoke(message);
            }
        }
        protected void OnDestroy()
        {
            if (_ws == null)
            {
                return;
            }
            // Unsubscribe from events
            _ws.OnMessage -= OnMessage;
            _ws.OnError -= OnError;
            _ws.Close();
            _ws = null;
        }

        public void ChangeURL(string newURL)
        {
            url = newURL;
        }

        private void OnMessage(object sender, MessageEventArgs e) => _receivedMessages.Enqueue(e.Data);
        private void OnError(object sender, ErrorEventArgs e) => _receivedErrors.Enqueue(e.Message);
    }
}

