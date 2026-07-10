using System.Collections.Generic;
using UnityEngine;

namespace GameJamUniverse.World
{
    // Generates combined meshes for a chunk with hidden-face culling.
    // Each exposed quad is a flat rectangle — no stud geometry.
    // Returns one Mesh per unique material index used.
    public static class ChunkMeshBuilder
    {
        private const float PW = WorldConstants.PlateWidth;
        private const float PH = WorldConstants.PlateHeight;

        // Result: list of (mesh, materialIndex) pairs
        public static List<(Mesh mesh, int materialIndex)> Build(ChunkData chunk, ChunkData[] neighbors)
        {
            // neighbors: [+X, -X, +Z, -Z] chunks (null = treat as air)
            var perMat = new Dictionary<int, MeshAccumulator>();

            for (int x = 0; x < ChunkData.SizeX; x++)
            for (int z = 0; z < ChunkData.SizeZ; z++)
            for (int y = 0; y < ChunkData.SizeY; y++)
            {
                var cell = chunk.Get(x, y, z);
                if (cell.IsEmpty) continue;

                // Tall bricks mark y,y+1,y+2 — skip the filler cells
                if (y > 0)
                {
                    var below = chunk.Get(x, y - 1, z);
                    if (!below.IsEmpty &&
                        BrickShapeInfo.HeightInPlates(below.type) > 1 &&
                        below.materialIndex == cell.materialIndex)
                        continue; // filler slot, already covered
                }

                int matIdx = cell.materialIndex;
                if (!perMat.TryGetValue(matIdx, out var acc))
                {
                    acc = new MeshAccumulator();
                    perMat[matIdx] = acc;
                }

                float brickH  = BrickShapeInfo.HeightInPlates(cell.type) * PH;
                float wx      = x * PW;
                float wy      = y * PH;
                float wz      = z * PW;

                // Top face — exposed if cell above is empty
                int topY = y + BrickShapeInfo.HeightInPlates(cell.type);
                if (IsSolidAt(chunk, neighbors, x, topY, z) == false)
                    acc.AddQuad(
                        new Vector3(wx,      wy + brickH, wz),
                        new Vector3(wx + PW, wy + brickH, wz),
                        new Vector3(wx + PW, wy + brickH, wz + PW),
                        new Vector3(wx,      wy + brickH, wz + PW),
                        Vector3.up);

                // Bottom face
                if (IsSolidAt(chunk, neighbors, x, y - 1, z) == false)
                    acc.AddQuad(
                        new Vector3(wx,      wy, wz + PW),
                        new Vector3(wx + PW, wy, wz + PW),
                        new Vector3(wx + PW, wy, wz),
                        new Vector3(wx,      wy, wz),
                        Vector3.down);

                // +X face
                if (IsSolidAt(chunk, neighbors, x + 1, y, z) == false)
                    acc.AddQuad(
                        new Vector3(wx + PW, wy,          wz + PW),
                        new Vector3(wx + PW, wy + brickH, wz + PW),
                        new Vector3(wx + PW, wy + brickH, wz),
                        new Vector3(wx + PW, wy,          wz),
                        Vector3.right);

                // -X face
                if (IsSolidAt(chunk, neighbors, x - 1, y, z) == false)
                    acc.AddQuad(
                        new Vector3(wx, wy,          wz),
                        new Vector3(wx, wy + brickH, wz),
                        new Vector3(wx, wy + brickH, wz + PW),
                        new Vector3(wx, wy,          wz + PW),
                        Vector3.left);

                // +Z face
                if (IsSolidAt(chunk, neighbors, x, y, z + 1) == false)
                    acc.AddQuad(
                        new Vector3(wx,      wy,          wz + PW),
                        new Vector3(wx,      wy + brickH, wz + PW),
                        new Vector3(wx + PW, wy + brickH, wz + PW),
                        new Vector3(wx + PW, wy,          wz + PW),
                        Vector3.forward);

                // -Z face
                if (IsSolidAt(chunk, neighbors, x, y, z - 1) == false)
                    acc.AddQuad(
                        new Vector3(wx + PW, wy,          wz),
                        new Vector3(wx + PW, wy + brickH, wz),
                        new Vector3(wx,      wy + brickH, wz),
                        new Vector3(wx,      wy,          wz),
                        Vector3.back);
            }

            var result = new List<(Mesh, int)>();
            foreach (var kvp in perMat)
            {
                var mesh = kvp.Value.ToMesh();
                if (mesh != null) result.Add((mesh, kvp.Key));
            }
            return result;
        }

        // Returns true if the given plate-unit position contains a solid block.
        // neighbors: [+X=0, -X=1, +Z=2, -Z=3]
        private static bool IsSolidAt(ChunkData chunk, ChunkData[] neighbors, int x, int y, int z)
        {
            if (y < 0 || y >= ChunkData.SizeY) return false;

            if (ChunkData.InBounds(x, y, z))
                return !chunk.IsEmpty(x, y, z);

            // Cross-chunk lookup
            if (x >= ChunkData.SizeX)
            {
                var nb = neighbors?[0];
                return nb != null && !nb.IsEmpty(x - ChunkData.SizeX, y, z);
            }
            if (x < 0)
            {
                var nb = neighbors?[1];
                return nb != null && !nb.IsEmpty(x + ChunkData.SizeX, y, z);
            }
            if (z >= ChunkData.SizeZ)
            {
                var nb = neighbors?[2];
                return nb != null && !nb.IsEmpty(x, y, z - ChunkData.SizeZ);
            }
            if (z < 0)
            {
                var nb = neighbors?[3];
                return nb != null && !nb.IsEmpty(x, y, z + ChunkData.SizeZ);
            }
            return false;
        }

        private class MeshAccumulator
        {
            private readonly List<Vector3> _verts  = new();
            private readonly List<int>     _tris   = new();
            private readonly List<Vector3> _norms  = new();
            private readonly List<Vector2> _uvs    = new();

            public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                int i = _verts.Count;
                _verts.AddRange(new[] { a, b, c, d });
                _norms.AddRange(new[] { normal, normal, normal, normal });
                _uvs.AddRange(new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up });
                _tris.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }

            public Mesh ToMesh()
            {
                if (_verts.Count == 0) return null;
                var mesh = new Mesh();
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.SetVertices(_verts);
                mesh.SetNormals(_norms);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_tris, 0);
                mesh.RecalculateBounds();
                return mesh;
            }
        }
    }
}
