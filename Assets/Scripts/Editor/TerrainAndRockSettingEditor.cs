using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Create customized editor in inspector 
/// </summary>
[CustomEditor(typeof(TerrainAndRockSetting))]
public class TerrainAndRockSettingEditor : Editor 
{
    public override void OnInspectorGUI()
    {
        TerrainAndRockSettingForEditor terrainAndRockSettingForEditor =
            CreateInstance<TerrainAndRockSettingForEditor>();
        
        //Update map once anything changes in the editor if autoUpdate = true
        if (DrawDefaultInspector())
        {
            terrainAndRockSettingForEditor.GetValue();
            if (terrainAndRockSettingForEditor.autoUpdate)
            {
                terrainAndRockSettingForEditor.DrawMapInEditor(); 
            }
        }
        //Update map once "Generate" button is pressed in the editor 
        if (GUILayout.Button("Generate"))
        {
            terrainAndRockSettingForEditor.GetValue();
            terrainAndRockSettingForEditor.DrawMapInEditor();
        }

    }



}
