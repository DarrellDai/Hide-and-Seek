using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Mathematics;
using Unity.MLAgents.Actuators;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

public class TargetDetectingAgent : NavigationAgent
{
    private List<Ray> rays;
    private RaycastHit hitInfo;
    private LayerMask hiderLayer;
    private int count;
    //Mesh of fieldOfView
    private Mesh mesh;
    private FieldOfView fieldOfView;
    
    
    /// <summary>
    /// Initialize ML-agent.
    /// </summary>
    public override void Initialize() 
    {
        base.Initialize();
        /*//Enable inputs
        moveInput.Enable();
        dirInput.Enable();
        
        TerrainAndRockSetting mapGenerator = FindObjectOfType<TerrainAndRockSetting>();
        mapSize = mapGenerator.CalculateMapSize()/2-PlayerSpawner.distanceFromBound;
        destinationSpawner = GameObject.Find("DestinationSpawner");
        //The box size the destination needs to be away from rocks
        overlapTestBoxSize = GetComponent<Collider>().bounds.extents + Vector3.one*extraOffset;
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        //Turn off auto-pilot in NavMeshAgent so the agent can move manually
        navMeshAgent.updatePosition = false;
        //Set follow-up camera
        vcam = GameObject.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>();
        vcam.LookAt = transform;
        vcam.Follow = transform;*/
        
        //Initialize field of view
        fieldOfView = FindObjectOfType<FieldOfView>();
        fieldOfView.InitializeParameters();
        fieldOfView.InitializeMesh(); 
        
        path = new NavMeshPath();

    }
    
    /// <summary>
    /// Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MakeDetection();
        //Set the position of destination to a detected hider if any
        if (fieldOfView.isDetected)
        {
            position = fieldOfView.detectPosition;
            MakeNewDestination();
            fieldOfView.isDetected = false;
        }
        
        //To choose a new destination if toChooseNextDestination=true
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
    /// Perform detection within field of view.
    /// </summary>
    public void MakeDetection()
    {
        fieldOfView.CalculateFieldOfView();
        if (fieldOfView.drawFieldOfView)
            fieldOfView.DrawFieldOfView();
    }

    /// <summary>
    /// Highlight the seeker in sphere and destination in box with red color 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.position, Vector3.one*2f); 
    }
}
