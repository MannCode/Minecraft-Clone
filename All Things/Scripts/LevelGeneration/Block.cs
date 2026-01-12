using UnityEngine;
using System.Collections.Generic;

public class Block
{
    public BlockData.BlockType blockType;
    public List<int> blockTextureCords;
    public bool isSolid = true;
    public bool isCross = false;

    public Block(BlockData.BlockType blockType)
    {
        this.blockType = blockType;

        blockTextureCords = BlockDatabase.GetTextureCoords(blockType, out isCross);

        if(blockType == BlockData.BlockType.air || blockType == BlockData.BlockType.water || blockType.ToString().Contains("leaves") || isCross)
        {
            isSolid = false;
        }
    }
}
