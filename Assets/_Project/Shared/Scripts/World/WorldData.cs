using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameJamUniverse.World
{
    [CreateAssetMenu(menuName = "GameJam/World Data", fileName = "NewWorldData")]
    public class WorldData : ScriptableObject
    {
        [SerializeField] private List<SerializedChunk> _chunks = new();

        // Runtime lookup — rebuilt from _chunks on load
        private Dictionary<Vector3Int, ChunkData> _map;

        public IEnumerable<(Vector3Int coord, ChunkData data)> AllChunks
        {
            get
            {
                EnsureMap();
                foreach (var kvp in _map) yield return (kvp.Key, kvp.Value);
            }
        }

        public ChunkData GetOrCreateChunk(Vector3Int coord)
        {
            EnsureMap();
            if (!_map.TryGetValue(coord, out var chunk))
            {
                chunk = new ChunkData();
                _map[coord] = chunk;
                _chunks.Add(new SerializedChunk { cx = coord.x, cz = coord.z, data = chunk });
            }
            return chunk;
        }

        public ChunkData GetChunk(Vector3Int coord)
        {
            EnsureMap();
            _map.TryGetValue(coord, out var chunk);
            return chunk;
        }

        private void EnsureMap()
        {
            if (_map != null) return;
            _map = new Dictionary<Vector3Int, ChunkData>();
            foreach (var sc in _chunks)
                _map[new Vector3Int(sc.cx, 0, sc.cz)] = sc.data;
        }

        private void OnEnable() => _map = null; // reset on reload

        [Serializable]
        private class SerializedChunk
        {
            public int       cx, cz;
            public ChunkData data;
        }
    }
}
