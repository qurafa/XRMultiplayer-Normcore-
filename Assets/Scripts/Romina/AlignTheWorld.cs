using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;

namespace com.perceptlab.armultiplayer
{
    public class AlignTheWorld : MonoBehaviour
    {

        private ARSession _ARSession;
        
        [SerializeField]
        GameObject room;
        [SerializeField, Tooltip("The topmost game object in hierarchy that is holding the player in the game (MRTK XR Rig in AR)")]
        GameObject player;
        [SerializeField, Tooltip("The camera")]
        GameObject player_camera;

        // assign the actions asset to this field in the inspector:
        [SerializeField, Tooltip("The action asset with MoveX, MoveY, MoveZ, RotateY, 1D axes and ActivateAlign and DoneAlign buttons")]
        private InputActionAsset actions;

        [SerializeField]
        private float move_speed = 0.003f;

        [SerializeField]
        private float rotatoin_speed = 0.1f;

        [SerializeField]
        public UnityEvent onDoneAlign;


        private bool active = true;
        private bool done = false;

        private InputAction moveX;
        private InputAction moveY;
        private InputAction moveZ;
        private InputAction rotateY;

        private void Awake()
        {
            RLogger.Log("Align the world is active");
            actions.FindActionMap("Align").FindAction("ActivateAlign").performed += OnActivateAlign;
            actions.FindActionMap("Align").FindAction("DoneAlign").performed += OnDoneAlign;
            moveX = actions.FindActionMap("Align").FindAction("MoveX");
            moveY = actions.FindActionMap("Align").FindAction("MoveY");
            moveZ = actions.FindActionMap("Align").FindAction("MoveZ");
            rotateY = actions.FindActionMap("Align").FindAction("RotateY");
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
            RLogger.Log("DoneAlign was pressed, world position is: " + room.transform.position.ToString() + " player position is: " + player.transform.position);
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


            Vector3 player_forward = player_camera.transform.forward; player_forward.y = 0f; player_forward = Vector3.Normalize(player_forward);
            Vector3 player_right = player_camera.transform.right; player_right.y = 0f; player_right = Vector3.Normalize(player_right);
            
            Vector3 translate = - player_forward*movez + player_right*movex - Vector3.up*movey;

            player.transform.Translate(-1f* translate * move_speed, Space.World);

            // if we assume objects are rendered straight up, which we did!, we only need to rotate the room around world y axis to adjust (we have assuemd x and z axes rotations are 0)
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
         * MoveX: axis -1 to 1
         * MoveZ: axis -1 to 1
         * MoveY: axix -1 to 1
         * RotateY: axis -1 to 1
         *********************************************/
    }
}