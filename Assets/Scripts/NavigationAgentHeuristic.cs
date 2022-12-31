using System;
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

public class NavigationAgentHeuristic : NavigationAgent
{
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        if (Mouse.current.leftButton.isPressed)
        {
            Ray ray = Camera.main.ScreenPointToRay(
                new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, 0)); 
            if (Physics.Raycast(ray, out RaycastHit hit))
            {

                var selectedPosition = GetGridFromPosition(hit.point);
                discreteActionsOut[0] = (int)selectedPosition.x;
                discreteActionsOut[1] = (int)selectedPosition.y;
            }
        }
        else
        {
            discreteActionsOut[0] = lastAction[0];
            discreteActionsOut[1] = lastAction[1];
        }
    }
}