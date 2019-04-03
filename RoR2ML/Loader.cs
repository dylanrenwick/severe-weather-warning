using System.ComponentModel;
using UnityEngine;

namespace RoR2ML
{
    public class Loader
    {
        private const string ML_VER = "v0.1.0";
        
        private static bool isLoaded = false;
        
        public static void Init()
        {
            if (isLoaded) return;
            // This is our "entry point" where stuff starts
            
            // We need to create a GameObject running a custom MonoBehaviour
            // Then flag it as DontDestroyOnLoad so that it remains persistent
            
            Log("RoR2ML Installed");
            isLoaded = true;
        }

        public static void Log(string message)
        {
            Debug.Log($"[RoR2ML {ML_VER}] {message}");
        }
    }
}