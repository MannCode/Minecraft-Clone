using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class CheckIfUnderWater : MonoBehaviour
{

    public Volume volume;
    public LayerMask waterLayer;
    public Transform cameraT;
    
    public MeshGenerator meshGenerator;

    // Update is called once per frame
    void Update()
    {
        float newY = cameraT.position.y + 0.2f;

        if(meshGenerator.renderedChunks.Count == 0 || newY < 0 || newY > meshGenerator.c_height) return;

        volume.gameObject.SetActive(false);

        float c_x = Mathf.Floor(cameraT.position.x / meshGenerator.c_size);
        float c_z = Mathf.Floor(cameraT.position.z / meshGenerator.c_size);

        Chunk chunk = meshGenerator.renderedChunks[c_x + "_" + c_z];
        
        Vector3Int blockPos = new Vector3Int(
            Mathf.FloorToInt(cameraT.position.x - c_x * meshGenerator.c_size),
            Mathf.FloorToInt(newY),
            Mathf.FloorToInt(cameraT.position.z - c_z * meshGenerator.c_size)
        );

        BlockData block = chunk.getBlock(blockPos);

        if(block.blockType == BlockData.BlockType.WATER) {
            volume.gameObject.SetActive(true);
        }

    }
}
