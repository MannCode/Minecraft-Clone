using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class chunkCuller : MonoBehaviour
{
    public Camera cam;
    public MeshGenerator meshGenerator;

    private Plane[] frustumPlanes;

    void Update()
    {
        Dictionary<string, Chunk> activeChunks = meshGenerator.activeChunk;

        // Get the frustum planes
        frustumPlanes = GeometryUtility.CalculateFrustumPlanes(cam);

        // Loop through each chunk
        foreach (Chunk chunk in activeChunks.Values)
        {
            //if player is stanting on that chunk dont hide it
            if(chunk.c_x == Mathf.Floor(meshGenerator.player.transform.position.x / meshGenerator.c_size) && chunk.c_z == Mathf.Floor(meshGenerator.player.transform.position.z / meshGenerator.c_size)) {
                chunk.chunkObj.SetActive(true);
                continue;
            }

            GameObject mesh = chunk.chunkObj.transform.Find("Mesh").gameObject;

            if (GeometryUtility.TestPlanesAABB(frustumPlanes, mesh.GetComponent<MeshRenderer>().bounds))
            {
                mesh.gameObject.GetComponent<MeshRenderer>().enabled = true;
            }
            else
            {
                mesh.gameObject.GetComponent<MeshRenderer>().enabled = false;
            }
            
        }
    }
}
