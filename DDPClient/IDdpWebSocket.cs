using System;
using System.Collections.Generic;
using System.IO;
using WebSocketSharp;
using WebSocketSharp.Net;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;

namespace DdpClient
{
    public interface IDdpWebSocket
    {
        

        CompressionMethod Compression { get; set; }
        IEnumerable<Cookie> Cookies { get; }
        NetworkCredential Credentials { get; }
        bool EmitOnPing { get; set; }
        bool EnableRedirection { get; set; }
        string Extensions { get; }
        bool IsAlive { get; }
        bool IsSecure { get; }
        Logger Log { get; }
        string Origin { get; set; }
        WebSocketState ReadyState { get; }
        ClientSslConfiguration SslConfiguration { get; set; }
        Uri Url { get; }
        TimeSpan WaitTime { get; set; }
        void SendJson(object body);
        void Close();
        void Close(ushort code);
        void Close(CloseStatusCode code);
        void Close(ushort code, string reason);
        void Close(CloseStatusCode code, string reason);
        void CloseAsync();
        void CloseAsync(ushort code);
        void CloseAsync(CloseStatusCode code);
        void CloseAsync(ushort code, string reason);
        void CloseAsync(CloseStatusCode code, string reason);
        void Connect();
        void ConnectAsync();
        bool Ping();
        bool Ping(string message);
        void Send(byte[] data);
        void Send(FileInfo file);
        void Send(string data);
        void SendAsync(byte[] data, Action<bool> completed);
        void SendAsync(FileInfo file, Action<bool> completed);
        void SendAsync(string data, Action<bool> completed);
        void SendAsync(Stream stream, int length, Action<bool> completed);
        void SetCookie(Cookie cookie);
        void SetCredentials(string username, string password, bool preAuth);
        void SetProxy(string url, string username, string password);
        event EventHandler<CloseEventArgs> OnClose;
        event EventHandler<ErrorEventArgs> OnError;
        event EventHandler<MessageEventArgs> OnMessage;
        event EventHandler OnOpen;

        event EventHandler<DdpMessage> DdpMessage;
    }
}