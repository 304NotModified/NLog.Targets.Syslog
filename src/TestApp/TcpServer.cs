// Licensed under the BSD license
// See the LICENSE file in the project root for more information

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestApp
{
    internal class TcpServer: ServerSocket
    {
        private const int DefaultListeningSocketBacklog = 1000;
        private readonly ManualResetEvent signal;
        private readonly int listeningSocketBacklog;

        public TcpServer(int socketBacklog = 0)
        {
            ProtocolType = ProtocolType.Tcp;
            SocketType = SocketType.Stream;
            signal = new ManualResetEvent(false);
            listeningSocketBacklog = socketBacklog == 0 ? DefaultListeningSocketBacklog : socketBacklog;
        }

        protected override void SetupSocket(IPEndPoint ipEndPoint)
        {
            base.SetupSocket(ipEndPoint);
            BoundSocket.Listen(listeningSocketBacklog);
        }

        protected override void Receive()
        {
            if (!KeepGoing)
                return;

            signal.Reset();
            BoundSocket.BeginAccept(AcceptCallback, BoundSocket);
            signal.WaitOne();
        }

        private void AcceptCallback(IAsyncResult asyncResult)
        {
            if (!KeepGoing)
                return;

            signal.Set();
            var boundSocket = (Socket)asyncResult.AsyncState;
            var receivingSocket = boundSocket.EndAccept(asyncResult);
            var state = new TcpState(receivingSocket);
            state.BeginReceive(ReadCallback);
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            var state = (TcpState)asyncResult.AsyncState;

            if (!KeepGoing)
            {
                state.Dispose();
                return;
            }

            state.EndReceive(asyncResult, ReadCallback, OnReceivedString);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                signal.Dispose();
        }
    }
}