using UnityEngine;

public static class TextureGenerator
{
    /// <summary>
    ///     Generate texture from color map
    /// </summary>
    /// <param name="colorMap"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(colorMap);
        texture.Apply();
        return texture;
    }

    /// <summary>
    ///     Generate texture from height map
    /// </summary>
    /// <param name="heightMap"></param>
    /// <returns></returns>
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        var width = heightMap.GetLength(0);
        var height = heightMap.GetLength(1);

        var colorMap = new Color[width * height];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            colorMap[y * width + x] = Color.Lerp(Color.white, Color.black, heightMap[x, y]);

        return TextureFromColorMap(colorMap, width, height);
    }
}