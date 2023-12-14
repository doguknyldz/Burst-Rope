using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BurstRope.Utils
{
    public class ScreenDebugger : MonoBehaviour
    {
        public static ScreenDebugger Instance;
        void Awake() => Instance = this;

        public bool ShowScreenDebugs;

        Dictionary<string, string> _debugs = new Dictionary<string, string>();

        public static void Log(string index, string text)
        {
            if (Instance._debugs.ContainsKey(index))
                Instance._debugs[index] = text;
            else
                Instance._debugs.Add(index, text);
        }

        private void OnGUI()
        {
            if (!ShowScreenDebugs) return;

            float mult = Screen.height / 200;
            GUIStyle style = GUI.skin.textArea;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = (int)(5 * mult);
            int i = 0;
            foreach (var item in _debugs)
            {
                float size = Mathf.Min((item.Key.Length + item.Value.Length) * 10 * mult, 540);
                GUI.Label(new Rect(Screen.width - 10 - size, 10 + 8 * mult * i, size, 8 * mult), item.Key + ": " + item.Value, style);
                i++;
            }
        }
    }

}
