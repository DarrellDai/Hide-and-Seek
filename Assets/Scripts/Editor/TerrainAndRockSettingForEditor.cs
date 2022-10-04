using System;
using UnityEngine;

public class TerrainAndRockSettingForEditor : ScriptableObject
{
    //Terrain setting
    //Spawner object for terrain
    public GameObject terrainSpawner;
    
    //Obtain information from TerrainAndRockSetting outside Edit folder
    private TerrainAndRockSetting terrainAndRockSetting;
    //Number of vertices on edges for each mesh
    public int meshNumVertices;

    //Size of the complete map, however, it's not the exact size
    public int mapSize;

    //Number of veritices as width for each mesh
    private int mapWidth;

    //Number of veritices as height for each mesh
    private int mapHeight;
    [HideInInspector] public int detailLevel = 0;

    //Level of difference of generated noise
    public float noiseScale;

    //Auto update terrain when variables change
    public bool autoUpdate;

    //Types of region
    public TerrainType[] regions;
    private Texture2D texture;
    private MeshData meshData;

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

    //Range of spread on each axis
    float itemXSpread;
    public float itemYSpread;
    float itemZSpread;

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
    
    //Get values from TerrainAndRockSetting
    public void GetValue()
    {
        terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        terrainSpawner = terrainAndRockSetting.terrainSpawner;
        meshNumVertices = terrainAndRockSetting.meshNumVertices;
        mapSize = terrainAndRockSetting.mapSize;
        detailLevel = terrainAndRockSetting.detailLevel;
        noiseScale = terrainAndRockSetting.noiseScale;
        autoUpdate = terrainAndRockSetting.autoUpdate;
        regions = new TerrainType[terrainAndRockSetting.regions.Length];
        for (int i = 0; i < terrainAndRockSetting.regions.Length; i++)
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
    }

    /// <summary>
    /// Calculate the exact map size.
    /// </summary>
    /// <returns></returns>
    public float CalculateMapSize()
    {
        return terrainAndRockSetting.CalculateMapSize();
    }

    /// <summary>
    /// Destroy all terrain chunk before generate new terrain.
    /// </summary>
    /// <param name="transform"></param>
    public static void DestoryChildren(Transform transform)
    {
        TerrainAndRockSetting.DestoryChildren(transform);
    }

    /// <summary>
    /// Draw complete map in editor.
    /// </summary>
    public void DrawMapInEditor()
    {
        DestoryChildren(terrainSpawner.transform);
        EndlessTerrain endlessTerrain = CreateInstance<EndlessTerrain>();
        endlessTerrain.terrainAndRockSettingForEditor = this;
        endlessTerrain.UpdateChunks();
        ItemAreaSpawner itemAreaSpawner = CreateInstance<ItemAreaSpawner>();
        itemAreaSpawner.terrainAndRockSettingForEditor = this;
        itemAreaSpawner.StartSpawn();
    }

    /// <summary>
    /// Generate noise map and color map.
    /// </summary>
    /// <returns></returns>
    public MapData GenerateMap()
    {
        mapWidth = meshNumVertices;
        mapHeight = meshNumVertices;
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, octave, persistence, lacunarity,
            seed, presetOffset);
        Color[] colorMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                for (int i = 0; i < regions.Length; i++)
                {
                    if (noiseMap[x, y] < regions[i].height)
                    {
                        colorMap[y * mapWidth + x] = regions[i].color;
                        break;
                    }
                }
            }
        }

        return new MapData(noiseMap, colorMap);
    }
}


