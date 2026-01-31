using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem.Interactions;
using System;
using Icaria.Engine.Procedural;



public class Chunk
{
    public int[,] heights;
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

    Vector3Int[] directions =
    {
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.up,
        Vector3Int.down
    };


    // private float[,] heights;
    // private Block[,,] blocks;

    private Mesh mesh;
    private Mesh waterMesh;

    public Chunk(MeshGenerator meshGenerator, int c_x, int c_z)
    {
        this.meshGenerator = meshGenerator;
        this.chunkName = "Chunk" + c_x + "_" + c_z;
        this.c_size = meshGenerator.c_size;
        this.c_height = meshGenerator.c_height;
        this.c_x = c_x;
        this.c_z = c_z;
        this.chunkContainer = meshGenerator.chunkContainer;
        heights = new int[c_size + 2, c_size + 2];
        blocks = new Block[c_size + 2, c_height, c_size + 2];

        rng = new RandomNumberGenerator(meshGenerator.seed);

        // world mesh
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        triangles = new List<int>();
        colors = new List<Color>();

        // water mesh
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
        for (int z = -1; z < c_size + 1; z++)
        {
            int worldZ = c_z * c_size + z;
            for (int x = -1; x < c_size + 1; x++)
            {
                int worldX = c_x * c_size + x;
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
                float cntns_height = 0;
                float ersn_height = 0;
                float pv_height = 0;
                float _amplitude = 1f;
                float _cntns_frequency = meshGenerator.Continentalnessfrequency;
                float _ersn_frequency = meshGenerator.erosionFrequency;
                float _pv_frequency = meshGenerator.pvFrequency;
                int _octaves = 7;
                for (int i = 0; i < _octaves; i++)
                {
                    cntns_height += _amplitude * IcariaNoise.GradientNoise(worldX * _cntns_frequency + meshGenerator.randomOffset.x, worldZ * _cntns_frequency + meshGenerator.randomOffset.z, meshGenerator.seed);
                    ersn_height += _amplitude * IcariaNoise.GradientNoise(worldX * _ersn_frequency + meshGenerator.randomOffset.x + 10000f, worldZ * _ersn_frequency + meshGenerator.randomOffset.z + 10000f, meshGenerator.seed);
                    pv_height += _amplitude * IcariaNoise.GradientNoise(worldX * _pv_frequency + meshGenerator.randomOffset.x + 20000f, worldZ * _pv_frequency + meshGenerator.randomOffset.z + 20000f, meshGenerator.seed);
                    _pv_frequency *= 2;
                    _amplitude *= 0.5f;
                    _cntns_frequency *= 2;
                    _ersn_frequency *= 2;

                }
                cntns_height = (cntns_height + 1) / 2; // normalize to 0-1
                // cntns_height = cntns_height + meshGenerator.heightOffset / c_height;
                cntns_height = meshGenerator.ContinentalnessHeightCurve.Evaluate(cntns_height);

                ersn_height = (ersn_height + 1) / 2; // normalize to 0-1
                ersn_height = meshGenerator.erosionHeightCurve.Evaluate(ersn_height);

                pv_height = math.abs(pv_height); // normalize to 0-1
                pv_height = meshGenerator.pvHeightCurve.Evaluate(pv_height);


                float f_height = (cntns_height + ersn_height + pv_height) / 3f;
                // convert 0-1 to waterlevel - 32 to c_height
                int height = Mathf.FloorToInt(Mathf.Lerp(meshGenerator.waterLevel - 32, c_height-5, f_height));

                if (x < c_size - 1 && z < c_size - 1 && x >= 0 && z >= 0)
                {
                    heights[x + 1, z + 1] = height;
                }



                int dirtHeight = rng.getInt((uint)worldX, (uint)worldZ, 3, 6);
                int bedrockLayerHeight = rng.getInt((uint)worldX, (uint)worldZ, 1, 3);

                for (int y = c_height - 1; y >= 0; y--)
                {
                    if (y < height + 1)
                    {
                        if (y == Mathf.Floor(height))
                        {
                            if (height < 175)
                            {
                                blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.sand);
                            }
                            else
                            {
                                blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.grass_block);
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
                        if (y < meshGenerator.waterLevel)
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

        float invHeight = 1f / c_height;

        // caves Generation
        if (meshGenerator.generateCaves)
        {
            for (int z = -1; z < c_size + 1; z++)
            {
                int worldZ = c_z * c_size + z;
                float baseZ = worldZ * 0.6f;
                for (int x = -1; x < c_size + 1; x++)
                {
                    int worldX = c_x * c_size + x;
                    float baseX = worldX * 0.6f;
                    for (int y = 0; y < c_height; y++)
                    {
                        var blockType = blocks[x + 1, y, z + 1].blockType;

                        if (blockType == BlockData.BlockType.air ||
                           blockType == BlockData.BlockType.water ||
                           blockType == BlockData.BlockType.bedrock)
                        {
                            continue;
                        }

                        float depth = 1f - y * invHeight;

                        if (carveCave(baseX, baseZ, y, depth))
                        {
                            if (y < meshGenerator.lavaLevel)
                            {
                                blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.lava);
                                blockType = BlockData.BlockType.lava;
                            }
                            else
                            {
                                blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.air);
                                blockType = BlockData.BlockType.air;
                            }
                        }

                        // add ores underground
                        if (blockType != BlockData.BlockType.stone)
                        {
                            continue;
                        }

                        if (y < 0.3f * c_height)
                        {
                            float isSelected_block_for_diamond = rng.getFloat((uint)worldX, (uint)y, (uint)worldZ);
                            float d_chance_modifier = (1f - (y / (0.3f * c_height))) * 0.0003f; // more chance to spawn diamond ore at lower depth

                            if (isSelected_block_for_diamond < d_chance_modifier)
                            {
                                // cluster of diamond ore
                                GenerateOreVein(x, y, z, 1, 7, BlockData.BlockType.diamond_ore);
                                // blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.diamond_ore);
                            }
                        }

                        if (y < 0.7f * c_height)
                        {
                            float isSelected_block_for_iron = rng.getFloat((uint)worldX, (uint)y, (uint)worldZ);

                            if (isSelected_block_for_iron < 0.0006f)
                            {
                                // cluster of iron ore
                                GenerateOreVein(x, y, z, 3, 10, BlockData.BlockType.iron_ore);
                            }
                        }
                        if (y < 0.9f * c_height)
                        {
                            float isSelected_block_for_coal = rng.getFloat((uint)worldX, (uint)y, (uint)worldZ);
                            float chance_modifier = 0.0015f;
                            if (y < 0.5f * c_height)
                            {
                                chance_modifier = (float)y / (0.5f * c_height) * 0.0015f; // less chance to spawn coal ore at lower depth
                            }

                            if (isSelected_block_for_coal < chance_modifier)
                            {
                                // cluster of coal ore
                                GenerateOreVein(x, y, z, 4, 12, BlockData.BlockType.coal_ore);
                            }
                        }
                    }
                }
            }
        }

        // Additional Features Generation (e.g., Trees, ores) can be added here
        if (meshGenerator.generateFolliage)
        {
            for (int z = 0; z < c_size; z++)
            {
                int worldZ = c_z * c_size + z;
                for (int x = 0; x < c_size; x++)
                {
                    int worldX = c_x * c_size + x;
                    // Check if the block is grass to plant a tree
                    int height = heights[x, z];

                    if (blocks[x, height, z].blockType == BlockData.BlockType.grass_block && height > meshGenerator.waterLevel)
                    {
                        float isSelected_block_for_tree = rng.getFloat((uint)worldX, (uint)worldZ);

                        // Debug.Log(isSelected_block_for_tree);                
                        if (isSelected_block_for_tree < 0.002f)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                if (i < 4)
                                {
                                    blocks[x, height + 1 + i, z] = new Block(BlockData.BlockType.oak_log);
                                }
                                else
                                {
                                    int dia = 7 - i;
                                    for (int t_x = -dia; t_x <= dia; t_x++)
                                    {
                                        for (int t_z = -dia; t_z <= dia; t_z++)
                                        {
                                            try
                                            {
                                                blocks[x + t_x, height + 1 + i, z + t_z] = new Block(BlockData.BlockType.oak_leaves);
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
                        else
                        {
                            //add grass plant on top of grass block
                            float isSelected_block_for_grass = rng.getFloat((uint)worldX, (uint)worldZ);
                            if (isSelected_block_for_grass < 0.05f)
                            {
                                //make sure we don't go out of bounds
                                if (height + 1 < c_height)
                                {
                                    blocks[x, height + 1, z] = new Block(BlockData.BlockType.short_grass);
                                }
                            }
                        }
                    }
                }
            }
        }

        // // only show specific blocks for testing
        // for (int y = 0; y < c_height; y++)
        // {
        //     for (int z = 0; z < c_size; z++)
        //     {
        //         for (int x = 0; x < c_size; x++)
        //         {
        //             if (blocks[x + 1, y, z + 1].blockType != BlockData.BlockType.iron_ore &&
        //                 blocks[x + 1, y, z + 1].blockType != BlockData.BlockType.grass_block)
        //             {
        //                 blocks[x + 1, y, z + 1] = new Block(BlockData.BlockType.air);
        //             }
        //         }
        //     }
        // }
    }

    public float myClamp(float val)
    {
        return math.pow((val * 2 - 1), 3);
    }

    bool carveCave(float baseX, float baseZ, int y, float depth)
    {
        // long caves and big caves
        float _amplitude = 1f;
        float _frequency = meshGenerator.long_caveFrequency;
        float _octaves = meshGenerator.long_caveOctaves;

        float noise1_opactiy = 0;
        float noise2_opacity = 0;
        for (int i = 0; i < _octaves; i++)
        {
            noise1_opactiy += _amplitude * IcariaNoise.GradientNoise3D(baseX * _frequency + meshGenerator.randomOffset.x, y * _frequency + meshGenerator.randomOffset.y, baseZ * _frequency + meshGenerator.randomOffset.z, meshGenerator.seed);
            noise2_opacity += _amplitude * IcariaNoise.GradientNoise3D(baseX * _frequency + meshGenerator.randomOffset.x + 100000f, y * _frequency + meshGenerator.randomOffset.y + 100000f, baseZ * _frequency + meshGenerator.randomOffset.z + 100000f, meshGenerator.seed);
            _amplitude *= 0.5f;
            _frequency *= 2;
        }
        noise1_opactiy = MathF.Abs(noise1_opactiy);
        noise2_opacity = MathF.Abs(noise2_opacity);

        float opacity = noise1_opactiy + noise2_opacity;
        float gap = Mathf.Lerp(meshGenerator.long_caveGapTop, meshGenerator.long_caveGapBottom, depth);
        if (opacity < gap)
        {
            return true;
        }

        // big caves
        float _amplitude_b = 1f;
        float _frequency_b = meshGenerator.big_caveFrequency;
        float _octaves_b = meshGenerator.big_caveOctaves;
        float noise_b_opacity = 0;

        for (int i = 0; i < _octaves_b; i++)
        {
            noise_b_opacity += _amplitude_b * IcariaNoise.GradientNoise3D(baseX * _frequency_b + meshGenerator.randomOffset.x + 20000f, y * _frequency_b + meshGenerator.randomOffset.y + 20000f, baseZ * _frequency_b + meshGenerator.randomOffset.z + 20000f, meshGenerator.seed);
            _amplitude_b *= 0.5f;
            _frequency_b *= 2;
        }
        noise_b_opacity = (noise_b_opacity + 1f) / 2f;
        float big_opacity = noise_b_opacity;
        float big_gap = Mathf.Lerp(meshGenerator.big_caveGapTop, meshGenerator.big_caveGapBottom, depth);
        if (big_opacity < big_gap)
        {
            return true;
            // float edge_dist = big_opacity / big_gap;
            // int pillar_width = (int)(edge_dist * 3f);
            // // for (int px = -pillar_width; px <= pillar_width; px++)
            // // {
            // //     for (int pz = -pillar_width; pz <= pillar_width; pz++)
            // //     {
            // //         // do nothing, leave pillar blocks
            // //     }
            // // }

            // x += pillar_width;
            // z += pillar_width;

            // if (x > c_size)
            // {
            //     x = c_size - 1;
            // }
            // if (z > c_size)
            // {
            //     z = c_size - 1;
            // }
        }

        return false;
    }

    void GenerateOreVein(int startX, int startY, int startZ, int veinSizeMin, int veinSizeMax, BlockData.BlockType oreType)
    {
        int veinSize = rng.getInt((uint)startX, (uint)startY, (uint)startZ, veinSizeMin, veinSizeMax);

        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        queue.Enqueue(new Vector3Int(startX, startY, startZ));

        int placed = 0;

        while (queue.Count > 0 && placed < veinSize)
        {
            Vector3Int pos = queue.Dequeue();

            if (!InBounds(pos)) continue;

            ref Block block = ref blocks[pos.x, pos.y, pos.z];

            if (block.blockType != BlockData.BlockType.stone)
                continue;

            block = new Block(oreType);
            placed++;

            // Randomly spread
            foreach (Vector3Int dir in directions)
            {
                if (rng.getFloat((uint)pos.x, (uint)pos.y, (uint)pos.z) < 0.8f)
                    queue.Enqueue(pos + dir);
            }
        }
    }

    bool InBounds(Vector3Int p)
    {
        return p.x > 0 && p.x < c_size &&
            p.y > 0 && p.y < c_height &&
            p.z > 0 && p.z < c_size;
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
                            for (int i = 8 - 1; i >= 0; i--)
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

                            for (int i = 0; i < 4; i++)
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
                            if (currentBlock.blockType == BlockData.BlockType.water || currentBlock.blockType == BlockData.BlockType.lava)
                            {

                                faceConditions = new bool[] {
                                    blocks[block_x+1, block_y, block_z+1].blockType.Equals(BlockData.BlockType.air), // front
                                    blocks[block_x, block_y, block_z+1].blockType.Equals(BlockData.BlockType.air), // left
                                    blocks[block_x+1, block_y, block_z+2].blockType.Equals(BlockData.BlockType.air), // back
                                    blocks[block_x+2, block_y, block_z+1].blockType.Equals(BlockData.BlockType.air), // right
                                    block_y == c_height-1 || !blocks[block_x+1, block_y+1, block_z+1].blockType.Equals(currentBlock.blockType), // top
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
                                    if (currentBlock.blockType == BlockData.BlockType.water || currentBlock.blockType == BlockData.BlockType.lava)
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
            colors.Add(color / 255f);
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
    static readonly uint prime = 4294967291;
    static readonly uint ord = 4294967290;
    static readonly uint generator = 4294967279;

    int seed;

    public RandomNumberGenerator(int seed)
    {
        this.seed = seed;
    }

    public float getFloat(uint x, uint z)
    {
        uint sy = ModPow(generator, (((ulong)seed) << 32) | z, prime);
        uint xs = ModPow(generator, (((ulong)x) << 32) | (ulong)seed, prime);
        uint xy = ModPow(generator, (((ulong)sy) << 32) | xs, prime);

        return (float)xy / ord;
    }

    public float getFloat(uint x, uint y, uint z)
    {
        uint sy = ModPow(generator, (((ulong)seed) << 32) | y, prime);
        uint xs = ModPow(generator, (((ulong)x) << 32) | (ulong)seed, prime);
        uint sz = ModPow(generator, (((ulong)seed) << 32) | z, prime);
        uint xyz = ModPow(generator, (((ulong)sy) << 32) | xs, prime);
        uint xyz_final = ModPow(generator, (((ulong)xyz) << 32) | sz, prime);

        return (float)xyz_final / ord;
    }

    public int getInt(uint x, uint z, int min, int max)
    {
        float val = getFloat(x, z);
        return Mathf.FloorToInt(val * (max - min)) + min;
    }

    public int getInt(uint x, uint y, uint z, int min, int max)
    {
        float val = getFloat(x, y, z);
        return Mathf.FloorToInt(val * (max - min)) + min;
    }


    static uint ModPow(uint b, ulong e, uint m)
    {
        ulong result = 1;
        ulong baseVal = b;

        while (e > 0)
        {
            if ((e & 1) == 1)
                result = (result * baseVal) % m;

            e >>= 1;
            baseVal = (baseVal * baseVal) % m;
        }
        return (uint)result;
    }
}