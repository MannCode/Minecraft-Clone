using System.Collections.Generic;
using System.Data;
// using Unity.Mathematics;
using UnityEngine;
using System.Threading;
using System;
using UnityEditor;

public class MeshGenerator : MonoBehaviour
{   
    public GameObject chunks;
    public GameObject player;

    public Material material;
    public Material waterMaterial;

    public Texture2D texture;

    public int c_size;
    public int c_height;
    public int size;

    public float amplitude;
    public float frequency;
    public float squashingFactor;
    public float heightOffset;
    public int octaves;
    public int seed;

    [HideInInspector]
    public Vector3 randomOffset;

    public Dictionary<string, Chunk> activeChunk = new Dictionary<string, Chunk>();

    public List<Thread> activeThreads = new List<Thread>();
    public List<Chunk> chunksGenerating = new List<Chunk>();


    // faces
    // 0 = front, 1 = left, 2 = back, 3 = right, 4 = top, 5 = bottom

    void Start() {
        seed = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.InitState(seed);
        randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));

        texture.mipMapBias = -4;
        texture.Apply();
        material.mainTexture = texture;

        

        // randomOffset = new Vector3(1000, 1000, 1000);
        // drawOneChunk(0, 0);

        // updateWorld();


        //set the player y position to the height of the terrain

        //get the height of the terrain at the player position from the active chunk
        // float height = activeChunk["0_0"].getHeight(player.transform.position.x, player.transform.position.z);
        // player.transform.position = new Vector3(player.transform.position.x, height + 3f, player.transform.position.z);
    }

    void Update() {
        CheckChange();
    }

    void FixedUpdate() {
        if(activeThreads.Count > 0) {
            for(int i = 0; i < activeThreads.Count; i++) {
                if(activeThreads[i].ThreadState.Equals(ThreadState.Stopped)) {
                    // get chunk from thread
                    // activeThreads[i].Abort();
                    // activeThreads[i] = ChunkG
                    chunksGenerating[i].applyMesh(material, waterMaterial);
                    activeThreads.RemoveAt(i);
                    chunksGenerating.RemoveAt(i);
                    // activeChunk.Add(chunksGenerating[i].c_x + "_" + chunksGenerating[i].c_z, chunksGenerating[i]);
                }
            }
        }
    }

    void CheckChange() {
        int c_x = Mathf.FloorToInt(player.transform.position.x / c_size);
        int c_z = Mathf.FloorToInt(player.transform.position.z / c_size);

        for(int z = -size/2; z < size/2; z++) {
            for(int x = -size/2; x < size/2; x++) {
                string key = (c_x + x) + "_" + (c_z + z);
                if(!activeChunk.ContainsKey(key)) {
                    drawOneChunk(c_x + x, c_z + z);
                }
            }
        }



        List<string> keys = new List<string>(activeChunk.Keys);
        foreach(string key in keys) {
            string[] split = key.Split('_');
            int x = int.Parse(split[0]);
            int z = int.Parse(split[1]);

            if(x < c_x - size/2 || x > c_x + size/2 || z < c_z - size/2 || z > c_z + size/2) {
                activeChunk[key].destroyChunk();
                activeChunk.Remove(key);
            }
        }
    }

    void drawOneChunk(int c_x, int c_z) {
        Chunk chunk = new Chunk(this, c_x, c_z);
        Thread newThread = new Thread(() => chunk.generateChunk());
        activeThreads.Add(newThread);
        chunksGenerating.Add(chunk);
        newThread.Start();
        activeChunk.Add(c_x + "_" + c_z, chunk);
    }

    public void reintialize() {
        foreach(KeyValuePair<string, Chunk> chunk in activeChunk) {
            chunk.Value.destroyChunk();
        }
        activeChunk.Clear();
        randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));
        CheckChange();
    }
}
