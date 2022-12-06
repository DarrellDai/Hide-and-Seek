using System;
using System.Collections.Generic;
using Unity.MLAgents.Actuators;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class TargetDetectingAgent : RandomNavigationAgent
{
    private int count;

    //Current number of steps since last destination update
    private FieldOfView fieldOfView;
    
    GameObject[] hiders;
    private Renderer[] renderers;

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
        topDownView = false;
        //Initialize field of view
        fieldOfView = GetComponent<FieldOfView>();
        fieldOfView.isDetected = false;
        hiders = GameObject.FindGameObjectsWithTag("Hider");
        renderers = new Renderer[hiders.Length];
        
        for (int i = 0; i < hiders.Length; i++)
        {
            renderers[i] = hiders[i].transform.Find("Body").GetComponent<Renderer>();
        }
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity; 
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
        // Prevent NavMeshAgent is not active on NavMesh issue
        var detectedRenderersByCamera=MakeDetectionByCamera();
        List<Renderer> detectedRenderers=new List<Renderer>();
        foreach (Renderer renderer in detectedRenderersByCamera)
        {
            MakeDetectionByRaycast(renderer.transform);
            if (fieldOfView.isDetected)
            {
                detectedRenderers.Add(renderer);
                renderer.transform.parent.gameObject.GetComponent<GameAgent>().detected.Add(true);
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
    public void MakeDetectionByRaycast(Transform transform)
    {
        fieldOfView.CalculateFieldOfView(transform);
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