using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.AI;

public class NavigationAgent : GameAgent
{
    //Destination spawner as the parent of all destinations
    [HideInInspector] public GameObject destinationSpawner;

    //Additional offset beside player's collider size the destination needs to be away from rocks
    [HideInInspector] public float extraOffset = 0.1f;

    //The box size the destination needs to be away from rocks
    [HideInInspector] public Vector3 overlapTestBoxSizeForDestination;
    [HideInInspector] public Vector3 destinationPosition;
    [HideInInspector] public bool toChooseNextDestination = true;
    [HideInInspector] public GameObject destination;
    [HideInInspector] public NavMeshAgent navMeshAgent;

    private bool overlap;

    //Path planned by NavMesh
    public NavMeshPath path;

    /// <summary>
    ///     Initialize Navigation agent.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        destinationSpawner = FindObjectOfType<PlayerSpawner>().destinationSpawner;

        overlapTestBoxSizeForDestination = GetComponent<Collider>().bounds.extents + Vector3.one * extraOffset;

        navMeshAgent = GetComponent<NavMeshAgent>();
        //Turn off auto-pilot in NavMeshAgent so the agent can move manually
        navMeshAgent.updatePosition = false;
    }

    /// <summary>
    ///     Check if the agent already reached the destination, and set toChooseNextDestination = true if so.
    /// </summary>
    public void CheckIfArrived()
    {
        if (!navMeshAgent.isActiveAndEnabled)
            toChooseNextDestination = true;
        else if (!navMeshAgent.pathPending)
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                    toChooseNextDestination = true;
    }

    /// <summary>
    ///     Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        base.OnActionReceived(actionBuffers);
        //DrawPath();
        CheckIfArrived();
    }

    /// <summary>
    ///     Move the agent along the path planned by NavMesh, and place the agent on and normal to surface after each move,
    ///     skip placing if it makes the agent out of bound.
    /// </summary>
    public override void MoveAgent(ActionSegment<int> act)
    {
        if (toChooseNextDestination)
        {
            selectNextRandomDestination();
            MakeNewDestination();
        }
        transform.position = navMeshAgent.nextPosition;
        GetComponent<PlaceObjectsToSurface>().StartPlacing();
    }

    /// <summary>
    ///     Generate a random destinationPosition for destination.
    /// </summary>
    public void selectNextRandomDestination()
    {
        overlap = true;
        while (overlap)
        {
            var randPosition = new Vector3(Random.Range(-mapSize, mapSize), 30,
                Random.Range(-mapSize, mapSize));
            destinationPosition = GetNoneOverlappedPosition(randPosition);
        }
    }

    /// <summary>
    ///     Create an destination object, and plan a path for agent.
    /// </summary>
    public void MakeNewDestination()
    {
        Destroy(destination);
        destination = new GameObject("Destination");
        destination.transform.position = destinationPosition;
        destination.transform.parent = destinationSpawner.transform;
        toChooseNextDestination = false;
        navMeshAgent.Warp(transform.position);
        navMeshAgent.destination = destination.transform.position;
    }

    /// <summary>
    ///     Draw the planned path.
    /// </summary>
    public void DrawPath()
    {
        path = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, destination.transform.position, NavMesh.AllAreas, path);
        for (var i = 0; i < path.corners.Length - 1; i++)
            Debug.DrawLine(path.corners[i], path.corners[i + 1], Color.magenta);
    }

    /// <summary>
    ///     Ensure the destination destinationPosition is not close to rocks.
    /// </summary>
    /// <param name="position">Destination destinationPosition</param>
    /// <returns></returns>
    private Vector3 GetNoneOverlappedPosition(Vector3 position)
    {
        var center = new Vector3();
        RaycastHit hit;

        if (Physics.Raycast(position, Vector3.down, out hit))
        {
            var spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            var collidersInsideOverlapBox = new Collider[1];
            center = hit.point + new Vector3(0, overlapTestBoxSizeForDestination.y, 0);
            var numberOfCollidersFound = Physics.OverlapBoxNonAlloc(center, overlapTestBoxSizeForDestination,
                collidersInsideOverlapBox, spawnRotation, 1 << LayerMask.NameToLayer("Rock"));
            if (numberOfCollidersFound == 0) overlap = false;
        }

        return center;
    }

    /*/// <summary>
    /// Highlight the hider in sphere and destination in box with green color 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.destinationPosition,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.destinationPosition, Vector3.one*2f); 
    }*/
}