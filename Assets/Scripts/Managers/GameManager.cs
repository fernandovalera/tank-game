using UnityEngine;
using System.Collections;
using Tanks.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using Tanks.TankControllers;
using System.Collections.Generic;
using TanksNetworkManager = Tanks.Networking.NetworkManager;


namespace Tanks
{
    /// <summary>
    /// Game state.
    /// </summary>
    public enum GameState
    {
        Inactive,
        TimedTransition,
        StartUp,
        Preplay,
        Preround,
        Playing,
        RoundEnd,
        EndGame,
        PostGame,
        EveryoneBailed
    }

    public class GameManager : NetworkBehaviour
    {

        //Singleton reference
        static public GameManager s_Instance;

        //This list is ordered descending by player score.
        static public List<TankManager> s_Tanks = new List<TankManager>();

        public int m_NumRoundsToWin = 5;
        public float m_StartDelay = 3f;
        public float m_EndDelay = 3f;

        public Text m_MessageText;

        public GameObject m_TankPrefab;
        public CameraControl m_CameraControl;


        private int m_RoundNumber;
        private TankManager m_RoundWinner;
        private TankManager m_GameWinner;

        private int m_RegisteredPlayers;

        //Transition state variables
        private float m_TransitionTime = 0f;
        private GameState m_NextState;

        //Current game state - starts inactive
        protected GameState m_State = GameState.Inactive;

        //Getter of current game state
        public GameState state
        {
            get { return m_State; }
        }

        [Server]
        public void RegisterNewPlayer(NetworkPlayer player, Transform startPosition)
        {
            m_RegisteredPlayers++;

            TankManager tank = Instantiate(m_TankPrefab, player.transform.position, player.transform.rotation).GetComponent<TankManager>();
            NetworkServer.Spawn(tank.gameObject);

            tank.SetPlayerId(m_RegisteredPlayers);
            tank.m_SpawnPoint = startPosition;
            tank.Setup(player);

            s_Tanks.Add(tank);

            GameObject[] targets = new GameObject[s_Tanks.Count];
            int i = 0;

            foreach (TankManager tankManager in s_Tanks)
            {
                targets[i++] = tankManager.gameObject;
            }

            player.RpcSetCameraTargets(targets);
            Debug.Log("Added new player: " + player.netId);
        }

        /// <summary>
        /// Unity message: Awake
        /// </summary>
        private void Awake()
        {
            //Sets up the singleton instance
            s_Instance = this;
        }


        [ServerCallback]
        private void Start()
        {
            //Set the state to startup
            Debug.Log("Starting game");
            m_State = GameState.StartUp;

            m_RoundNumber = 0;
            m_RegisteredPlayers = 0;
        }


        /// <summary>
        /// Unity message: Update
        /// Runs only on server
        /// </summary>
        [ServerCallback]
        protected void Update()
        {
            HandleStateMachine();
        }


        #region STATE HANDLING

        /// <summary>
        /// Handles the state machine.
        /// </summary>
        protected void HandleStateMachine()
        {
            switch (m_State)
            {
                case GameState.StartUp:
                    StartUp();
                    break;
                case GameState.TimedTransition:
                    TimedTransition();
                    break;
                case GameState.Preplay:
                    Preplay();
                    break;
                case GameState.Preround:
                    Preround();
                    break;
                case GameState.Playing:
                    Playing();
                    break;
                case GameState.RoundEnd:
                    RoundEnd();
                    break;
                case GameState.EndGame:
                    EndGame();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// State up state function
        /// </summary>
        protected void StartUp()
        {
            if (m_RegisteredPlayers >= 2)
            {
                Debug.Log("Transitioning from StartUp to Preplay");
                m_State = GameState.Preplay;
            }
        }
        
        /// <summary>
        /// Time transition state function
        /// </summary>
        protected void TimedTransition()
        {
            m_TransitionTime -= Time.deltaTime;
            if (m_TransitionTime <= 0f)
            {
                m_State = m_NextState;
            }
        }

        /// <summary>
        /// State up state function
        /// </summary>
        protected void Preplay()
        {
            Debug.Log("Starting Round");

            ResetAllTanks();
            DisableTankControl();

            m_RoundNumber += 1;

            SetTimedTransition(GameState.Preround, m_StartDelay);

            RpcRoundStarting(m_RoundNumber);
        }

        /// <summary>
        /// Clear messages and enable tanks
        /// </summary>
        protected void Preround()
        {
            EnableTankControl();

            RpcRoundPlaying();

            m_State = GameState.Playing;
        }

        /// <summary>
        /// Playing state function
        /// </summary>
        protected void Playing()
        {
            if (OneTankLeft())
            {
                m_State = GameState.RoundEnd;
            }
        }

        /// <summary>
        /// RoundEnd state function
        /// </summary>
        protected void RoundEnd()
        {
            DisableTankControl();

            m_RoundWinner = GetRoundWinner();

            if (m_RoundWinner != null)
            {
                m_RoundWinner.AddWin();
            }

            m_GameWinner = GetGameWinner();

            RpcRoundEnding();

            if (m_GameWinner != null)
            {
                SetTimedTransition(GameState.EndGame, m_EndDelay);
            }
            else
            {
                SetTimedTransition(GameState.Preplay, m_EndDelay);
            }
        }

        private void EndGame()
        {
            SceneManager.LoadScene(0);
        }

        /// <summary>
        /// Sets the timed transition
        /// </summary>
        /// <param name="nextState">Next state</param>
        /// <param name="transitionTime">Transition time</param>
        protected void SetTimedTransition(GameState nextState, float transitionTime)
        {
            Debug.Log("Transitioning from " + m_State + " to " + nextState);
            this.m_NextState = nextState;
            this.m_TransitionTime = transitionTime;
            m_State = GameState.TimedTransition;
        }

        #endregion


        [ClientRpc]
        private void RpcRoundStarting(int round)
        {
            m_MessageText.text = "Round " + round;
        }


        [ClientRpc]
        private void RpcRoundPlaying()
        {
            m_MessageText.text = "";
        }


        [ClientRpc]
        private void RpcRoundEnding()
        {
            m_MessageText.text = "Round ended";
        }

        private bool OneTankLeft()
        {
            int numTanksLeft = 0;

            foreach(TankManager tank in s_Tanks)
            {
                if (tank.gameObject.activeSelf)
                    numTanksLeft++;
            }

            return numTanksLeft <= 1;
        }

        private TankManager GetRoundWinner()
        {
            foreach (TankManager tank in s_Tanks)
            {
                if (tank.gameObject.activeSelf)
                    return tank;
            }

            return null;
        }


        private TankManager GetGameWinner()
        {
            foreach (TankManager tank in s_Tanks)
            {
                if (tank.wins == m_NumRoundsToWin)
                    return tank;
            }

            return null;
        }


        private string EndMessage()
        {
            string message = "DRAW!";

            if (m_RoundWinner != null)
                message = m_RoundWinner.m_ColoredPlayerText + " WINS THE ROUND!";

            message += "\n\n\n\n";

            foreach(TankManager tank in s_Tanks)
            {
                message += tank.m_ColoredPlayerText + ": " + tank.wins + " WINS\n";
            }

            if (m_GameWinner != null)
                message = m_GameWinner.m_ColoredPlayerText + " WINS THE GAME!";

            return message;
        }

        private void ResetAllTanks()
        {
            foreach (TankManager tank in s_Tanks)
            {
                tank.Reset();
            }
        }


        private void EnableTankControl()
        {
            foreach (TankManager tank in s_Tanks)
            {
                tank.EnableControl();
            }
        }


        private void DisableTankControl()
        {
            foreach(TankManager tank in s_Tanks)
            {
                tank.DisableControl();
            }
        }
    }
}