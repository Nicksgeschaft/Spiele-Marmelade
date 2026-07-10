using System.Collections.Generic;
using UnityEngine;

namespace GameJamUniverse.World
{
    // One per chunk. Owned and created by BrickWorld.
    // Call Rebuild() after any block change in this chunk.
    [RequireComponent(typeof(MeshCollider))]
    public class ChunkRenderer : MonoBehaviour
    {
        private ChunkData   _data;
        private BrickWorld  _world;
        private Vector3Int  _chunkCoord;

        private readonly List<GameObject> _subMeshObjects = new();

        public void Init(BrickWorld world, Vector3Int chunkCoord, ChunkData data)
        {
            _world      = world;
            _chunkCoord = chunkCoord;
            _data       = data;
        }

        public void Rebuild(Material[] materialPalette)
        {
            // Destroy old sub-mesh objects
            foreach (var go in _subMeshObjects)
                if (go) Destroy(go);
            _subMeshObjects.Clear();

            var neighbors = new ChunkData[4]
            {
                _world.GetChunkData(_chunkCoord + new Vector3Int( 1, 0,  0)),
                _world.GetChunkData(_chunkCoord + new Vector3Int(-1, 0,  0)),
                _world.GetChunkData(_chunkCoord + new Vector3Int( 0, 0,  1)),
                _world.GetChunkData(_chunkCoord + new Vector3Int( 0, 0, -1)),
            };

            var meshes = ChunkMeshBuilder.Build(_data, neighbors);
            foreach (var (mesh, matIdx) in meshes)
            {
                var go = new GameObject($"Sub_{matIdx}");
                go.transform.SetParent(transform, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;

                Material mat = matIdx < materialPalette?.Length ? materialPalette[matIdx] : null;
                go.AddComponent<MeshRenderer>().sharedMaterial = mat;
                _subMeshObjects.Add(go);
            }

            // Combine all sub-meshes for the collider
            RebuildCollider();
        }

        private void RebuildCollider()
        {
            var filters = GetComponentsInChildren<MeshFilter>();
            if (filters.Length == 0)
            {
                GetComponent<MeshCollider>().sharedMesh = null;
                return;
            }

            var combine = new CombineInstance[filters.Length];
            for (int i = 0; i < filters.Length; i++)
                combine[i] = new CombineInstance
                    { mesh = filters[i].sharedMesh, transform = filters[i].transform.localToWorldMatrix };

            var combined = new Mesh();
            combined.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            combined.CombineMeshes(combine, mergeSubMeshes: true, useMatrices: true);
            GetComponent<MeshCollider>().sharedMesh = combined;
        }
    }
}
