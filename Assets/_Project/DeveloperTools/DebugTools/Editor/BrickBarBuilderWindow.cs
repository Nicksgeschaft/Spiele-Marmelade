using SpieleMarmelade.Shared.World;
using SpieleMarmelade.World;
using UnityEditor;
using UnityEngine;

namespace SpieleMarmelade.DevTools.Editor
{
    // Builds a row/stack of N bricks as a reusable prefab — either a passive segment display
    // (BrickBar, e.g. a health bar) or a draggable value track (BrickSliderTrack, e.g. a
    // volume slider). Pure form tool, no Scene-View painting — same style as BrickVfxBuilderWindow.
    public class BrickBarBuilderWindow : EditorWindow
    {
        private enum Orientation { Row, Stack }
        private enum Mode { Bar, Slider }

        private BrickType _brickType = BrickType.PlateRound;
        private Material _material;
        private int _count = 10;
        private Orientation _orientation = Orientation.Row;
        private Mode _mode = Mode.Bar;
        private Material _handleMaterial;
        private string _barName = "Bar_Default";

        [MenuItem("Tools/Prefab Creation/Brick Bar Builder")]
        public static void Open()
        {
            var w = GetWindow<BrickBarBuilderWindow>("Brick Bar Builder");
            w.minSize = new Vector2(320, 320);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Baut eine Reihe/einen Stapel aus Bricks als Prefab. 'Bar' = Segmente, die " +
                "sich ein-/ausblenden lassen (z. B. Lebensbalken, siehe BrickHealthBarView). " +
                "'Slider' = dieselbe Schiene + ein Griff-Brick zum Ziehen (z. B. Lautstärke).",
                MessageType.Info);

            EditorGUILayout.Space();
            _brickType = (BrickType)EditorGUILayout.EnumPopup("Brick-Typ", _brickType);
            _material = (Material)EditorGUILayout.ObjectField("Material", _material, typeof(Material), false);
            _count = Mathf.Max(1, EditorGUILayout.IntField("Anzahl", _count));
            _orientation = (Orientation)EditorGUILayout.EnumPopup("Ausrichtung", _orientation);

            EditorGUILayout.Space();
            _mode = (Mode)EditorGUILayout.EnumPopup("Modus", _mode);
            if (_mode == Mode.Slider)
            {
                _handleMaterial = (Material)EditorGUILayout.ObjectField(
                    "Griff-Material (leer = Fallback)", _handleMaterial, typeof(Material), false);
            }

            EditorGUILayout.Space();
            _barName = EditorGUILayout.TextField("Name", _barName);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_barName)))
            {
                if (GUILayout.Button("Als Prefab speichern"))
                    BuildAndSave();
            }
        }

        private void BuildAndSave()
        {
            var brickPrefab = LoadBrickPrefab(_brickType);
            if (brickPrefab == null)
            {
                EditorUtility.DisplayDialog("Fehler", $"Brick-Prefab für '{_brickType}' nicht gefunden.", "OK");
                return;
            }

            float spacingXZ = WorldConstants.PlateWidth;
            float spacingY = BrickShapeInfo.HeightInPlates(_brickType) * WorldConstants.PlateHeight;
            float spacing = _orientation == Orientation.Row ? spacingXZ : spacingY;
            Vector3 axisDir = _orientation == Orientation.Row ? Vector3.right : Vector3.up;

            var root = new GameObject(_barName);
            var segments = new GameObject[_count];

            for (int i = 0; i < _count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(brickPrefab, root.transform);
                go.transform.localPosition = axisDir * (i * spacing);
                ApplyMaterial(go, _material);

                var marker = go.GetComponent<PlacedBrick>();
                if (marker != null) marker.shape = _brickType;

                segments[i] = go;
            }

            if (_mode == Mode.Bar)
            {
                var bar = root.AddComponent<BrickBar>();
                var so = new SerializedObject(bar);
                var segProp = so.FindProperty("segments");
                segProp.arraySize = segments.Length;
                for (int i = 0; i < segments.Length; i++)
                    segProp.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                var handleGo = (GameObject)PrefabUtility.InstantiatePrefab(brickPrefab, root.transform);
                handleGo.name = "Handle";
                Material handleMat = _handleMaterial != null ? _handleMaterial : FindMaterial("M_Special_GlowWhite");
                ApplyMaterial(handleGo, handleMat);

                // Offset sideways so the handle doesn't z-fight with the background bricks.
                Vector3 sideOffset = _orientation == Orientation.Row ? Vector3.back : Vector3.right;
                handleGo.transform.localPosition = sideOffset * spacingXZ;

                if (handleGo.GetComponent<Collider>() == null)
                    handleGo.AddComponent<BoxCollider>();

                var track = root.AddComponent<BrickSliderTrack>();
                var trackSo = new SerializedObject(track);
                trackSo.FindProperty("handle").objectReferenceValue = handleGo.transform;
                trackSo.FindProperty("axis").vector3Value = axisDir;
                trackSo.FindProperty("trackLength").floatValue = (_count - 1) * spacing;
                trackSo.ApplyModifiedPropertiesWithoutUndo();
            }

            const string folder = "Assets/_Project/Shared/Prefabs/UIBars";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/Shared/Prefabs", "UIBars");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_barName}.prefab");
            var saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            string extra = _mode == Mode.Slider
                ? "\n\nWichtig: 'Raycast Camera' auf dem BrickSliderTrack im Inspector zuweisen " +
                  "(z. B. die MenuCamera), sonst fällt es auf Camera.main zurück."
                : "\n\nFür einen Lebensbalken: BrickHealthBarView draufpacken und 'Health' zuweisen.";
            EditorUtility.DisplayDialog("Gespeichert", $"Gespeichert unter:\n{path}{extra}", "OK");
            Selection.activeObject = saved;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                mr.sharedMaterial = mat;
        }

        private static GameObject LoadBrickPrefab(BrickType type)
        {
            string name = type switch
            {
                BrickType.Plate => "Plate",
                BrickType.Brick => "Brick",
                BrickType.PlateRound => "PlateRound",
                BrickType.BrickRound => "BrickRound",
                _ => null,
            };
            return name == null ? null : AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/_Project/Shared/Prefabs/Bricks/{name}.prefab");
        }

        private static Material FindMaterial(string name)
        {
            var guids = AssetDatabase.FindAssets($"{name} t:Material", new[] { "Assets/_Project/Shared/Materials" });
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
