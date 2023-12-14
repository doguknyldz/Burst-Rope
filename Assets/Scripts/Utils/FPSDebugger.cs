using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BurstRope.Utils
{
    public class FPSDebugger : MonoBehaviour
    {
        public FPSLimit FPSLimit;
        public bool ShowFPS;

        Dictionary<int, string> cachedNumberStrings = new Dictionary<int, string>();
        int[] frameRateSamples;
        int cacheNumbersAmount = 300;
        int averageFromAmount = 30;
        int averageCounter;
        int currentAveraged;
        string fpsCount;

        private void Start()
        {
            SetFPS(FPSLimit);

            for (int i = 0; i < cacheNumbersAmount; i++)
            {
                cachedNumberStrings[i] = i.ToString();
            }
            frameRateSamples = new int[averageFromAmount];
        }

        private void Update()
        {
            if (!ShowFPS) return;
            var currentFrame = (int)Mathf.Round(1f / Time.smoothDeltaTime);
            frameRateSamples[averageCounter] = currentFrame;
            var average = 0f;
            foreach (var frameRate in frameRateSamples)
                average += frameRate;
            currentAveraged = (int)Mathf.Round(average / averageFromAmount);
            averageCounter = (averageCounter + 1) % averageFromAmount;
            fpsCount = currentAveraged switch
            {
                var x when x >= 0 && x < cacheNumbersAmount => cachedNumberStrings[x],
                var x when x >= cacheNumbersAmount => $"> {cacheNumbersAmount}",
                var x when x < 0 => "< 0",
                _ => "?"
            };
        }

        private void OnValidate()
        {
            SetFPS(FPSLimit);
        }

        public void SetFPS(FPSLimit limit)
        {
            FPSLimit = limit;
            switch (FPSLimit)
            {
                case FPSLimit.FPS0:
                    Application.targetFrameRate = -1;
                    break;
                case FPSLimit.FPS15:
                    Application.targetFrameRate = 15;
                    break;
                case FPSLimit.FPS30:
                    Application.targetFrameRate = 30;
                    break;
                case FPSLimit.FPS60:
                    Application.targetFrameRate = 60;
                    break;
                case FPSLimit.FPS144:
                    Application.targetFrameRate = 144;
                    break;
            }
        }

        private void OnGUI()
        {
            if (!ShowFPS) return;
            float mult = Screen.height / 200;
            GUIStyle style = GUI.skin.textArea;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = (int)(5 * mult);
            GUI.Label(new Rect(10, 10, 12 * mult, 8 * mult), fpsCount, style);
        }
    }

    public enum FPSLimit
    {
        FPS0,
        FPS15,
        FPS30,
        FPS60,
        FPS144
    }
}
