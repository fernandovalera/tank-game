using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace Tanks.TankControllers
{
    /// <summary>
    /// This class is to manage various settings on a tank.
    /// It works with the GameManager class to control how the tanks behave
    /// and whether or not players have control of their tank in the
    /// different phases of the game.
    /// </summary>
    [RequireComponent(typeof(TankMovement))]
    [RequireComponent(typeof(TankShooting))]
    [RequireComponent(typeof(TankHealth))]
    public class TankManager : NetworkBehaviour
    {

        #region Fields

        //Current spawn point used
        private Transform m_AssignedSpawnPoint;

        //Synced player ID, -1 means it has not changed yet (as the lowest valid player id is 0)
        [SyncVar(hook = "OnPlayerIdChanged")]
        protected int m_PlayerId = -1;

        //Synced score
        [SyncVar]
        protected int m_Wins = 0;

        //Synced rank, used at the end of the game to calculate the player's award
        [SyncVar(hook = "OnRankChanged")]
        protected int m_Rank = -1;

        public Color m_PlayerColor;
        public Transform m_SpawnPoint;
        [HideInInspector] public string m_ColoredPlayerText;

        private GameObject m_CanvasGameObject;

        #endregion


        #region Events

        //Fired when the player's rank has changed
        public event Action rankChanged;

        #endregion


        #region Properties

        public NetworkPlayer player
        {
            get;
            protected set;
        }

        public TankMovement movement
        {
            get;
            protected set;
        }

        public TankShooting shooting
        {
            get;
            protected set;
        }

        public TankHealth health
        {
            get;
            protected set;
        }

        public int playerNumber
        {
            get { return m_PlayerId; }
        }

        public int wins
        {
            get { return m_Wins; }
        }

        #endregion


        #region Methods

        public void Setup(NetworkPlayer player)
        {
            this.player = player;

            movement = GetComponent<TankMovement>();
            shooting = GetComponent<TankShooting>();
            health = GetComponent<TankHealth>();

            m_CanvasGameObject = GetComponentInChildren<Canvas>().gameObject;

            m_ColoredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">PLAYER " + m_PlayerId + "</color>";

            MeshRenderer[] renderers = player.GetComponentsInChildren<MeshRenderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material.color = m_PlayerColor;
            }
        }


        public void DisableControl()
        {
            movement.enabled = false;
            shooting.enabled = false;

            m_CanvasGameObject.SetActive(false);
        }


        public void EnableControl()
        {
            movement.enabled = true;
            shooting.enabled = true;

            m_CanvasGameObject.SetActive(true);
        }


        public void Reset()
        {
            transform.position = m_SpawnPoint.position;
            transform.rotation = m_SpawnPoint.rotation;

            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        #region SYNCVAR HOOKS

        private void OnRankChanged(int rank)
        {
            this.m_Rank = rank;
            if (rankChanged != null)
            {
                rankChanged();
            }
        }

        private void OnPlayerIdChanged(int playerId)
        {
            this.m_PlayerId = playerId;
        }

        #endregion

        #region Networking

        [Server]
        public void SetPlayerId(int id)
        {
            m_PlayerId = id;
        }


        [Server]
        public void AddWin()
        {
            m_Wins++;
        }




        #endregion


        #endregion
    }
}