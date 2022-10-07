using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class FindRandomPos : MonoBehaviour
{
    private bool overlap;
    private Vector3 randPosition;
    private float itemSpread;
    [FormerlySerializedAs("overlapTestBoxSize")] public float radius;
    [HideInInspector] public LayerMask hiderLayer;
    [HideInInspector] public LayerMask seekerLayer;
    [HideInInspector] public LayerMask rockLayer;
    [HideInInspector] public LayerMask terrainLayer;
    [HideInInspector] public TerrainAndRockSetting terrainAndRockSetting;
    public int seed;
    public float distanceFromBound = 3;
    RaycastHit hit;
    private bool touch;
    
    public void Initialize()
    {
        hiderLayer = LayerMask.NameToLayer("Hider");
        seekerLayer = LayerMask.NameToLayer("Seeker");
        rockLayer = LayerMask.NameToLayer("Rock");
        terrainLayer = LayerMask.NameToLayer("Terrain");
        terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        var mapSize = terrainAndRockSetting.CalculateMapSize();
        itemSpread = mapSize / 2 - distanceFromBound;
    }
    public void FindRandPosition()
    {
        randPosition = new Vector3(Random.Range(-itemSpread, itemSpread), 100,
                Random.Range(-itemSpread, itemSpread));
        Debug.Log(randPosition);
    }

    public void DoSphereCast()
    {
        FindRandPosition();

        if (Physics.SphereCast(randPosition, radius, Vector3.down, out hit, 1000,
                1 << seekerLayer | 1 << hiderLayer | 1 << rockLayer))
        {
            touch = true;
        }
        else
        {
            touch = false;
        }
    }

    /*private void OnDrawGizmos() 
    {
        if (touch)
        {
            Gizmos.color = Color.green;
            Vector3 sphereCastMidpoint = hit.point + radius * hit.normal;
            Gizmos.DrawWireSphere(sphereCastMidpoint, radius);
            Gizmos.DrawSphere(hit.point, 0.1f);
            Debug.DrawLine(randPosition, sphereCastMidpoint);
        }
        else
        {
            Physics.Raycast(randPosition, Vector3.down, out hit, 1000,
                1 << terrainLayer);
            Gizmos.color = Color.red;
            Vector3 sphereCastMidpoint = hit.point + radius * hit.normal;
            Gizmos.DrawWireSphere(sphereCastMidpoint, radius);
            Gizmos.DrawSphere(hit.point, 0.1f);
            Debug.DrawLine(randPosition, sphereCastMidpoint);
        }
    }*/
}
