using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;

namespace com.perceptlab.armultiplayer
{
    public class AlignTheWorld : MonoBehaviour
    {

        private ARSession _ARSession;

        [Header("XR Rig and Player Set up")]
        [SerializeField, Tooltip("The game object in hierarchy that moves with locomotion; \"Camera Offset\" in MRTK XR Rig and \"XR Origin\" in VR")]
        GameObject player;
        [SerializeField, Tooltip("The camera")]
        GameObject player_camera;
        [SerializeField, Tooltip("When reset is pressed the camera's position will be set to this transform's position, if not provided, the starting position will be used")]
        private Transform defaultCameraTransform;

        [Header("Input")]
        // assign the actions asset to this field in the inspector:
        [SerializeField, Tooltip("The action asset with MoveX, MoveY, MoveZ, RotateY, 1D axes and ActivateAlign and DoneAlign buttons")]
        private InputActionAsset actions;

        [Header("Speed")]
        [SerializeField]
        private float move_speed = 0.03f;

        [SerializeField]
        private float rotatoin_speed = 1f;

        [SerializeField]
        public UnityEvent onDoneAlign;


        private bool active = true;
        private bool done = false;

        private InputAction moveX;
        private InputAction moveY;
        private InputAction moveZ;
        private InputAction rotateY;

        private Vector3 defaultCameraPosition;

        private void Awake()
        {
            RLogger.Log("Align the world is active");
            actions.FindActionMap("Align").FindAction("ActivateAlign").performed += OnActivateAlign;
            actions.FindActionMap("Align").FindAction("DoneAlign").performed += OnDoneAlign;
            actions.FindActionMap("Align").FindAction("Reset").performed += OnReset;
            moveX = actions.FindActionMap("Align").FindAction("MoveX");
            moveY = actions.FindActionMap("Align").FindAction("MoveY");
            moveZ = actions.FindActionMap("Align").FindAction("MoveZ");
            rotateY = actions.FindActionMap("Align").FindAction("RotateY");

            defaultCameraPosition = (defaultCameraTransform == null)? player_camera.transform.position: defaultCameraTransform.position;
        }

        private void Start()
        {
            RLogger.Log("Align the world is active");
            _ARSession = GetComponent<ARSession>();
            if (_ARSession != null)
            {
                _ARSession.enabled = true;
            }
        }

        private void OnActivateAlign(InputAction.CallbackContext context)
        {
            RLogger.Log("BeginEndAlign was pressed");
            active = !active;
            if (_ARSession != null)
            {
                _ARSession.enabled = active;
            }
        }

        private void OnDoneAlign(InputAction.CallbackContext context)
        {
            if (done)
                return;
            RLogger.Log("DoneAlign was pressed, player position is: " + player.transform.position);
            active = false;
            done = true;
            onDoneAlign?.Invoke();
            if (_ARSession != null)
            {
                _ARSession.enabled = false;
                // revert the changes caused by _ARSession.MatchFrameRate = True
                Application.targetFrameRate = 0;
                QualitySettings.vSyncCount = 1;
            }
            actions.FindActionMap("Align").Disable();
        }

        private void OnReset(InputAction.CallbackContext context)
        {
            RLogger.Log("Reset was pressed");
            moveCameraToDestination(defaultCameraPosition);
            makeCameraFaceDirection(Vector3.right);
        }

        private void moveCameraToDestination(Vector3 destination)
        {
            Vector3 diff = player_camera.transform.position - player.transform.position;
            player.transform.position = destination + diff;
        }

        private void makeCameraFaceDirection(Vector3 direction)
        {
            direction.y = 0; direction.Normalize();
            Vector3 cameraFacing = player_camera.transform.forward; cameraFacing.y = 0; cameraFacing.Normalize();
            float angle = Vector3.SignedAngle(cameraFacing, direction, Vector3.up);
            player.transform.RotateAround(player_camera.transform.position, Vector3.up, angle);
        }

        // Moves the player instead of the world. Player is in the same real position, thus, the virtual world is being moved!
        void Update()
        {
            if (done)
            {
                RLogger.Log("align the world is destroyed cause: done");
                Destroy(this);
            }
            if (active)
            {
                movePlayer();
            }
        }

        void movePlayer()
        {
            float movex = moveX.ReadValue<float>();
            float movez = -moveZ.ReadValue<float>();
            float movey = -moveY.ReadValue<float>();
            float rotatey = rotateY.ReadValue<float>();


            //Vector3 player_forward = player_camera.transform.forward; player_forward.y = 0f; player_forward = Vector3.Normalize(player_forward);
            //Vector3 player_right = player_camera.transform.right; player_right.y = 0f; player_right = Vector3.Normalize(player_right);
            //Vector3 translate = - player_forward*movez + player_right*movex - Vector3.up*movey;

            Vector3 translate = -Vector3.right * movez - Vector3.forward * movex - Vector3.up * movey;

            player.transform.Translate(-1f* translate * move_speed, Space.World);

            // if we assume objects are rendered straight up, which we did!, we only need to rotate the room about the world y axis to adjust (we have assuemd x and z axes rotations are 0)
            player.transform.RotateAround(player.transform.position, Vector3.up, -1f * rotatey * rotatoin_speed);
        }

        void OnEnable()
        {
            actions.FindActionMap("Align").Enable();
        }
        void OnDisable()
        {
            actions.FindActionMap("Align").Disable();
        }


        // alternatively, you can move the room instead of player (the logic might be easier) and when done align is pressed, call relocate to add room to origin and relocate the player
        // this method is not tested
        //private void PerformRelocatin()
        //{
        //    // a_pos = diff_pos + b_pos => diff_pos = a_pos - b_pos => diff_pos = new_a_pos - new_b_pos => new_a_pos = diff_pos + new_b_pos
        //    // a_rot = diff_rot * b_rot => diff_rot = a_rot * inverse(b_rot) => diff_rot = new_a_rot * inverse(new_b_rot) => new_a_rot = diff_rot*new_b_rot
        //    RLogger.Log("performing relocation");
        //    Vector3 diff_pos = player.transform.position - room.transform.position;
        //    Quaternion diff_rot = player.transform.rotation * Quaternion.Inverse(room.transform.rotation);
        //    room.transform.position = Vector3.zero;
        //    room.transform.rotation = Quaternion.identity;
        //    player.transform.position = diff_pos;
        //    player.transform.rotation = diff_rot;
        //    RLogger.Log("relocation performed");
        //}


        /*********************************************
         * list of all required defined buttons and axes:
         * BegnEndAlign : button
         * DoneAlign: button
         * Reset: button 
         * MoveX: axis -1 to 1
         * MoveZ: axis -1 to 1
         * MoveY: axix -1 to 1
         * RotateY: axis -1 to 1
         *********************************************/
    }
}