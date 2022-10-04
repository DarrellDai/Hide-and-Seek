using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using UnityEngine;
using UnityEngine.AI;

public class TargetDetectingAgent : NavigationAgent
{
    //Minimal number of steps before updating destination to prevent getting stuck 
    public int minDestinationUpdateStep = 5;

    private int count;

    //Current number of steps since last destination update
    private int destinationUpdateStepCount;
    private FieldOfView fieldOfView;

    private RaycastHit hitInfo;

    //Mesh of fieldOfView
    private Mesh mesh;
    private List<Ray> rays;

    /// <summary>
    ///     Initialize ML-agent.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();

        //Initialize field of view
        fieldOfView = FindObjectOfType<FieldOfView>();
        fieldOfView.InitializeParameters();
        fieldOfView.InitializeMesh();

        path = new NavMeshPath();
    }

    /// <summary>
    ///     Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MakeDetection();
        //Set the destinationPosition of destination to a detected hider if any
        if (destinationUpdateStepCount == minDestinationUpdateStep)
        {
            if (fieldOfView.isDetected)
            {
                destinationPosition = fieldOfView.detectPosition;
                MakeNewDestination();
                fieldOfView.isDetected = false;
            }

            destinationUpdateStepCount = 0;
        }

        base.OnActionReceived(actionBuffers);
        destinationUpdateStepCount++;
    }

    /// <summary>
    ///     Perform detection within field of view.
    /// </summary>
    public void MakeDetection()
    {
        fieldOfView.CalculateFieldOfView();
        if (fieldOfView.drawFieldOfView)
            fieldOfView.DrawFieldOfView();
    }
    /*/// <summary>
    /// Highlight the seeker in sphere and destination in box with red color 
    /// </summary>
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.destinationPosition,2f);
        if (destination!=null)
            Gizmos.DrawWireCube(destination.transform.destinationPosition, Vector3.one*2f); 
    }*/
}