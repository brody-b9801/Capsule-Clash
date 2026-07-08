#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class LightProblePlacerEditor : EditorWindow
{
    float spacingX = 5f;
    float spacingZ = 5f;
    float heightOffset = 1f;
    float areaWidth = 100f;
    float areaDepth = 100f;
    Vector3 centerPosition = Vector3.zero;

    [MenuItem("Tools/Light Probe Placer")]
    static void Open() => GetWindow<LightProblePlacerEditor>("Light Probe Placer");

    void OnGUI()
    {
        GUILayout.Label("Light Probe Grid Placer", EditorStyles.boldLabel);
        centerPosition = EditorGUILayout.Vector3Field("Center Position", centerPosition);
        areaWidth = EditorGUILayout.FloatField("Area Width", areaWidth);
        areaDepth = EditorGUILayout.FloatField("Area Depth", areaDepth);
        spacingX = EditorGUILayout.FloatField("Spacing X", spacingX);
        spacingZ = EditorGUILayout.FloatField("Spacing Z", spacingZ);
        heightOffset = EditorGUILayout.FloatField("Height Offset", heightOffset);

        if (GUILayout.Button("Place Probes"))
            PlaceProbes();
    }

    void PlaceProbes()
    {
        GameObject probeObj = new GameObject("Light Probe Group");
        LightProbeGroup group = probeObj.AddComponent<LightProbeGroup>();

        var positions = new System.Collections.Generic.List<Vector3>();

        for (float x = -areaWidth / 2; x <= areaWidth / 2; x += spacingX)
        {
            for (float z = -areaDepth / 2; z <= areaDepth / 2; z += spacingZ)
            {
                Vector3 pos = new Vector3(centerPosition.x + x, centerPosition.y, centerPosition.z + z);

                if (Physics.Raycast(pos + Vector3.up * 100f, Vector3.down, out RaycastHit hit, 200f))
                    pos.y = hit.point.y + heightOffset;
                else
                    pos.y = centerPosition.y + heightOffset;

                positions.Add(pos);
            }
        }

        group.probePositions = positions.ToArray();
        Debug.Log($"Placed {positions.Count} light probes");
    }
}
#endif
