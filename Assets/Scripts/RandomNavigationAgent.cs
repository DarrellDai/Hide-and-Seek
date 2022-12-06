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
    private bool[,] destinationVisited;
    private bool[,] egocentricMask;
    private Vector2 currentGrid;
    private Vector2 sampledGrid;
    private Vector2 chosenGrid;
    private Vector2 nextGrid;
    private GameObject[,] gridsVisulization;

    [HideInInspector] public GameObject destination;
    [HideInInspector] public NavMeshAgent navMeshAgent;
    private Camera camera;

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
        destinationVisited = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        gridsVisulization = new GameObject[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        egocentricMask = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        gridSize = mapSize / halfNumDivisionEachSide;
        camera = transform.Find("Camera").GetComponent<Camera>();
        transform.Find("Camera").GetComponent<Camera>().transform.localPosition = new Vector3(0,
            rangeAsNumGrids * gridSize / Mathf.Tan(camera.fieldOfView / 2 * Mathf.PI / 180) -
            GetComponent<Collider>().bounds.extents.y, 0);
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
                        new Vector3(destinationSpace[i, j].x, 0.01f, destinationSpace[i, j].y);
                    gridsVisulization[i, j].transform.localScale = Vector3.one * gridSize / 10f;
                    gridsVisulization[i, j].GetComponent<Renderer>().material.SetFloat("_Mode", 3);
                    gridsVisulization[i, j].GetComponent<Renderer>().material.color = Color.clear;
                }

                destinationVisited[i, j] = false;
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
        UpdateDestinationAndEgocentricMask();
        if (CompareTag("Hider"))
        {
            var color = Color.yellow;
            color.a = 0.1f;
            gridsVisulization[(int)sampledGrid.x, (int)sampledGrid.y].GetComponent<Renderer>().material.color = color;
        }
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
            GetComponent<PlaceObjectsToSurface>().StartPlacing(
                navMeshAgent.velocity, false, true);
            camera.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
        }
    }

    /// <summary>
    ///     Generate a random destinationPosition for destination.
    /// </summary>
    public void selectNextRandomDestination()
    {
        sampledGrid = new Vector2(Random.Range(0, halfNumDivisionEachSide * 2),
            Random.Range(0, halfNumDivisionEachSide * 2));
        if (!destinationVisited[(int)sampledGrid.x, (int)sampledGrid.y])
        {
            float distance = Mathf.Infinity;
            for (var i = 0; i < destinationVisited.GetLength(0); i++)
            {
                for (var j = 0; j < destinationVisited.GetLength(1); j++)
                {
                    if (destinationVisited[i, j])
                    {
                        var testGrid = new Vector2(i, j);
                        var newDistance = Vector2.Distance(sampledGrid, testGrid);
                        if (newDistance < distance)
                        {
                            distance = newDistance;
                            chosenGrid = testGrid;
                        }
                    }
                }
            }
        }
        else
            chosenGrid = sampledGrid;
        NavMeshHit hit;
        chosenGrid = Vector2.zero;
        NavMesh.SamplePosition(GetPositionFromGrid(chosenGrid), out hit, Mathf.Infinity, NavMesh.AllAreas);
        destinationPosition = hit.position;
        nextGrid = new Vector2(Mathf.Floor((destinationPosition.x + mapSize) / gridSize),
            Mathf.Floor((destinationPosition.z + mapSize) / gridSize));
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


    private Vector3 GetPositionFromGrid(Vector2 gridIndex)
    {
        var center = new Vector3();
        var position = new Vector3(destinationSpace[(int)gridIndex.x, (int)gridIndex.y].x, 30,
            destinationSpace[(int)gridIndex.x, (int)gridIndex.y].y);
        RaycastHit hit;

        if (Physics.Raycast(position, Vector3.down, out hit))
        {
            center = hit.point + new Vector3(0, overlapTestBoxSizeForDestination.y, 0);
        }

        return center;
    }

    private void UpdateDestinationAndEgocentricMask()
    {
        for (var i = 0; i < destinationVisited.GetLength(0); i++)
        {
            for (var j = 0; j < destinationVisited.GetLength(1); j++)
            {
                egocentricMask[i, j] = false;
                //gridsVisulization[i, j].GetComponent<Renderer>().material.color = Color.clear;
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
                    destinationVisited[(int)currentGrid.x + i, (int)currentGrid.y + j] = true;
                    egocentricMask[(int)currentGrid.x + i, (int)currentGrid.y + j] = true;
                }
            }
        }

        for (var i = 0; i < destinationVisited.GetLength(0); i++)
        {
            for (var j = 0; j < destinationVisited.GetLength(1); j++)
            {
                if (destinationVisited[i, j])
                {
                    var color = Color.blue;
                    color.a = 0.1f;
                    //gridsVisulization[i, j].GetComponent<Renderer>().material.color = color;
                }

                if (egocentricMask[i, j])
                {
                    var color = Color.red;
                    color.a = 0.1f;
                    //gridsVisulization[i, j].GetComponent<Renderer>().material.color = color;
                }
            }
        }
    }

    /// <summary>
    /// Highlight the hider in sphere and destination in box with green color 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        /*Gizmos.DrawWireSphere(transform.position,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.position, Vector3.one*2f); */
        if (nextGrid != null && chosenGrid != null && gridsVisulization != null)
        {
            if (gridsVisulization[(int)nextGrid.x, (int)nextGrid.y] != null)
            {
                Gizmos.DrawWireCube(gridsVisulization[(int)nextGrid.x, (int)nextGrid.y].transform.position,
                    Vector3.one * 2f);
            }
            if (gridsVisulization[(int)chosenGrid.x, (int)chosenGrid.y] != null)
            {
                Gizmos.DrawWireSphere(gridsVisulization[(int)chosenGrid.x, (int)chosenGrid.y].transform.position,
                    2f);
            }
        }
    }
}