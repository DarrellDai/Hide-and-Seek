using System;
using UnityEngine;

public class TerrainAndRockSetting : MonoBehaviour
{   [Header("Terrain setting")]
    [Tooltip("Spawner object for terrain")]
    public GameObject terrainSpawner;
    [Tooltip("Number of veritices on edges for each mesh")]
    public int meshNumVertices=11; 
    [Tooltip("Size of the complete map, however, it's not the exact size")]
    public int mapSize;  
    //Number of veritices as width for each mesh
    private int mapWidth;
    //Number of veritices as height for each mesh
    private int mapHeight;
    [HideInInspector]
    public int detailLevel = 0;
    [Tooltip("Level of difference of generated noise")]
    public float noiseScale;
    [Tooltip("Auto update terrain when variables change")]
    public bool autoUpdate;
    [Tooltip("Types of region")]
    public TerrainType[] regions;
    private Texture2D texture;
    private MeshData meshData;
    
    [Tooltip("Multiplier for the height of mesh")]
    public float meshHeightMultiplier;
    [Tooltip("Number of octaves, e.g. the number of curves to form the terrain")]
    public int octave;
    [Tooltip("Controls decrease in amplitude of octaves")]
    [Range(0,1)]
    public float persistence;
    [Tooltip("Controls increse in frequency of octaves")]
    public float lacunarity;
    [Tooltip("Seed for randomization")]
    public int seed;
    [HideInInspector]
    //The destinationPosition offset from center
    public Vector2 presetOffset;

    [Header("Rock setting")] 
    [Tooltip("Spawner object for rocks")]
    public GameObject rockSpawner;
    [Tooltip("The item to spawn")]
    public GameObject itemToSpawn;
    public int numberOfItemsToSpawn;
    //Range of spread on each axis
    float itemXSpread;
    public float itemYSpread = 10;
    float itemZSpread;
    
    [Tooltip("Range of rotation")]
    public Vector3 randomRotationRange;
    
    [Tooltip("Multiplier for scale")]
    public float globalScaleMultiplier = 1f;
    
    [Tooltip("minimum of scale on x-axis")]
    public float xScaleMin = .1f;
    [Tooltip("maximum of scale on x-axis")]
    public float xScaleMax = 3f;
    [Tooltip("minimum of scale on y-axis")]
    public float yScaleMin = .1f;
    [Tooltip("maximum of scale on y-axis")]
    public float yScaleMax = 3f;
    [Tooltip("minimum of scale on z-axis")]
    public float zScaleMin = .1f;
    [Tooltip("maximum of scale on z-axis")]
    public float zScaleMax = 3f;
    
    /// <summary>
    /// Calculate the exact map size.
    /// </summary>
    /// <returns></returns>
    public float CalculateMapSize()
    {
        Collider[] m_Collider = terrainSpawner.GetComponentsInChildren<Collider>(); 
        float volume=0f;
        for (int i=0;i<m_Collider.Length;i++)
        {
            volume += m_Collider[i].bounds.size.x * m_Collider[i].bounds.size.z;
        }
        return Mathf.Sqrt(volume);
        
    }
    
    /// <summary>
    /// Destroy all terrain chunk before generate new terrain.
    /// </summary>
    /// <param name="transform"></param>
    public static void DestoryChildren(Transform transform)
    {
        if (transform.childCount>0)
        {
            var tempArray = new GameObject[transform.childCount];

            for(int i = 0; i < tempArray.Length; i++)
            {
                tempArray[i] = transform.GetChild(i).gameObject;
            }

            foreach(var child in tempArray) 
            {
                DestroyImmediate(child);
            }
            
        }
    }
    
    /// <summary>
    /// Generate noise map and color map.
    /// </summary>
    /// <returns></returns>
    public MapData GenerateMap()
    {
        mapWidth = meshNumVertices;
        mapHeight = meshNumVertices;
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, noiseScale, octave, persistence, lacunarity, seed, presetOffset); 
        Color[] colorMap = new Color[mapWidth*mapHeight];
        for (int y=0;y<mapHeight;y++)
        {
            for (int x=0;x<mapWidth;x++)
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

    /// <summary>
    /// Avoid invalid value
    /// </summary>
    public void OnValidate()
    {
        if (octave < 1)
            octave = 1;
        if (lacunarity < 1)
            lacunarity = 1;
    }
}
/// <summary>
/// Terrains types for terrain map.
/// </summary>
[Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color color;
}

public struct MapData
{
    public float[,] heightMap;
    public Color[] colorMap;

    public MapData(float[,] heightMap, Color[] colorMap)
    {
        this.heightMap = heightMap;
        this.colorMap = colorMap;
    }
}
