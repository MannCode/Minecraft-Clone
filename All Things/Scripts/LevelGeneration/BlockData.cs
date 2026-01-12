using System.Collections;
using System.Collections.Generic;
using Unity.Serialization.Json;
using UnityEngine;
using UnityEngine.Animations;


[System.Serializable]
public class BlockData
{
    public enum BlockType
    {
        air,
        stone,
        dirt,
        grass_block,
        bedrock,
        water,
        short_grass,
        sand,
        oak_log,
        oak_leaves,
    }
}
