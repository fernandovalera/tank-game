using System;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;

namespace Tanks.Networking
{
    public class NetworkManager : UnityEngine.Networking.NetworkManager
    {
        public GameManager m_GameManager;
        public NetworkPlayer m_NetworkPlayerPrefab;

        private String m_ExternalIp;
        private short m_NumConnections;

        private static int s_BasePort = 11000;

        #region Singleton

        /// <summary>
        /// Gets the NetworkManager instance if it exists
        /// </summary>
        public static NetworkManager s_Instance
        {
            get;
            protected set;
        }

        public static bool s_InstanceExists
        {
            get { return s_Instance != null; }
        }

        #endregion


        #region Events

        /// <summary>
        /// Called on a client when they connect to a game
        /// </summary>
        public event Action<NetworkConnection> clientConnected;

        #endregion


        #region Unity Methods

        /// <summary>
        /// Initialize our singleton
        /// </summary>
        protected virtual void Awake()
        {
            if (s_Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                s_Instance = this;

                m_ExternalIp = new WebClient().DownloadString("http://icanhazip.com");
                m_NumConnections = 0;
            }
        }


        public override void OnClientConnect(NetworkConnection conn)
        {
            Debug.Log("OnClientConnect");

            ClientScene.Ready(conn);
            ClientScene.AddPlayer(0);

            clientConnected?.Invoke(conn);
        }


        public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
        {
            Transform startPosition = this.GetStartPosition();

            NetworkPlayer player = Instantiate<NetworkPlayer>(m_NetworkPlayerPrefab, startPosition.position, Quaternion.identity);
            NetworkServer.AddPlayerForConnection(conn, player.gameObject, playerControllerId);

            m_GameManager.RegisterNewPlayer(player, startPosition);

            int playerPort = s_BasePort + ++m_NumConnections;
            Debug.Log("Added player with player id: " + m_NumConnections);

            StartCoroutine(AsyncSocketListener.StartListening(conn, playerPort));
            player.RpcSetPlayerConnectionInfo(m_ExternalIp + ":" + playerPort);
        }


        #endregion
    }
}