using UnityEngine;
using System.Collections.Generic;
using Unity.AppUI.UI;
using Unity.Serialization.Json;

public static class BlockDatabase
{


    public struct BlockJSONData
    {
        // public string parent;
        public Dictionary<string, string> textures;
    }

    public struct TextureJSONData
    {
        public Frame frame;

        public struct Frame
        {
            public int x;
            public int y;
        }
    }


    private static bool initialized = false;

    private static TextAsset blockDataJSON;
    private static TextAsset textureDataJSON;

    // public static Dictionary<string, BlockJSONData> blockData;
    // public static Dictionary<string, TextureJSONData> textureData;

    // Holds parsed JSON data per block
    private static Dictionary<BlockData.BlockType, BlockJSONData> BlockCache;
    private static Dictionary<string, TextureJSONData> TextureCache;

    private static Dictionary<BlockData.BlockType, List<int>> textureCords = new Dictionary<BlockData.BlockType, List<int>>();
    private static Dictionary<BlockData.BlockType, bool> isCrossCache = new Dictionary<BlockData.BlockType, bool>();

    public static void Init()
    {
        if (initialized) return;

        blockDataJSON = Resources.Load<TextAsset>("Blocks/block_data");
        textureDataJSON = Resources.Load<TextAsset>("Blocks/Block_textures/texture");

        if (blockDataJSON == null)
        {
            Debug.LogError("Failed to load block data JSON file.");
            return;
        }

        if (textureDataJSON == null)
        {
            Debug.LogError("Failed to load texture data JSON file.");
            return;
        }

        BlockCache = new Dictionary<BlockData.BlockType, BlockJSONData>();
        Dictionary<string, BlockJSONData> blockData = JsonSerialization.FromJson<Dictionary<string, BlockJSONData>>(blockDataJSON.text);
        foreach (var kvp in blockData)
        {
            BlockData.BlockType type;
            if (System.Enum.TryParse(kvp.Key, out type))
            {
                BlockCache[type] = kvp.Value;
            }
        }
        TextureCache = JsonSerialization.FromJson<Dictionary<string, TextureJSONData>>(textureDataJSON.text);

        foreach (var block in BlockCache)
        {
            BlockData.BlockType type = block.Key;
            BlockJSONData blockModel = block.Value;
            isCrossCache[type] = false;

            // remove minecraft: and block/ prefixes from texture names
            Dictionary<string, string> tempTextures = new Dictionary<string, string>(blockModel.textures);
            foreach (var tex in blockModel.textures)
            {
                string cleanName = tex.Value.Replace("minecraft:", "").Replace("block/", "");
                tempTextures[tex.Key] = cleanName + ".png";
            }
            blockModel.textures = tempTextures;

            List<int> coords = new List<int>(12);

            if (blockModel.textures != null)
            {
                if (blockModel.textures.ContainsKey("all"))
                {
                    // int texX = GetTextureData(blockModel.textures["all"]).frame.x;
                    // int texY = GetTextureData(blockModel.textures["all"]).frame.y;
                    int texX = TextureCache[blockModel.textures["all"]].frame.x;
                    int texY = TextureCache[blockModel.textures["all"]].frame.y;

                    for (int i = 0; i < 6; i++)
                    {
                        coords.Add(texX);
                        coords.Add(texY);
                    }
                }
                else
                {
                    if (blockModel.textures.ContainsKey("side") && blockModel.textures.ContainsKey("top") && blockModel.textures.ContainsKey("bottom"))
                    {
                        int sideX = TextureCache[blockModel.textures["side"]].frame.x;
                        int sideY = TextureCache[blockModel.textures["side"]].frame.y;

                        int topX = TextureCache[blockModel.textures["top"]].frame.x;
                        int topY = TextureCache[blockModel.textures["top"]].frame.y;
                        int bottomX = TextureCache[blockModel.textures["bottom"]].frame.x;
                        int bottomY = TextureCache[blockModel.textures["bottom"]].frame.y;

                        //sides
                        for (int i = 0; i < 4; i++)
                        {
                            coords.Add(sideX);
                            coords.Add(sideY);
                        }
                        //top
                        coords.Add(topX);
                        coords.Add(topY);
                        //bottom
                        coords.Add(bottomX);
                        coords.Add(bottomY);
                    }
                    else if (blockModel.textures.ContainsKey("side") && blockModel.textures.ContainsKey("end"))
                    {
                        int sideX = TextureCache[blockModel.textures["side"]].frame.x;
                        int sideY = TextureCache[blockModel.textures["side"]].frame.y;

                        int endX = TextureCache[blockModel.textures["end"]].frame.x;
                        int endY = TextureCache[blockModel.textures["end"]].frame.y;

                        // sides
                        for (int i = 0; i < 4; i++)
                        {
                            coords.Add(sideX);
                            coords.Add(sideY);
                        }
                        // top
                        coords.Add(endX);
                        coords.Add(endY);
                        // bottom
                        coords.Add(endX);
                        coords.Add(endY);
                    }
                    else if (blockModel.textures.ContainsKey("cross"))
                    {
                        isCrossCache[type] = true;
                        int texX = TextureCache[blockModel.textures["cross"]].frame.x;
                        int texY = TextureCache[blockModel.textures["cross"]].frame.y;

                        for (int i = 0; i < 6; i++)
                        {
                            coords.Add(texX);
                            coords.Add(texY);
                        }
                    }
                    else if (blockModel.textures.ContainsKey("particle"))
                    {
                        if (blockModel.textures["particle"] == "missingno.png")
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                coords.Add(7);
                                coords.Add(1);
                            }
                            textureCords[type] = coords;
                        }
                        else
                        {
                            int texX = TextureCache[blockModel.textures["particle"]].frame.x;
                            int texY = TextureCache[blockModel.textures["particle"]].frame.y;

                            for (int i = 0; i < 6; i++)
                            {
                                coords.Add(texX);
                                coords.Add(texY);
                            }
                        }
                    }
                    else
                    {
                        // Default texture coordinates (e.g., for blocks without defined textures)
                        for (int i = 0; i < 6; i++)
                        {
                            coords.Add(0);
                            coords.Add(0);
                        }
                    }
                }
            }

            textureCords[type] = coords;
        }

        initialized = true;

        // Debug.Log(textureCords.Count + " block texture coordinates loaded.");
    }

    public static List<int> GetTextureCoords(BlockData.BlockType blockType, out bool isCross)
    {
        Init();
        isCross = isCrossCache[blockType];
        return textureCords[blockType];
    }
}
