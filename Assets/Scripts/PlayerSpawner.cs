using System;
using Unity.Mathematics;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class PlayerSpawner : MonoBehaviour
{
    //Auto update players when variables change
    public bool autoUpdate;

    [Tooltip("Player spawner as the parent of all players")]
    public GameObject playerSpawner;

    [HideInInspector] public int numHiders;
    [HideInInspector] public int numSeekers;
    

    [Tooltip("If for human play")] public bool humanPlay;

    public bool singlePlayer;
    [HideInInspector] public TerrainAndRockSetting terrainAndRockSetting;
    public int seed;
    [HideInInspector] public float itemSpread;

    [Tooltip("The distance away from bound as the range for spawning")]
    public float distanceFromBound = 3;

    [HideInInspector] public Vector3 randPosition;

    [FormerlySerializedAs("overlapTestBoxSize")] [Tooltip("The box size the destination needs to be away from rocks and other players")]
    public float radius = 2;

    [HideInInspector] public LayerMask hiderLayer;
    [HideInInspector] public LayerMask seekerLayer;
    [HideInInspector] public LayerMask rockLayer;
    [HideInInspector] public LayerMask terrainLayer;
    [HideInInspector] public bool overlap;

    [Tooltip("Player spawner as the parent of all destinations")]
    public GameObject destinationSpawner;

    [Tooltip("Player spawner as the parent of all field of views mesh")]
    public GameObject fieldOfViewSpawner;

    [Tooltip("Players to spawn")] public Player[] players;
    
    [Tooltip("Number of steps to freeze seekers to give hiders Preparation time")] public int numStepToFreeze;
    
    [Tooltip("Player's camera is active if true")]
    public bool hasCameras;

    [HideInInspector] public Camera[] cameras;

    [Tooltip("field of view of player's camera")]
    public int fieldOfView;

    private RaycastHit hit;
    /// <summary>
    ///     Initialized variables.
    /// </summary>
    public void Initialize()
    {
        hiderLayer = LayerMask.NameToLayer("Hider");
        seekerLayer = LayerMask.NameToLayer("Seeker");
        rockLayer = LayerMask.NameToLayer("Rock");
        terrainLayer = LayerMask.NameToLayer("Terrain");
        terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        var mapSize = terrainAndRockSetting.CalculateMapSize();
        itemSpread = mapSize / 2 - distanceFromBound;
        Random.InitState(seed);
        cameras = new Camera[players.Length];
        playerSpawner.transform.position=Vector3.zero;
        playerSpawner.transform.rotation=quaternion.identity;
        playerSpawner.transform.localScale=Vector3.one;
        CountNumHidersAndSeekers();

    }

    public void CountNumHidersAndSeekers()
    {
        numHiders = 0;
        numSeekers = 0;
        for (var i = 0; i < players.Length; i++)
        {
            if (players[i].playerToSpawn.CompareTag("Seeker"))
                numSeekers++;
            else if (players[i].playerToSpawn.CompareTag("Hider"))
                numHiders++;
        }
    }
    /// <summary>
    ///     Start Spawn hiders and seeker.
    /// </summary>
    public void StartSpawning()
    {
        for (var i = 0; i < players.Length; i++)
        {
            SpawnPlayer(i);
        }
    }

    public void RespawnWhenFinished()
    {
        //Respawn all players if all hiders are caught
        if (CountActiveNumHider(playerSpawner) == 0)
            for (var i = 0; i < playerSpawner.transform.childCount; i++)
                if (playerSpawner.transform.GetChild(i).tag == "Hider")
                    ResetCamera(playerSpawner.transform.GetChild(i));
    }

    /// <summary>
    ///     Spawn players which are not too close to each other.
    /// </summary>
    /// <param name="gameObject"></param>
    public void SpawnPlayer(int order)
    {
        FindRandPosition();
        GameObject clone = Instantiate(players[order].playerToSpawn, randPosition,
            quaternion.identity);
        //Don't use InverseTransformPoint, it'll use the future transform.position to infer current collider's local position
        clone.transform.position = 2 * clone.transform.position - clone.GetComponent<Collider>().bounds.center;
        clone.transform.parent = playerSpawner.transform;
        var gameAgent = clone.GetComponent<GameAgent>();
        if (hasCameras)
        {
            //Set camera as field of view
            gameAgent.transform.Find("Eye").Find("Camera").gameObject.SetActive(true);
            cameras[order] = gameAgent.transform.Find("Eye").Find("Camera").GetComponent<Camera>();
            cameras[order].fieldOfView = fieldOfView;

            if (humanPlay)
            {
                if (singlePlayer && players[order].playerToSpawn.GetComponent<BehaviorParameters>().BehaviorType !=
                    BehaviorType.HeuristicOnly)
                    cameras[order].rect = new Rect(0, 0, 1, 1);
                else if (!singlePlayer)
                    cameras[order].rect = new Rect((float)order / players.Length,
                        0, 1f / players.Length, 1);
            }
            else
            {
                float halfNumOfWindows = players.Length > 1 ? Mathf.RoundToInt(players.Length / 2f) : 1;
                cameras[order].rect = new Rect(
                    0.3f + (1 - 0.3f) / halfNumOfWindows * Mathf.Floor(order % halfNumOfWindows),
                    0.5f * (1 - Mathf.Floor(order / halfNumOfWindows)), (1 - 0.3f) / halfNumOfWindows,
                    0.5f);
            }
        }

        gameAgent.trainingMode = players[order].trainingMode;
        var placeObjectsToSurface = clone.GetComponent<PlaceObjectsToSurface>();
        placeObjectsToSurface.StartPlacing(Vector3.zero, false,false);
        Physics.SyncTransforms();
    }

    public void FindRandPosition()
    {
        overlap = true;
        while (overlap)
        {
            // Don't need to add TerrainSpawner's position, because when an object is
            // added to another one as child, the current position will become local position
            randPosition = new Vector3(Random.Range(-itemSpread, itemSpread), 100,
                Random.Range(-itemSpread, itemSpread));
            CheckOverlap();
        }
    }

    /// <summary>
    ///     Check if the spawned player is too close to rocks or other players.
    /// </summary>
    public void CheckOverlap()
    {

        if (!Physics.SphereCast(randPosition, radius,Vector3.down, out hit, 1000, 1 << seekerLayer | 1 << hiderLayer | 1 << rockLayer))
        {
            overlap = false;
            //var spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            //var overlapTestBoxScale = new Vector3(overlapTestBoxSize, overlapTestBoxSize*2, overlapTestBoxSize);
            /*var collidersInsideOverlapBox = new Collider[1];
            var numberOfCollidersFound =
                Physics.OverlapSphereNonAlloc(hit.point+0.55f*hit.normal, overlapTestBoxSize, collidersInsideOverlapBox,
                    1 << seekerLayer | 1 << hiderLayer | 1 << rockLayer);
            Debug.Log(numberOfCollidersFound);
            if (numberOfCollidersFound == 0) overlap = false;*/
        }
    }

    /// <summary>
    ///     Count the number of hiders left
    /// </summary>
    /// <returns></returns>
    public static int CountNumHider(GameObject gameObject)
    {
        if (gameObject.transform.childCount == 0)
            return 0;
        var numHider = 0;
        for (var i = 0; i < gameObject.transform.childCount; i++)
            if (gameObject.transform.GetChild(i).tag == "Hider")
                numHider++;

        return numHider;
    }

    /// <summary>
    ///     Count the number of hiders left
    /// </summary>
    /// <returns></returns>
    public static int CountActiveNumHider(GameObject gameObject)
    {
        if (gameObject.transform.childCount == 0)
            return 0;
        var numHider = 0;
        for (var i = 0; i < gameObject.transform.childCount; i++)
            if (gameObject.transform.GetChild(i).tag == "Hider")
                /*if (gameObject.transform.GetChild(i).GetChild(0).gameObject.activeSelf &&
                    gameObject.transform.GetChild(i).GetChild(1).gameObject.activeSelf)
                {
                    numHider++;
                }*/
                if (gameObject.transform.GetChild(i).gameObject.GetComponent<GameAgent>().alive)
                    numHider++;
        /*if (gameObject.transform.GetChild(i).gameObject.activeSelf)
                {
                    numHider++;
                }*/
        return numHider;
    }

    /// <summary>
    ///     Destroy all players before respawning ew players.
    /// </summary>
    /// <param name="transform"></param>
    public void DestoryChildren()
    {
        if (playerSpawner.transform.childCount > 0)
        {
            var tempArray = new GameObject[playerSpawner.transform.childCount];

            for (var i = 0; i < tempArray.Length; i++) tempArray[i] = playerSpawner.transform.GetChild(i).gameObject;

            foreach (var child in tempArray) DestroyImmediate(child);
        }
    }

    /// <summary>
    ///     Reset the camera to normal from blackout
    /// </summary>
    /// <param name="transform"></param>
    public static void ResetCamera(Transform transform)
    {
        //Turn its camera to black when a hider is caught 
        var camera = transform.Find("Eye").Find("Camera").GetComponent<Camera>();
        var position = camera.transform.position;
        var rotation = camera.transform.rotation;
        var rect = camera.rect;
        camera.Reset();
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        camera.rect = rect;
    }

    /// <summary>
    ///     Place the player to a random destinationPosition
    /// </summary>
    public void RelocatePlayer(Transform agent)
    {
        agent.rotation = quaternion.identity;
        FindRandPosition();
        Physics.SyncTransforms();
        agent.position = randPosition + agent.position - agent.GetComponent<Collider>().bounds.center;
        Physics.SyncTransforms();
        agent.GetComponent<PlaceObjectsToSurface>().StartPlacing(Vector3.zero,false, false);
        Physics.SyncTransforms();
    }
    /*private void OnDrawGizmos() 
    {
        
        for (var i = 0; i < players.Length; i++)
        {
            Transform go=playerSpawner.transform.GetChild(i);
            Gizmos.color = Color.green;
            Vector3 sphereCastMidpoint = hit.point + radius * hit.normal;
            Gizmos.DrawWireSphere(sphereCastMidpoint, radius);
            Gizmos.DrawSphere(hit.point, 0.1f);
            Debug.DrawLine(randPosition, sphereCastMidpoint, Color.white,15);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(go.transform.position, radius);
        }

    }*/

    [Serializable]
    public struct Player
    {
        public GameObject playerToSpawn;
        public bool trainingMode;
    }
}