using UnityEditor;
using UnityEngine;

public class EndlessTerrain : ScriptableObject
{
    //The destinationPosition of viewer (center of map)
    public static Vector2 viewerPosition;
    private static int chunkSize;
    public static int mapWidth;
    public static int mapHeight;
    public TerrainAndRockSettingForEditor terrainAndRockSettingForEditor;
    private int chunkVisibleInViewDst;
    private Material material; //[-maxViewDst/chunkSize, maxViewDst/chunkSize] is the range of index for chunks
    private int maxViewDst;
    private bool visible;

    /// <summary>
    ///     Update all terrain chunks
    /// </summary>
    public void UpdateChunks()
    {
        viewerPosition = Vector2.zero;
        //Chunk size is number of vertices - 1
        chunkSize = terrainAndRockSettingForEditor.meshNumVertices - 1;
        mapWidth = terrainAndRockSettingForEditor.meshNumVertices;
        mapHeight = terrainAndRockSettingForEditor.meshNumVertices;
        maxViewDst = terrainAndRockSettingForEditor.mapSize;
        chunkVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);
        material = new Material(Shader.Find("Standard"));
        var chunkViewerCoordinateX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        var chunkViewerCoordinateY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        for (var offSetY = -chunkVisibleInViewDst; offSetY <= chunkVisibleInViewDst; offSetY++)
        for (var offSetX = -chunkVisibleInViewDst; offSetX <= chunkVisibleInViewDst; offSetX++)
        {
            Vector2 chunkPosition = new(chunkViewerCoordinateX + offSetX, chunkViewerCoordinateY + offSetY);
            new TerrainChunk(chunkPosition, chunkSize, terrainAndRockSettingForEditor.terrainSpawner.transform,
                material, terrainAndRockSettingForEditor);
        }
    }

    /// <summary>
    ///     Generate a terrain chunk
    /// </summary>
    public class TerrainChunk
    {
        private readonly MeshCollider mapCollider;
        private readonly MeshFilter mapFilter;
        private readonly Renderer mapRenderer;
        private readonly GameObject meshObject;

        public TerrainChunk(Vector2 chunkPosition, int chunkSize, Transform parent, Material mapMaterial,
            TerrainAndRockSettingForEditor terrainAndRockSettingForEditor)
        {
            var realPosition = chunkPosition * chunkSize;
            terrainAndRockSettingForEditor.presetOffset = realPosition;
            meshObject = new GameObject("Terrain Chunk");
            meshObject.layer = LayerMask.NameToLayer("Terrain");

            meshObject.transform.position = new Vector3(realPosition.x, 0, realPosition.y);
            mapRenderer = meshObject.AddComponent<MeshRenderer>();
            mapFilter = meshObject.AddComponent<MeshFilter>();
            mapCollider = meshObject.AddComponent<MeshCollider>();
            mapRenderer.sharedMaterial = mapMaterial;
            var mapData = terrainAndRockSettingForEditor.GenerateMap();
            var meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap,
                terrainAndRockSettingForEditor.meshHeightMultiplier, terrainAndRockSettingForEditor.detailLevel);
            var texture =
                TextureGenerator.TextureFromColorMap(mapData.colorMap, mapWidth, mapHeight);
            var mesh = meshData.createMesh();
            mapFilter.sharedMesh = mesh;
            mapCollider.sharedMesh = mesh;
            mapRenderer.sharedMaterial.mainTexture = texture;
            meshObject.transform.parent = parent;
            meshObject.tag = "Terrain";
            var flags = StaticEditorFlags.NavigationStatic;
            GameObjectUtility.SetStaticEditorFlags(meshObject, flags);
        }
    }
}