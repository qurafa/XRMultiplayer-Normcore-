using Microsoft.MixedReality.OpenXR.Remoting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;

namespace com.perceptlab.armultiplayer
{
    public class RLogger: MonoBehaviour
    {
        private static RLogger instance;
        private static string GUIText = "";
        private static Queue<int> nextLineIdx = new Queue<int>();
        private static int lastIdx = 0;

        private static int maxLines = 5;
        private static bool showOnUnityConsole = true;
        private static bool showOnAppGui = true; 

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
            if (showOnUnityConsole)
            {
                Debug.Log(message);
            }
            GUIText += "\n" + message;
            nextLineIdx.Enqueue(GUIText.Length-lastIdx);
            lastIdx = GUIText.Length;
            if (maxLines == -1)
                    return;
            if (nextLineIdx.Count>maxLines)
            {
                int cut = nextLineIdx.Dequeue();
                GUIText = GUIText.Substring(cut);
                lastIdx -= cut;
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