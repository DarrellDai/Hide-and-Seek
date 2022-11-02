using System;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.AI;

public class TargetDetectingAgent : NavigationAgent
{
    private int count;

    //Current number of steps since last destination update
    private FieldOfView fieldOfView;

    Camera camera;
    GameObject[] hiders;
    private Renderer[] renderers;

    private RaycastHit hitInfo;

    //Mesh of fieldOfView
    private Mesh mesh;
    private List<Ray> rays;
    
    private Color originalColor;
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

        camera = transform.Find("Eye").Find("Camera").GetComponent<Camera>();
        hiders = GameObject.FindGameObjectsWithTag("Hider");
        renderers = new Renderer[hiders.Length];
        
        for (int i = 0; i < hiders.Length; i++)
        {
            renderers[i] = hiders[i].transform.Find("Body").GetComponent<Renderer>();
        }

        originalColor = renderers[0].material.color;
        path = new NavMeshPath();
    }

    /// <summary>
    ///     Update when action received.
    /// </summary>
    /// <param name="actionBuffers">Buffers storing actions in real time</param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        /*foreach (Renderer renderer in renderers)
            renderer.material.color = originalColor;*/
        //Set the destinationPosition of destination to a detected hider if any
        MakeDetectionByRaycast();
        // Prevent NavMeshAgent is not active on NavMesh issue
        var detectedRenderersByCamera=MakeDetectionByCamera();
        List<Renderer> detectedRenderers=new List<Renderer>();
        foreach (Renderer renderer in detectedRenderersByCamera)
        {
            if (fieldOfView.detectedRenderers.Contains(renderer))
            {
                detectedRenderers.Add(renderer);
                /*renderer.material.color = Color.yellow;*/
            }

        }
        // Find the closest detected renderer
        Renderer detectedRenderer = null;
        if (detectedRenderers.Count > 0)
        {
            float minDistance=Single.PositiveInfinity;
            foreach (Renderer renderer in detectedRenderers)
            {
                float distance=Vector3.Distance(renderer.transform.position, transform.position);
                if (distance < minDistance)
                {
                    detectedRenderer = renderer;
                    minDistance = distance;
                }
            }
        }
        if (detectedRenderer!=null)
        {
            destinationPosition = detectedRenderer.transform.position;
        }
        if (navMeshAgent.isActiveAndEnabled)
        {
            MakeNewDestination();
        }
        
        base.OnActionReceived(actionBuffers);
    }
    
    /// <summary>
    /// Detect hider's from camera view, but can't take into account occlusion 
    /// </summary>
    /// <returns></returns>
    public List<Renderer> MakeDetectionByCamera()
    {
        List<Renderer> detectedRenderersByCamera=new List<Renderer>();
        for (int i = 0; i < hiders.Length; i++)
        {
            if (CameraDetection.IsVisibleFrom(renderers[i], camera))
            {
                detectedRenderersByCamera.Add(renderers[i]);
            }
        }
        return detectedRenderersByCamera;
    }
    
    /// <summary>
    ///     Perform detection within field of view.
    /// </summary>
    public void MakeDetectionByRaycast()
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
        Gizmos.DrawWireSphere(destinationPosition, 2f);
        Gizmos.color = Color.green;
        if (destination != null)
            Gizmos.DrawWireCube(destinationPosition, Vector3.one * 2f);
    }*/
}