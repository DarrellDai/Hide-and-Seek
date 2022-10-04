using UnityEngine;

namespace Editor
{
    [CreateAssetMenu(fileName = "FILENAME", menuName = "MENUNAME", order = 0)]
    public class PlayerSpawnerForEditor : ScriptableObject
    {
        public bool autoUpdate;

        //Obtain information from PlayerSpawner outside Edit folder
        private PlayerSpawner playerSpawnerScript;

        public void OnEnable()
        {
            playerSpawnerScript = FindObjectOfType<PlayerSpawner>();
            autoUpdate = playerSpawnerScript.autoUpdate;
            playerSpawnerScript.Initialize();
        }

        public void StartSpawning()
        {
            playerSpawnerScript.StartSpawning();
        }

        public void RespawnWhenFinished()
        {
            playerSpawnerScript.RespawnWhenFinished();
        }

        public void SpawnPlayer(int order)
        {
            playerSpawnerScript.SpawnPlayer(order);
        }

        public void FindRandPosition()
        {
            playerSpawnerScript.FindRandPosition();
        }

        public void CheckOverlap()
        {
            playerSpawnerScript.CheckOverlap();
        }

        public static int CountNumHider(GameObject gameObject)
        {
            return PlayerSpawner.CountNumHider(gameObject);
        }

        public static int CountActiveNumHider(GameObject gameObject)
        {
            return PlayerSpawner.CountActiveNumHider(gameObject);
        }

        public void DestoryChildren()
        {
            playerSpawnerScript.DestoryChildren();
        }

        public static void ResetCamera(Transform transform)
        {
            PlayerSpawner.ResetCamera(transform);
        }

        public void RelocatePlayer(Transform agent)
        {
            playerSpawnerScript.RelocatePlayer(agent);
        }
    }
}