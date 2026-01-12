using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem.Interactions;
using System;
// using System.Threading;
using Unity.VisualScripting;
using Icaria.Engine.Procedural;



public class Chunk
{
    public Block[,,] blocks;

    public string chunkName;
    public GameObject meshObject;
    public GameObject chunkContainer;

    private MeshGenerator meshGenerator;

    RandomNumberGenerator rng;

    public int c_x;
    public int c_z;
    private int c_size;
    private int c_height;
    private List<Vector3> vertices;
    private List<int> triangles;
    private List<Color> colors;
    private List<Vector2> uvs;

    private List<Vector3> waterVertices;
    private List<int> waterTriangles;
    private List<Color> waterColors;
    private List<Vector2> waterUvs;
    public bool isGenerated = false;
    public bool isRendering = false;

    // some global variables
    private Color grassColor = new Color(90, 199, 89, 255);


    // private float[,] heights;
    // private Block[,,] blocks;

    private Mesh mesh;
    private Mesh waterMesh;

    public Chunk(MeshGenerator meshGenerator, int c_x, int c_z, GameObject chunkContainer)
    {
        this.meshGenerator = meshGenerator;
        this.chunkName = "Chunk" + c_x + "_" + c_z;
        this.c_size = meshGenerator.c_size;
        this.c_height = meshGenerator.c_height;
        this.c_x = c_x;
        this.c_z = c_z;
        this.chunkContainer = chunkContainer;
        // blocks = new Block[c_size + 2, c_height, c_size + 2];
        blocks = new Block[c_size + 2, c_height, c_size + 2];




        rng = new RandomNumberGenerator((int)(meshGenerator.seed + (uint)(c_x * 73856093) + (uint)(c_z * 19349663)));

        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        triangles = new List<int>();
        colors = new List<Color>();

        waterVertices = new List<Vector3>();
        waterUvs = new List<Vector2>();
        waterTriangles = new List<int>();
        waterColors = new List<Color>();
    }

    public static float PerlinNoise3D(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ac = Mathf.PerlinNoise(x, z);

        return (ab + bc + ac) / 3f;
    }

    public void initializeChunk()
    {
        this.meshObject = new GameObject(chunkName);
        this.meshObject.transform.parent = chunkContainer.transform;
        mesh = new Mesh();
        waterMesh = new Mesh();
    }

    public void generateChunkData()
    {
        generateTerrainData();
        generateMeshData();
        isGenerated = true;
    }

    public void generateTerrainData()
    {
        // Terrain Generation

        //Store heights for tree placement
        int[,] heights = new int[c_size, c_size];

        for (int z = -1; z < c_size + 1; z++)
        {
            for (int x = -1; x < c_size + 1; x++)
            {
                // // float _y = y / (float)c_height;
                // float isVisible = 0;
                // float _amplitude = amplitude;
                // float _frequency = frequency;
                // float range = 0f;
                // for(int i = 0; i < octaves; i++) {
                //     isVisible += _amplitude * PerlinNoise3D((c_x*c_size + x) * scale * _frequency + randomOffset.x, y * scale * _frequency + randomOffset.y, (c_z*c_size + z) * scale * _frequency + randomOffset.z);
                //     range += Mathf.Sqrt(3f/4f);
                //     _amplitude *= 0.5f;
                //     _frequency *= 2;
                // }
                // // Debug.Log(range);
                // isVisible = isVisible / range;

                // float density = 1 - (float)y / c_height * squashingFactor;
                // density = myClamp(density);
                float f_height = 0;
                float _amplitude = meshGenerator.amplitude;
                float _frequency = meshGenerator.frequency;
                float range = _amplitude;
                int _octaves = meshGenerator.octaves;
                for (int i = 0; i < _octaves; i++)
                {
                    // height += _amplitude * Mathf.PerlinNoise((c_x*c_size + x) * _frequency + meshGenerator.randomOffset.x, (c_z*c_size + z) * _frequency + meshGenerator.randomOffset.z);
                    f_height += _amplitude * IcariaNoise.GradientNoise((c_x * c_size + x) * _frequency + meshGenerator.randomOffset.x, (c_z * c_size + z) * _frequency + meshGenerator.randomOffset.z);
                    _amplitude *= 0.5f;
                    _frequency *= 2;
                    range += _amplitude;
                }
                f_height = f_height + 0.5f - range / 2;
                f_height = f_height + meshGenerator.heightOffset / c_height;
                f_height = meshGenerator.heightCurve.Evaluate(f_height);
                int height = Mathf.FloorToInt(f_height * c_height);

                if (x < c_size - 1 && z < c_size - 1 && x >= 0 && z >= 0)
                {
                    heights[x + 1, z + 1] = height;
                }

                int dirtHeight = rng.getInt((uint)(c_x * c_size + x), (uint)(c_z * c_size + z), 3, 6);
                int bedrockLayerHeight = rng.getInt((uint)(c_x * c_size + x), (uint)(c_z * c_size + z), 1, 3);

                for (int y = c_height-1; y >= 0; y--)
                {
                    if (y < height)
                    {
                        if (y == Mathf.Floor(height) - 1)
                        {
                            if (height < 175)
                            {
                                blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.sand);
                            }
                            else
                            {
                                blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.grass_block);
                                //add grass plant on top of grass block
                                float isSelected_block_for_grass = rng.getFloat((uint)(c_x * c_size + x), (uint)(c_z * c_size + z));
                                if (isSelected_block_for_grass < 0.05f)
                                {
                                    //make sure we don't go out of bounds
                                    if (y + 1 < c_height)
                                    {
                                        blocks[x + 1, y + 1, z + 1] = new Block(BlockData.BlockType.short_grass);
                                    }
                                }
                            }
                        }
                        else if (y >= Mathf.Floor(height) - dirtHeight)
                        {
                            blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.dirt);
                        }
                        else if (y < bedrockLayerHeight)
                        {
                            blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.bedrock);
                        }
                        else
                        {
                            // underground
                            blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.stone);
                        }
                    }
                    else
                    {
                        if (y < 170)
                        {
                            blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.water);
                        }
                        else
                        {
                            blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.air);
                        }
                    }
                }
            }
        }

        // //caves Generation
        // for (int y = 0; y < c_height; y++)
        // {
        //     for (int z = -1; z < c_size + 1; z++)
        //     {
        //         for (int x = -1; x < c_size + 1; x++)
        //         {
        //             if (blocks[x + 1, y, z + 1].blockType != BlockData.BlockType.air && blocks[x + 1, y, z + 1].blockType != BlockData.BlockType.water && blocks[x + 1, y, z + 1].blockType != BlockData.BlockType.BEDROCK)
        //             {
        //                 float opacity = 0;
        //                 float _amplitude = 0.1f;
        //                 float _frequency = 0.03f;
        //                 float range = _amplitude;
        //                 float _octaves = 7;
        //                 for (int i = 0; i < _octaves; i++)
        //                 {
        //                     opacity += _amplitude * PerlinNoise3D((c_x * c_size + x) * _frequency + meshGenerator.randomOffset.x, y * _frequency + meshGenerator.randomOffset.y, (c_z * c_size + z) * _frequency + meshGenerator.randomOffset.z);
        //                     _amplitude *= 0.5f;
        //                     _frequency *= 2;
        //                     range += _amplitude;
        //                 }
        //                 opacity = opacity / range;
        //                 // float gap = (1f-(float)y / c_height) * 0.05f;
        //                 float gap = 0.005f;
        //                 // if(opacity > 0.5f - gap && opacity < 0.5f + gap) {
        //                 //     blocks[x+1, y, z+1].isOpaque = false;
        //                 //     blocks[x+1, y, z+1].type = "air";
        //                 // }
        //                 if (opacity > 0.55f)
        //                 {
        //                     blocks[x + 1, y, z + 1].isSolid = false;
        //                     blocks[x + 1, y, z + 1].blockType = BlockData.BlockType.air;
        //                 }
        //             }

        //         }
        //     }
        // }

        // Additional Features Generation (e.g., Trees) can be added here
        for(int z = 0; z < c_size; z++) {
            for(int x = 0; x < c_size; x++) {
                // Check if the block is grass to plant a tree
                int height = heights[x, z];
                if(blocks[x+1,  height, z+1].blockType == BlockData.BlockType.grass_block && height > 0.4f*c_height) {
                    float isSelected_block_for_tree = rng.getFloat((uint)(c_x*c_size + x), (uint)(c_z*c_size + z));

                    // Debug.Log(isSelected_block_for_tree);                
                    if(isSelected_block_for_tree < 0.002f) {
                        for(int i=0; i<8; i++) {
                            if (i < 4) {
                                blocks[x+1, height+1+i, z+1] = new Block(BlockData.BlockType.oak_log);
                            }
                            else {
                                int dia = 7 - i;
                                for(int t_x = -dia; t_x <= dia; t_x++) {
                                    for(int t_z = -dia; t_z <= dia; t_z++) {
                                        try
                                        {
                                            blocks[x+1 + t_x, height+1+i, z+1 + t_z] = new Block(BlockData.BlockType.oak_leaves);
                                        }
                                        catch (System.Exception)
                                        {
                                            Debug.Log("out of range");
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    public float myClamp(float val)
    {
        return math.pow((val * 2 - 1), 3);
    }


    public void generateMeshData()
    {

        //cube
        int[] vertices_x = new int[] {
            0, 0, 1, 1,
            0, 0, 0, 0,
            1, 1, 0, 0,
            1, 1, 1, 1,
            0, 0, 1, 1,
            1, 0, 0, 1,
        };
        int[] vertices_y = new int[] {
            0, 1, 1, 0,
            0, 1, 1, 0,
            0, 1, 1, 0,
            0, 1, 1, 0,
            1, 1, 1, 1,
            0, 0, 0, 0,
        };
        int[] vertices_z = new int[] {
            0, 0, 0, 0,
            1, 1, 0, 0,
            1, 1, 1, 1,
            0, 0, 1, 1,
            0, 1, 1, 0,
            1, 1, 0, 0,
        };

        //2 crossing plane for plants
        int[] p_vertices = new int[]
        {
            1, 0, 1,
            1, 1, 1,
            0, 1, 0,
            0, 0, 0,

            0, 0, 1,
            0, 1, 1,
            1, 1, 0,
            1, 0, 0,
        };

        float water_offset = 0.2f;

        int block_i = 0;
        for (int block_y = 0; block_y < c_height; block_y++)
        {
            for (int block_z = 0; block_z < c_size; block_z++)
            {
                for (int block_x = 0; block_x < c_size; block_x++)
                {
                    Block currentBlock = blocks[block_x + 1, block_y, block_z + 1];
                    if (currentBlock.blockType != BlockData.BlockType.air)
                    {
                        if (currentBlock.isCross)
                        {
                            // vertices
                            for (int i = 0; i < 8; i++)
                            {
                                vertices.Add(new Vector3(
                                    block_x + (p_vertices[i * 3] == 1 ? 0.854f : 0.146f),
                                    block_y + p_vertices[i * 3 + 1],
                                    block_z + (p_vertices[i * 3 + 2] == 1 ? 0.854f : 0.146f)
                                ));
                            }

                            // revered vertices
                            for (int i = 8-1; i >= 0; i--)
                            {
                                vertices.Add(new Vector3(
                                    block_x + (p_vertices[i * 3] == 1 ? 0.854f : 0.146f),
                                    block_y + p_vertices[i * 3 + 1],
                                    block_z + (p_vertices[i * 3 + 2] == 1 ? 0.854f : 0.146f)
                                ));
                            }

                            //uvs
                            int x = currentBlock.blockTextureCords[0];
                            int y = currentBlock.blockTextureCords[1];

                            for (int i=0; i<4; i++)
                            {
                                addUV(uvs, x, y);
                                // int brightnessOffset = rng.getInt((uint)(c_x * c_size + block_x), (uint)(c_z * c_size + block_z), 30, 50);
                                // addfourColors(new Color(grassColor.r + brightnessOffset, grassColor.g + brightnessOffset, grassColor.b + brightnessOffset, grassColor.a));
                                addfourColors(grassColor);
                            }
                        }
                        else
                        {
                            bool[] faceConditions;
                            if (currentBlock.blockType == BlockData.BlockType.water)
                            {
                                faceConditions = new bool[] {
                                    blocks[block_x+1, block_y, block_z+1].blockType.Equals(BlockData.BlockType.air), // front
                                    blocks[block_x, block_y, block_z+1].blockType.Equals(BlockData.BlockType.air), // left
                                    blocks[block_x+1, block_y, block_z+2].blockType.Equals(BlockData.BlockType.air), // back
                                    blocks[block_x+2, block_y, block_z+1].blockType.Equals(BlockData.BlockType.air), // right
                                    block_y == c_height-1 || blocks[block_x+1, block_y+1, block_z+1].blockType.Equals(BlockData.BlockType.air), // top
                                    block_y != 0 && blocks[block_x+1, block_y-1, block_z+1].blockType.Equals(BlockData.BlockType.air), // bottom
                                    // true, true, true, true, true, true
                                };
                            }
                            else
                            {
                                    faceConditions = new bool[] {
                                    !blocks[block_x+1, block_y, block_z].isSolid, // front
                                    !blocks[block_x, block_y, block_z+1].isSolid, // left
                                    !blocks[block_x+1, block_y, block_z+2].isSolid, // back
                                    !blocks[block_x+2, block_y, block_z+1].isSolid, // right
                                    block_y == c_height-1 || !blocks[block_x+1, block_y+1, block_z+1].isSolid, // top
                                    block_y != 0 && !blocks[block_x+1, block_y-1, block_z+1].isSolid, // bottom
                                    // true, true, true, true, true, true
                                };
                            }



                            int index = 0;
                            for (int face = 0; face < faceConditions.Length; face++)
                            {
                                if (faceConditions[face])
                                {
                                    if (currentBlock.blockType == BlockData.BlockType.water)
                                    {
                                        // vertices
                                        for (int i = 0; i < 4; i++)
                                        {
                                            waterVertices.Add(new Vector3(
                                                block_x + vertices_x[index],
                                                block_y + vertices_y[index] - water_offset,
                                                block_z + vertices_z[index]
                                            ));
                                            index++;
                                        }

                                        //uvs
                                        addUV(waterUvs, currentBlock.blockTextureCords[face * 2], currentBlock.blockTextureCords[face * 2 + 1]);
                                    }
                                    else
                                    {
                                        // vertices
                                        for (int i = 0; i < 4; i++)
                                        {

                                            vertices.Add(new Vector3(
                                                block_x + vertices_x[index],
                                                block_y + vertices_y[index],
                                                block_z + vertices_z[index]
                                            ));

                                            index++;
                                        }

                                        //uvs
                                        int x = currentBlock.blockTextureCords[face * 2];
                                        int y = currentBlock.blockTextureCords[face * 2 + 1];
                                        addUV(uvs, x, y);


                                        //colors
                                        if (currentBlock.blockType.ToString().Contains("leaves") || (currentBlock.blockType == BlockData.BlockType.grass_block && face == 4))
                                        {
                                            addfourColors(grassColor);
                                        }
                                        else
                                        {
                                            addfourColors(new Color(255, 255, 255, 255));
                                        }
                                    }
                                }
                                else
                                {
                                    index += 4;
                                }
                            }
                        }
                    }
                    block_i++;
                }
            }
        }

        int[] triangles_template = new int[] { 0, 1, 2, 2, 3, 0 };
        for (int i = 0; i < vertices.Count; i += 4)
        {
            for (int j = 0; j < 6; j++)
            {
                triangles.Add(i + triangles_template[j]);
            }
        }

        int[] water_triangles_template = new int[] { 0, 1, 2, 2, 3, 0 };
        for (int i = 0; i < waterVertices.Count; i += 4)
        {
            for (int j = 0; j < 6; j++)
            {
                waterTriangles.Add(i + water_triangles_template[j]);
            }
        }

    }

    public void renderChunk(Material material, Material waterMaterial)
    {
        // mesh.Clear();


        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);


        meshObject.AddComponent<MeshFilter>().mesh = mesh;
        meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        meshObject.AddComponent<MeshRenderer>().material = material;
        mesh.RecalculateNormals();


        if (waterVertices.Count != 0)
        {
            //water
            // waterMesh.Clear();
            // Debug.Log("water");

            waterMesh.SetVertices(waterVertices);
            waterMesh.SetTriangles(waterTriangles, 0);
            waterMesh.SetUVs(0, waterUvs);



            GameObject waterMeshObject = new GameObject("waterMesh")
            {
                tag = "Water"
            };
            waterMeshObject.transform.parent = meshObject.transform;
            waterMeshObject.AddComponent<MeshFilter>().mesh = waterMesh;
            waterMeshObject.AddComponent<MeshRenderer>().material = waterMaterial;

            waterMesh.RecalculateNormals();
        }

        // fake plane for testing

        meshObject.transform.position = new Vector3(c_x * c_size, 0, c_z * c_size);
    }

    public void addUV(List<Vector2> _uvs, int x, int y)
    {
        x = x / 16;
        y = y / 16;
        // texture atlas size is 544 * 544 with 16 * 16 textures
        float atlasSize = 544f;
        float textureSize = 16f;
        float x1 = x * textureSize / atlasSize;
        float y1 = 1f - (y + 1) * textureSize / atlasSize;
        float x2 = (x + 1) * textureSize / atlasSize;
        float y2 = 1f - y * textureSize / atlasSize;

        _uvs.AddRange(new[]
        {
            new Vector2(x1, y1), // Top-left
            new Vector2(x1, y2), // Bottom-left
            new Vector2(x2, y2), // Bottom-right
            new Vector2(x2, y1) // Top-right
        });
    }

    public void addfourColors(Color color)
    {
        for (int i = 0; i < 4; i++)
        {
            colors.Add(color/255f);
        }
    }

    public void destroyChunk()
    {
        GameObject.Destroy(meshObject);
    }

    public Block getBlock(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= c_size || pos.y < 0 || pos.y >= c_height || pos.z < 0 || pos.z >= c_size)
        {
            return new Block(BlockData.BlockType.air);
        }
        return blocks[pos.x, pos.y, pos.z];
    }
}

public class RandomNumberGenerator
{
    static uint prime = 4294967291;
    static uint ord = 4294967290;
    static uint generator = 4294967279;
    static uint sy;
    static uint xs;
    static uint xy;

    static int seed;

    public RandomNumberGenerator(int seed)
    {
        RandomNumberGenerator.seed = seed;
    }

    public float getFloat(uint x, uint y)
    {
        //will return values 1=> x >0; replace 'ord' with 'prime' to get 1> x >0
        //one call to modPow would be enough if all data fits into an ulong
        sy = modPow(generator, (((ulong)seed) << 32) + (ulong)y, prime);
        xs = modPow(generator, (((ulong)x) << 32) + (ulong)seed, prime);
        xy = modPow(generator, (((ulong)sy) << 32) + (ulong)xy, prime);
        return ((float)xy) / ord;
    }

    public int getInt(uint x, uint y, int min, int max)
    {
        float val = getFloat(x, y);
        return Mathf.FloorToInt(val * (max - min)) + min;
    }
    static ulong b;
    static ulong ret;
    static uint modPow(uint bb, ulong e, uint m)
    {
        b = bb;
        ret = 1;
        while (e > 0)
        {
            if (e % 2 == 1)
            {
                ret = (ret * b) % m;
            }
            e = e >> 1;
            b = (b * b) % m;
        }
        return (uint)ret;
    }
}