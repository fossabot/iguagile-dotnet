﻿using System;
using System.Linq;
using System.Threading.Tasks;

namespace Iguagile
{
    class TcpClient : IClient
    {
        private System.Net.Sockets.TcpClient _client;
        private System.Net.Sockets.NetworkStream _stream;

        public event ConnectionEventHandler Open;
        public event ConnectionEventHandler Close;
        public event ReceivedEventHandler Received;

        public void Connect(string address, int port)
        {
            _client = new System.Net.Sockets.TcpClient(address, port);
            Open?.Invoke();
            _stream = _client.GetStream();
            var messageSize = new byte[2];
            Task.Run(() =>
            {
                while (_client.Connected)
                {
                    _stream.Read(messageSize, 0, 2);
                    var size = BitConverter.ToUInt16(messageSize, 0);
                    var readSum = 0;
                    var buf = new byte[size];
                    var message = new byte[0];
                    while (readSum < size)
                    {
                        var readSize = _stream.Read(buf, 0, size - readSum);
                        message = message.Concat(buf.Take(readSize)).ToArray();
                        readSum += readSize;
                    }

                    Received?.Invoke(message);
                }

                _client.Dispose();
                _stream.Dispose();
                Close?.Invoke();
            });
        }

        public void Disconnect()
        {
            if (IsConnect())
            {
                _client.Close();
                _client.Dispose();
                _client = null;
            }
        }

        public bool IsConnect()
        {
            return _client?.Connected ?? false;
        }

        public void Send(byte[] data)
        {
            if (IsConnect() && (_stream?.CanWrite ?? false))
            {
                var size = data.Length;
                var message = BitConverter.GetBytes((ushort)size);
                message = message.Concat(data).ToArray();
                _stream.Write(message, 0, message.Length);
            }
        }
    }
}
