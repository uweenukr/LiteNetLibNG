using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mirror;
using UnityEngine;

namespace LiteNetLibMirror
{
    public class LiteNetLibTransport : Transport
    {
        static readonly ILogger logger = LogFactory.GetLogger<LiteNetLibTransport>();

        [Header("Config")]
        public ushort port = 8888;
        public int updateTime = 15;
        public int disconnectTimeout = 5000;

        /// <summary>
        /// Active Client, null is no client is active
        /// </summary>
        Client client;
        /// <summary>
        /// Active Server, null is no Server is active
        /// </summary>
        Server server;
        List<int> newConnections = new List<int>();

        public override IEnumerable<string> Scheme => new[] { "litenet" };

        public override bool Supported => Application.platform != RuntimePlatform.WebGLPlayer;

        public override Task ListenAsync()
        {
            if (server != null)
            {
                logger.LogWarning("Can't start server as one was already active");
                return null;
            }

            server = new Server(port, updateTime, disconnectTimeout, logger);
            server.onConnected += OnNewConnection;
            server.Start();
            return Task.CompletedTask;
        }

        public override void Disconnect()
        {
            logger.Log("LiteNetLibTransport Shutdown");
            client?.Disconnect();
            client = null;
            server?.Stop();
            server = null;
        }

        public async override Task<IConnection> ConnectAsync(Uri uri)
        {
            if (client != null)
            {
                logger.LogWarning("Can't start client as one was already connected");
                return null;
            }

            client = new Client(port, updateTime, disconnectTimeout, logger);

            client.Connect(uri.Host);
            await Task.CompletedTask;
            return new LiteNetConnection(client);
        }

        public async override Task<IConnection> AcceptAsync()
        {
            try
            {
                await WaitFor(() => server == null || newConnections.Count > 0);
                if (server == null)
                    return null;

                LiteNetConnection conn = new LiteNetConnection(server, newConnections[0]);
                newConnections.RemoveAt(0);
                return conn;
            }
            catch (ObjectDisposedException)
            {
                // expected,  the connection was closed
                return null;
            }
        }

        void OnNewConnection(int id)
        {
            newConnections.Add(id);
        }

        void LateUpdate()
        {
            if (client != null)
            {
                client.OnUpdate();
            }
            if (server != null)
            {
                server.OnUpdate();
            }
        }

        public bool ClientConnected() => client != null && client.Connected;

        public bool ServerActive() => server != null;

        public override IEnumerable<Uri> ServerUri()
        {
            return new[] { server?.GetUri() };
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
