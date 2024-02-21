using UnityEngine;
using UnityEngine.Animations;
using Normal.Realtime;
using static Normal.Realtime.Realtime;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.perceptlab.armultiplayer
{
    [RequireComponent(typeof(Realtime))]
    public class NetworkManager : MonoBehaviour
    {
        [SerializeField]
        private string roomName = "MyRoomName";
        private Realtime _realtime;
        [SerializeField] List<string> interactablePrefabNames;
        [SerializeField, Tooltip("The first object will be put here")] private Vector3 firstInstantiationPosition = new Vector3(0, -0.7f, 3.6f);
        [SerializeField, Tooltip("The distance between two consecutive instantiated objects")] private Vector3 InstantiationPositionOffset = new Vector3(0.1f, 0, 0);
        private string originPrefabName = "OriginRT";

        private void Awake()
        {
            // Get the Realtime component on this game object
            _realtime = GetComponent<Realtime>();

            // Notify us when Realtime successfully connects to the room
            _realtime.didConnectToRoom += DidConnectToRoom;
        }

        public void Connect()
        {
            RLogger.Log("NetworkManager calling _realtime.Connect");
            _realtime.Connect(roomName);
            RLogger.Log("NetworkManger _realtime.Connect called and finished");
        }

        private void DidConnectToRoom(Realtime realtime)
        {
            RLogger.Log("NetworkManager DidConnectToRoom called and client id is " + _realtime.room.clientID);
            if (_realtime.room.clientID == 0)
                instantiateObjects(realtime);

            instantiaceOrigin(realtime);
            
        }

        void instantiateObjects(Realtime realtime)
        {
            // Instantiate the Player for this client once we've successfully connected to the room
            Realtime.InstantiateOptions options = new InstantiateOptions
            {
                ownedByClient = true,
                preventOwnershipTakeover = false,
                destroyWhenOwnerLeaves = false,
                destroyWhenLastClientLeaves = true,
                useInstance = realtime
            };

            int i = 0;
            foreach (string name in interactablePrefabNames)
            {
                GameObject cube = Realtime.Instantiate(prefabName: name, position: Vector3.zero, rotation: Quaternion.identity, options);
                RealtimeTransform realtimeTransform = cube.GetComponent<RealtimeTransform>();
                cube.transform.localPosition = firstInstantiationPosition + i * InstantiationPositionOffset;
                i += 1;
                cube.transform.localRotation = Quaternion.identity;
                realtimeTransform.RequestOwnership();
            }
        }

        void instantiaceOrigin(Realtime realtime)
        {
            GameObject origin = Realtime.Instantiate(
                prefabName: originPrefabName, Vector3.zero, Quaternion.identity,
                new InstantiateOptions
                {
                    ownedByClient = true,
                    preventOwnershipTakeover = true,
                    destroyWhenOwnerLeaves = true,
                    destroyWhenLastClientLeaves = true,
                    useInstance = realtime
                });
        }

    }
}