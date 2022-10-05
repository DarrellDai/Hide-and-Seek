using System;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Editor
{
    [CustomEditor(typeof(FindRandomPos))]
    public class FindRandomPosEditor : UnityEditor.Editor
    {
        private FindRandomPos findRandomPos;
        private void OnEnable()
        {
            findRandomPos = (FindRandomPos)target;
            findRandomPos.Initialize();
            Random.InitState(findRandomPos.seed);
        }

        public override void OnInspectorGUI()
        {
            
            
            if (DrawDefaultInspector())
            {
                Random.InitState(findRandomPos.seed);
            }
            
            //Update map once "Generate" button is pressed in the editor 
            if (GUILayout.Button("Generate"))
            {
                findRandomPos.DoSphereCast();
            }
        }
    }
}