using UnityEngine;
using Random = System.Random;

public static class Noise
{
    /// <summary>
    ///     Generate noise map
    /// </summary>
    /// <param name="mapWidth"></param>
    /// <param name="mapHeight"></param>
    /// <param name="scale"></param>
    /// <param name="octave"></param>
    /// <param name="persistence"></param>
    /// <param name="lacunarity"></param>
    /// <param name="seed"></param>
    /// <param name="presetOffset"></param>
    /// <returns></returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int octave, float persistence,
        float lacunarity, int seed, Vector2 presetOffset)
    {
        Random prng = new(seed);
        var offset = new Vector2[octave];

        //Initialize normalization factor
        float maxPossibleHeight = 0;
        //Speed of change
        float frequency = 1;
        //Maximal magnitude
        float amplitude = 1;
        for (var i = 0; i < octave; i++)
        {
            var offsetX = prng.Next(-10000, 10000) + presetOffset.x;
            var offsetY = prng.Next(-10000, 10000) - presetOffset.y;
            offset[i] = new Vector2(offsetX, offsetY);
            //Set normalization factor to maximal possible height
            maxPossibleHeight += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        var noiseMap = new float[mapWidth, mapHeight];
        if (scale % Mathf.Min(mapWidth, mapHeight) == 0) scale++;
        var maxNoise = float.MinValue;
        var minNoise = float.MaxValue;
        for (var y = 0; y < mapHeight; y++)
        for (var x = 0; x < mapWidth; x++)
        {
            frequency = 1;
            amplitude = 1;
            for (var i = 0; i < octave; i++)
            {
                //Pairs (sampleX, sampleY) need to have at least one fraction to have different Perlin Noise
                var sampleX = (x - mapWidth / 2 + offset[i].x) / mapWidth * scale * frequency;
                var sampleY = (y - mapHeight / 2 + offset[i].y) / mapHeight * scale * frequency;
                var perlinVaule = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                noiseMap[x, y] += perlinVaule * amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            if (noiseMap[x, y] > maxNoise)
                maxNoise = noiseMap[x, y];
            else if (noiseMap[x, y] < minNoise) minNoise = noiseMap[x, y];
        }


        for (var y = 0; y < mapHeight; y++)
        for (var x = 0; x < mapWidth; x++)
            //Normalize noise
            noiseMap[x, y] = (noiseMap[x, y] + 1) / (2f * maxPossibleHeight);

        return noiseMap;
    }
}