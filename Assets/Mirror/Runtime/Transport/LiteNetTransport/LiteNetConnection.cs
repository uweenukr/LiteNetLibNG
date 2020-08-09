using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace LiteNetLibMirror
{
    public class LiteNetConnection : IConnection
    {
        Client client;
        Server server;
        int connectionId;

        ArraySegment<byte> newData;

        public LiteNetConnection(Client client)
        {
            this.client = client;
            client.onData += ClientReceive;
        }

        public LiteNetConnection(Server server, int id)
        {
            this.server = server;
            this.connectionId = id;
            server.onData += ServerRecive;
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
            newData = data;
        }

        void ServerRecive(int id, ArraySegment<byte> data, int channel)
        {
            newData = data;
        }

        public async Task<bool> ReceiveAsync(MemoryStream buffer)
        {
            try
            {
                if (client != null)
                {
                    //Wait for new data to land in the queue
                    await WaitFor(() => newData.Count > 0 || client == null);
                    if (client == null)
                        return false;

                    buffer.SetLength(0);
                    buffer.Write(newData.Array, 0, newData.Array.Length);
                    //Empty the queue
                    newData = new ArraySegment<byte>();
                    return true;
                }
                if (server != null)
                {
                    //Wait for new data to land in the queue
                    await WaitFor(() => newData.Count > 0 || server == null);
                    if (server == null)
                        return false;

                    buffer.SetLength(0);
                    buffer.Write(newData.Array, 0, newData.Array.Length);
                    //Empty the queue
                    newData = new ArraySegment<byte>();
                    return true;
                }
                return await Task.FromResult(false);
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
        }

        public Task SendAsync(ArraySegment<byte> data)
        {
            if (client != null)
            {
                client.Send(0, data);

            }
            if (server != null)
            {
                server.Send(connectionId, 0, data);
            }
            return Task.CompletedTask;
        }

        public static async Task WaitFor(Func<bool> predicate)
        {
            while (!predicate())
            {
                await Task.Delay(1);
            }
        }
    }
}
