using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoR2ML
{
    public class ModManager : MonoBehaviour
    {
        private Dictionary<string, List<Action<Scene>>> sceneCallbacks;

        public void AddSceneCallback(string sceneName, Action<Scene> callback)
        {
            if (!sceneCallbacks.ContainsKey(sceneName))
            {
                sceneCallbacks.Add(sceneName, new List<Action<Scene>>());
            }

            sceneCallbacks.TryGetValue(sceneName, out List<Action<Scene>> callbackList);
            callbackList.Add(callback);
        }
        
        private void Start()
        {
            Loader.Log("ModManager component loaded");
            DontDestroyOnLoad(gameObject);
            
            sceneCallbacks = new Dictionary<string, List<Action<Scene>>>();
            
            SceneManager.activeSceneChanged += OnSceneChanged;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene newScene, LoadSceneMode arg1)
        {
            Loader.Log($"Changing to {newScene.name} ({newScene.path})");

            HandleSceneLoadEvents(newScene);
        }

        private void OnSceneChanged(Scene currentScene, Scene nextScene)
        {
            Loader.Log($"Changing from {currentScene.name} ({currentScene.path}) to {nextScene.name} ({nextScene.path})");
        }
        private void HandleSceneLoadEvents(Scene newScene)
        {
            if (!sceneCallbacks.TryGetValue(newScene.name, out List<Action<Scene>> callbacks)) return;

            foreach (var callback in callbacks)
            {
                callback.Invoke(newScene);
            }
        }
    }
}