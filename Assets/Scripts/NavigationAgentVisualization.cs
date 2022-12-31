using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class NavigationAgentVisualization : NavigationAgent
{
    private GameObject[,] gridsVisulization;
    private Color originalColor;

    /// <summary>
    ///     Initialize Navigation agent.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        gridsVisulization = new GameObject[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        for (int i = 0; i < 2 * halfNumDivisionEachSide; i++)
        {
            for (int j = 0; j < 2 * halfNumDivisionEachSide; j++)
            {
                if (CompareTag("Hider"))
                {
                    gridsVisulization[i, j] = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    gridsVisulization[i, j].transform.SetParent(GameObject.Find("PlaneSpawner").transform);
                    gridsVisulization[i, j].transform.position =
                        new Vector3(destinationSpace[i, j].x, 0.01f, destinationSpace[i, j].y);
                    gridsVisulization[i, j].transform.localScale = Vector3.one * gridSize / 10f;
                    gridsVisulization[i, j].GetComponent<Renderer>().material.SetFloat("_Mode", 3);
                    gridsVisulization[i, j].GetComponent<Renderer>().material.color = Color.clear;
                }
            }
        }

        originalColor = transform.Find("Body").GetComponent<Renderer>().material.color;
    }


    /// <summary>
    ///     Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);
        transform.Find("Body").GetComponent<Renderer>().material.color = originalColor;
        if (CompareTag("Hider"))
        {
            var color = Color.gray;
            color.a = 0.1f;
            gridsVisulization[(int)nextGrid.x, (int)nextGrid.y].GetComponent<Renderer>().material.color = color;
            color = Color.yellow;
            gridsVisulization[(int)sampledGrid.x, (int)sampledGrid.y].GetComponent<Renderer>().material.color = color;
        }
    }

    public override void UpdateDestinationAndEgocentricMask()
    {
        base.UpdateDestinationAndEgocentricMask();
        if (CompareTag("Hider"))
        {
            for (var i = 0; i < destinationVisited.GetLength(0); i++)
            {
                for (var j = 0; j < destinationVisited.GetLength(1); j++)
                {
                    gridsVisulization[i, j].GetComponent<Renderer>().material.color = Color.clear;
                    if (destinationVisited[i, j])
                    {
                        var color = Color.blue;
                        color.a = 0.1f;
                        gridsVisulization[i, j].GetComponent<Renderer>().material.color = color;
                    }

                    if (egocentricMask[i, j])
                    {
                        var color = Color.red;
                        color.a = 0.1f;
                        gridsVisulization[i, j].GetComponent<Renderer>().material.color = color;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Highlight the hider in sphere and destination in box with green color 
    /// </summary>
    public void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            if (destination != null)
                Gizmos.DrawWireCube(destination.transform.position, Vector3.one * 1f);
            Gizmos.color = Color.white;
            if (chosenGrid != null)
            {
                Gizmos.DrawWireCube(GetPositionFromGrid(chosenGrid), Vector3.one * 1f);
            }
        }
    }
}