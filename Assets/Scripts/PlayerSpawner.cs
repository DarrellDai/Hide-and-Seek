using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class PlayerSpawner : MonoBehaviour
{
    [Tooltip("Player spawner as the parent of all players")]
    public GameObject playerSpawner;

    private TerrainAndRockSetting terrainAndRockSetting;
    private int seed;
    private float itemSpread;

    [Tooltip("The distance away from bound as the range for spawning")]
    public static float distanceFromBound = 10;

    [HideInInspector] public Vector3 randPosition;

    [Tooltip("The box size the destination needs to be away from rocks and other players")]
    public float overlapTestBoxSize = 2;

    private LayerMask hiderLayer;
    private LayerMask seekerLayer;
    private LayerMask rockLayer;
    private LayerMask terrainLayer;
    private bool overlap;

    [Tooltip("Player spawner as the parent of all destinations")]
    public GameObject destinationSpawner;

    [Tooltip("Player spawner as the parent of all field of views mesh")]
    public GameObject fieldOfViewSpawner;

    [Tooltip("Players to spawn")] public Player[] players;

    [Tooltip("Player's camera is active if true")]
    public bool hasCameras;

    private Camera[] cameras;

    [Tooltip("field of view of player's camera")]
    public int fieldOfView;

    /// <summary>
    /// Initialized variables.
    /// </summary>
    private void Awake()
    {
        hiderLayer = LayerMask.NameToLayer("Hider");
        seekerLayer = LayerMask.NameToLayer("Seeker");
        rockLayer = LayerMask.NameToLayer("Rock");
        terrainLayer = LayerMask.NameToLayer("Terrain");
        terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        float mapSize = terrainAndRockSetting.CalculateMapSize();
        itemSpread = mapSize / 2 - distanceFromBound;
        seed = terrainAndRockSetting.seed;
        Random.InitState(seed);
        cameras = new Camera[players.Length];
    }

    /// <summary>
    /// Start Spawn hiders and seeker.
    /// </summary>
    void Start()
    {
        for (int i = 0; i < players.Length; i++)
        {
            SpawnPlayer(i);
        }
    }

    private void Update()
    {
        //Respawn all players if all hiders are caught
        if (CountActiveNumHider(playerSpawner) == 0)
        {
            for (int i = 0; i < playerSpawner.transform.childCount; i++)
            {
                if (playerSpawner.transform.GetChild(i).tag == "Hider")
                {
                    ResetCamera(playerSpawner.transform.GetChild(i));
                }
            }
        }
    }
    /// <summary>
    /// Spawn players which are not too close to each other.
    /// </summary>
    /// <param name="gameObject"></param>
    void SpawnPlayer(int order)
    {
        FindRandPosition();
        GameObject clone = Instantiate(players[order].playerToSpawn, randPosition,
            players[order].playerToSpawn.transform.rotation);
        clone.transform.parent = playerSpawner.transform;
        GameAgent gameAgent = clone.GetComponent<GameAgent>();
        gameAgent.orderOfPlayer = order;
        //Set up players' camera if hasCameras=true
        if (hasCameras)
        {
            //Set camera as field of view
            gameAgent.transform.Find("Eye").Find("Camera").gameObject.SetActive(true);
            cameras[order] = gameAgent.transform.Find("Eye").Find("Camera").GetComponent<Camera>();
            cameras[order].fieldOfView = fieldOfView;
            float halfNumOfWindows = players.Length > 1 ? Mathf.RoundToInt(players.Length / 2f) : 1;
            cameras[order].rect = new Rect(0.3f + (1 - 0.3f) / halfNumOfWindows * Mathf.Floor(order % halfNumOfWindows),
                0.5f * (1 - Mathf.Floor(order / halfNumOfWindows)), (1 - 0.3f) / halfNumOfWindows,
                0.5f);
        }

        gameAgent.trainingMode = players[order].trainingMode;
        PlaceObjectsToSurface placeObjectsToSurface = clone.GetComponent<PlaceObjectsToSurface>();
        placeObjectsToSurface.StartPlacing();
    }

    public void FindRandPosition()
    {
        overlap = true;
        while (overlap)
        {
            randPosition = new Vector3(Random.Range(-itemSpread, itemSpread), 100,
                Random.Range(-itemSpread, itemSpread)) + terrainAndRockSetting.terrainSpawner.transform.position;
            CheckOverlap();
        }
    }

    /// <summary>
    /// Check if the spawned player is too close to rocks or other players.
    /// </summary>
    void CheckOverlap()
    {
        RaycastHit hit;

        if (Physics.Raycast(randPosition, Vector3.down, out hit, 1000, 1 << terrainLayer))
        {
            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            Vector3 overlapTestBoxScale = new Vector3(overlapTestBoxSize, overlapTestBoxSize, overlapTestBoxSize);
            Collider[] collidersInsideOverlapBox = new Collider[1];
            int numberOfCollidersFound =
                Physics.OverlapBoxNonAlloc(hit.point, overlapTestBoxScale, collidersInsideOverlapBox, spawnRotation,
                    1 << seekerLayer | 1 << hiderLayer) + Physics.OverlapBoxNonAlloc(hit.point, overlapTestBoxScale,
                    collidersInsideOverlapBox,
                    spawnRotation, 1 << rockLayer);
            if (numberOfCollidersFound == 0)
            {
                overlap = false;
            }
        }
    }

    /// <summary>
    /// Count the number of hiders left
    /// </summary>
    /// <returns></returns>
    public static int CountNumHider(GameObject gameObject)
    {
        if (gameObject.transform.childCount == 0)
            return 0;
        int numHider = 0;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).tag == "Hider")
                numHider++;
        }

        return numHider;
    }

    /// <summary>
    /// Count the number of hiders left
    /// </summary>
    /// <returns></returns>
    public static int CountActiveNumHider(GameObject gameObject)
    {
        if (gameObject.transform.childCount == 0)
            return 0;
        int numHider = 0;
        for (int i = 0; i < gameObject.transform.childCount; i++)
        {
            if (gameObject.transform.GetChild(i).tag == "Hider")
            {
                /*if (gameObject.transform.GetChild(i).GetChild(0).gameObject.activeSelf &&
                    gameObject.transform.GetChild(i).GetChild(1).gameObject.activeSelf)
                {
                    numHider++;
                }*/
                if (gameObject.transform.GetChild(i).gameObject.GetComponent<GameAgent>().alive)
                {
                    numHider++;
                }
                /*if (gameObject.transform.GetChild(i).gameObject.activeSelf)
                {
                    numHider++;
                }*/
            }
        }

        return numHider;
    }

    /// <summary>
    /// Destroy all terrain chunk before generate new terrain.
    /// </summary>
    /// <param name="transform"></param>
    public static void DestoryChildren(Transform transform)
    {
        if (transform.childCount > 0)
        {
            var tempArray = new GameObject[transform.childCount];

            for (int i = 0; i < tempArray.Length; i++)
            {
                tempArray[i] = transform.GetChild(i).gameObject;
            }

            foreach (var child in tempArray)
            {
                DestroyImmediate(child);
            }
        }
    }
    /// <summary>
    /// Reset the camera to normal from blackout
    /// </summary>
    /// <param name="transform"></param>
    public static void ResetCamera(Transform transform)
    {
        //Turn its camera to black when a hider is caught 
        Camera camera = transform.Find("Eye").Find("Camera").GetComponent<Camera>();
        Vector3 position = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        Rect rect = camera.rect;
        camera.Reset();
        camera.transform.position = position;
        camera.transform.rotation = rotation;
        camera.rect = rect;
    }

    [System.Serializable]
    public struct Player
    {
        public GameObject playerToSpawn;
        public bool trainingMode;
    }
}