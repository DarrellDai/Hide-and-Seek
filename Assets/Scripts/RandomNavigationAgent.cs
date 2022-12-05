using System;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.TextCore.Text;
using Random = UnityEngine.Random;

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
    private int halfNumDivisionEachSide = 4;
    private int rangeAsNumGrids = 2;
    private float gridSize;
    private Vector2[,] destinationSpace;
    private bool[,] destinationMask;
    private bool[,] egocentricMask;
    private Vector2 currentGrid;
    private Vector2 nextGrid;
    private GameObject[,] gridsVisulization;

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
        destinationSpace = new Vector2[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        destinationMask = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        gridsVisulization = new GameObject[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        egocentricMask = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        gridSize = mapSize / halfNumDivisionEachSide;
        for (int i = 0; i < 2 * halfNumDivisionEachSide; i++)
        {
            for (int j = 0; j < 2 * halfNumDivisionEachSide; j++)
            {
                destinationSpace[i, j] = new Vector2(gridSize * (i - halfNumDivisionEachSide + 1 / 2f),
                    gridSize * (j - halfNumDivisionEachSide + 1 / 2f));
                if (CompareTag("Hider"))
                {
                    gridsVisulization[i, j] = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    gridsVisulization[i, j].transform.SetParent(GameObject.Find("PlaneSpawner").transform);
                    gridsVisulization[i, j].transform.position =
                        new Vector3(destinationSpace[i, j].x, 0.0001f, destinationSpace[i, j].y);
                    gridsVisulization[i, j].transform.localScale = Vector3.one * gridSize / 10f;
                    gridsVisulization[i, j].GetComponent<Renderer>().material.color = Color.clear;
                }

                destinationMask[i, j] = false;
                egocentricMask[i, j] = false;
            }
        }

        destinationSpawner = FindObjectOfType<PlayerSpawner>().destinationSpawner;

        overlapTestBoxSizeForDestination = GetComponent<Collider>().bounds.extents + Vector3.one * extraOffset;

        navMeshAgent = GetComponent<NavMeshAgent>();
        //Turn off auto-pilot in NavMeshAgent so the agent can move manually
        navMeshAgent.updatePosition = false;
        navMeshAgent.enabled = false;
        originalColor = transform.Find("Body").GetComponent<Renderer>().material.color;
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
                    toChooseNextDestination = true;
                    transform.Rotate(transform.up, 90f);
                    /*arrived = true;
                    if (!turned)
                    {
                        transform.Rotate(transform.up, 270f);
                        turned = true;
                        if (CompareTag("Hider"))
                            Debug.Log("Turned");
                    }*/
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
        if (CompareTag("Hider"))
        {
            UpdateDestinationAndEgocentricMask();
            var color = Color.yellow;
            color.a = 0.1f;
            gridsVisulization[(int)nextGrid.x, (int)nextGrid.y].GetComponent<Renderer>().material.color = color;
        }
        //DrawPath();
    }

    private void UpdateDestinationAndEgocentricMask()
    {
        for (var i = 0; i < destinationMask.GetLength(0); i++)
        {
            for (var j = 0; j < destinationMask.GetLength(1); j++)
            {
                egocentricMask[i, j] = false;
            }
        }

        currentGrid = new Vector2(Mathf.Floor((transform.position.x + mapSize) / gridSize),
            Mathf.Floor((transform.position.z + mapSize) / gridSize));
        for (int i = -rangeAsNumGrids; i < rangeAsNumGrids; i++)
        {
            for (int j = -rangeAsNumGrids; j < rangeAsNumGrids; j++)
            {
                if (currentGrid.x + i >= 0 && currentGrid.y + j >= 0 &&
                    currentGrid.x + i < 2 * halfNumDivisionEachSide && currentGrid.y + j < 2 * halfNumDivisionEachSide)
                {
                    destinationMask[(int)currentGrid.x + i, (int)currentGrid.y + j] = true;
                    egocentricMask[(int)currentGrid.x + i, (int)currentGrid.y + j] = true;
                }
            }
        }

        for (var i = 0; i < destinationMask.GetLength(0); i++)
        {
            for (var j = 0; j < destinationMask.GetLength(1); j++)
            {
                if (destinationMask[i, j])
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
            GetComponent<PlaceObjectsToSurface>().StartPlacing(
                navMeshAgent.velocity, false, true);
        }
        
    }

    /// <summary>
    ///     Generate a random destinationPosition for destination.
    /// </summary>
    public void selectNextRandomDestination()
    {
        /*overlap = true;
        while (overlap)
        {
            var randPosition = new Vector3(Random.Range(-mapSize, mapSize), 30,
                Random.Range(-mapSize, mapSize));
            destinationPosition = GetNoneOverlappedPosition(randPosition);
        }*/
        nextGrid = new Vector2(Random.Range(0, halfNumDivisionEachSide * 2),
            Random.Range(0, halfNumDivisionEachSide * 2));
        NavMeshHit hit;
        NavMesh.SamplePosition(GetNoneOverlappedPosition(new Vector3(nextGrid.x, 30,
            nextGrid.y)), out hit, Mathf.Infinity, NavMesh.AllAreas);
        destinationPosition = hit.position;
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