using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class NavigationAgent : GameAgent
{
    bool overlap;
    [HideInInspector] public GameObject destinationSpawner;
    [HideInInspector] public float extraOffset=0.1f; 
    [HideInInspector] public Vector3 overlapTestBoxSize;
    [HideInInspector] public Vector3 position;
    private Vector3 center;
    [HideInInspector] public bool toChooseNextDestination = true;
    public NavMeshPath path;
    [HideInInspector] public GameObject destination;
    [HideInInspector] public NavMeshAgent navMeshAgent;
    
    
    /// <summary>
    /// Initialize Navigation agent.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        destinationSpawner = FindObjectOfType<PlayerSpawner>().destinationSpawner;
        //The box size the destination needs to be away from rocks
        overlapTestBoxSize = GetComponent<Collider>().bounds.extents + Vector3.one*extraOffset;
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        //Turn off auto-pilot in NavMeshAgent so the agent can move manually
        navMeshAgent.updatePosition = false;

    }

    /// <summary>
    /// Check if the agent already reached the destination, and set toChooseNextDestination = true if so.
    /// </summary>
    public void CheckIfArrived()
    {
        if (!navMeshAgent.isActiveAndEnabled)
            toChooseNextDestination = true;
        else if (!navMeshAgent.pathPending)
        {
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    toChooseNextDestination = true;
                }
            }
        }
    }

    /// <summary>
    /// Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        if (toChooseNextDestination)
        {
            selectNextRandomDestination();
            MakeNewDestination();
        }
        MoveAgentByNavigation();
        DrawPath();
        CheckIfArrived();
    }

    /// <summary>
    /// Move the agent along the path planned by NavMesh, and place the agent on and normal to surface after each move,
    /// skip placing if it makes the agent out of bound.
    /// </summary>
    public void MoveAgentByNavigation()
    {
        transform.position = navMeshAgent.nextPosition;
        Vector3 lastPostion = transform.position;
        GetComponent<PlaceObjectsToSurface>().StartPlacing();
        CheckIfOut();
        if (isOut)
            transform.position = lastPostion;
    }
    
    /// <summary>
    /// Generate a random position for destination.
    /// </summary>
    public void selectNextRandomDestination()
    {
        overlap = true;
        while (overlap)
        {
            Vector3 randPosition = new Vector3(Random.Range(-mapSize, mapSize), 30,
                Random.Range(-mapSize, mapSize));
            position = GetNoneOverlappedPosition(randPosition);
        }
    }
    /// <summary>
    /// Create an destination object, and plan a path for agent. 
    /// </summary>
    public void MakeNewDestination()
    {
        Destroy(destination);
        destination = new GameObject("Destination");
        destination.transform.position = position;
        destination.transform.parent = destinationSpawner.transform;
        toChooseNextDestination = false;
        navMeshAgent.Warp(transform.position);
        navMeshAgent.destination = destination.transform.position;  
    }
    /// <summary>
    /// Draw the planned path.
    /// </summary>
    public void DrawPath()
    {
        path = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, destination.transform.position, NavMesh.AllAreas, path);
        for (int i = 0; i < path.corners.Length - 1; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.magenta);
    }
    /// <summary>
    /// Ensure the destination position is not close to rocks.
    /// </summary>
    /// <param name="position">Destination position</param>
    /// <returns></returns>
    Vector3 GetNoneOverlappedPosition(Vector3 position)
    {
        RaycastHit hit;

        if (Physics.Raycast(position, Vector3.down, out hit))
        {

            Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            Collider[] collidersInsideOverlapBox = new Collider[1];
            center = hit.point + new Vector3(0, overlapTestBoxSize.y, 0);
            int numberOfCollidersFound = Physics.OverlapBoxNonAlloc(center, overlapTestBoxSize,
                collidersInsideOverlapBox, spawnRotation, 1 << LayerMask.NameToLayer("Rock"));
            if (numberOfCollidersFound == 0)
            {
                overlap=false;
            }
        }
        return center;  
    }
    
    /// <summary>
    /// Highlight the hider in sphere and destination in box with green color 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.position, Vector3.one*2f); 
    }
}
