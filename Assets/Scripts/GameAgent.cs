using System;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>
///     Base class for game agents
/// </summary>
public class GameAgent : Agent
{
    //Set input for players
    public InputAction moveInput;
    public InputAction dirInput;
    
    [HideInInspector] public float mapSize;

    //Player's parameter
    public float moveSpeed = 0.5f;
    public float rotateSpeed = 200f;

    //If in training or inference mode
    public bool trainingMode;

    //If true, meaning the agent is still activated 
    public bool alive;

    //If true, destroy the hider on the next step 
    private bool hiderDestroyFlag;

    //Player's destinationPosition and rotation on the last step
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    //Player spawner as the parent of all players
    private PlayerSpawner playerSpawner;
    

    // Step count in an episode
    private int step;

    public List<bool> detected;
    //private Color originalColor;
    
    //Steps to freeze seekers, so hiders have preparation time
    private int stepLeftToFreeze;
    /// <summary>
    ///     Disable inputs when agent is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        moveInput.Disable();
        dirInput.Disable();
    }

    public void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.CompareTag("Seeker") && gameObject.CompareTag("Hider"))
        {
            //Add reward when get caught as a hider
            hiderDestroyFlag = true;

            //Turn its camera to black when a hider is caught 
            var camera = transform.Find("Eye").Find("Camera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
        }

        if (collision.gameObject.CompareTag("Hider") && gameObject.CompareTag("Seeker"))
        {
            //Add reward when catch a hider
            AddReward(2); 
            //print("Caught");
        }
        
    }

    /// <summary>
    ///     Initialize ML-agent.
    /// </summary>
    public override void Initialize()
    {
        
        //Enable inputs
        moveInput.Enable();
        dirInput.Enable();
        
        playerSpawner = FindObjectOfType<PlayerSpawner>();
        
        //Get map size
        var terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        mapSize = terrainAndRockSetting.CalculateMapSize() / 2;

        /*//Ignore collision between same agents
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Seeker"), LayerMask.NameToLayer("Seeker"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hider"), LayerMask.NameToLayer("Hider"));*/


        //Set the MaxStep as 5000 in training mode, 0 (inf) in inference mode
        MaxStep = trainingMode ? 5000 : 0;
        
        step = 1;
        
        //originalColor=transform.Find("Body").GetComponent<Renderer>().material.color;
    }

    /// <summary>
    ///     Heuristic control, where W: go forward, S: go backward, A: turn left, D: turn right.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = (int)moveInput.ReadValue<float>();
        discreteActionsOut[1] = (int)dirInput.ReadValue<float>();
    }

    /// <summary>
    ///     Initialize player when episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        stepLeftToFreeze = playerSpawner.numStepToFreeze;
        alive = true;
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        gameObject.transform.GetChild(1).gameObject.SetActive(true);
        gameObject.layer = LayerMask.NameToLayer(gameObject.tag);
        gameObject.GetComponent<Collider>().enabled = true;
        PlayerSpawner.ResetCamera(gameObject.transform);
        playerSpawner.RelocatePlayer(gameObject.transform);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        step = 1;

    }

    /// <summary>
    ///     Collect obsevrations
    /// </summary>
    /// <param name="sensor"></param>
    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(alive);
        sensor.AddObservation(PlayerSpawner.CountActiveNumHider(transform.parent.gameObject));
        if (gameObject.CompareTag("Seeker")) AddReward(-0.01f); 

        //Add reward for surviving each step
        if (gameObject.CompareTag("Hider") && alive)
            AddReward(0.01f);
        step++;
    }

    /// <summary>
    ///     Update agent's status when action is received.
    /// </summary>
    /// <param name="actionBuffers"></param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        //transform.Find("Body").GetComponent<Renderer>().material.color = originalColor; 
        if (detected.Count > 0)
        {
            //transform.Find("Body").GetComponent<Renderer>().material.color = Color.yellow;
            detected.Clear();
        }
        if (gameObject.CompareTag("Seeker") && stepLeftToFreeze > 0)
        {
            stepLeftToFreeze--; 
            return;
        }
        
        if (alive)
            MoveAgent(actionBuffers.DiscreteActions);

        //Destroy hiders when caught
        if (gameObject.CompareTag("Hider") && hiderDestroyFlag)
        {
            AddReward(-2);
            hiderDestroyFlag = false;
            alive = false;
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
            gameObject.transform.GetChild(1).gameObject.SetActive(false);
            gameObject.GetComponent<Collider>().enabled = false;
            gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }
    }

    /// <summary>
    ///     Move agent by control.
    /// </summary>
    /// <param name="act"></param>

    public virtual void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var flag = false;
        dirToGo = transform.forward * act[0];
        rotateDir = Vector3.up * act[1];
        transform.Rotate(rotateDir, Time.deltaTime * rotateSpeed);
        GetComponent<Rigidbody>().velocity = dirToGo * moveSpeed;
        if (act[0] != 0)
        {
            GetComponent<PlaceObjectsToSurface>().StartPlacing(moveSpeed * dirToGo,true, false);
        }
    }
}