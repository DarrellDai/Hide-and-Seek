using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class AgentManager : MonoBehaviour
{
    public bool ifEndEpisode;
    private PlayerSpawner playerSpawner;
    private Transform[] players;
    private void Awake()
    {
        playerSpawner = FindObjectOfType<PlayerSpawner>();
        players = new Transform[playerSpawner.playerSpawner.transform.childCount];
        for (var i = 0; i < playerSpawner.playerSpawner.transform.childCount; i++)
        {
            players[i] = playerSpawner.playerSpawner.transform.GetChild(i);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (PlayerSpawner.CountActiveNumHider(playerSpawner.playerSpawner)==0)
        {
            for (var i = 0; i < players.Length; i++)
            {
                players[i].GetComponent<GameAgent>().EndEpisode(); 
            }
        }
    }
    
}
