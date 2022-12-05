using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.AI;

public class RandomNavigationAgent : GameAgent
{
    //Destination spawner as the parent of all destinations
    [HideInInspector] public GameObject destinationSpawner;

    //Additional offset beside player's collider size the destination needs to be away from rocks
    [HideInInspector] public float extraOffset = 0.1f;

    //The box size the destination needs to be away from rocks
    [HideInInspector] public Vector3 overlapTestBoxSizeForDestination;
    [HideInInspector] public Vector3 destinationPosition;
    [HideInInspector] public bool toChooseNextDestination = true;
    private bool arrived;
    private bool turned;
    [HideInInspector] public GameObject destination;
    [HideInInspector] public NavMeshAgent navMeshAgent;

    private Color originalColor;
    private bool overlap;

    //Path planned by NavMesh
    public NavMeshPath path;

    private Vector2 last2dDestination;
    private float rotation;

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
        navMeshAgent.enabled = false; 
        originalColor=transform.Find("Body").GetComponent<Renderer>().material.color;
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        // Disable navMeshAgent so it's nextPosition won't move to cause teleport
        navMeshAgent.enabled = false; 
    }

    /// <summary>
    ///     Check if the agent already reached the destination, and set toChooseNextDestination = true if so.
    /// </summary>
    public void CheckIfArrived()
    {
        if (!navMeshAgent.isActiveAndEnabled)
        {
            toChooseNextDestination = true;
        }
        else if (!navMeshAgent.pathPending)
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    //toChooseNextDestination = true;
                    arrived = true;
                    if (!turned)
                    {
                        transform.Rotate(transform.up, 270f);
                        turned = true;
                        if (CompareTag("Hider"))
                            Debug.Log("Turned");
                    }
                    transform.Find("Body").GetComponent<Renderer>().material.color = Color.yellow;
                }
            }
                
    }

    /// <summary>
    ///     Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        transform.Find("Body").GetComponent<Renderer>().material.color = originalColor;
        CheckIfArrived();
        base.OnActionReceived(actionBuffers);
        //DrawPath();
        
    }

    /// <summary>
    ///     Move the agent along the path planned by NavMesh, and place the agent on and normal to surface after each move,
    ///     skip placing if it makes the agent out of bound.
    /// </summary>
    public override void MoveAgent(ActionSegment<int> act)
    {
        // Enable navMeshAgent when it's able to move
        navMeshAgent.enabled = true;
        // Prevent NavMeshAgent is not active on NavMesh issue
        if (toChooseNextDestination && navMeshAgent.isActiveAndEnabled)
        {
            selectNextRandomDestination();
            MakeNewDestination();
        }
        if (!arrived & navMeshAgent.isActiveAndEnabled)
        {
            transform.position = navMeshAgent.nextPosition;
        }
        CheckIfArrived();
        
        if (!arrived)
            GetComponent<PlaceObjectsToSurface>().StartPlacing(
                navMeshAgent.velocity, false, true);
        

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
        //navMeshAgent.ResetPath();
        //navMeshAgent.Warp(transform.position);
        navMeshAgent.destination = destination.transform.position;
        turned = false;
        if (CompareTag("Hider"))
            Debug.Log("new destination");
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
        Gizmos.DrawWireSphere(transform.position,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.position, Vector3.one*2f); 
    }*/
}