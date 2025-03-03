using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SR2MP
{
    public class MultiplayerCore : MonoBehaviour
    {
        public static int GetIdentID(IdentifiableType ident)
        {
            return GameContext.Instance.AutoSaveDirector.SavedGame.identifiableTypeToPersistenceId.GetPersistenceId(ident);
        }
        
        public static Dictionary<int, IdentifiableType> identifiableTypes = new Dictionary<int, IdentifiableType>();

        internal static void InitializeIdentifiableTypes()
        {
            int idx = 0;
            foreach (var id in GameContext.Instance.AutoSaveDirector.SavedGame.identifiableTypeToPersistenceId._reverseIndex)
            {
                identifiableTypes.Add(idx, GameContext.Instance.AutoSaveDirector.SavedGame.identifiableTypeLookup[id.key]);
                idx++;
            }
        }
        
        bool getBeatrix = true;
        bool setUpBeatrix = false;
        GameObject localPlayer;

        void Start()
        {

        }

        void Update()
        {
            if (localPlayer == null)
            {
                if (SceneContext.Instance != null)
                {
                    if (SceneContext.Instance.Player != null)
                    {
                        SceneContext.Instance.Player.AddComponent<ReadData>();
                        localPlayer = SceneContext.Instance.Player;
                    }
                }
            }

            if (setUpBeatrix)
            {
                if (localPlayer != null)
                {
                    SetNetworkPlayerAnimator();
                    setUpBeatrix = false;
                }
            }

            if (getBeatrix)
            {
                if (SceneManager.GetActiveScene().name == "MainMenuEnvironment")
                {
                    CreateNetworkPlayer();
                    getBeatrix = false;
                    setUpBeatrix = true;
                }
            }
        }

        private void CreateNetworkPlayer()
        {
            var networkPlayer = GameObject.Find("BeatrixMainMenu");
            Instantiate(networkPlayer);

            networkPlayer.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
            networkPlayer.AddComponent<NetworkPlayer>();
            DontDestroyOnLoad(networkPlayer);
        }

        private void SetNetworkPlayerAnimator()
        {
            var localPlayerAnimator = localPlayer.GetComponent<Animator>();
            NetworkPlayer.Instance.PlayerAnimator.avatar = localPlayerAnimator.avatar;
            NetworkPlayer.Instance.PlayerAnimator.runtimeAnimatorController = localPlayerAnimator.runtimeAnimatorController;
        }
    }
}
