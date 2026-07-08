#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ClosedMazeGenerator))]
public class ClosedMazeGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ClosedMazeGenerator gen = (ClosedMazeGenerator)target;

        if (GUILayout.Button("Generate Maze"))
        {
            gen.Generate();
        }
    }
}
#endif