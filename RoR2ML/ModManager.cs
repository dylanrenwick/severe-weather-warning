using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoR2ML
{
    public class ModManager : MonoBehaviour
    {
        private void Start()
        {
            Loader.Log("ModManager component loaded");
            DontDestroyOnLoad(gameObject);
            
            
            
            SceneManager.activeSceneChanged += OnSceneChanged;
        }

        private void OnSceneChanged(Scene currentScene, Scene nextScene)
        {
            Loader.Log($"Changing from {currentScene.name} ({currentScene.path}) to {nextScene.name} ({nextScene.path})");
        }
    }
}