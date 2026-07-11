using SpieleMarmelade.Shared.VFX;
using UnityEditor;
using UnityEngine;

namespace SpieleMarmelade.DevTools.Editor
{
    // Configures a BrickShatterEffect and saves it as a reusable prefab. No interactive Scene
    // View painting here (unlike Level/Character Builder) — physics doesn't simulate outside
    // Play Mode, so "Vorschau" only shows fragment count/size/color, not real motion.
    public class BrickVfxBuilderWindow : EditorWindow
    {
        private const string PreviewRootName = "__BrickShatterPreview";

        private int _fragmentCount = 8;
        private float _fragmentSize = SpieleMarmelade.World.WorldConstants.PlateWidth;
        private Vector2 _forceRange = new(1f, 3f);
        private Vector2 _torqueRange = new(1f, 4f);
        private float _lifetime = 2f;
        private Color _previewColor = Color.white;
        private string _effectName = "Shatter_Default";

        [MenuItem("Tools/Prefab Creation/Brick VFX Builder")]
        public static void Open()
        {
            var w = GetWindow<BrickVfxBuilderWindow>("Brick VFX Builder");
            w.minSize = new Vector2(320, 260);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Baut einen Brick-Shatter-Effekt (Objekt zerfällt in kleine Brick-Fragmente). " +
                "Als Prefab speichern, dann z. B. Health.OnDeath im Inspector auf " +
                "BrickShatterEffect.Shatter() legen.",
                MessageType.Info);

            EditorGUILayout.Space();
            _fragmentCount = EditorGUILayout.IntSlider("Fragment-Anzahl", _fragmentCount, 1, 40);
            _fragmentSize = EditorGUILayout.FloatField("Fragment-Größe", _fragmentSize);
            _forceRange = EditorGUILayout.Vector2Field("Kraft (min/max)", _forceRange);
            _torqueRange = EditorGUILayout.Vector2Field("Drehimpuls (min/max)", _torqueRange);
            _lifetime = EditorGUILayout.FloatField("Lebensdauer (s)", _lifetime);
            _previewColor = EditorGUILayout.ColorField("Vorschau-Farbe", _previewColor);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Vorschau")) SpawnPreview();
                if (GUILayout.Button("Vorschau entfernen")) ClearPreview();
            }

            EditorGUILayout.Space();
            _effectName = EditorGUILayout.TextField("Name", _effectName);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_effectName)))
            {
                if (GUILayout.Button("Als Prefab speichern"))
                {
                    SaveAsPrefab();
                }
            }
        }

        private void SpawnPreview()
        {
            ClearPreview();

            var root = new GameObject(PreviewRootName) { hideFlags = HideFlags.DontSave };
            for (int i = 0; i < _fragmentCount; i++)
            {
                GameObject fragment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fragment.hideFlags = HideFlags.DontSave;
                fragment.transform.SetParent(root.transform);
                fragment.transform.localPosition = Random.insideUnitSphere * (_fragmentSize * 4f);
                fragment.transform.localScale = Vector3.one * _fragmentSize;

                var renderer = fragment.GetComponent<MeshRenderer>();
                renderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"))
                {
                    color = _previewColor,
                    hideFlags = HideFlags.DontSave
                };

                Object.DestroyImmediate(fragment.GetComponent<Collider>());
            }

            Selection.activeGameObject = root;
            SceneView.lastActiveSceneView?.FrameSelected();
        }

        private void ClearPreview()
        {
            var existing = GameObject.Find(PreviewRootName);
            if (existing != null) Object.DestroyImmediate(existing);
        }

        private void SaveAsPrefab()
        {
            const string folder = "Assets/_Project/Shared/Prefabs/VFX";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Shared/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets/_Project/Shared", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/_Project/Shared/Prefabs", "VFX");
            }

            var temp = new GameObject(_effectName);
            var effect = temp.AddComponent<BrickShatterEffect>();

            var so = new SerializedObject(effect);
            so.FindProperty("fragmentCount").intValue = _fragmentCount;
            so.FindProperty("fragmentSize").floatValue = _fragmentSize;
            so.FindProperty("forceRange").vector2Value = _forceRange;
            so.FindProperty("torqueRange").vector2Value = _torqueRange;
            so.FindProperty("lifetime").floatValue = _lifetime;
            so.ApplyModifiedPropertiesWithoutUndo();

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_effectName}.prefab");
            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);

            EditorUtility.DisplayDialog("Gespeichert",
                $"Brick-VFX-Prefab gespeichert unter:\n{path}\n\n" +
                "Component 'BrickShatterEffect' auf ein Enemy/Player-Prefab packen (oder dieses " +
                "Prefab dort referenzieren) und z. B. Health.OnDeath im Inspector auf " +
                "Shatter() legen.",
                "OK");
            Selection.activeObject = saved;
        }
    }
}
