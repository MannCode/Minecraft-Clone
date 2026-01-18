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
using UnityEngine.UI;

public class MeshGenerator : MonoBehaviour
{
    public GameObject chunkContainer;
    public GameObject player;
    public Image loadingScreen;

    public Material material;
    public Material waterMaterial;

    // public Texture2D texture;

    public int c_size;
    public int c_height;
    public int size;

    // terrain noise parameters
    public bool randomSeed = true;
    public int seed;
    public float amplitude;
    public float frequency;
    public float heightOffset;
    public int octaves;
    public AnimationCurve heightCurve;

    // Cave noise parameters
    public float long_caveAmplitude;
    public float long_caveFrequency;
    public float long_caveOctaves;
    public float long_caveGapTop;
    public float long_caveGapBottom;


    public float big_caveAmplitude;
    public float big_caveFrequency;
    public float big_caveOctaves;
    public float big_caveGapTop;
    public float big_caveGapBottom;

    Thread calculationThread;
    Vector3 playerPosition;

    [HideInInspector]
    public Vector3 randomOffset;


    public Dictionary<Vector2Int, Chunk> renderedChunks = new Dictionary<Vector2Int, Chunk>();
    public Dictionary<Vector2Int, Chunk> initializedChunks = new Dictionary<Vector2Int, Chunk>();

    public Dictionary<Vector2Int, Chunk> uninitializedChunks = new Dictionary<Vector2Int, Chunk>();
    public Queue<Vector2Int> unitializedChunkQueue = new Queue<Vector2Int>();

    public Dictionary<Vector2Int, Chunk> deletionChunks = new Dictionary<Vector2Int, Chunk>();

    // public bool isRenderingState = false;
    int currentlyRunningThreads;
    bool firstGenerationDone = false;
    // faces
    // 0 = front, 1 = left, 2 = back, 3 = right, 4 = top, 5 = bottom

    void Awake()
    {
        BlockDatabase.Init();  
    }
    void Start()
    {
        reintialize();
        
        calculationThread = new Thread(() =>
        {
            while (true)
            {
                CheckChange();
                Thread.Sleep(1000);
            }
        });
        calculationThread.Start();

        ThreadPool.SetMaxThreads(10, 10);
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
            Vector2Int key = new List<Vector2Int>(initializedChunks.Keys)[0];
            Chunk chunk = initializedChunks[key];
            if(chunk.isGenerated && !chunk.isRendering) {
                chunk.isRendering = true;
                chunk.renderChunk(material, waterMaterial);
                renderedChunks.Add(key, chunk);
                initializedChunks.Remove(key);
                Interlocked.Decrement(ref currentlyRunningThreads);
            }
            // float elapsedTime = Time.realtimeSinceStartup - startTime;
            // Debug.Log("Rendering chunk: " + elapsedTime.ToString("F8") + " seconds");
        }
        // initialize five chunks from uninitializedChunks per frame
        else if (uninitializedChunks.Count > 0 && currentlyRunningThreads <= 10)
        {
            //remove last five chunks from uninitializedChunks and generate them
            // List<string> keys = new List<string>(uninitializedChunks.Keys);
            var key = unitializedChunkQueue.Dequeue();
            Chunk chunk = uninitializedChunks[key];

            // float startTime = Time.realtimeSinceStartup;
            chunk.initializeChunk();


            ThreadPool.QueueUserWorkItem(state =>
            {
                Interlocked.Increment(ref currentlyRunningThreads);
                chunk.generateChunkData();;
            });

            initializedChunks.Add(key, chunk);
            uninitializedChunks.Remove(key);
            // float elapsedTime = Time.realtimeSinceStartup - startTime;
            // Debug.Log("Intializing chunk: " + elapsedTime.ToString("F8") + " seconds");

        }

        if(!firstGenerationDone && uninitializedChunks.Count == 0 && initializedChunks.Count == 0 && renderedChunks.Count > 0) {
            firstGenerationDone = true;
            float yPos = c_height;
            Chunk midChunk = renderedChunks[new Vector2Int(0, 0)];
            while(yPos > 0)
            {
                Block block = midChunk.getBlock(new Vector3Int(0, Mathf.FloorToInt(yPos), 0));
                if(block != null && block.blockType != BlockData.BlockType.air) {
                    break;
                }
                yPos--;
            }
            player.transform.position = new Vector3(0,  yPos+1, 0);
            player.SetActive(true);
            loadingScreen.gameObject.SetActive(false);
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
                    unitializedChunkQueue = new Queue<Vector2Int>(unitializedChunkQueue.Where(k => k != key));
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
        unitializedChunkQueue.Enqueue(new Vector2Int(c_x, c_z));
    }

    public void reintialize()
    {

        foreach (KeyValuePair<Vector2Int, Chunk> chunk in renderedChunks)
        {
            chunk.Value.destroyChunk();
        }
        foreach (KeyValuePair<Vector2Int, Chunk> chunk in initializedChunks)
        {
            chunk.Value.destroyChunk();
        }
        foreach (KeyValuePair<Vector2Int, Chunk> chunk in deletionChunks)
        {
            chunk.Value.destroyChunk();
        }
        renderedChunks.Clear();
        initializedChunks.Clear();
        uninitializedChunks.Clear();
        unitializedChunkQueue.Clear();
        deletionChunks.Clear();
        
        player.transform.position = new Vector3(0, 0, 0);
        player.SetActive(false);
        firstGenerationDone = false;
        loadingScreen.gameObject.SetActive(true);
        

        if(randomSeed) seed = UnityEngine.Random.Range(0, 1000000);
        UnityEngine.Random.InitState(seed);
        randomOffset = new Vector3(UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000), UnityEngine.Random.Range(0, 1000));
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
