using System;
using Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
///     Create customized editor in inspector
/// </summary>
[CustomEditor(typeof(PlayerSpawner))]
public class PlayerSpawnerEditor : UnityEditor.Editor
{
    private PlayerSpawnerForEditor playerSpawnerForEditor;
    private void OnEnable()
    {
        playerSpawnerForEditor = CreateInstance<PlayerSpawnerForEditor>(); 
    }

    public override void OnInspectorGUI()
    {
        

        //Update map once anything changes in the editor if autoUpdate = true
        if (DrawDefaultInspector())
            if (playerSpawnerForEditor.autoUpdate)
            {
                playerSpawnerForEditor.Initialize();
                playerSpawnerForEditor.DestoryChildren();
                playerSpawnerForEditor.StartSpawning();
            }

        //Update map once "Generate" button is pressed in the editor 
        if (GUILayout.Button("Generate"))
        {
            playerSpawnerForEditor.Initialize();
            playerSpawnerForEditor.DestoryChildren();
            playerSpawnerForEditor.StartSpawning();
        }
    }
}