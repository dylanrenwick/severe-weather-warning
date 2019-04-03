using System.ComponentModel;
using UnityEngine;

namespace RoR2ML
{
    public class Loader
    {
        public static void Init()
        {
            // This is our "entry point" where stuff starts
            
            // We need to create a GameObject running a custom MonoBehaviour
            // Then flag it as DontDestroyOnLoad so that it remains persistent
            
            Debug.Log("RoR2ML Installed");
        }
    }
}