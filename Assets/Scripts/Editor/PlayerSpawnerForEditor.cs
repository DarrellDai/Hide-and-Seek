using UnityEngine;

namespace Editor
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "MENUNAME", order = 0)]
    public class PlayerSpawnerForEditor : ScriptableObject
    {
        public bool autoUpdate;

        //Obtain information from PlayerSpawner outside Edit folder
        private PlayerSpawner playerSpawnerScript;

        public void Initialize()
        {
            playerSpawnerScript = FindObjectOfType<PlayerSpawner>();
            playerSpawnerScript.Initialize();
            autoUpdate = playerSpawnerScript.autoUpdate;
            
        }

        public void StartSpawning()
        {
            playerSpawnerScript.StartSpawning();
        }
        
        public void DestoryChildren()
        {
            playerSpawnerScript.DestoryChildren();
        }
        
    }
}