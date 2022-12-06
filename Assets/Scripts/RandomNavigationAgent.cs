using System;
using Unity.Mathematics;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.TextCore.Text;
using Random = UnityEngine.Random;

public class RandomNavigationAgent : NavigationAgent
{
    /// <summary>
    ///     Move the agent along the path planned by NavMesh, and place the agent on and normal to surface after each move,
    ///     skip placing if it makes the agent out of bound.
    /// </summary>
    public override void MoveAgent(ActionSegment<int> act)
    {
        // Enable navMeshAgent when it's able to move
        navMeshAgent.enabled = true;
        // Prevent NavMeshAgent is not active on NavMesh issue
        MakeRandomDestination();

        if (!arrived & navMeshAgent.isActiveAndEnabled)
        {
            GoToNextPosition();
        }

    }

    public void MakeRandomDestination()
    {
        if (toChooseNextDestination && navMeshAgent.isActiveAndEnabled)
        {
            sampledGrid = new Vector2(Random.Range(0, halfNumDivisionEachSide * 2),
                Random.Range(0, halfNumDivisionEachSide * 2));
            selectNextDestination();
            MakeNewDestination();
            toChooseNextDestination = false;
        }
    }

    /*/// <summary>
    /// Highlight the hider in sphere and destination in box with green color 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.position, Vector3.one*2f); 
    }*/
}