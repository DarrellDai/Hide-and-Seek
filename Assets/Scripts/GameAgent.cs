using System;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Base class for game agents
/// </summary>
public class GameAgent : Agent
{
    //Set input for players
    public InputAction moveInput;
    public InputAction dirInput;

    //Check if a player collide with rocks
    [HideInInspector] public bool isCollided;

    //Check if a player collide is out of bound
    [HideInInspector] public bool isOut;

    [HideInInspector] public float mapSize;

    //Player's parameter
    public float moveSpeed = 0.5f;
    public float rotateSpeed = 200f;

    //The order of this in Player Spawner
    [HideInInspector] public int orderOfPlayer;

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
        //Set isCollided = true when agent collides a rock
        if (collision.gameObject.CompareTag("Rock")) isCollided = true;

        if (collision.gameObject.CompareTag("Seeker") && gameObject.CompareTag("Hider"))
        {
            Debug.Log("Player" + orderOfPlayer + " is caught by Player" +
                      collision.gameObject.GetComponent<GameAgent>().orderOfPlayer);
            //Add reward when get caught as a hider
            AddReward(-100);
            hiderDestroyFlag = true;
            alive = false;

            //Turn its camera to black when a hider is caught 
            var camera = transform.Find("Eye").Find("Camera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.cullingMask = 0;
        }

        if (collision.gameObject.CompareTag("Hider") && gameObject.CompareTag("Seeker"))
            //Add reward when catch a hider
            AddReward(100);
    }

    private void OnCollisionExit(Collision collision)
    {
        //Set isCollided = false when agent exit collision from a rock
        if (collision.gameObject.CompareTag("Rock")) isCollided = false;
    }

    /// <summary>
    ///     Initialize ML-agent.
    /// </summary>
    public override void Initialize()
    {
        playerSpawner = FindObjectOfType<PlayerSpawner>();
        orderOfPlayer = playerSpawner.playerIndex;
        Debug.Log("Player: " + orderOfPlayer + " is created");
        //Enable inputs
        moveInput.Enable();
        dirInput.Enable();

        //Get map size
        var terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        mapSize = terrainAndRockSetting.CalculateMapSize() / 2;

        //Ignore collision between same agents
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Seeker"), LayerMask.NameToLayer("Seeker"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hider"), LayerMask.NameToLayer("Hider"));


        //Set the MaxStep as 5000 in training mode, 0 (inf) in inference mode
        MaxStep = trainingMode ? 5000 : 0;

        //playerSpawner.RelocatePlayer(transform);
        Debug.Log("Player: " + orderOfPlayer + " is relocated");
        step = 1;
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
        Debug.Log("Player: " + orderOfPlayer + " begins");
        alive = true;
        gameObject.transform.GetChild(0).gameObject.SetActive(true);
        gameObject.transform.GetChild(1).gameObject.SetActive(true);
        PlayerSpawner.ResetCamera(gameObject.transform);
        playerSpawner.RelocatePlayer(gameObject.transform);
        Debug.Log("Player: " + orderOfPlayer + " is relocated");
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
        if (gameObject.CompareTag("Seeker")) AddReward(-0.01f * step);

        //Add reward for surviving each step
        if (gameObject.CompareTag("Hider") && alive)
            AddReward(0.01f * step);
        step++;
    }

    /// <summary>
    ///     Update agent's status when action is received.
    /// </summary>
    /// <param name="actionBuffers"></param>
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (alive)
            MoveAgent(actionBuffers.DiscreteActions);

        //Destroy hiders when caught
        if (gameObject.CompareTag("Hider") && hiderDestroyFlag)
        {
            Debug.Log("destory");
            hiderDestroyFlag = false;
            gameObject.transform.GetChild(0).gameObject.SetActive(false);
            gameObject.transform.GetChild(1).gameObject.SetActive(false);
        }

        //End episode when all hiders are caught
        if (PlayerSpawner.CountActiveNumHider(transform.parent.gameObject) == 0)
        {
            Debug.Log("Player: " + orderOfPlayer + "alive: " + alive);
            EndEpisode();
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
        CheckIfOut();
        //If isOut or isCollided = true, set flag = true, so agent can go back to previous status on the next step.
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
        rotateDir = transform.up * act[1];
        transform.Rotate(rotateDir, Time.deltaTime * rotateSpeed);
        transform.position += dirToGo * moveSpeed;
        /*GetComponent<PlaceObjectsToSurface>().StartPlacing();*/
        if (act[0] != 0)
        {
            GetComponent<PlaceObjectsToSurface>().StartPlacing();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.black;
        Gizmos.DrawSphere(transform.position, 0.1f);
    }
    /*public virtual void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        var flag = false;
        CheckIfOut();
        //If isOut or isCollided = true, set flag = true, so agent can go back to previous status on the next step.
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
        //GetComponent<Rigidbody>().AddForce(dirToGo,ForceMode.Acceleration);

    }*/

    /// <summary>
    ///     Check if agent is out of bound.
    /// </summary>
    public void CheckIfOut() 
    {
        Vector2 playerSize = GetComponent<Collider>().bounds.extents;
        if ((transform.position.x - playerSize.x < -mapSize) | (transform.position.x + playerSize.x > mapSize) |
            (transform.position.y - playerSize.y < -mapSize) | (transform.position.y + playerSize.y > mapSize))
            isOut = true;
        else
            isOut = false;
    }
}