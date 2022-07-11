using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemAreaSpawner : MonoBehaviour
{
    //The item to spawn
    GameObject itemToSpawn;

    public TerrainAndRockSettingForEditor terrainAndRockSettingForEditor;
    //Number of items to spawn
    int numItemsToSpawn;
    //Range of spread on each axis
    float itemXSpread;
    float itemYSpread;
    float itemZSpread;
    //Range of rotation
    Vector3 randomRotationRange;
    //Multiplier for scale
    float globalScaleMultiplier;
    
    //Range of scale on each axis
    float xScaleMin;
    float xScaleMax;
    float yScaleMin;
    float yScaleMax;
    float zScaleMin;
    float zScaleMax;

    //Spawner as parent for spawned gameobjects
    private GameObject spawner;


    public void StartSpawn()
    {
        spawner = GameObject.Find("RockSpawner");
        if (spawner.transform.childCount > 0)
        {
            TerrainAndRockSettingForEditor.DestoryChildren(spawner.transform);
        }

        //Get value from TerrainAndRockSettingForEditor
        itemToSpawn = terrainAndRockSettingForEditor.itemToSpawn;
        numItemsToSpawn = terrainAndRockSettingForEditor.numberOfItemsToSpawn;
        itemYSpread = terrainAndRockSettingForEditor.itemYSpread;
        randomRotationRange=terrainAndRockSettingForEditor.randomRotationRange;
        globalScaleMultiplier = terrainAndRockSettingForEditor.globalScaleMultiplier;
        xScaleMin = terrainAndRockSettingForEditor.xScaleMin;
        xScaleMax = terrainAndRockSettingForEditor.xScaleMax;
        yScaleMin = terrainAndRockSettingForEditor.yScaleMin;
        yScaleMax = terrainAndRockSettingForEditor.yScaleMax;
        zScaleMin = terrainAndRockSettingForEditor.zScaleMin;
        zScaleMax = terrainAndRockSettingForEditor.zScaleMax;
        int seed = terrainAndRockSettingForEditor.seed;
        float length = terrainAndRockSettingForEditor.CalculateMapSize();
        
        //Initialize
        itemXSpread = length / 2;
        itemZSpread = length / 2;
        Random.InitState(seed);
        spawner.transform.position = Vector3.zero;
        spawner.transform.localScale=Vector3.one;
        spawner.transform.rotation=quaternion.identity;
        
        
        for (int i = 0; i < numItemsToSpawn; i++)
        {
            SpawnItem();
            
        }
    }
    /// <summary>
    /// Spawn items.
    /// </summary>
    void SpawnItem()
    {
        Vector3 randPosition = new Vector3(Random.Range(-itemXSpread, itemXSpread), Random.Range(3, itemYSpread),
            Random.Range(-itemZSpread, itemZSpread));
        GameObject clone = Instantiate(itemToSpawn, randPosition, itemToSpawn.transform.rotation);
        clone.transform.parent = spawner.transform;
        clone.tag = "Rock";
        var flags = StaticEditorFlags.NavigationStatic;
        GameObjectUtility.SetStaticEditorFlags(clone, flags);
        GameObjectUtility.SetNavMeshArea(clone, GameObjectUtility.GetNavMeshAreaFromName("Not Walkable"));
        RandomizeByAxis(randomRotationRange, clone);
        RandomizeObjectScale(clone);
        PlaceToSurface(clone);
    }
    /// <summary>
    /// Place items to surface in case they are on air.
    /// </summary>
    /// <param name="gameObject"></param>
    void PlaceToSurface(GameObject gameObject)
    {
        //Set the clone object to Ignore Raycast layer so that it will not affect the raycast
        int layerIgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
        int originalLayer=0;
        gameObject.layer = layerIgnoreRaycast;
        Ray ray = new Ray(gameObject.transform.position, Vector3.down);
        RaycastHit hitInfoTerrainAndRock;
        //RaycastHit hitInfoRock;
        if (Physics.Raycast(ray, out hitInfoTerrainAndRock, 1000,
                1 << LayerMask.NameToLayer("Terrain") | 1 << LayerMask.NameToLayer("Rock")))
        {
            gameObject.transform.position = hitInfoTerrainAndRock.point;
            
            /*originalLayer = hitInfoTerrainAndRock.transform.gameObject.layer;
            hitInfoTerrainAndRock.transform.gameObject.layer = layerIgnoreRaycast;
            gameObject.layer = LayerMask.NameToLayer("Rock");
            Ray rayRock = new Ray(hitInfoTerrainAndRock.point, Vector3.up);
            if (Physics.Raycast(rayRock, out hitInfoRock, 100, 1 << LayerMask.NameToLayer("Rock"))) 
            {
                Debug.DrawLine(hitInfoTerrainAndRock.point, hitInfoRock.point, Color.red, 10f,false);
                float offset = Vector3.Distance(gameObject.transform.destinationPosition, hitInfoRock.point);
                gameObject.transform.destinationPosition = hitInfoTerrainAndRock.point+offset*Vector3.up;
                
            }*/
            
        }
        
        //hitInfoTerrainAndRock.transform.gameObject.layer = originalLayer;
        
        //Set the layer back
        gameObject.layer = LayerMask.NameToLayer("Rock");
    }

    /// <summary>
    /// Randomize rotation by axis.
    /// </summary>
    /// <param name="randomRotationConstraints"></param>
    /// <param name="gameObject"></param>
    public void RandomizeByAxis(Vector3 randomRotationConstraints, GameObject gameObject)
    {
        Quaternion randomConstrainedRotation = Quaternion.Euler(
            gameObject.transform.rotation.eulerAngles.x +
            Random.Range(-randomRotationConstraints.x, randomRotationConstraints.x),
            gameObject.transform.rotation.eulerAngles.y +
            Random.Range(-randomRotationConstraints.y, randomRotationConstraints.y),
            gameObject.transform.rotation.eulerAngles.z +
            Random.Range(-randomRotationConstraints.z, randomRotationConstraints.z));

        gameObject.transform.rotation = randomConstrainedRotation;
    }

    /// <summary>
    /// Randomize scale.
    /// </summary>
    /// <param name="gameObject"></param>
    void RandomizeObjectScale(GameObject gameObject)
    {
        Vector3 randomizedScale = Vector3.one;
        randomizedScale = new Vector3(Random.Range(xScaleMin, xScaleMax), Random.Range(yScaleMin, yScaleMax),
            Random.Range(zScaleMin, zScaleMax));
        gameObject.transform.localScale = randomizedScale * globalScaleMultiplier;
    }
}