using System;
using Unity.Collections;
using Unity.Mathematics;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions.Must;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;
using Random = UnityEngine.Random;

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
    [HideInInspector] public bool arrived;
    public bool topDownView = true;
    public int halfNumDivisionEachSide = 4;
    public int halfRangeAsNumGrids = 3;
    private float gridSize;
    private Vector2[,] destinationSpace;
    private bool[,] destinationVisited;
    private bool[,] egocentricMask;
    private Vector2 currentGrid;
    [HideInInspector] public Vector2 sampledGrid;
    private Vector2 chosenGrid;
    private Vector2 nextGrid;
    private NavMeshPath navMeshPath;
    public Vector3 agentPosition;
    [HideInInspector] public float cameraDistance;

    [HideInInspector] public GameObject destination;
    [HideInInspector] public NavMeshAgent navMeshAgent;
    

    private bool overlap;

    //Path planned by NavMesh

    private Vector2 last2dDestination;
    private float rotation;

    /// <summary>
    ///     Initialize Navigation agent.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        navMeshPath = new NavMeshPath();
        destinationSpace = new Vector2[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        destinationVisited = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        egocentricMask = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        gridSize = mapSize / halfNumDivisionEachSide; 
        if (topDownView)
        {
            camera.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
            camera.transform.position = new Vector3(0,
                halfRangeAsNumGrids * gridSize / Mathf.Tan(camera.fieldOfView / 2 * Mathf.PI / 180) -
                GetComponent<Collider>().bounds.extents.y, 0);
            cameraDistance = camera.transform.position.y;
        }
        
        
        for (int i = 0; i < 2 * halfNumDivisionEachSide; i++)
        {
            for (int j = 0; j < 2 * halfNumDivisionEachSide; j++)
            {
                destinationSpace[i, j] = new Vector2(gridSize * (i - halfNumDivisionEachSide + 1 / 2f),
                    gridSize * (j - halfNumDivisionEachSide + 1 / 2f));
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
        //ScreenCapture.CaptureScreenshot("C:/Users/daish/Desktop/TopDown.png");
        
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        for (int i = 0; i < 2 * halfNumDivisionEachSide; i++)
        {
            for (int j = 0; j < 2 * halfNumDivisionEachSide; j++)
            {
                destinationVisited[i, j] = false;
            }
        }

        UpdateDestinationAndEgocentricMask();
        // Disable navMeshAgent so it's nextPosition won't move to cause teleport
        navMeshAgent.enabled = false;
    }

    /// <summary>
    ///     Collect observations
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        CorrectCamera();
        //camera.transform.rotation = Quaternion.Slerp(camera.transform.rotation, Quaternion.Euler(new Vector3(90, 0, 0)), Time.fixedDeltaTime * navMeshAgent.angularSpeed);
        base.CollectObservations(sensor);
        sensor.AddObservation(currentGrid);
        for (var i = 0; i < destinationVisited.GetLength(0); i++)
        {
            for (var j = 0; j < destinationVisited.GetLength(1); j++)
            {
                sensor.AddObservation(destinationVisited[i, j]);
            }
        }
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
        /*else if (!navMeshAgent.pathPending)
            if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
            {
                if (!navMeshAgent.hasPath || navMeshAgent.velocity.sqrMagnitude == 0f)
                {
                    arrived = true;
                    toChooseNextDestination = true;
                }
            }*/
    }

    /// <summary>
    ///     Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        arrived = false;
        CheckIfArrived();
        base.OnActionReceived(actionBuffers);
        UpdateDestinationAndEgocentricMask();
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
        sampledGrid = new Vector2((float)act[0], (float)act[1]);
        if (navMeshAgent.isActiveAndEnabled)
        {
            selectNextDestination();
            MakeNewDestination();

            navMeshAgent.nextPosition = transform.position;
            agentPosition = transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position,
                    out hit, 1.0f,
                    NavMesh.AllAreas))
            {
                agentPosition = hit.position;
                /*transform.position = agentPosition;
                GetComponent<PlaceObjectsToSurface>().StartPlacing(
                    navMeshAgent.velocity, false, true);*/
            }

            if (Vector3.Distance(destinationPosition, agentPosition) > 0.1f)
                GoToNextPosition();
            else
            {
                toChooseNextDestination = true;
                transform.Rotate(transform.up, act[2] * 360f / 16);
            }
        }

        CorrectCamera();
    }

    public override void CorrectCamera()
    {
        if (topDownView)
        {
            camera.transform.position = new Vector3(transform.position.x, cameraDistance, transform.position.z);
            camera.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
        }
        else
        {
            camera.transform.localPosition = transform.localPosition;
            camera.transform.localRotation = transform.localRotation; 
        }
    }

    public virtual void GoToNextPosition()
    {
        navMeshAgent.CalculatePath(destination.transform.position, navMeshPath);
        if (navMeshPath.corners.Length > 1)
        {
            var target = (navMeshPath.corners[1] - agentPosition).normalized;
            var newRot = Quaternion.LookRotation(target);
            transform.rotation =
                Quaternion.Slerp(transform.rotation,
                    newRot, Time.fixedDeltaTime * 10f);
            if ((navMeshPath.corners[1] - agentPosition).magnitude>Time.fixedDeltaTime * navMeshAgent.speed)
                transform.position += Time.fixedDeltaTime * navMeshAgent.speed * target;
            else
            {
                transform.position += (navMeshPath.corners[1] - agentPosition).magnitude * target;
            }
                /*Vector3.Lerp(transform.position,
                    transform.position + Time.fixedDeltaTime * navMeshAgent.speed * transform.forward,
                    Time.fixedDeltaTime * navMeshAgent.speed * 10f);*/

            GetComponent<PlaceObjectsToSurface>().StartPlacing(
                navMeshAgent.velocity, false, true);
        }
    }

    /// <summary>
    ///     Generate a random destinationPosition for destination.
    /// </summary>
    public void selectNextDestination()
    {
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
        //navMeshAgent.destination = destination.transform.position;
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
            }
        }

        currentGrid = new Vector2(Mathf.Floor((transform.position.x + mapSize) / gridSize),
            Mathf.Floor((transform.position.z + mapSize) / gridSize));
        for (int i = -halfRangeAsNumGrids; i < halfRangeAsNumGrids; i++)
        {
            for (int j = -halfRangeAsNumGrids; j < halfRangeAsNumGrids; j++)
            {
                if (currentGrid.x + i >= 0 && currentGrid.y + j >= 0 &&
                    currentGrid.x + i < 2 * halfNumDivisionEachSide && currentGrid.y + j < 2 * halfNumDivisionEachSide)
                {
                    destinationVisited[(int)currentGrid.x + i, (int)currentGrid.y + j] = true;
                    egocentricMask[(int)currentGrid.x + i, (int)currentGrid.y + j] = true;
                }
            }
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