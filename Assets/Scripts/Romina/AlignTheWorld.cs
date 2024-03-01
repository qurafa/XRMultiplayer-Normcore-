using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.ARFoundation;

namespace com.perceptlab.armultiplayer
{
    public class AlignTheWorld : MonoBehaviour
    {
        [Serializable]
        struct ButtonHandler
        {
            public string name;
            public UnityEvent onPressed;
        }
        private ARSession _ARSession;
        [SerializeField]
        GameObject room;
        [SerializeField, Tooltip("The topmost game object in hierarchy that is holding the player in the game (MRTK XR Rig in AR)")]
        GameObject player;
        [SerializeField, Tooltip("The camera")]
        GameObject player_camera;

        [SerializeField]
        private float move_speed = 0.003f;

        [SerializeField]
        private float rotatoin_speed = 0.01f;

        private bool active = true;
        private bool done = false;

        [SerializeField]
        public UnityEvent onDoneAlign;

        [SerializeField]
        private List<ButtonHandler> OptionalButtons;


        //List<string> xboxButtons = new List<string> { "X", "Y", "A", "B", "Left Stick Button", "Right Stick Button", "Start", "Back", "RB", "LB", };
        //List<string> xboxButtons = new List<string> { "BeginEndAlign", "DoneAlign" };
        //List<string> xboxAxes = new List<string> { "Left Stick X", "Left Stick Y", "Right Stick X", "Right Stick Y", "D-pad X", "D-pad Y", "RT", "LT", "Triggers" };
        //List<string> xboxAxes = new List<string> { "MoveX", "MoveZ", "MoveY", "RotateY" };

        //void LogButtons()
        //{
        //    //Debug.Log("printing buttons");
        //    foreach (string name in xboxButtons)
        //    {
        //        //Debug.Log("checking " + name);
        //        if (Input.GetButtonDown(name))
        //            RLogger.Log("Button Down: " + name.ToString());
        //    }
        //}

        //void LogAxes()
        //{
        //    foreach (string name in xboxAxes)
        //    {
        //        if (Input.GetAxis(name) != 0)
        //            RLogger.Log("Axis: " + name + " is: " + Input.GetAxis(name).ToString());
        //    }
        //}

        private void Start()
        {
            _ARSession = GetComponent<ARSession>();
            if (_ARSession != null)
            {
                _ARSession.enabled = true;
            }
        }

        // Moves the player instead of the world. Player is in the same real position, thus, the virtual world is being moved!
        void Update()
        {
            if (done)
            {
                Destroy(this);
            }
            if (Input.GetButtonDown("BeginEndAlign"))
            {
                RLogger.Log("BeginEndAlign was pressed");
                active = !active;
                if (_ARSession != null) {
                    _ARSession.enabled = !_ARSession.enabled;
                }

            }
            if (Input.GetButtonDown("DoneAlign") && !done)
            {
                RLogger.Log("DoneAlign was pressed, world position is: " + room.transform.position.ToString() + " player posisiont is: "+ player.transform.position);
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

            }
            if (active)
            {
                movePlayer();
            }
            handleExtraButtons();
        }

        void movePlayer()
        {
            float keyboard_movex = Input.GetKey(KeyCode.A) ? 1f : Input.GetKey(KeyCode.D) ? -1f : 0f;
            float keyboard_movez = Input.GetKey(KeyCode.W) ? 1f : Input.GetKey(KeyCode.S) ? -1f : 0f;
            float keyboard_movey = Input.GetKey(KeyCode.UpArrow) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f;
            float keyboard_rotatey = Input.GetKey(KeyCode.RightArrow) ? 1f : Input.GetKey(KeyCode.LeftArrow) ? -1f : 0f;

            float movex = Input.GetAxis("MoveX") + keyboard_movex;
            float movey= Input.GetAxis("MoveY") + keyboard_movey;
            float movez = Input.GetAxis("MoveZ") + keyboard_movez;

            Vector3 player_forward = player_camera.transform.forward; player_forward.y = 0f; player_forward = Vector3.Normalize(player_forward);
            Vector3 player_right = player_camera.transform.right; player_right.y = 0f; player_right = Vector3.Normalize(player_right);
            
            Vector3 translate = - player_forward*movez + player_right*movex - Vector3.up*movey;

            player.transform.Translate(-1f* translate * move_speed, Space.World);


            // if we assume objects are renders straight up, which we did!, we won't need the room.transform.rotation multiplication. 
            player.transform.Rotate( room.transform.rotation * Vector3.up * -1f *(Input.GetAxis("RotateY") + keyboard_rotatey) * rotatoin_speed, Space.Self);
        }

        void handleExtraButtons()
        {
            foreach (ButtonHandler b in OptionalButtons)
            {
                if (Input.GetButtonDown(b.name))
                {
                    b.onPressed.Invoke();
                }
            }
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