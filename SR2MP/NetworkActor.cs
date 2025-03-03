using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace SR2MP
{
    public class NetworkActor : MonoBehaviour
    {
        private bool HandlingDestruction;

        public bool HandleSync = true;
        
        public static Dictionary<long, NetworkActor> All = new Dictionary<long, NetworkActor>();

        public IdentifiableActor Identifiable;
        public Rigidbody RB;
        
        public Vector3 ReceivedPosition = Vector3.zero;
        public Vector3 ReceivedRotation = Vector3.zero;
        
        // Transform Smoothing
        private float PositionTimer;
        private const float INTERPOLATE_INTERVAL = 0.1f;
        
        private float ForceTimer;

        void Start()
        {
            Identifiable = GetComponent<IdentifiableActor>();
            RB = GetComponent<Rigidbody>();
            All.Add(Identifiable._model.actorId.Value, this);
        }

        public void ApplyForceFromClient(Vector3 force)
        {
            if (Time.unscaledTime < ForceTimer) return;

            SendData.SendActorMovementInteraction(Identifiable._model.actorId.Value, force);
            
            ForceTimer = Time.unscaledTime + .15f;
        }
        
        public void LateUpdate() => RB.velocity = Vector3.zero;
        
        void FixedUpdate()
        {
            if (!SteamLobby.Host)
            {
                if (!HandleSync) return;
                
                float t = 1.0f - ((PositionTimer - Time.unscaledTime) / INTERPOLATE_INTERVAL);
                transform.position = Vector3.Lerp(transform.position, ReceivedPosition, t);

                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(ReceivedRotation), t);

                PositionTimer = Time.unscaledTime + INTERPOLATE_INTERVAL;
                transform.position = ReceivedPosition;
                transform.eulerAngles = ReceivedRotation;
            }
            else
                SendData.SendActorUpdate(Identifiable);
            
        }

        void OnDestroy()
        {
            All.Remove(Identifiable._model.actorId.Value);
            
            if (!HandlingDestruction)
                SendData.SendActorDestroy(Identifiable);
        }
        
        
        public void Destroy()
        {
            HandlingDestruction = true;
            Destroy(gameObject);
        }

        void OnEnable()
        {
            if (SteamLobby.Host)
                SendData.SendActorSyncToggle(Identifiable._model.actorId.Value, true);
        }
        void OnDisable()
        {
            if (SteamLobby.Host)
                SendData.SendActorSyncToggle(Identifiable._model.actorId.Value, false);
        }
    }
}
