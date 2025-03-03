﻿using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SR2MP
{
    public class HandleData
    {
        public static void HandleMessage(Packet _packet)
        {
            var msg = _packet.ReadString();
            Console.WriteLine(msg);
        }

        public static void HandleMovement(Packet _packet)
        {
            var pos = _packet.ReadVector3();
            var rot = _packet.ReadFloat();

            if (NetworkPlayer.Instance != null)
            {
                NetworkPlayer.Instance.ReceivedPosition = pos;
                NetworkPlayer.Instance.ReceivedRotation = rot;
                NetworkPlayer.Instance.MovementReceived = true;
            }
        }

        public static void HandleAnimations(Packet _packet)
        {
            var horizontalMovement = _packet.ReadFloat();
            var forwardMovement = _packet.ReadFloat();
            var yaw = _packet.ReadFloat();
            var airborneState = _packet.ReadInt();
            var moving = _packet.ReadBool();
            var horizontalSpeed = _packet.ReadFloat();
            var forwardSpeed = _packet.ReadFloat();

            if (NetworkPlayer.Instance != null)
            {
                NetworkPlayer.Instance.HorizontalMovement = horizontalMovement;
                NetworkPlayer.Instance.ForwardMovement = forwardMovement;
                NetworkPlayer.Instance.Yaw = yaw;
                NetworkPlayer.Instance.AirborneState = airborneState;
                NetworkPlayer.Instance.Moving = moving;
                NetworkPlayer.Instance.HorizontalSpeed = horizontalSpeed;
                NetworkPlayer.Instance.ForwardSpeed = forwardSpeed;
                NetworkPlayer.Instance.AnimationsReceived = true;
            }
        }

        public static void HandleTime(Packet _packet)
        {
            var time = _packet.ReadDouble();

            if (SRSingleton<SceneContext>.Instance != null)
            {
                if (SRSingleton<SceneContext>.Instance.TimeDirector != null)
                {
                    SRSingleton<SceneContext>.Instance.TimeDirector._worldModel.worldTime = time;
                }
            }
        }

        public static void HandleInGame(Packet _packet)
        {
            var inGame = _packet.ReadBool();
            SteamLobby.FriendInGame = inGame;
        }

        public static void HandleSaveDataRequest(Packet _packet)
        {
            var memoryStream = new Il2CppSystem.IO.MemoryStream();
            SRSingleton<GameContext>.Instance.AutoSaveDirector.SaveGame();
            SRSingleton<GameContext>.Instance.AutoSaveDirector.SavedGame.Save(memoryStream);
            memoryStream.Seek(0L, Il2CppSystem.IO.SeekOrigin.Begin);

            var arraySave = memoryStream.ToArray();
            using (MemoryStream outputStream = new MemoryStream())
            {
                using (GZipStream gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gzipStream.Write(arraySave, 0, arraySave.Length);
                }
                arraySave = outputStream.ToArray();
            }

            SendData.SendSaveData(arraySave);
        }

        public static void HandleSaveData(Packet _packet)
        {
            var length = _packet.ReadInt();
            var array = _packet.ReadBytes(length);

            using (MemoryStream inputStream = new MemoryStream(array))
            {
                using (GZipStream gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    using (MemoryStream outputStream = new MemoryStream())
                    {
                        gzipStream.CopyTo(outputStream);
                        array = outputStream.ToArray();
                    }
                }
            }

            var memoryStream = new Il2CppSystem.IO.MemoryStream(array);
            memoryStream.Seek(0L, Il2CppSystem.IO.SeekOrigin.Begin);
            SRSingleton<GameContext>.Instance.AutoSaveDirector.BeginLoad(memoryStream, "SR2MP", "SR2MP");
        }

        public static void HandleLandPlotUpgrade(Packet _packet)
        {
            var name = _packet.ReadString();
            var upgrade = _packet.ReadInt();

            var gameObject = GameObject.Find(name);
            if (gameObject != null)
            {
                var landPlot = gameObject.GetComponentInChildren<LandPlot>();
                landPlot.AddUpgrade((LandPlot.Upgrade)upgrade);
            }
        }

        public static void HandleLandPlotReplace(Packet _packet)
        {
            var name = _packet.ReadString();
            var type = _packet.ReadInt();

            var gameObject = GameObject.Find(name);
            if (gameObject != null)
            {
                var landPlotLocation = gameObject.GetComponent<LandPlotLocation>();
                var oldLandPlot = landPlotLocation.GetComponentInChildren<LandPlot>();
                var replacementPrefab = SRSingleton<GameContext>.Instance.LookupDirector.GetPlotPrefab((LandPlot.Id)type);
                landPlotLocation.Replace(oldLandPlot, replacementPrefab);
            }
        }

        public static int ReceivedCurrency;
        public static bool CurrencyReceived;
        public static void HandleCurrency(Packet _packet)
        {
            var currency = _packet.ReadInt();

            if (SRSingleton<SceneContext>.Instance != null)
            {
                int difference = currency - SRSingleton<SceneContext>.Instance.PlayerState._model.currency;
                SRSingleton<SceneContext>.Instance.PlayerState._model.currency = currency;
                SRSingleton<PopupElementsUI>.Instance.CreateCoinsPopup(difference, PlayerState.CoinsType.NORM);
            }

            ReceivedCurrency = currency;
            CurrencyReceived = true;
        }

        public static void HandleSleep(Packet _packet)
        {
            var endTime = _packet.ReadDouble();

            if (SRSingleton<LockOnDeath>.Instance != null)
            {
                SRSingleton<LockOnDeath>.Instance.LockUntil(endTime, 0f);
            }
        }

        public static void HandlePrices(Packet _packet)
        {
            var count = _packet.ReadInt();

            var prices = new float[count];
            for (int i = 0; i < count; i++)
            {
                prices[i] = _packet.ReadFloat();
            }
            EconomyDirector_ResetPrices.ReceivedPrices = prices;
        }

        public static void HandleMapOpen(Packet _packet)
        {
            var name = _packet.ReadString();

            var gameObject = GameObject.Find(name);
            if (gameObject != null)
            {
                var techUIInteractable = gameObject.GetComponent<TechUIInteractable>();
                techUIInteractable.OnInteract();
            }
        }

        public static void HandleGordoEat(Packet _packet)
        {
            var name = _packet.ReadString();
            var count = _packet.ReadInt();

            var gameObject = GameObject.Find(name);
            if (gameObject != null)
            {
                var gordoEat = gameObject.GetComponent<GordoEat>();
                gordoEat.GordoModel.GordoEatenCount = count;

                if (gordoEat.GetEatenCount() >= gordoEat.GetTargetCount())
                    gordoEat.StartCoroutine(gordoEat.ReachedTarget());
            }
        }
        public static void HandleActorSpawn(Packet _packet)
        {
            var ident = _packet.ReadInt();
            var objID = _packet.ReadLong();
            var initPos = _packet.ReadVector3();

            var gameObject = InstantiationHelpers.InstantiateActor(
                MultiplayerCore.identifiableTypes[ident].prefab,
                SystemContext.Instance.SceneLoader.CurrentSceneGroup, // Just a place holder, please add to the packet soon!
                initPos,
                Quaternion.identity);
            
            if (objID != -1)
                gameObject.GetComponent<IdentifiableActor>()._model.actorId = new ActorId(objID);

            gameObject.AddComponent<NetworkActor>();

            if (SteamLobby.Host)
            {
                SendData.SendActorSpawn(gameObject.GetComponent<IdentifiableActor>());
            }
        }
        
        public static void HandleActorUpdate(Packet _packet)
        {
            var objID = _packet.ReadLong();
            var initPos = _packet.ReadVector3();
            var initAngles = _packet.ReadVector3();

            if (NetworkActor.All.TryGetValue(objID, out var actor))
            {
                actor.ReceivedPosition = initPos;
                actor.ReceivedRotation = initAngles;
            }
        }
        
        public static void HandleActorDestroy(Packet _packet)
        {
            var objID = _packet.ReadLong();

            if (NetworkActor.All.TryGetValue(objID, out var actor))
                actor.Destroy();
        }
        
        public static void HandleActorMovementInteraction(Packet _packet)
        {
            var objID = _packet.ReadLong();
            var force = _packet.ReadVector3();

            if (NetworkActor.All.TryGetValue(objID, out var actor))
                if(actor.TryGetComponent<Rigidbody>(out var rigidbody))
                    rigidbody.velocity = force;
        }
        
        public static void HandleActorSyncToggle(Packet _packet)
        {
            var objID = _packet.ReadLong();
            var toggle = _packet.ReadBool();

            if (NetworkActor.All.TryGetValue(objID, out var actor))
                actor.HandleSync = toggle;
        }
    }
}
