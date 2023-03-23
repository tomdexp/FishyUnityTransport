﻿using System;
using FishNet.Managing.Logging;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using UnityEngine;

namespace FishNet.Transporting.FishyUnityTransport
{
    [Serializable]
    internal class ServerSocket : CommonSocket
    {
        /// <summary>
        /// Maximum number of connections allowed.
        /// </summary>
        [Range(1, 4095)]
        public int MaximumClients = short.MaxValue; // TODO

        #region Start And Stop Server

        /// <summary>
        /// Starts the server.
        /// </summary>
        public bool StartConnection()
        {
            if (Driver.IsCreated || State != LocalConnectionState.Stopped)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError("Attempting to start a server that is already active.");
                return false;
            }

            InitializeNetworkSettings();

            SetLocalConnectionState(LocalConnectionState.Starting);

            bool succeeded = Transport.ProtocolType switch
            {
                ProtocolType.UnityTransport => ServerBindAndListen(Transport.ConnectionData.ListenEndPoint),
                ProtocolType.RelayUnityTransport => StartRelayServer(ref Transport.RelayServerData, Transport.HeartbeatTimeoutMS),
                _ => false
            };

            if (!succeeded && Driver.IsCreated)
            {
                Driver.Dispose();
                SetLocalConnectionState(LocalConnectionState.Stopped);
            }

            SetLocalConnectionState(LocalConnectionState.Started);

            return succeeded;
        }

        private bool StartRelayServer(ref RelayServerData relayServerData, int heartbeatTimeoutMS)
        {
            //This comparison is currently slow since RelayServerData does not implement a custom comparison operator that doesn't use
            //reflection, but this does not live in the context of a performance-critical loop, it runs once at initial connection time.
            if (relayServerData.Equals(default(RelayServerData)))
            {
                Debug.LogError("You must call SetRelayServerData() at least once before calling StartRelayServer.");
                return false;
            }

            NetworkSettings.WithRelayParameters(ref relayServerData, heartbeatTimeoutMS);
            return ServerBindAndListen(NetworkEndPoint.AnyIpv4);
        }

        private bool ServerBindAndListen(NetworkEndPoint endPoint)
        {
            InitDriver();

            // Bind the driver to the endpoint
            Driver.Bind(endPoint);
            if (!Driver.Bound)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                    Debug.LogError($"Unable to bind to the specified port {endPoint.Port}.");

                return false;
            }

            // and start listening for new connections.
            Driver.Listen();
            if (!Driver.Listening)
            {
                if (Transport.NetworkManager.CanLog(LoggingType.Error))
                {
                    Debug.LogError("Server failed to listen");
                }
                return false;
            }

            return true;
        }

        /// <summary>
        /// Stops the server.
        /// </summary>
        public bool StopServer()
        {
            SetLocalConnectionState(LocalConnectionState.Stopping);

            Shutdown();

            SetLocalConnectionState(LocalConnectionState.Stopped);
            return true;
        }

        #endregion

        /// <summary>
        /// Stops a remote client from the server, disconnecting the client.
        /// </summary>
        /// <param name="clientId"></param>
        public bool DisconnectRemoteClient(ulong clientId)
        {
            if (State != LocalConnectionState.Started) return false;

            FlushSendQueuesForClientId(clientId);

            ReliableReceiveQueues.Remove(clientId);
            ClearSendQueuesForClientId(clientId);

            NetworkConnection connection = ParseClientId(clientId);
            if (Driver.GetConnectionState(connection) != NetworkConnection.State.Disconnected)
            {
                Driver.Disconnect(connection);
            }

            Transport.HandleRemoteConnectionState(RemoteConnectionState.Stopped, clientId, Transport.Index);

            return true;
        }

        protected override void HandleDisconnectEvent(ulong clientId)
        {
            Transport.HandleRemoteConnectionState(RemoteConnectionState.Stopped, clientId, Transport.Index);
        }

        public string GetConnectionAddress(ulong clientId)
        {
            NetworkConnection connection = ParseClientId(clientId);
            return connection.GetState(Driver) == NetworkConnection.State.Disconnected
                ? string.Empty
                : Driver.RemoteEndPoint(connection).Address;
        }

        protected override void HandleIncomingConnection(NetworkConnection incomingConnection)
        {
            if (NetworkManager.ServerManager.Clients.Count >= MaximumClients)
            {
                DisconnectRemoteClient(ParseClientId(incomingConnection));
                return;
            }

            Transport.HandleRemoteConnectionState(new RemoteConnectionStateArgs(RemoteConnectionState.Started, incomingConnection.GetHashCode(), Transport.Index));
        }
        
        /// <summary>
        /// Gets the current ConnectionState of a remote client on the server.
        /// </summary>
        /// <param name="clientId">ConnectionId to get ConnectionState for.</param>
        public NetworkConnection.State GetConnectionState(ulong clientId)
        {
            return ParseClientId(clientId).GetState(Driver);
        }

        protected override void SetLocalConnectionState(LocalConnectionState state)
        {
            if (state == State) return;

            State = state;

            Transport.HandleServerConnectionState(new ServerConnectionStateArgs(state, Transport.Index));
        }

        protected override void OnPushMessageFailure(int channelId, ArraySegment<byte> payload, ulong clientId)
        {
            DisconnectRemoteClient(clientId);
        }

        protected override void HandleReceivedData(ArraySegment<byte> message, Channel channel, ulong clientId)
        {
            Transport.HandleReceivedData(message, channel, clientId, Transport.Index, true);
        }
    }
}