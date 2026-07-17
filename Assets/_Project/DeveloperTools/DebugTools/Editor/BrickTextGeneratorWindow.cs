using System.Collections.Generic;
using SpieleMarmelade.World;
using UnityEditor;
using UnityEngine;

namespace SpieleMarmelade.DevTools.Editor
{
    // Generates text/logos/buttons built entirely out of Wand-Baustein (Brick) bricks — each
    // letter is a fixed 3-wide x 5-tall grid (see BrickFont), with a mandatory 1-brick gap
    // between letters. The whole bounding box is filled: letter-stroke cells get one material,
    // every other cell gets a second (background) material, so the result reads as one solid
    // brick wall with the letters highlighted, not floating letters. No Scene-View painting
    // needed — purely deterministic, so Generate builds and saves the prefab in one click.
    public class BrickTextGeneratorWindow : EditorWindow
    {
        private const string BrickPrefabPath = "Assets/_Project/Shared/Prefabs/Bricks/Brick.prefab";
        private const string OutputFolder    = "Assets/_Project/Shared/Prefabs/Text";

        private string _text       = "PLAY";
        private string _objectName = "Text_PLAY";
        private int    _letterMatIndex;
        private int    _bgMatIndex;
        private bool   _asButton = true;
        private bool   _includeBackground = true;

        private Material[] _mats     = System.Array.Empty<Material>();
        private string[]   _matNames = System.Array.Empty<string>();

        private GUIStyle _paintBtn;
        private bool     _stylesReady;

        private static readonly Color BG     = new(0.13f, 0.13f, 0.19f);
        private static readonly Color Accent = new(0.45f, 0.75f, 1.00f);

        [MenuItem("Tools/Prefab Creation/Brick Text Generator")]
        public static void Open()
        {
            var w = GetWindow<BrickTextGeneratorWindow>("Brick Text Generator");
            w.minSize = new Vector2(320, 360);
        }

        private void OnEnable() => LoadMaterials();

        private void LoadMaterials()
        {
            var mats  = new List<Material>();
            var names = new List<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Shared/Materials" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                mats.Add(mat); names.Add(mat.name);
            }
            _mats     = mats.ToArray();
            _matNames = names.ToArray();
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _paintBtn = new GUIStyle(GUI.skin.button)
            { fixedHeight = 42, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        private void OnGUI()
        {
            EnsureStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BG);
            DrawHeader();
            DrawHelpText();

            GUILayout.Space(8);
            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
            using (new GUILayout.VerticalScope())
            {
                _text = EditorGUILayout.TextField("Text", _text).ToUpperInvariant();
                GUILayout.Space(6);

                if (_matNames.Length > 0)
                {
                    _letterMatIndex = EditorGUILayout.Popup("Buchstaben-Material", Mathf.Clamp(_letterMatIndex, 0, _matNames.Length - 1), _matNames);
                    using (new EditorGUI.DisabledScope(!_includeBackground))
                        _bgMatIndex = EditorGUILayout.Popup("Hintergrund-Material", Mathf.Clamp(_bgMatIndex, 0, _matNames.Length - 1), _matNames);
                }
                else
                {
                    EditorGUILayout.HelpBox("Keine Materialien unter Shared/Materials gefunden.", MessageType.Warning);
                }

                GUILayout.Space(6);
                _includeBackground = EditorGUILayout.ToggleLeft(
                    "Background-Bricks einbauen (aus = nur die Buchstaben-Pixel selbst, offene Lücken dazwischen)",
                    _includeBackground);

                GUILayout.Space(6);
                _asButton = EditorGUILayout.ToggleLeft("Als Button nutzbar (klickbar per Mausklick, BrickTextButton)", _asButton);

                GUILayout.Space(6);
                _objectName = EditorGUILayout.TextField("Name", _objectName);

                GUILayout.Space(12);
                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.30f, 0.70f, 0.45f);
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_text) || _mats.Length == 0 || string.IsNullOrWhiteSpace(_objectName)))
                {
                    if (GUILayout.Button("⬡  Generate", _paintBtn))
                        Generate();
                }
                GUI.backgroundColor = old;
            } GUILayout.Space(10); }
        }

        private void DrawHeader()
        {
            var r = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.09f, 0.09f, 0.14f));
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 3, r.width, 3), Accent);
            GUI.Label(r, "Brick Text Generator", new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        private void DrawHelpText()
        {
            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
                GUILayout.Label(
                    "Baut Text/Logos/Buttons komplett aus Wand-Bausteinen: jeder Buchstabe 5 hoch, " +
                    "3 breit, 1 Brick Abstand zwischen Buchstaben. Die ganze Fläche wird gefüllt — " +
                    "Buchstaben-Pixel bekommen Material A, der Rest Material B. " +
                    "A-Z, 0-9, Leerzeichen sowie ! ? . - werden unterstützt.",
                    new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.75f) } });
            GUILayout.Space(10); }
            GUILayout.Space(4);
        }

        private void Generate()
        {
            var brickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BrickPrefabPath);
            if (brickPrefab == null)
            {
                Debug.LogError($"[BrickTextGenerator] Brick-Prefab fehlt: {BrickPrefabPath}");
                return;
            }

            var letterMat = _mats[Mathf.Clamp(_letterMatIndex, 0, _mats.Length - 1)];
            var bgMat     = _mats[Mathf.Clamp(_bgMatIndex, 0, _mats.Length - 1)];

            var result = BrickTextBuilder.Build(brickPrefab, _text, letterMat, bgMat, _objectName, _includeBackground);
            if (_asButton) BrickTextBuilder.MakeClickable(result);

            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Shared/Prefabs", "Text");

            string path  = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{_objectName}.prefab");
            var    saved = PrefabUtility.SaveAsPrefabAssetAndConnect(result.Root, path, InteractionMode.UserAction);

            Selection.activeObject = saved;
            EditorUtility.DisplayDialog("Erstellt", $"Gespeichert unter:\n{path}", "OK");
        }
    }
}
