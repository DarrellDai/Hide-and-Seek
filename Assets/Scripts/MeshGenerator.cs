using UnityEngine;

public static class MeshGenerator
{
    /// <summary>
    ///     Create MeshData.
    /// </summary>
    /// <param name="heightMap"></param>
    /// <param name="meshHeightMultiplier"></param>
    /// <param name="detailLevel"></param>
    /// <returns></returns>
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float meshHeightMultiplier, int detailLevel)
    {
        int width = heightMap.GetLength(0), height = heightMap.GetLength(1);
        var meshData = new MeshData(width, height);
        var verticeIndex = 0;
        var leftMostX = (width - 1) / -2f;
        var topMostY = (height - 1) / 2f;
        var meshSimpleficationIncrement = detailLevel == 0 ? 1 : detailLevel * 2;
        var verticePerline = (width - 1) / meshSimpleficationIncrement + 1;
        for (var y = 0; y < height; y += meshSimpleficationIncrement)
        for (var x = 0; x < width; x += meshSimpleficationIncrement)
        {
            meshData.vertices[verticeIndex] =
                new Vector3(x + leftMostX, heightMap[x, y] * meshHeightMultiplier, topMostY - y);
            if (x < width - 1 && y < height - 1)
            {
                meshData.AddTriangles(verticeIndex, verticeIndex + verticePerline + 1, verticeIndex + verticePerline);
                meshData.AddTriangles(verticeIndex, verticeIndex + 1, verticeIndex + verticePerline + 1);
            }

            meshData.uv[verticeIndex] = new Vector2((float)x / width, (float)y / height);
            verticeIndex++;
        }

        return meshData;
    }
}

public class MeshData
{
    private int triangleIndex;
    public int[] triangles;
    public Vector2[] uv;
    public Vector3[] vertices;

    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        uv = new Vector2[meshWidth * meshHeight];
    }

    public void AddTriangles(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        triangleIndex += 3;
    }

    public Mesh createMesh()
    {
        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        return mesh;
    }
}