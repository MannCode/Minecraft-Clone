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
        meshGenerator.loadingScreen = (UnityEngine.UI.Image)EditorGUILayout.ObjectField("Loading Screen", meshGenerator.loadingScreen, typeof(UnityEngine.UI.Image), true);

        meshGenerator.material = (Material)EditorGUILayout.ObjectField("Material", meshGenerator.material, typeof(Material), true);
        meshGenerator.waterMaterial = (Material)EditorGUILayout.ObjectField("Water Material", meshGenerator.waterMaterial, typeof(Material), true);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("World Parameters", EditorStyles.boldLabel);
        meshGenerator.c_size = EditorGUILayout.IntField("Chunk Size", meshGenerator.c_size);
        meshGenerator.c_height = EditorGUILayout.IntField("Chunk Height", meshGenerator.c_height);
        meshGenerator.size = EditorGUILayout.IntField("Size", meshGenerator.size);
        meshGenerator.randomSeed = EditorGUILayout.Toggle("Random Seed", meshGenerator.randomSeed);
        meshGenerator.seed = EditorGUILayout.IntField("Seed", meshGenerator.seed);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Terrain Noise Parameters", EditorStyles.boldLabel);
        meshGenerator.amplitude = EditorGUILayout.FloatField("Amplitude", meshGenerator.amplitude);
        meshGenerator.frequency = EditorGUILayout.FloatField("Frequency", meshGenerator.frequency);
        meshGenerator.heightOffset = EditorGUILayout.FloatField("Height Offset", meshGenerator.heightOffset);
        meshGenerator.octaves = EditorGUILayout.IntField("Octaves", meshGenerator.octaves);
        meshGenerator.heightCurve = EditorGUILayout.CurveField("Height Curve", meshGenerator.heightCurve);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Cave Noise Parameters", EditorStyles.boldLabel);
        meshGenerator.long_caveAmplitude = EditorGUILayout.FloatField("Long Cave Amplitude", meshGenerator.long_caveAmplitude);
        meshGenerator.long_caveFrequency = EditorGUILayout.FloatField("Long Cave Frequency", meshGenerator.long_caveFrequency);
        meshGenerator.long_caveOctaves = EditorGUILayout.FloatField("Long Cave Octaves", meshGenerator.long_caveOctaves);
        meshGenerator.long_caveGapTop = EditorGUILayout.FloatField("Long Cave Gap Top", meshGenerator.long_caveGapTop);
        meshGenerator.long_caveGapBottom = EditorGUILayout.FloatField("Long Cave Gap Bottom", meshGenerator.long_caveGapBottom);
        EditorGUILayout.Space();

        meshGenerator.big_caveAmplitude = EditorGUILayout.FloatField("Big Cave Amplitude", meshGenerator.big_caveAmplitude);
        meshGenerator.big_caveFrequency = EditorGUILayout.FloatField("Big Cave Frequency", meshGenerator.big_caveFrequency);
        meshGenerator.big_caveOctaves = EditorGUILayout.FloatField("Big Cave Octaves", meshGenerator.big_caveOctaves);
        meshGenerator.big_caveGapTop = EditorGUILayout.FloatField("Big Cave Gap Top", meshGenerator.big_caveGapTop);
        meshGenerator.big_caveGapBottom = EditorGUILayout.FloatField("Big Cave Gap Bottom", meshGenerator.big_caveGapBottom);
        EditorGUILayout.Space();

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
