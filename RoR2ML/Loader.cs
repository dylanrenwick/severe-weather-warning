using System.ComponentModel;
using UnityEngine;
using RoR2;

namespace RoR2ML
{
    public class Loader
    {
        private const string ML_VER = "v0.1.0";
        private static Object modManager;
        
        public static void Init()
        {
            if (RoR2Application.isModded) return;
            RoR2Application.isModded = true;
            // This is our "entry point" where stuff starts
            
            // We need to create a GameObject running a custom MonoBehaviour
            // Then flag it as DontDestroyOnLoad so that it remains persistent
            
            Log("RoR2ML Installed");
            Log("Creating ModManager object");

            modManager = CreateManagerObject();
        }

        private static Object CreateManagerObject()
        {
            GameObject managerObject = new GameObject();
            managerObject.name = "RoR2ML Manager";
            managerObject.AddComponent<ModManager>();

            return managerObject;
        }

        public static void Log(string message)
        {
            Debug.Log($"[RoR2ML {ML_VER}] {message}");
        }
    }
}