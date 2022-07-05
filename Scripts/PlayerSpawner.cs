using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public GameObject playerSpawner;
    public GameObject hider, seeker;
    public int numberOfHider, numberOfSeeker;
    private int seed;
    private float itemSpread;
    public static float distanceFromBound=10;
    private Vector3 randPosition;
    public float overlapTestBoxSize = 2;
    private LayerMask hiderLayer;
    private LayerMask seekerLayer;
    private LayerMask rockLayer;
    private bool overlap;
    public GameObject destinationSpawner;
    public GameObject fieldOfViewSpawner;
    /// <summary>
    /// Initialized variables.
    /// </summary>
    private void Awake()
    {
        hiderLayer = LayerMask.NameToLayer("Hider");
        seekerLayer = LayerMask.NameToLayer("Seeker");
        rockLayer = LayerMask.NameToLayer("Rock");
        TerrainAndRockSetting terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        float mapSize = terrainAndRockSetting.CalculateMapSize();
        itemSpread = mapSize / 2-distanceFromBound;
        seed = terrainAndRockSetting.seed;
        Random.InitState(seed);
    }

    /// <summary>
    /// Start Spawn hiders and seeker.
    /// </summary>
    void Start()
    {
        
        for (int i = 0; i < numberOfHider; i++)
        {
            SpawnPlayer(hider);
        }
        for (int i = 0; i < numberOfSeeker; i++)
        {
            SpawnPlayer(seeker);
            

        }
    }
    /// <summary>
    /// Spawn players which are not too close to each other.
    /// </summary>
    /// <param name="gameObject"></param>
    void SpawnPlayer(GameObject gameObject)
    {
        overlap = true;
        while (overlap)
        {
            randPosition = new Vector3(Random.Range(-itemSpread, itemSpread), 30,
                Random.Range(-itemSpread, itemSpread))+transform.position;
            CheckOverlap();
        }
        GameObject clone = Instantiate(gameObject, randPosition, gameObject.transform.rotation);
        clone.transform.parent = playerSpawner.transform;
        PlaceObjectsToSurface placeObjectsToSurface = clone.GetComponent<PlaceObjectsToSurface>();
        placeObjectsToSurface.StartPlacing();

    }
    
    /// <summary>
    /// Check if the spawned player is too close to rocks or other players.
    /// </summary>
    void CheckOverlap()
    {
        RaycastHit hit;

        if (Physics.Raycast(randPosition, Vector3.down, out hit))
        {

            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            Vector3 overlapTestBoxScale = new Vector3(overlapTestBoxSize, overlapTestBoxSize, overlapTestBoxSize);
            Collider[] collidersInsideOverlapBox = new Collider[1];
            int numberOfCollidersFound =
                Physics.OverlapBoxNonAlloc(hit.point, overlapTestBoxScale, collidersInsideOverlapBox, spawnRotation,
                    1<<seekerLayer|1<<hiderLayer) + Physics.OverlapBoxNonAlloc(hit.point, overlapTestBoxScale, collidersInsideOverlapBox,
                    spawnRotation, 1<<rockLayer);
            if (numberOfCollidersFound == 0)
            {
                overlap=false;
            }
        }
    }
}
