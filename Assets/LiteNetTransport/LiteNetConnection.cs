using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Mirror;

namespace LiteNetLibMirror
{
    public class LiteNetConnection : IConnection
    {
        Client client;
        Server server;
        int connectionId;

        List<ArraySegment<byte>> newData = new List<ArraySegment<byte>>();

        public LiteNetConnection(Client client)
        {
            this.client = client;
            this.client.onData += ClientReceive;
        }

        public LiteNetConnection(Server server, int id)
        {
            this.server = server;
            connectionId = id;
            this.server.onData += ServerRecive;
        }

        public void Disconnect()
        {
            if (client != null)
            {
                client.Disconnect();
                client = null;
            }
            if (server != null)
            {
                server.Stop();
                server = null;
            }
        }

        public EndPoint GetEndPointAddress()
        {
            if (client != null)
            {
                return client.RemoteEndPoint;
            }
            if (server != null)
            {
                return null; //TODO
            }
            return null;
        }

        void ClientReceive(ArraySegment<byte> data, int channel)
        {
            newData.Add(data);
        }

        void ServerRecive(int id, ArraySegment<byte> data, int channel)
        {
            newData.Add(data);
        }

        public async UniTask<int> ReceiveAsync(MemoryStream buffer)
        {
            if (client != null)
            {
                await ClientReceive(buffer);
                return Mirror.Channel.Reliable;
            }
            if (server != null)
            {
                await ServerReceive(buffer);
                return Mirror.Channel.Reliable;
            }
            await UniTask.FromResult(false);
            return Mirror.Channel.Reliable;
        }

        async UniTask<bool> ClientReceive(MemoryStream buffer)
        {
            try
            {
                //Wait for connected client to have data in queue. Early return if null or not connected
                await WaitFor(() => client == null || client.Connected && newData.Count > 0);
                if (client == null)
                    return false;

                buffer.SetLength(0);
                buffer.Write(newData[0].Array, newData[0].Offset, newData[0].Array.Length - newData[0].Offset);
                //Empty the queue
                newData.RemoveAt(0);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        async UniTask<bool> ServerReceive(MemoryStream buffer)
        {
            try
            {
                //Wait for new data to land in the queue. Early return if null or not connected
                await WaitFor(() => server == null || newData.Count > 0);
                if (server == null)
                    return false;

                buffer.SetLength(0);
                buffer.Write(newData[0].Array, newData[0].Offset, newData[0].Array.Length - newData[0].Offset);
                //Empty the queue
                newData.RemoveAt(0);
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public UniTask SendAsync(ArraySegment<byte> data, int channel = Mirror.Channel.Reliable)
        {
            if (client != null && client.Connected)
            {
                client.Send(0, data);
            }
            if (server != null)
            {
                server.Send(connectionId, channel, data);
            }
            return UniTask.CompletedTask;
        }

        public static async UniTask WaitFor(Func<bool> predicate)
        {
            while (!predicate())
            {
                await UniTask.Delay(1);
            }
        }
    }
}
