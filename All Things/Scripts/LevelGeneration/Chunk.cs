using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.InputSystem.Interactions;
using System;
using System.Threading;
using Unity.VisualScripting;

public class Chunk
{
    public BlockData[,,] blocks;

    public string chunkName;
    GameObject chunks;
    public GameObject meshObject;

    private MeshGenerator meshGenerator;
    
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
        // blocks = new Block[c_size + 2, c_height, c_size + 2];
        blocks = new BlockData[c_size + 2, c_height, c_size + 2];

        chunks = GameObject.Find("Chunks");

        this.meshObject = new GameObject(chunkName);

        mesh = new Mesh();
        waterMesh = new Mesh();

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

    public void generateChunk() {
        // Thread thread = new Thread(() => GenerateChunkData(amplitude, frequency, octaves, randomOffset, squashingFactor, heightOffset, material, waterMaterial));
        GenerateChunkData();
        renderChunk();
    }

    public void GenerateChunkData() {
        //add squasing factor and hieght offset
        // float scale = 0.1f;
        for(int z = -1; z < c_size+1; z++) {
            for(int x = -1; x < c_size+1; x++) {
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
                float height = 0;
                float _amplitude = meshGenerator.amplitude;
                float _frequency = meshGenerator.frequency;
                float range = _amplitude;
                int _octaves = meshGenerator.octaves;
                for(int i=0; i<_octaves; i++) {
                    height += _amplitude * Mathf.PerlinNoise((c_x*c_size + x) * _frequency + meshGenerator.randomOffset.x, (c_z*c_size + z) * _frequency + meshGenerator.randomOffset.z);
                    _amplitude *= 0.5f;
                    _frequency *= 2;
                    range += _amplitude;
                }
                height = height + 0.5f - range/2;
                for(int y = 0; y < c_height; y++) {
                    if(y < height * c_height) {
                        blocks[x+1,y,z+1] = new BlockData(BlockData.BlockType.GRASS);
                    }
                    else
                    {
                        if(y < 0.45f*c_height) {
                            blocks[x+1,y,z+1] = new BlockData(BlockData.BlockType.WATER);
                        }
                        else {
                            blocks[x+1,y,z+1] = new BlockData(BlockData.BlockType.AIR);
                        }
                    }
                }

                // //add trees
                // float isSelected_block_for_tree = Mathf.PerlinNoise((c_x*c_size + x) * 0.2f + meshGenerator.randomOffset.x, (c_z*c_size + z) * 0.2f + meshGenerator.randomOffset.z) * 10;
                // // Debug.Log(isSelected_block_for_tree);
                // if(isSelected_block_for_tree < 0.5f) {
                //     for(int i=0; i<7; i++) {
                //         blocks[x+1, (int)(height*c_height)+1+i, z+1] = new BlockData(BlockData.BlockType.OAK_LOG);
                //         if(i > 4) {
                //             for(int t_z=0; t_z<7-i; t_z++) {
                //                 for(int t_x=0; t_x<7-i; t_x++) {
                //                     try
                //                     {
                //                         blocks[x+1+t_x, (int)(height*c_height)+1+i, z+1+t_z] = new BlockData(BlockData.BlockType.GRASS);
                //                     }
                //                     catch (System.Exception)
                //                     {
                //                         Debug.Log("out of range");
                //                     }
                //                 }
                //             }
                //         }
                //     }

                // }
            }
        }

        // //caves
        // for(int y = 0; y < c_height; y++) {
        //     for(int z = -1; z < c_size+1; z++) {
        //         for(int x = -1; x < c_size+1; x++) {
        //             if(blocks[x+1, y, z+1].blockType != BlockData.BlockType.AIR && blocks[x+1, y, z+1].blockType != BlockData.BlockType.WATER) {
        //                 float opacity = 0;
        //                 float _amplitude = 0.1f;
        //                 float _frequency = 0.03f;
        //                 float range = _amplitude;
        //                 float _octaves = 7;
        //                 for(int i=0; i<_octaves; i++) {
        //                     opacity += _amplitude * PerlinNoise3D((c_x*c_size + x) * _frequency + meshGenerator.randomOffset.x, y * _frequency + meshGenerator.randomOffset.y, (c_z*c_size + z) * _frequency + meshGenerator.randomOffset.z);
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
        //                 if(opacity > 0.55f) {
        //                     blocks[x+1, y, z+1].isSolid = false;
        //                     blocks[x+1, y, z+1].blockType = BlockData.BlockType.AIR;
        //                 }
        //             }
        //         }
        //     }
        // }
    }

    public float myClamp(float val) {
        return math.pow((val*2 -1), 3);
    }


    public void renderChunk() {
        
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

        float water_offset = 0.2f;

        int block_i = 0;
        for(int block_y = 0; block_y < c_height; block_y++) {
            for(int block_z = 0; block_z < c_size; block_z++) {
                for(int block_x = 0; block_x < c_size; block_x++) {
                    BlockData currentBlock = blocks[block_x+1, block_y, block_z+1];
                    if(currentBlock.blockType != BlockData.BlockType.AIR) {
                        bool[] faceConditions;
                        if(currentBlock.blockType == BlockData.BlockType.WATER) {
                            faceConditions = new bool[] {
                                blocks[block_x+1, block_y, block_z+1].blockType.Equals(BlockData.BlockType.AIR), // front
                                blocks[block_x, block_y, block_z+1].blockType.Equals(BlockData.BlockType.AIR), // left
                                blocks[block_x+1, block_y, block_z+2].blockType.Equals(BlockData.BlockType.AIR), // back
                                blocks[block_x+2, block_y, block_z+1].blockType.Equals(BlockData.BlockType.AIR), // right
                                block_y == c_height-1 || blocks[block_x+1, block_y+1, block_z+1].blockType.Equals(BlockData.BlockType.AIR), // top
                                block_y != 0 && blocks[block_x+1, block_y-1, block_z+1].blockType.Equals(BlockData.BlockType.AIR), // bottom
                                // true, true, true, true, true, true
                            };
                        }
                        else {
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
                        for(int face = 0; face < faceConditions.Length; face++) {
                            if(faceConditions[face]) {
                                if(currentBlock.blockType == BlockData.BlockType.WATER) {
                                    // vertices
                                    for(int i = 0; i < 4; i++) {
                                        waterVertices.Add(new Vector3(
                                            block_x + vertices_x[index],
                                            block_y + vertices_y[index] - water_offset,
                                            block_z + vertices_z[index]
                                        ));
                                        index++;
                                    }

                                    //uvs
                                    addUV(waterUvs, 13, 3);
                                }
                                else {
                                    // vertices
                                    for(int i = 0; i < 4; i++) {

                                        vertices.Add(new Vector3(
                                            block_x + vertices_x[index],
                                            block_y + vertices_y[index],
                                            block_z + vertices_z[index]
                                        ));
                                        
                                        index++;
                                    }

                                    //uvs
                                    Vector2 start = get_Texure_cord_from_type(blocks[block_x+1, block_y, block_z+1].blockType);
                                    addUV(uvs, (int)start.x, (int)start.y);


                                    //colors
                                    if(currentBlock.blockType == BlockData.BlockType.GRASS) {
                                        addfourColors(new Color(0, 1, 0, 1));
                                    }
                                    else {
                                        addfourColors(new Color(1, 1, 1, 1));
                                    }
                                }
                            }
                            else {
                                index += 4;
                            }
                        }
                    }
                    block_i++;
                }
            }
        }

        int[] triangles_template = new int[] {0, 1, 2, 2, 3, 0};
        for(int i = 0; i < vertices.Count; i+=4) {
            for(int j = 0; j < 6; j++) {
                triangles.Add(i + triangles_template[j]);
            }
        }

        int[] water_triangles_template = new int[] {0, 1, 2, 2, 3, 0};
        for(int i = 0; i < waterVertices.Count; i+=4) {
            for(int j = 0; j < 6; j++) {
                waterTriangles.Add(i + water_triangles_template[j]);
            }
        }

        
    }

    public void applyMesh(Material material, Material waterMaterial) {
        // mesh.Clear();

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetColors(colors);
        mesh.SetUVs(0, uvs);


        
        meshObject.transform.parent = chunks.transform;
        meshObject.AddComponent<MeshFilter>().mesh = mesh;
        meshObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        meshObject.AddComponent<MeshRenderer>().material = material;
        mesh.RecalculateNormals();


        if(waterVertices.Count != 0) {
            //water
            // waterMesh.Clear();
            // Debug.Log("water");

            waterMesh.SetVertices(waterVertices);
            waterMesh.SetTriangles(waterTriangles, 0);
            waterMesh.SetUVs(0, waterUvs);



            GameObject waterMeshObject = new GameObject("WaterMesh")
            {
                tag = "Water"
            };
            waterMeshObject.transform.parent = meshObject.transform;
            waterMeshObject.AddComponent<MeshFilter>().mesh = waterMesh;
            waterMeshObject.AddComponent<MeshRenderer>().material = waterMaterial;
            
            waterMesh.RecalculateNormals();
        }

        meshObject.transform.position = new Vector3(c_x * c_size, 0, c_z * c_size);
    }

    public void addUV(List<Vector2> _uvs, int x, int y) {
        float pixelSize = 1f / 256f;
        float x1 = (x * 16f + 3f) * pixelSize;
        float x2 = (x * 16f + 12f) * pixelSize;
        float y1 = (y * 16f + 3f) * pixelSize;
        float y2 = (y * 16f + 12f) * pixelSize;

        _uvs.AddRange(new[]
        {
            new Vector2(x1, y1), // Top-left
            new Vector2(x1, y2), // Bottom-left
            new Vector2(x2, y2), // Bottom-right
            new Vector2(x2, y1) // Top-right
        });
    }

    public Vector2 get_Texure_cord_from_type(BlockData.BlockType type) {
        if(type == BlockData.BlockType.GRASS) {
            return new Vector2(0, 15);
        }
        else if(type == BlockData.BlockType.WATER) {
            return new Vector2(13, 3);
        }
        else if(type == BlockData.BlockType.STONE) {
            return new Vector2(1, 15);
        }
        else if(type == BlockData.BlockType.OAK_LOG) {
            return new Vector2(7, 10);
        }
        else if(type == BlockData.BlockType.Leaves) {
            return new Vector2(5, 11);
        }
        else {
            return new Vector2(7, 1);
        }
    }

    public void addfourColors(Color color) {
        for(int i=0; i<4; i++) {
            colors.Add(color);
        }
    }

    public void destroyChunk()
    {
        GameObject.Destroy(meshObject);
    }

    public BlockData getBlock(Vector3Int pos)
    {
        if(pos.x < 0 || pos.x >= c_size || pos.y < 0 || pos.y >= c_height || pos.z < 0 || pos.z >= c_size) {
            return new BlockData(BlockData.BlockType.AIR);
        }
        return blocks[pos.x, pos.y, pos.z];
    }
}
