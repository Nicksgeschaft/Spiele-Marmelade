using SpieleMarmelade.World;
using UnityEditor;
using UnityEngine;

namespace SpieleMarmelade.DevTools.Editor
{
    // Builds small starter levels for the New Game Wizard out of the same 4 brick prefabs the
    // Level Painter uses. Procedural rather than a pre-baked prefab, so it always reflects
    // whatever currently lives in Shared/Prefabs/Bricks instead of a frozen hand-placed layout.
    public static class StarterLevelBuilder
    {
        private const string BricksFolder = "Assets/_Project/Shared/Prefabs/Bricks";

        // A short flat run with one step to jump onto, plus a round decoration piece so all
        // 4 brick shapes show up at least once.
        public static GameObject BuildPlatformerStarter(Transform parent)
        {
            var root = new GameObject("StarterLevel_Platformer");
            if (parent != null) root.transform.SetParent(parent, false);

            var plate      = Load("Plate");
            var brick      = Load("Brick");
            var plateRound = Load("PlateRound");

            float pw = WorldConstants.PlateWidth;
            float ph = WorldConstants.PlateHeight;

            // Flat ground: a row of plates to run on.
            for (int i = 0; i < 12; i++)
                Place(plate, root.transform, new Vector3(i * pw, 0f, 0f));

            // A one-brick-tall step with a small landing plate on top, to jump onto.
            Place(brick, root.transform, new Vector3(6 * pw, ph, 0f));
            Place(plate, root.transform, new Vector3(6 * pw, ph + BrickShapeInfo.HeightInPlates(BrickType.Brick) * ph, 0f));

            // Decoration, sitting on the ground row.
            Place(plateRound, root.transform, new Vector3(2 * pw, ph, 0f));

            return root;
        }

        // A small square arena bordered by brick walls (Bomberman-style): floor tiles inside,
        // a wall ring around the edge so movement/collision can be tested immediately.
        public static GameObject BuildTopDownGridStarter(Transform parent)
        {
            var root = new GameObject("StarterLevel_TopDownGrid");
            if (parent != null) root.transform.SetParent(parent, false);

            var plate      = Load("Plate");
            var brick      = Load("Brick");
            var plateRound = Load("PlateRound");

            float pw = WorldConstants.PlateWidth;
            float ph = WorldConstants.PlateHeight;
            const int size = 6;

            for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                Place(plate, root.transform, new Vector3(x * pw, 0f, z * pw));

            for (int x = -1; x <= size; x++)
            {
                Place(brick, root.transform, new Vector3(x * pw, ph, -1 * pw));
                Place(brick, root.transform, new Vector3(x * pw, ph, size * pw));
            }
            for (int z = 0; z < size; z++)
            {
                Place(brick, root.transform, new Vector3(-1 * pw, ph, z * pw));
                Place(brick, root.transform, new Vector3(size * pw, ph, z * pw));
            }

            Place(plateRound, root.transform, new Vector3(2 * pw, ph, 2 * pw));

            return root;
        }

        // A flat, open area with a couple of decoration pieces — used by the free-roaming
        // archetypes (Top-Down Free, Free 3D/Third-Person) that don't need walls to test movement.
        public static GameObject BuildOpenGroundStarter(Transform parent, string name)
        {
            var root = new GameObject(name);
            if (parent != null) root.transform.SetParent(parent, false);

            var plate      = Load("Plate");
            var plateRound = Load("PlateRound");

            float pw = WorldConstants.PlateWidth;
            float ph = WorldConstants.PlateHeight;
            const int size = 8;

            for (int x = 0; x < size; x++)
            for (int z = 0; z < size; z++)
                Place(plate, root.transform, new Vector3(x * pw, 0f, z * pw));

            Place(plateRound, root.transform, new Vector3(2 * pw, ph, 2 * pw));
            Place(plateRound, root.transform, new Vector3(5 * pw, ph, 5 * pw));

            return root;
        }

        private static GameObject Load(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{BricksFolder}/{name}.prefab");
            if (prefab == null) Debug.LogWarning($"[StarterLevelBuilder] Brick-Prefab fehlt: {name}");
            return prefab;
        }

        private static void Place(GameObject prefab, Transform parent, Vector3 localPos)
        {
            if (prefab == null) return;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = localPos;
        }
    }
}
