using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class TerrainAndRockSettingForEditor : ScriptableObject
{
    //Terrain setting
    //Spawner object for terrain
    public GameObject terrainSpawner;

    //Number of vertices on edges for each mesh
    public int meshNumVertices;

    //Size of the complete map, however, it's not the exact size
    public int mapSize;
    [HideInInspector] public int detailLevel;

    //Level of difference of generated noise
    public float noiseScale;

    //Auto update terrain when variables change
    public bool autoUpdate;

    //Types of region
    public TerrainType[] regions;

    //Multiplier for the height of mesh
    public float meshHeightMultiplier;

    //Number of octaves, e.g. the number of curves to form the terrain
    public int octave;

    //Controls decrease in amplitude of octaves [Range(0, 1)]
    public float persistence;

    //Controls increse in frequency of octaves
    public float lacunarity;

    //Seed for randomization
    public int seed;

    [HideInInspector]
    //The destinationPosition offset from center
    public Vector2 presetOffset;

    //Rock setting
    //Spawner object for rocks
    public GameObject rockSpawner;

    //The item to spawn
    public GameObject itemToSpawn;

    public int numberOfItemsToSpawn;
    public float itemYSpread;

    //Range of rotation
    public Vector3 randomRotationRange;

    //Multiplier for scale
    public float globalScaleMultiplier;

    //minimum of scale on x-axis
    public float xScaleMin;

    //maximum of scale on x-axis
    public float xScaleMax;

    //minimum of scale on y-axis
    public float yScaleMin;

    //maximum of scale on y-axis
    public float yScaleMax;

    //minimum of scale on z-axis
    public float zScaleMin;

    //maximum of scale on z-axis
    public float zScaleMax;

    //Range of spread on each axis
    private float itemXSpread;
    private float itemZSpread;

    //Number of veritices as height for each mesh
    private int mapHeight;

    //Number of veritices as width for each mesh
    private int mapWidth;
    private MeshData meshData;

    //Obtain information from TerrainAndRockSetting outside Edit folder
    private TerrainAndRockSetting terrainAndRockSetting;
    private Texture2D texture;
    

    //Get values from TerrainAndRockSetting
    public void Initialize()
    {
        terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        terrainSpawner = terrainAndRockSetting.terrainSpawner;
        meshNumVertices = terrainAndRockSetting.meshNumVertices; 
        mapSize = terrainAndRockSetting.mapSize;
        detailLevel = terrainAndRockSetting.detailLevel;
        noiseScale = terrainAndRockSetting.noiseScale;
        autoUpdate = terrainAndRockSetting.autoUpdate;
        regions = new TerrainType[terrainAndRockSetting.regions.Length];
        for (var i = 0; i < terrainAndRockSetting.regions.Length; i++)
        {
            regions[i].name = terrainAndRockSetting.regions[i].name;
            regions[i].height = terrainAndRockSetting.regions[i].height;
            regions[i].color = terrainAndRockSetting.regions[i].color;
        }

        meshHeightMultiplier = terrainAndRockSetting.meshHeightMultiplier;
        octave = terrainAndRockSetting.octave;
        persistence = terrainAndRockSetting.persistence;
        lacunarity = terrainAndRockSetting.lacunarity;
        seed = terrainAndRockSetting.seed;
        presetOffset = terrainAndRockSetting.presetOffset;
        rockSpawner = terrainAndRockSetting.rockSpawner;
        itemToSpawn = terrainAndRockSetting.itemToSpawn;
        numberOfItemsToSpawn = terrainAndRockSetting.numberOfItemsToSpawn;
        itemYSpread = terrainAndRockSetting.itemYSpread;
        randomRotationRange = terrainAndRockSetting.randomRotationRange;
        globalScaleMultiplier = terrainAndRockSetting.globalScaleMultiplier;
        xScaleMin = terrainAndRockSetting.xScaleMin;
        xScaleMax = terrainAndRockSetting.xScaleMax;
        yScaleMin = terrainAndRockSetting.yScaleMin;
        yScaleMax = terrainAndRockSetting.yScaleMax;
        zScaleMin = terrainAndRockSetting.zScaleMin;
        zScaleMax = terrainAndRockSetting.zScaleMax;
        Random.InitState(seed);
        rockSpawner.transform.position=Vector3.zero;
        rockSpawner.transform.rotation=quaternion.identity;
        rockSpawner.transform.localScale=Vector3.one;
        terrainSpawner.transform.position=Vector3.zero;
        terrainSpawner.transform.rotation=quaternion.identity;
        terrainSpawner.transform.localScale=Vector3.one;
        
    }

    /// <summary>
    ///     Calculate the exact map size.
    /// </summary>
    /// <returns></returns>
    public float CalculateMapSize()
    {
        return terrainAndRockSetting.CalculateMapSize();
    }

    /// <summary>
    ///     Destroy all terrain chunk before generate new terrain.
    /// </summary>
    /// <param name="transform"></param>
    public static void DestoryChildren(Transform transform)
    {
        TerrainAndRockSetting.DestoryChildren(transform);
    }

    /// <summary>
    ///     Draw complete map in editor.
    /// </summary>
    public void DrawMapInEditor()
    {
        DestoryChildren(terrainSpawner.transform);
        var endlessTerrain = CreateInstance<EndlessTerrain>();
        endlessTerrain.terrainAndRockSettingForEditor = this;
        endlessTerrain.UpdateChunks();
        var itemAreaSpawner = CreateInstance<ItemAreaSpawner>();
        itemAreaSpawner.terrainAndRockSettingForEditor = this;
        itemAreaSpawner.StartSpawn();
        CreateBoundary();
    }

    public void CreateBoundary()
    {
        float boundarySize = CalculateMapSize();
        GameObject boundary  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.localScale = new Vector3(boundarySize, boundarySize, 1);
        boundary.transform.position = new Vector3(0, 0, boundarySize / 2);
        boundary.transform.parent=terrainSpawner.transform;
        boundary.GetComponent<MeshRenderer>().enabled = false;
        boundary  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.localScale = new Vector3(boundarySize, boundarySize, 1);
        boundary.transform.position = new Vector3(0, 0, -boundarySize / 2);
        boundary.transform.parent=terrainSpawner.transform;
        boundary.GetComponent<MeshRenderer>().enabled = false;
        boundary  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.localScale = new Vector3(boundarySize, boundarySize, 1);
        boundary.transform.rotation=Quaternion.Euler(0,90,0);
        boundary.transform.position = new Vector3(boundarySize / 2, 0, 0);
        boundary.transform.parent=terrainSpawner.transform;
        boundary.GetComponent<MeshRenderer>().enabled = false;
        boundary  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.transform.localScale = new Vector3(boundarySize, boundarySize, 1);
        boundary.transform.rotation=Quaternion.Euler(0,90,0);
        boundary.transform.position = new Vector3(-boundarySize / 2, 0, 0);
        boundary.transform.parent=terrainSpawner.transform;
        boundary.GetComponent<MeshRenderer>().enabled = false;

    }

    /// <summary>
    ///     Generate noise map and color map.
    /// </summary>
    /// <returns></returns>
    public MapData GenerateMap()
    {
        mapWidth = meshNumVertices;
        mapHeight = meshNumVertices;
        var noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, octave, persistence, lacunarity,
            seed, presetOffset);
        var colorMap = new Color[mapWidth * mapHeight];
        for (var y = 0; y < mapHeight; y++)
        for (var x = 0; x < mapWidth; x++)
        for (var i = 0; i < regions.Length; i++)
            if (noiseMap[x, y] < regions[i].height)
            {
                colorMap[y * mapWidth + x] = regions[i].color;
                break;
            }

        return new MapData(noiseMap, colorMap);
    }
}