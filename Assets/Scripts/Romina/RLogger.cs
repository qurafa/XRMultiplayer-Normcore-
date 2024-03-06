using Microsoft.MixedReality.OpenXR.Remoting;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;


///< summary >
/// Logs statically on the console. Also logs on GUI or a manually set TMPro text.
/// </summary>
namespace com.perceptlab.armultiplayer
{
    public class RLogger: MonoBehaviour
    {
        private static RLogger instance;
        private static string GUIText = "";
        private static Queue<int> nextLineIdx = new Queue<int>();
        private static int lastIdx = 0;

        private static int maxLines = 5;
        [SerializeField]
        private bool showOnUnityConsole = true;
        [SerializeField]
        private bool showOnAppGui = true;
        [SerializeField]
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