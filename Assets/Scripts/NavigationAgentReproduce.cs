using System.IO;
using System.Linq;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using UnityEngine;

public class NavigationAgentReproduce : NavigationAgent
{
    private int stepIdx;
    public string actionLogPath;
    public string positionLogPath;
    private Vector2Int[] actions;
    private Vector3[] positions;
    private int decisionPeriod;

    public override void Initialize()
    {
        
        var actionList = File.ReadLines(actionLogPath).ToList();
        actions = new Vector2Int[actionList.Count];
        for (int i = 0; i < actionList.Count; i++)
        {
            var actionString = actionList[i].Split(",");
            actions[i] = new Vector2Int((int)float.Parse(actionString[0]), (int)float.Parse(actionString[1])); 
        }
        var positionList = File.ReadLines(positionLogPath).ToList();
        positions = new Vector3[positionList.Count];
        for (int i = 0; i < positionList.Count; i++)
        {
            var positionString = positionList[i].Split(",");
            positions[i] = new Vector3(float.Parse(positionString[0]), float.Parse(positionString[1]), float.Parse(positionString[2]));
        }

        decisionPeriod = GetComponent<DecisionRequester>().DecisionPeriod;  
        transform.position = positions[0];
        base.Initialize(); 
        
        
    }

    public override void OnEpisodeBegin()
    {
        base.OnEpisodeBegin();
        stepIdx = 1;
        
        
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (stepIdx == actions.Length * decisionPeriod)
        {
            EndEpisode();
            return;
        }
        var discreteActionsOut = actionBuffers.DiscreteActions;
        discreteActionsOut[0]= actions[Mathf.FloorToInt(stepIdx/decisionPeriod)][0];
        discreteActionsOut[1] = actions[stepIdx/decisionPeriod][1];
        stepIdx++;
        base.OnActionReceived(actionBuffers);
    }
}