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
        TerrainAndRockSetting terrainAndRockSetting = (TerrainAndRockSetting)target;
        //Update map once anything changes in the editor if autoUpdate = true
        if (DrawDefaultInspector())
            if (terrainAndRockSetting.autoUpdate)
            {
                terrainAndRockSetting.DrawMapInEditor();
            }
        //Update map once "Generate" button is pressed in the editor 
        if (GUILayout.Button("Generate"))
        {
            terrainAndRockSetting.DrawMapInEditor();
        }

    }



}
