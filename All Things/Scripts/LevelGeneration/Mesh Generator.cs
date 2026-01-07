using System.Collections.Generic;
using System.Collections;
using System.Data;
// using Unity.Mathematics;
using UnityEngine;
using System.Threading;
using System;
using UnityEditor;
using Unity.Mathematics;

public class MeshGenerator : MonoBehaviour
{
    public GameObject chunkContainer;
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
    List<string> keysToRemove;

    Thread calculationThread;
    Vector3 playerPosition;

    [HideInInspector]
    public Vector3 randomOffset;

    public Dictionary<string, Chunk> renderedChunks = new Dictionary<string, Chunk>();
    public Dictionary<string, Chunk> initializedChunks = new Dictionary<string, Chunk>();

    public Dictionary<string, Chunk> uninitializedChunks = new Dictionary<string, Chunk>();

    // public bool isRenderingState = false;
    int currentlyRunningThreads;
    float renderCooldown = 0.03f;
    // faces
    // 0 = front, 1 = left, 2 = back, 3 = right, 4 = top, 5 = bottom
    void Start()
    {
        seed = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.InitState(seed);
        randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));

        texture.mipMapBias = -4;
        texture.Apply();
        material.mainTexture = texture;
        keysToRemove = new List<string>();

        bool yo = ThreadPool.SetMaxThreads(10, 10);
        Debug.Log("Set max threads: " + yo);

        calculationThread = new Thread(() =>
        {
            while (true)
            {
                CheckChange();
                Thread.Sleep(100);
            }
        });
        calculationThread.Start();

        // ThreadPool.SetMinThreads(10, 10);
    }

    void Update()
    {
        // send player position to calculation thread
        playerPosition = player.transform.position;

        if (keysToRemove.Count > 0) {
            deleteOldestChunk();
        }
        // initialize five chunks from uninitializedChunks per frame
        else if (uninitializedChunks.Count > 0 && currentlyRunningThreads <= 2)
        {
            //remove last five chunks from uninitializedChunks and generate them
            // List<string> keys = new List<string>(uninitializedChunks.Keys);
            string key = new List<string>(uninitializedChunks.Keys)[UnityEngine.Random.Range(0, math.min(10, uninitializedChunks.Count))];
            Chunk chunk = uninitializedChunks[key];

            // float startTime = Time.realtimeSinceStartup;
            chunk.initializeChunk();

            currentlyRunningThreads++;
            ThreadPool.QueueUserWorkItem(state =>
            {
                chunk.generateChunkData();
                // Thread.Sleep(1);
            });

            initializedChunks.Add(key, chunk);
            uninitializedChunks.Remove(key);
            // float elapsedTime = Time.realtimeSinceStartup - startTime;
            // Debug.Log("Intializing chunk: " + elapsedTime.ToString("F8") + " seconds");

        }
        // move initializedChunks to renderedChunks
        // render last chunk from initializedChunks per frame
        else if (initializedChunks.Count > 0)
        {
            if(renderCooldown > 0f) {
                renderCooldown -= Time.deltaTime;
                return;
            }
            // float startTime = Time.realtimeSinceStartup;
            string key = new List<string>(initializedChunks.Keys)[0];
            Chunk chunk = initializedChunks[key];
            if(chunk.isGenerated && !chunk.isRendering) {
                chunk.isRendering = true;
                chunk.renderChunk(material, waterMaterial);
                renderedChunks.Add(key, chunk);
                initializedChunks.Remove(key);
                currentlyRunningThreads--;
                renderCooldown = 0.04f;
            }
            // float elapsedTime = Time.realtimeSinceStartup - startTime;
            // Debug.Log("Rendering chunk: " + elapsedTime.ToString("F8") + " seconds");
        }
    }

    void CheckChange()
    {
        int c_x = Mathf.FloorToInt(playerPosition.x / c_size);
        int c_z = Mathf.FloorToInt(playerPosition.z / c_size);

        int half = size / 2;

        // start at center
        int x = 0;
        int z = 0;

        // directions: right, forward, left, back (Unity XZ plane)
        Vector2Int[] dirs = new Vector2Int[]
        {
            new Vector2Int(1, 0),   // right
            new Vector2Int(0, 1),   // forward
            new Vector2Int(-1, 0),  // left
            new Vector2Int(0, -1)   // back
        };

        int dirIndex = 0;
        int steps = 1;

        while (Mathf.Abs(x) <= half && Mathf.Abs(z) <= half)
        {
            // repeat twice per step length
            for (int r = 0; r < 2; r++)
            {
                for (int i = 0; i < steps; i++)
                {
                    if (Mathf.Abs(x) <= half && Mathf.Abs(z) <= half)
                    {
                        string key = (c_x + x) + "_" + (c_z + z);
                        if (!renderedChunks.ContainsKey(key) && !initializedChunks.ContainsKey(key) && !uninitializedChunks.ContainsKey(key))
                        {
                            getOneChunk(c_x + x, c_z + z);
                        }
                    }

                    x += dirs[dirIndex].x;
                    z += dirs[dirIndex].y;
                }

                dirIndex = (dirIndex + 1) % 4;
            }

            steps++;
        }

        // for (int z = -size / 2; z < size / 2; z++)
        // {
        //     for (int x = -size / 2; x < size / 2; x++)
        //     {
                
        //     }
        // }

        List<string> keys = new List<string>(renderedChunks.Keys);
        foreach (string key in keys)
        {
            string[] split = key.Split('_');
            int x2 = int.Parse(split[0]);
            int z2 = int.Parse(split[1]);

            if (x2 < c_x - size / 2 || x2 > c_x + size / 2 || z2 < c_z - size / 2 || z2 > c_z + size / 2)
            {
                keysToRemove.Add(key);
            }
        }
    }

    void getOneChunk(int c_x, int c_z)
    {
        Chunk chunk = new Chunk(this, c_x, c_z, chunkContainer);
        uninitializedChunks.Add(c_x + "_" + c_z, chunk);
    }

    public void reintialize()
    {
        foreach (KeyValuePair<string, Chunk> chunk in renderedChunks)
        {
            chunk.Value.destroyChunk();
        }
        renderedChunks.Clear();
        randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));
        CheckChange();
    }

    void deleteOldestChunk()
    {
        foreach (string key in keysToRemove)
        {
            if (renderedChunks.ContainsKey(key))
            {
                renderedChunks[key].destroyChunk();
                renderedChunks.Remove(key);
            }
        }
        keysToRemove.Clear();
    }
}
