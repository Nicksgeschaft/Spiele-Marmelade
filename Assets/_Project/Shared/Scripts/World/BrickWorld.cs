using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.World
{
    // Central world manager. Place in scene, assign WorldData and a material palette.
    // Call SetBlock / RemoveBlock at runtime to mutate the world.
    public class BrickWorld : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private WorldData _worldData;

        [Header("Materials")]
        [Tooltip("Index matches BrickCell.materialIndex")]
        [SerializeField] private Material[] _materialPalette;

        private readonly Dictionary<Vector3Int, ChunkRenderer> _renderers = new();
        private readonly HashSet<Vector3Int>                   _dirty     = new();

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            if (_worldData == null) return;
            foreach (var (coord, _) in _worldData.AllChunks)
                GetOrCreateRenderer(coord);
            RebuildDirty();
        }

        private void LateUpdate()
        {
            if (_dirty.Count > 0) RebuildDirty();
        }

        // ── Public API ─────────────────────────────────────────────────────────

        public void SetBlock(Vector3 worldPos, BrickCell cell) =>
            SetBlock(WorldToGlobal(worldPos), cell);

        public void SetBlock(Vector3Int globalPlatePos, BrickCell cell)
        {
            if (!BrickShapeInfo.IsStructural(cell.type))
            {
                Debug.LogWarning($"[BrickWorld] Ignoring SetBlock with non-structural type '{cell.type}'. " +
                                  "Round bricks are decoration props and can't be voxelized.");
                return;
            }

            var (coord, local) = GlobalToChunkLocal(globalPlatePos);
            var data = _worldData.GetOrCreateChunk(coord);

            // For tall bricks: fill the vertical slots
            int slots = BrickShapeInfo.HeightInPlates(cell.type);
            for (int dy = 0; dy < slots && local.y + dy < ChunkData.SizeY; dy++)
                data.Set(local.x, local.y + dy, local.z,
                    dy == 0 ? cell : new BrickCell(cell.type, cell.materialIndex));

            MarkDirty(coord);
            MarkNeighborsDirtyIfEdge(coord, local);
            GetOrCreateRenderer(coord);
        }

        public void RemoveBlock(Vector3 worldPos) =>
            RemoveBlock(WorldToGlobal(worldPos));

        public void RemoveBlock(Vector3Int globalPlatePos)
        {
            var (coord, local) = GlobalToChunkLocal(globalPlatePos);
            var data = _worldData.GetChunk(coord);
            if (data == null) return;

            var cell = data.Get(local.x, local.y, local.z);
            int slots = BrickShapeInfo.HeightInPlates(cell.type);
            for (int dy = 0; dy < slots && local.y + dy < ChunkData.SizeY; dy++)
                data.Set(local.x, local.y + dy, local.z, BrickCell.Empty);

            MarkDirty(coord);
            MarkNeighborsDirtyIfEdge(coord, local);
        }

        public BrickCell GetBlock(Vector3 worldPos) =>
            GetBlock(WorldToGlobal(worldPos));

        public BrickCell GetBlock(Vector3Int globalPlatePos)
        {
            var (coord, local) = GlobalToChunkLocal(globalPlatePos);
            var data = _worldData?.GetChunk(coord);
            return data?.Get(local.x, local.y, local.z) ?? BrickCell.Empty;
        }

        // ── Internal ───────────────────────────────────────────────────────────

        public ChunkData GetChunkData(Vector3Int coord) => _worldData?.GetChunk(coord);

        private ChunkRenderer GetOrCreateRenderer(Vector3Int coord)
        {
            if (_renderers.TryGetValue(coord, out var r)) return r;

            var go = new GameObject($"Chunk_{coord.x}_{coord.z}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                coord.x * ChunkData.SizeX * WorldConstants.PlateWidth,
                0f,
                coord.z * ChunkData.SizeZ * WorldConstants.PlateDepth);

            r = go.AddComponent<ChunkRenderer>();
            r.Init(this, coord, _worldData.GetOrCreateChunk(coord));
            _renderers[coord] = r;
            _dirty.Add(coord);
            return r;
        }

        private void MarkDirty(Vector3Int coord) => _dirty.Add(coord);

        private void MarkNeighborsDirtyIfEdge(Vector3Int coord, Vector3Int local)
        {
            if (local.x == 0)                    _dirty.Add(coord + new Vector3Int(-1, 0,  0));
            if (local.x == ChunkData.SizeX - 1)  _dirty.Add(coord + new Vector3Int( 1, 0,  0));
            if (local.z == 0)                    _dirty.Add(coord + new Vector3Int( 0, 0, -1));
            if (local.z == ChunkData.SizeZ - 1)  _dirty.Add(coord + new Vector3Int( 0, 0,  1));
        }

        private void RebuildDirty()
        {
            foreach (var coord in _dirty)
            {
                if (_renderers.TryGetValue(coord, out var r))
                    r.Rebuild(_materialPalette);
            }
            _dirty.Clear();
        }

        private static Vector3Int WorldToGlobal(Vector3 worldPos)
        {
            const float pw = WorldConstants.PlateWidth;
            const float pd = WorldConstants.PlateDepth;
            const float ph = WorldConstants.PlateHeight;
            return new Vector3Int(
                Mathf.FloorToInt(worldPos.x / pw),
                Mathf.FloorToInt(worldPos.y / ph),
                Mathf.FloorToInt(worldPos.z / pd));
        }

        private static (Vector3Int chunk, Vector3Int local) GlobalToChunkLocal(Vector3Int g)
        {
            int cx = Mathf.FloorToInt((float)g.x / ChunkData.SizeX);
            int cz = Mathf.FloorToInt((float)g.z / ChunkData.SizeZ);
            return (new Vector3Int(cx, 0, cz),
                    new Vector3Int(g.x - cx * ChunkData.SizeX, g.y, g.z - cz * ChunkData.SizeZ));
        }
    }
}
