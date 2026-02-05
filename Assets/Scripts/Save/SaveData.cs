using System;
using System.Collections.Generic;
using UnityEngine;


namespace Save
{
    [Serializable]
    public class SaveData
    {
        public float GameTime;
        public string CurrentScene;
        public Vector3 PlayerPosition;
        public List<InteractableObjectState> InteractableStates = new List<InteractableObjectState>();

        private static readonly HashSet<string> InvalidScenes = new() { "Credits", "MainMenu" };

        public bool IsSceneValidForSaving()
        {
            return !string.IsNullOrEmpty(CurrentScene) && !InvalidScenes.Contains(CurrentScene);
        }
    }
}