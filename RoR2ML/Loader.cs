using System.ComponentModel;
using UnityEngine;

namespace RoR2ML
{
    public class Loader
    {
        private const string ML_VER = "v0.1.0";
        
        private static bool isLoaded = false;
        private static Object modManager;
        
        public static void Init()
        {
            if (isLoaded) return;
            // This is our "entry point" where stuff starts
            
            // We need to create a GameObject running a custom MonoBehaviour
            // Then flag it as DontDestroyOnLoad so that it remains persistent
            
            Log("RoR2ML Installed");
            Log("Creating ModManager object");

            modManager = CreateManagerObject();
            isLoaded = true;
        }

        private static Object CreateManagerObject()
        {
            GameObject managerObject = new GameObject();

            managerObject.AddComponent(typeof(ModManager));

            return managerObject;
        }

        public static void Log(string message)
        {
            Debug.Log($"[RoR2ML {ML_VER}] {message}");
        }
    }
}