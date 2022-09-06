using Unity.Mathematics;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.InputSystem;

/// <summary>
/// Base class for game agents
/// </summary>
public class GameAgent : Agent
{
    //Set input for players
    public InputAction moveInput;
    public InputAction dirInput;
    
    //Check if a player collide with rocks
    [HideInInspector] public bool isCollided = false;
    //Check if a player collide is out of bound
    [HideInInspector] public bool isOut = false;
    //Player's destinationPosition and rotation on the last step
    private Vector3 lastPosition;
    private Quaternion lastRotation;
    
    [HideInInspector] public float mapSize;
    
    //Player's parameter
    public float moveSpeed = 0.5f;
    public float rotateSpeed = 200f;
    
    //Player spawner as the parent of all players
    private PlayerSpawner playerSpawner;
    
    //The order of this in Player Spawner
    [HideInInspector] public int orderOfPlayer;
    
    //If in training or inference mode
    public bool trainingMode;
    
    //If true, destroy the hider on the next step 
    private bool hiderDestroyFlag;
    /// <summary>
    /// Initialize ML-agent.
    /// </summary>
    public override void Initialize()
    {
        //Enable inputs
        moveInput.Enable();
        dirInput.Enable();

        //Get map size
        TerrainAndRockSetting terrainAndRockSetting = FindObjectOfType<TerrainAndRockSetting>();
        mapSize = terrainAndRockSetting.CalculateMapSize() / 2;

        //Ignore collision between same agents
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Seeker"), LayerMask.NameToLayer("Seeker"));
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Hider"), LayerMask.NameToLayer("Hider"));

        playerSpawner = FindObjectOfType<PlayerSpawner>();
        
        //Set the MaxStep as 5000 in training mode, 0 (inf) in inference mode
        MaxStep = trainingMode ? 5000 : 0;
    }

    /// <summary>
    /// Move agent by control.
    /// </summary>
    /// <param name="act"></param>
    public virtual void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector3.zero;
        var rotateDir = Vector3.zero;
        bool flag = false;
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
        transform.position += dirToGo * moveSpeed;
        if (act[0] != 0)
        {
            GetComponent<PlaceObjectsToSurface>().StartPlacing();
        }
    }
    /// <summary>
    /// Check if agent is out of bound.
    /// </summary>
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
        //Destroy hiders when caught
        if (gameObject.CompareTag("Hider") && hiderDestroyFlag)
        {
            Destroy(gameObject); 
        }
        //Add reward for surviving each step
        else if (gameObject.CompareTag("Hider") && !hiderDestroyFlag)
            AddReward(0.001f);
        MoveAgent(actionBuffers.DiscreteActions);
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
    /*/// <summary>
    /// Initialize player when episode begins
    /// </summary>
    public override void OnEpisodeBegin() 
    {
        RelocatePlayer();
        GetComponent<Rigidbody>().velocity=Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity=Vector3.zero;
    }*/

    /// <summary>
    /// Collect obsevrations
    /// </summary>
    /// <param name="sensor"></param>
    /*public override void CollectObservations(VectorSensor sensor)
    {
        
    }*/
    
    public void OnCollisionEnter(Collision collision)
    {
        //Set isCollided = true when agent collides a rock
        if (collision.gameObject.CompareTag("Rock"))
        {
            isCollided = true;
        }
        //End episode if self is a hider and get caught 
        if (collision.gameObject.CompareTag("Seeker") && gameObject.CompareTag("Hider"))
        {
            Debug.Log("Player"+orderOfPlayer+" is caught");
            //Add reward when get caught as a hider
            AddReward(-1);
            hiderDestroyFlag = true;
            
            //Turn its camera to black when a hider is caught 
            Camera camera = transform.Find("Eye").Find("Camera").GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor=Color.black;
            camera.cullingMask = 0;
            
            EndEpisode();

        }
        if (collision.gameObject.CompareTag("Hider") && gameObject.CompareTag("Seeker"))
        {
            //Add reward when catch a hider as a seeker
            AddReward(1);
            
            //End seeker's episode when all hiders are caught
            if (PlayerSpawner.CountNumHider(transform.parent.gameObject) == 0)
            {
                Debug.Log("episode ends");
                EndEpisode();
            }
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
    
    /*/// <summary>
    /// Place the player to a random destinationPosition
    /// </summary>
    void RelocatePlayer()
    {
        transform.rotation=quaternion.identity;
        playerSpawner.FindRandPosition();
        transform.destinationPosition = playerSpawner.randPosition;
        GetComponent<PlaceObjectsToSurface>().StartPlacing();
    }*/
    
    /// <summary>
    /// Disable inputs when agent is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        moveInput.Disable();
        dirInput.Disable(); 
    }
}