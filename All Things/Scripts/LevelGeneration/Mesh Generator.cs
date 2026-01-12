using System.Collections.Generic;
using System.Collections;
using System.Data;
// using Unity.Mathematics;
using UnityEngine;
using System.Threading;
using System;
using System.Linq;
using UnityEditor;
using Unity.Mathematics;

public class MeshGenerator : MonoBehaviour
{
    public GameObject chunkContainer;
    public GameObject player;

    public Material material;
    public Material waterMaterial;

    // public Texture2D texture;

    public int c_size;
    public int c_height;
    public int size;

    public float amplitude;
    public float frequency;
    public float squashingFactor;
    public float heightOffset;
    public int octaves;
    public int seed;

    public AnimationCurve heightCurve;

    Thread calculationThread;
    Vector3 playerPosition;

    [HideInInspector]
    public Vector3 randomOffset;

    public Dictionary<Vector2Int, Chunk> renderedChunks = new Dictionary<Vector2Int, Chunk>();
    public Dictionary<Vector2Int, Chunk> initializedChunks = new Dictionary<Vector2Int, Chunk>();

    public Dictionary<Vector2Int, Chunk> uninitializedChunks = new Dictionary<Vector2Int, Chunk>();
    public Dictionary<Vector2Int, Chunk> deletionChunks = new Dictionary<Vector2Int, Chunk>();

    // public bool isRenderingState = false;
    int currentlyRunningThreads;
    float renderCooldown = 0.03f;
    // faces
    // 0 = front, 1 = left, 2 = back, 3 = right, 4 = top, 5 = bottom

    void Awake()
    {
        BlockDatabase.Init();  
    }
    void Start()
    {
        // BlockDatabase.Init();  

        seed = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.InitState(seed);
        randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));
        // randomOffset = new Vector3(0, 0, 0);

        // texture.mipMapBias = -4;
        // texture.Apply();
        // material.mainTexture = texture;

        bool yo = ThreadPool.SetMaxThreads(10, 10);
        Debug.Log("Set max threads: " + yo);

        calculationThread = new Thread(() =>
        {
            while (true)
            {
                CheckChange();
                Thread.Sleep(1000);
            }
        });
        calculationThread.Start();

        ThreadPool.SetMinThreads(10, 10);
    }

    void Update()
    {
        // send player position to calculation thread
        playerPosition = player.transform.position;

        if (deletionChunks.Count > 0) {
            deleteOldestChunk();
        }
                // move initializedChunks to renderedChunks
        // render last chunk from initializedChunks per frame
        else if (initializedChunks.Count > 0)
        {
            // if(renderCooldown > 0f) {
            //     renderCooldown -= Time.deltaTime;
            //     return;
            // }
            // float startTime = Time.realtimeSinceStartup;
            Vector2Int key = new List<Vector2Int>(initializedChunks.Keys)[0];
            Chunk chunk = initializedChunks[key];
            if(chunk.isGenerated && !chunk.isRendering) {
                chunk.isRendering = true;
                chunk.renderChunk(material, waterMaterial);
                renderedChunks.Add(key, chunk);
                initializedChunks.Remove(key);
                currentlyRunningThreads--;
                // renderCooldown = 0.001f;
            }
            // float elapsedTime = Time.realtimeSinceStartup - startTime;
            // Debug.Log("Rendering chunk: " + elapsedTime.ToString("F8") + " seconds");
        }
        // initialize five chunks from uninitializedChunks per frame
        else if (uninitializedChunks.Count > 0 && currentlyRunningThreads <= 5)
        {
            //remove last five chunks from uninitializedChunks and generate them
            // List<string> keys = new List<string>(uninitializedChunks.Keys);
            Vector2Int key = new List<Vector2Int>(uninitializedChunks.Keys)[0];
            Chunk chunk = uninitializedChunks[key];

            // float startTime = Time.realtimeSinceStartup;
            chunk.initializeChunk();


            ThreadPool.QueueUserWorkItem(state =>
            {
                currentlyRunningThreads++;
                chunk.generateChunkData();
                // Thread.Sleep(1);
            });

            initializedChunks.Add(key, chunk);
            uninitializedChunks.Remove(key);
            // float elapsedTime = Time.realtimeSinceStartup - startTime;
            // Debug.Log("Intializing chunk: " + elapsedTime.ToString("F8") + " seconds");

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
                        Vector2Int key = new Vector2Int(c_x + x, c_z + z);
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

        List<Vector2Int> keys = new List<Vector2Int>(renderedChunks.Keys);
        keys.AddRange(new List<Vector2Int>(initializedChunks.Keys));
        keys.AddRange(new List<Vector2Int>(uninitializedChunks.Keys));
        foreach (Vector2Int key in keys)
        {
            int x2 = key.x;
            int z2 = key.y;
            if (x2 < c_x - size / 2 || x2 > c_x + size / 2 || z2 < c_z - size / 2 || z2 > c_z + size / 2)
            {
                if(!deletionChunks.ContainsKey(key)) {
                if(renderedChunks.ContainsKey(key)) {
                    deletionChunks.Add(key, renderedChunks[key]);
                    renderedChunks.Remove(key);
                }
                else if(initializedChunks.ContainsKey(key)) {
                    deletionChunks.Add(key, initializedChunks[key]);
                    initializedChunks.Remove(key);
                }
                else if(uninitializedChunks.ContainsKey(key)) {
                    deletionChunks.Add(key, uninitializedChunks[key]);
                    uninitializedChunks.Remove(key);
                }
                }
            }
        }
    }

    void getOneChunk(int c_x, int c_z)
    {
        Chunk chunk = new Chunk(this, c_x, c_z, chunkContainer);
        uninitializedChunks.Add(new Vector2Int(c_x, c_z), chunk);
    }

    public void reintialize()
    {
        foreach (KeyValuePair<Vector2Int, Chunk> chunk in renderedChunks)
        {
            chunk.Value.destroyChunk();
        }
        renderedChunks.Clear();
        initializedChunks.Clear();
        // uninitializedChunks.Clear();
        // randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));
        randomOffset = new Vector3(0, 0, 0);
        // CheckChange();
    }

    void deleteOldestChunk()
    {
        while(deletionChunks.Count > 0) {
            Vector2Int key = new List<Vector2Int>(deletionChunks.Keys)[0];
            Chunk chunk = deletionChunks[key];
            chunk.destroyChunk();
            deletionChunks.Remove(key);
        }
    }
}
