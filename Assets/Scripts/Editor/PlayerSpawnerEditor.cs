using Editor;
using UnityEditor;
using UnityEngine;

/// <summary>
///     Create customized editor in inspector
/// </summary>
[CustomEditor(typeof(PlayerSpawner))]
public class PlayerSpawnerEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var playerSpawnerForEditor =
            CreateInstance<PlayerSpawnerForEditor>();

        //Update map once anything changes in the editor if autoUpdate = true
        if (DrawDefaultInspector())
            if (playerSpawnerForEditor.autoUpdate)
            {
                playerSpawnerForEditor.DestoryChildren();
                playerSpawnerForEditor.StartSpawning();
            }

        //Update map once "Generate" button is pressed in the editor 
        if (GUILayout.Button("Generate"))
        {
            playerSpawnerForEditor.DestoryChildren();
            playerSpawnerForEditor.StartSpawning();
        }
    }
}