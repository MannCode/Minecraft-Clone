using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshGenerator))]
public class MeshGeneratorScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MeshGenerator meshGenerator = (MeshGenerator)target;

        meshGenerator.chunkContainer = (GameObject)EditorGUILayout.ObjectField("ChunkContainer", meshGenerator.chunkContainer, typeof(GameObject), true);
        meshGenerator.player = (GameObject)EditorGUILayout.ObjectField("Player", meshGenerator.player, typeof(GameObject), true);

        meshGenerator.material = (Material)EditorGUILayout.ObjectField("Material", meshGenerator.material, typeof(Material), true);
        meshGenerator.waterMaterial = (Material)EditorGUILayout.ObjectField("Water Material", meshGenerator.waterMaterial, typeof(Material), true);

        // meshGenerator.texture = (Texture2D)EditorGUILayout.ObjectField("Texture", meshGenerator.texture, typeof(Texture2D), true);

        meshGenerator.c_size = EditorGUILayout.IntField("Chunk Size", meshGenerator.c_size);
        meshGenerator.c_height = EditorGUILayout.IntField("Chunk Height", meshGenerator.c_height);
        meshGenerator.size = EditorGUILayout.IntField("Size", meshGenerator.size);

        meshGenerator.amplitude = EditorGUILayout.FloatField("Amplitude", meshGenerator.amplitude);
        meshGenerator.frequency = EditorGUILayout.FloatField("Frequency", meshGenerator.frequency);
        meshGenerator.squashingFactor = EditorGUILayout.FloatField("Squashing Factor", meshGenerator.squashingFactor);
        meshGenerator.heightOffset = EditorGUILayout.FloatField("Height Offset", meshGenerator.heightOffset);
        meshGenerator.octaves = EditorGUILayout.IntField("Octaves", meshGenerator.octaves);
        meshGenerator.seed = EditorGUILayout.IntField("Seed", meshGenerator.seed);

        meshGenerator.heightCurve = EditorGUILayout.CurveField("Height Curve", meshGenerator.heightCurve);

        // button to reinitialize the chunks
        if (GUILayout.Button("Reinitialize Chunks"))
        {
            //if in play mode
            if(Application.isPlaying) {
                meshGenerator.reintialize();
            }
        }
    }
}
