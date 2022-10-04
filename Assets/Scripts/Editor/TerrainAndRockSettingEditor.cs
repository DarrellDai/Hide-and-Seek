using UnityEditor;
using UnityEngine;

/// <summary>
///     Create customized editor in inspector
/// </summary>
[CustomEditor(typeof(TerrainAndRockSetting))]
public class TerrainAndRockSettingEditor : UnityEditor.Editor
{
    public override void OnInspectorGUI()
    {
        var terrainAndRockSettingForEditor =
            CreateInstance<TerrainAndRockSettingForEditor>();

        //Update map once anything changes in the editor if autoUpdate = true
        if (DrawDefaultInspector())
            if (terrainAndRockSettingForEditor.autoUpdate)
                terrainAndRockSettingForEditor.DrawMapInEditor();
        //Update map once "Generate" button is pressed in the editor 
        if (GUILayout.Button("Generate")) terrainAndRockSettingForEditor.DrawMapInEditor();
    }
}