using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockData : MonoBehaviour
{
    public enum BlockType
    {
        Air,
        Grass,
        Dirt,
        Stone,
        Water
    }

    public BlockType blockType;
    public bool isSolid;
    // public Color blockColor;

    public BlockData(BlockType blockType)
    {
        this.blockType = blockType;
        if (blockType == BlockType.Air)
        {
            isSolid = false;
        }
        else
        {
            isSolid = true;
        }
    }
}
