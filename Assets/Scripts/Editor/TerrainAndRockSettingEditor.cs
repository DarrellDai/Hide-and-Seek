using System;
using UnityEditor;
using UnityEngine;

/// <summary>
///     Create customized editor in inspector
/// </summary>
[CustomEditor(typeof(TerrainAndRockSetting))]
public class TerrainAndRockSettingEditor : UnityEditor.Editor
{
    private TerrainAndRockSettingForEditor terrainAndRockSettingForEditor;

    private void OnEnable()
    {
        terrainAndRockSettingForEditor =
            CreateInstance<TerrainAndRockSettingForEditor>();
        
    }

    public override void OnInspectorGUI()
    {

        //Update map once anything changes in the editor if autoUpdate = true
        if (DrawDefaultInspector())
            if (terrainAndRockSettingForEditor.autoUpdate)
            {
                terrainAndRockSettingForEditor.Initialize();
                terrainAndRockSettingForEditor.DrawMapInEditor();
            }
        //Update map once "Generate" button is pressed in the editor 
        if (GUILayout.Button("Generate"))
        {
            terrainAndRockSettingForEditor.Initialize();
            terrainAndRockSettingForEditor.DrawMapInEditor();
        }
    }
}