using Microsoft.MixedReality.OpenXR.Remoting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;


///< summary >
/// Simple class for seeing Logs in Builds. Will always call Debug.Log(), therefore always pritns logs ot Unity console 
/// If added to a GameObject, will show logs on screen.
/// </summary>
namespace com.perceptlab.armultiplayer
{
    public class RLogger: MonoBehaviour
    {
        private static RLogger instance;
        private static string GUIText = "";
        private static Queue<int> nextLineIdx = new Queue<int>();
        private static int lastIdx = 0;

        [SerializeField]
        private int maxLines = 5;
        [SerializeField]
        private bool showOnUnityConsole = true;
        [SerializeField, Tooltip("If true, the last 'maxLines' logs will be shown on screen on OnGUI callbacks (no textMesh needed)")]
        private bool showOnAppGui = true;
        [SerializeField, Tooltip("If set, the text will be set to the last 'maxLines' logs.")]
        private TMPro.TextMeshProUGUI custumeTextHolder;

        private void setInstance()
        {
            if (instance != this)
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
                instance = this;
            }
        }

        void Awake()
        {
            setInstance();
        }

        public static void Log(string message)
        {
            if (instance == null || instance?.showOnUnityConsole == true)
            {
                Debug.Log(message);
            }

            //adding message (implemented this way so there would be no need to concat strings)
            GUIText += "\n" + message;
            nextLineIdx.Enqueue(GUIText.Length-lastIdx);
            lastIdx = GUIText.Length;

            if (instance != null)
            {
                while (nextLineIdx.Count > instance.maxLines)
                {
                    //removing message
                    int cut = nextLineIdx.Dequeue();
                    GUIText = GUIText.Substring(cut);
                    lastIdx -= cut;
                }
            }
        }

        private void Update()
        {
            if (custumeTextHolder != null)
            {
                custumeTextHolder.text = GUIText;
            }
        }

        private void OnGUI()
        {
            if (showOnAppGui)
            {
                GUI.Label(new Rect(Screen.width-410, Screen.height-120, 400, 110), GUIText);
            }
        }

    }
}