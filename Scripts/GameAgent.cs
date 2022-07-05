using System;
using System.Linq;
using Cinemachine;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEditor.UIElements;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = System.Object;

/// <summary>
/// Base class for game agents
/// </summary>
public class GameAgent : Agent
{
    public InputAction moveInput;
    public InputAction dirInput;
    [HideInInspector] public bool isCollided = false;
    [HideInInspector] public bool isOut = false;
    [HideInInspector] public Vector3 lastPosition;
    private Quaternion lastRotation;
    [HideInInspector] public float mapSize;
    public float moveSpeed = 0.5f;
    public float rotateSpeed = 200f;
    [HideInInspector] public Cinemachine.CinemachineVirtualCamera vcam;
    
    /// <summary>
    /// Initialize ML-agent.
    /// </summary>
    public override void Initialize()
    {
        //Enable inputs
        moveInput.Enable();
        dirInput.Enable();

        TerrainAndRockSetting terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        mapSize = terrainAndRockSetting.CalculateMapSize() / 2;

        //Ignore collision between same agents
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Seeker"), LayerMask.NameToLayer("Seeker"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hider"), LayerMask.NameToLayer("Hider"));

        //Set follow-up camera
        vcam = GameObject.Find("CM vcam1").GetComponent<CinemachineVirtualCamera>();
        vcam.LookAt = transform;
        vcam.Follow = transform;
    }

    /// <summary>
    /// Move agent by control.
    /// </summary>
    /// <param name="act"></param>
    public void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        bool flag = false;
        CheckIfOut();
        //If isOut or isCollided = true, set flag = true, so agent can go back next time.
        if (isOut | isCollided)
        {
            transform.position = lastPosition;
            transform.rotation = lastRotation;
            flag = true;
        }
        else
        {
            lastPosition = transform.position;
            lastRotation = transform.rotation;
        }
        if (flag)
        {
            act[0] = 0;
            act[1] = 0;
        }

        dirToGo = transform.forward * act[0];
        rotateDir = Vector3.up * act[1];
        transform.Rotate(rotateDir, Time.deltaTime * rotateSpeed);
        transform.position += dirToGo * moveSpeed;
        if (act[0] != 0)
        {
            GetComponent<PlaceObjectsToSurface>().StartPlacing();
        }
    }
    //Check if agent is out of bound
    public void CheckIfOut()
    {
        Vector2 playerSize = GetComponent<Collider>().bounds.extents;
        if (transform.position.x - playerSize.x < -mapSize | transform.position.x + playerSize.x > mapSize |
            transform.position.y - playerSize.y < -mapSize | transform.position.y + playerSize.y > mapSize)
        {
            isOut = true;
        }
        else
        {
            isOut = false;
        }
    }
    /// <summary>
    /// Update agent's status when action is received.
    /// </summary>
    /// <param name="actionBuffers"></param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        MoveAgent(actionBuffers.DiscreteActions);
        if (CountNumHider()==0)
            EndEpisode();
    }

    public int CountNumHider()
    {
        if (transform.parent.childCount == 0)
            return 0;
        else
        {
            int numHider = 0;
            for (int i=0; i<transform.parent.childCount; i++)
            {
                if (transform.GetChild(i).tag == "Hider")
                    numHider++;
            }

            return numHider;
        }
    }
    /// <summary>
    /// Heuristic control, where W: go forward, S: go backward, A: turn left, D: turn right.
    /// </summary>
    /// <param name="actionsOut"></param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut[0] = (int)moveInput.ReadValue<float>();
        discreteActionsOut[1] = (int)dirInput.ReadValue<float>();
    }

    public override void OnEpisodeBegin()
    {
        var enumerable = Enumerable.Range(0, 9).OrderBy(x => Guid.NewGuid()).Take(9);
        var items = enumerable.ToArray();
    }
    
    
    public void OnCollisionEnter(Collision collision)
    {
        //Set isCollided = true when agent collides a rock
        if (collision.gameObject.CompareTag("Rock"))
        {
            isCollided = true;
        }
        //Destroy the hider when it gets caught
        if (collision.gameObject.CompareTag("Hider") && gameObject.CompareTag("Seeker"))
        {
            Destroy(collision.gameObject);
            
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        //Set isCollided = false when agent exit collision from a rock
        if (collision.gameObject.CompareTag("Rock"))
        {
            isCollided = false;
        }
    }
    
    /// <summary>
    /// Disable inputs when agent is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        moveInput.Disable();
        dirInput.Disable();
        if (gameObject.tag == "Hider")
            Debug.Log("Hider Found");
    }
}