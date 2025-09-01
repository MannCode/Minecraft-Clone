using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockData
{
    public enum BlockType
    {
        AIR,
        GRASS,
        DIRT,
        STONE,
        WATER,
        OAK_LOG,
        Leaves,
    }

    public BlockType blockType;
    public bool isSolid;
    // public Color blockColor;

    public BlockData(BlockType blockType)
    {
        this.blockType = blockType;
        if (blockType == BlockType.AIR ||
            blockType == BlockType.WATER)
        {
            isSolid = false;
        }
        else
        {
            isSolid = true;
        }
    }
}
