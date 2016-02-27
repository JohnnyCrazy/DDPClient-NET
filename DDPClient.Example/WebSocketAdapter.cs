using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using DdpClient;
using SuperSocket.ClientEngine;
using WebSocket4Net;

namespace DDPClientNet.Example
{
    public class WebSocketAdapter : WebSocketAdapterBase
    {
        private WebSocket _webSocket;

        public WebSocketAdapter()
        {

        }

        public override event EventHandler<EventArgs> Opened;
        public override event EventHandler<EventArgs> Closed;
        public override event EventHandler<Exception> Error;
        protected override event EventHandler<string> MessageReceived;

        public override void Connect(string url)
        {
            _webSocket = new WebSocket(url);

            _webSocket.EnableAutoSendPing = false;
            _webSocket.NoDelay = true;

            _webSocket.MessageReceived += WebSocketOnMessageReceived;
            _webSocket.Opened += WebSocketOnOpened;
            _webSocket.Error += WebSocketOnError;
            _webSocket.Closed += WebSocketOnClosed;
            _webSocket.Open();
        }

        public override void ConnectAsync(string url)
        {
            throw new NotSupportedException("WebSocket4Net has no Async-Connect method");
        }

        public override void Send(string message)
        {
            Debug.WriteLine($"Sending: {message}");
            _webSocket.Send(message);
        }

        public override void Close()
        {
            _webSocket.Close();
        }

        public override bool IsAlive() => _webSocket.State == WebSocketState.Open;

        private void WebSocketOnClosed(object sender, EventArgs e)
        {
            Closed?.Invoke(this, e);
        }

        private void WebSocketOnError(object sender, ErrorEventArgs e)
        {
            throw e.Exception;
            Error?.Invoke(this, e.Exception);
        }

        private void WebSocketOnOpened(object sender, EventArgs e)
        {
            Opened?.Invoke(this, e);
        }

        private void WebSocketOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Debug.WriteLine($"Received: {e.Message}");
            MessageReceived?.Invoke(this, e.Message);
        }
    }
}