using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
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
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
using UnityEngine.UIElements;
using MouseButton = UnityEngine.UIElements.MouseButton;
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
    [HideInInspector] public Transform fixedCamera;
    public int halfNumDivisionEachSide = 4;
    public int halfRangeAsNumGrids = 2;
    [HideInInspector] public float gridSize;
    public Vector2[,] destinationSpace;
    public bool[,] destinationVisited;
    public bool[,] egocentricMask;
    [HideInInspector] public Vector2Int currentGrid;
    [HideInInspector] public Vector2 sampledGrid;
    [HideInInspector] public Vector2 chosenGrid;
    [HideInInspector] public Vector2 nextGrid;
    private NavMeshPath navMeshPath;
    public Vector3 agentPositionOnNavMesh;
    [HideInInspector] public float cameraDistance;

    [HideInInspector] public GameObject destination;
    [HideInInspector] public NavMeshAgent navMeshAgent;


    private bool overlap;

    //Path planned by NavMesh

    [HideInInspector] public int[] lastAction;
    private float rotation;

    /// <summary>
    ///     Initialize Navigation agent.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        navMesh = true;
        navMeshPath = new NavMeshPath();
        destinationSpace = new Vector2[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        destinationVisited = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        egocentricMask = new bool[halfNumDivisionEachSide * 2, halfNumDivisionEachSide * 2];
        gridSize = mapSize / halfNumDivisionEachSide;


        if (transform.parent.Find("FixedCamera") != null)
        {
            if (GameObject.Find("Main Camera") != null)
                GameObject.Find("Main Camera").SetActive(false);
            fixedCamera = transform.parent.Find("FixedCamera");
            fixedCamera.tag = "MainCamera";

            fixedCamera.position = new Vector3(0,
                mapSize / Mathf.Tan(fixedCamera.GetComponent<Camera>().fieldOfView / 2 * Mathf.PI / 180), 0);
            fixedCamera.GetComponent<Camera>().rect = new Rect(0f, 0f,
                (float)1 / (GameObject.Find("PlayerSpawner").transform.childCount + 1), 1f);
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
        navMeshAgent.updateRotation = false;
        navMeshAgent.updateUpAxis = false;
        navMeshAgent.enabled = false;
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Contains("speed") && float.TryParse(args[i + 1], out var speed))
            {
                navMeshAgent.speed = speed;
            }

            if (args[i].Contains("half_range_as_num_grids") &&
                int.TryParse(args[i + 1], out var inputHalfRangeAsNumGrids))
            {
                halfRangeAsNumGrids = inputHalfRangeAsNumGrids;
            }
        }

        if (topDownView)
        {
            camera.transform.rotation = Quaternion.Euler(new Vector3(90, 0, 0));
            camera.transform.position = new Vector3(0,
                (halfRangeAsNumGrids + 1 / 2f) * gridSize / Mathf.Tan(camera.fieldOfView / 2 * Mathf.PI / 180) -
                GetComponent<Collider>().bounds.extents.y, 0);
            cameraDistance = camera.transform.position.y;
        }

        /*if (CompareTag("Hider"))
        {
            var fixedCamera = transform.parent.Find("FixedCamera").GetComponent<Camera>();
            fixedCamera.rect = new Rect(0f, 0f,
                1f, 1f);
            fixedCamera.targetDisplay = 1;
            fixedCamera.cullingMask = ~(1 << LayerMask.NameToLayer("Hider") | 1 << LayerMask.NameToLayer("Seeker"));

            ScreenCapture.CaptureScreenshot("/home/darrelldai/Desktop/TopDown_Big_Map.png");
        }*/
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

        // Disable navMeshAgent so it's nextPosition won't move to cause teleport
        navMeshAgent.enabled = false;
        lastAction = new int[GetComponent<BehaviorParameters>().BrainParameters.ActionSpec.NumDiscreteActions];
    }

    /// <summary>
    ///     Collect observations
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        UpdateDestinationAndEgocentricMask();
        CorrectCamera();
        base.CollectObservations(sensor);
        
        sensor.AddObservation(currentGrid);
        sensor.AddObservation(transform.position);
        sensor.AddObservation(sampledGrid);
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

        //DrawPath();
    }


    /// <summary>
    ///     Move the agent along the path planned by NavMesh, and place the agent on and normal to surface after each move,
    ///     skip placing if it makes the agent out of bound.
    /// </summary>
    public override void MoveAgent(ActionSegment<int> act)
    {
        for (int i = 0; i < act.Length; i++)
        {
            lastAction[i] = act[i];
        }

        // Enable navMeshAgent when it's able to move
        navMeshAgent.enabled = true;
        // Prevent NavMeshAgent is not active on NavMesh issue
        sampledGrid = new Vector2((float)act[0], (float)act[1]);
        if (navMeshAgent.isActiveAndEnabled)
        {
            selectNextDestination();
            MakeNewDestination();
            PlaceAgentToClosestNavMesh();
            DecideToMoveOrSelectNextDestination();
        }

        CorrectCamera();
    }

    public void DecideToMoveOrSelectNextDestination()
    {
        if (Vector3.Distance(destinationPosition, agentPositionOnNavMesh) > 0.1f)
            GoToNextPosition();
        else
        {
            toChooseNextDestination = true;
            /*transform.Rotate(transform.up, act[2] * 360f / 16);*/
        }
    }

    public void PlaceAgentToClosestNavMesh()
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position,
                out hit, Mathf.Infinity,
                NavMesh.AllAreas))
        {
            agentPositionOnNavMesh = hit.position;
            transform.position = agentPositionOnNavMesh;
            GetComponent<PlaceObjectsToSurface>().StartPlacing(
                navMeshAgent.velocity, false, true);
            navMeshAgent.nextPosition = agentPositionOnNavMesh;
        }
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
            var target = (navMeshPath.corners[1] - agentPositionOnNavMesh).normalized;
            var newRot = Quaternion.LookRotation(target);
            transform.rotation =
                Quaternion.Slerp(transform.rotation,
                    newRot, Time.fixedDeltaTime * 10f);
            if ((navMeshPath.corners[1] - agentPositionOnNavMesh).magnitude > Time.fixedDeltaTime * navMeshAgent.speed)
                transform.position += Time.fixedDeltaTime * navMeshAgent.speed * target;
            else
            {
                transform.position += (navMeshPath.corners[1] - agentPositionOnNavMesh).magnitude * target;
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
        nextGrid = GetGridFromPosition(destinationPosition);
    }

    public Vector2 GetGridFromPosition(Vector3 position)
    {
        return new Vector2(Mathf.Floor((position.x + mapSize) / gridSize),
            Mathf.Floor((position.z + mapSize) / gridSize));
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


    public Vector3 GetPositionFromGrid(Vector2 gridIndex)
    {
        var center = new Vector3();
        var position = new Vector3(destinationSpace[(int)gridIndex.x, (int)gridIndex.y].x, 30,
            destinationSpace[(int)gridIndex.x, (int)gridIndex.y].y);
        RaycastHit hit;

        if (Physics.Raycast(position, Vector3.down, out hit, Mathf.Infinity,
                ~(1 << LayerMask.NameToLayer("Hider") | 1 << LayerMask.NameToLayer("Seeker"))))
        {
            center = hit.point;
        }
        else
        {
            throw new Exception("The grid is not on the terrain");
        }

        return center;
    }

    public virtual void UpdateDestinationAndEgocentricMask()
    {
        for (var i = 0; i < destinationVisited.GetLength(0); i++)
        {
            for (var j = 0; j < destinationVisited.GetLength(1); j++)
            {
                egocentricMask[i, j] = false;
            }
        }

        currentGrid = new Vector2Int((int)Mathf.Floor((transform.position.x + mapSize) / gridSize),
            (int)Mathf.Floor((transform.position.z + mapSize) / gridSize));
        for (int i = -halfRangeAsNumGrids; i <= halfRangeAsNumGrids; i++)
        {
            for (int j = -halfRangeAsNumGrids; j <= halfRangeAsNumGrids; j++)
            {
                if (currentGrid.x + i >= 0 && currentGrid.y + j >= 0 &&
                    currentGrid.x + i < 2 * halfNumDivisionEachSide && currentGrid.y + j < 2 * halfNumDivisionEachSide)
                {
                    destinationVisited[currentGrid.x + i, currentGrid.y + j] = true;
                    egocentricMask[currentGrid.x + i, currentGrid.y + j] = true;
                }
            }
        }
    }


    /*/// <summary>
    /// Highlight the hider in sphere and destination in box with green color 
    /// </summary>
    public void OnDrawGizmos()
    {
        if (Application.isPlaying && CompareTag("Seeker"))
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetPositionFromGrid(sampledGrid), 2f);
            if (destination != null)
                Gizmos.DrawWireCube(destination.transform.position, Vector3.one * 2f);
        }
    }*/
}