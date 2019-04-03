using UnityEngine;

namespace RoR2ML.UI
{
    public class MainMenuExtension : MonoBehaviour
    {
        private void OnGUI()
        {
            if (GUI.Button(new Rect(20, 70, 80, 20), "Level 2"))
            {
                Loader.Log("Clicky Button");
            }
        }
    }
}