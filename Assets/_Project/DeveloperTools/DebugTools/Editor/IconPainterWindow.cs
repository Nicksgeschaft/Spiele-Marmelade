using System.Collections.Generic;
using GameJamUniverse.World;
using UnityEditor;
using UnityEngine;

namespace GameJamUniverse.DevTools.Editor
{
    // Builds a blank W x H grid of Wand-Bausteine (Brick — same scale as the Brick Text
    // Generator, so icons and text signs sit flush together) and lets you click individual
    // bricks in the Scene View to recolor them: simple pixel-art icon painting out of the same
    // 4 bricks used everywhere else.
    public class IconPainterWindow : EditorWindow
    {
        private const string BrickPrefabPath = "Assets/_Project/Shared/Prefabs/Bricks/Brick.prefab";
        private const string OutputFolder    = "Assets/_Project/Shared/Prefabs/Icons";

        private int    _width  = 8;
        private int    _height = 8;
        private int    _bgMatIndex;
        private int    _paintMatIndex;
        private bool   _active;
        private Transform _root;
        private string _iconName = "Icon_MyGame";
        private Vector2 _scroll;

        private readonly List<Material> _mats      = new();
        private readonly List<Color>    _matColors = new();
        private readonly List<string>   _matNames  = new();

        private GUIStyle _paintBtn;
        private bool     _stylesReady;

        private static readonly Color BG      = new(0.13f, 0.13f, 0.19f);
        private static readonly Color Accent  = new(0.45f, 0.75f, 1.00f);
        private static readonly Color GreenC  = new(0.30f, 0.88f, 0.50f);

        [MenuItem("Tools/GameJam/Icon Painter")]
        public static void Open()
        {
            var w = GetWindow<IconPainterWindow>("Icon Painter");
            w.minSize = new Vector2(300, 480);
        }

        private void OnEnable()
        {
            LoadMaterials();
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            _active = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        // ── Asset loading ──────────────────────────────────────────────────
        private void LoadMaterials()
        {
            _mats.Clear(); _matColors.Clear(); _matNames.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/_Project/Shared/Materials" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                Color c = Color.gray;
                if      (mat.HasProperty("_BaseColor")) c = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))     c = mat.GetColor("_Color");
                _mats.Add(mat); _matColors.Add(c); _matNames.Add(mat.name);
            }
        }

        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _paintBtn = new GUIStyle(GUI.skin.button)
            { fixedHeight = 42, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        // ── OnGUI ──────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();
            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BG);
            DrawHeader();
            DrawHelpText();

            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Space(8);
            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
            using (new GUILayout.VerticalScope())
            {
                DrawSectionTitle("GRÖSSE (in Bricks)");
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Breite", GUILayout.Width(50));
                    _width = EditorGUILayout.IntField(_width, GUILayout.Width(40));
                    GUILayout.Space(10);
                    GUILayout.Label("Höhe", GUILayout.Width(40));
                    _height = EditorGUILayout.IntField(_height, GUILayout.Width(40));
                }
                _width  = Mathf.Clamp(_width, 1, 64);
                _height = Mathf.Clamp(_height, 1, 64);
                GUILayout.Space(8);

                _iconName = EditorGUILayout.TextField("Name", _iconName);
                GUILayout.Space(8);

                DrawSectionTitle("HINTERGRUND-FARBE");
                DrawPalette(ref _bgMatIndex);
                GUILayout.Space(8);

                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.30f, 0.55f, 0.85f);
                using (new EditorGUI.DisabledScope(_mats.Count == 0))
                {
                    if (GUILayout.Button("⬡  Build Grid", _paintBtn))
                        BuildGrid();
                }
                GUI.backgroundColor = old;
                GUILayout.Space(12);

                DrawSectionTitle("PAINT-FARBE");
                DrawPalette(ref _paintMatIndex);
                GUILayout.Space(8);

                DrawRootRow();
                GUILayout.Space(8);

                DrawActivateBtn();
                GUILayout.Space(12);
                DrawSectionTitle("ALS PREFAB SPEICHERN");
                DrawSaveSection();
            } GUILayout.Space(10); }
            GUILayout.Space(12);
            GUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            var r = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.09f, 0.09f, 0.14f));
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 3, r.width, 3), Accent);
            GUI.Label(r, "Icon Painter", new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        private void DrawHelpText()
        {
            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
                GUILayout.Label(
                    "Breite/Höhe wählen → Hintergrund-Farbe wählen → Build Grid baut ein " +
                    "Gitter aus Bricks. Danach Paint-Farbe wählen → Start Painting → im " +
                    "Scene-View auf einzelne Bricks klicken/ziehen, um sie umzufärben — Pixel Art " +
                    "aus Bricks.",
                    new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.75f) } });
            GUILayout.Space(10); }
            GUILayout.Space(4);
        }

        private void DrawSectionTitle(string t) =>
            GUILayout.Label(t, new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, normal = { textColor = new Color(0.55f, 0.55f, 0.75f) } });

        private void DrawRootRow()
        {
            DrawSectionTitle("ICON ROOT");
            using (new GUILayout.HorizontalScope())
            {
                _root = (Transform)EditorGUILayout.ObjectField(_root, typeof(Transform), true, GUILayout.Height(20));
            }
            if (_root == null)
                EditorGUILayout.HelpBox("Wird beim ersten 'Build Grid' automatisch angelegt.", MessageType.None);
        }

        private void DrawPalette(ref int selIndex)
        {
            if (_mats.Count == 0)
            {
                EditorGUILayout.HelpBox("Keine Materialien unter Shared/Materials gefunden.", MessageType.Warning);
                return;
            }

            float availW = position.width - 20f;
            int   rows   = Mathf.CeilToInt(_mats.Count / Mathf.Max(1f, Mathf.Floor(availW / 32f)));
            var   prect  = GUILayoutUtility.GetRect(availW, rows * 32f + 8f);

            float dx = 10f, dy = 4f;
            for (int i = 0; i < _mats.Count; i++)
            {
                var r = new Rect(prect.x + dx, prect.y + dy, 28f, 28f);
                EditorGUI.DrawRect(r, _matColors[i]);
                DrawBorder(r, i == selIndex ? Color.white : new Color(0f, 0f, 0f, 0.35f), i == selIndex ? 2f : 1f);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                { selIndex = i; Repaint(); Event.current.Use(); }
                GUI.Label(r, new GUIContent("", _matNames[i]));
                dx += 32f;
                if (i < _mats.Count - 1 && dx + 28f > availW - 4f) { dx = 10f; dy += 32f; }
            }
            if (selIndex >= 0 && selIndex < _matNames.Count)
                GUILayout.Label(_matNames[selIndex], new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { normal = { textColor = new Color(0.75f, 0.75f, 0.9f) } });
        }

        private void DrawActivateBtn()
        {
            using (new EditorGUI.DisabledScope(_root == null))
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = _active ? GreenC : new Color(0.3f, 0.3f, 0.5f);
                if (GUILayout.Button(_active ? "● PAINTING ACTIVE — Click to Stop" : "Start Painting", _paintBtn))
                { _active = !_active; SceneView.RepaintAll(); }
                GUI.backgroundColor = old;
            }
        }

        private void DrawSaveSection()
        {
            using (new EditorGUI.DisabledScope(_root == null || _root.childCount == 0 || string.IsNullOrWhiteSpace(_iconName)))
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.55f, 0.85f);
                if (GUILayout.Button("⬡  Save as Prefab", _paintBtn))
                    SaveAsPrefab();
                GUI.backgroundColor = old;
            }
        }

        // ── Grid building ────────────────────────────────────────────────────
        private void BuildGrid()
        {
            var brickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BrickPrefabPath);
            if (brickPrefab == null)
            {
                Debug.LogError($"[IconPainter] Brick-Prefab fehlt: {BrickPrefabPath}");
                return;
            }
            if (_mats.Count == 0) return;

            if (_root == null)
            {
                var go = new GameObject($"IconRoot_{_iconName}");
                Undo.RegisterCreatedObjectUndo(go, "Create Icon Root");
                _root = go.transform;
            }
            else
            {
                for (int i = _root.childCount - 1; i >= 0; i--)
                    Undo.DestroyObjectImmediate(_root.GetChild(i).gameObject);
            }

            var bgMat = _mats[Mathf.Clamp(_bgMatIndex, 0, _mats.Count - 1)];
            float colStep = WorldConstants.PlateWidth;
            float rowStep = BrickShapeInfo.HeightInPlates(BrickType.Brick) * WorldConstants.PlateHeight;

            for (int y = 0; y < _height; y++)
            for (int x = 0; x < _width; x++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(brickPrefab);
                go.transform.SetParent(_root, false);
                go.transform.localPosition = new Vector3(x * colStep, y * rowStep, 0f);
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                    mr.sharedMaterial = bgMat;

                var marker = go.GetComponent<PlacedBrick>();
                if (marker != null) marker.shape = BrickType.Brick;

                Undo.RegisterCreatedObjectUndo(go, "Build Icon Grid");
            }
        }

        private void SaveAsPrefab()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Shared/Prefabs", "Icons");

            string path  = AssetDatabase.GenerateUniqueAssetPath($"{OutputFolder}/{_iconName}.prefab");
            var    saved = PrefabUtility.SaveAsPrefabAssetAndConnect(_root.gameObject, path, InteractionMode.UserAction);

            EditorUtility.DisplayDialog("Gespeichert", $"Icon gespeichert unter:\n{path}", "OK");
            Selection.activeObject = saved;
        }

        // ── Scene GUI ──────────────────────────────────────────────────────
        private void OnSceneGUI(SceneView sv)
        {
            if (!_active || _root == null) return;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var e = Event.current;
            DrawSceneHUD(sv);

            bool clicked = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                           && e.button == 0 && !e.alt;
            if (clicked)
            {
                var picked = HandleUtility.PickGameObject(e.mousePosition, false);
                if (picked != null)
                {
                    var marker = picked.GetComponentInParent<PlacedBrick>();
                    if (marker != null && marker.transform.IsChildOf(_root))
                    {
                        PaintBrick(marker.gameObject);
                        e.Use();
                    }
                }
            }
            sv.Repaint();
        }

        private void PaintBrick(GameObject brickGo)
        {
            if (_paintMatIndex < 0 || _paintMatIndex >= _mats.Count) return;
            var mat = _mats[_paintMatIndex];

            foreach (var mr in brickGo.GetComponentsInChildren<MeshRenderer>())
            {
                Undo.RecordObject(mr, "Paint Icon Pixel");
                mr.sharedMaterial = mat;
            }
        }

        private void DrawSceneHUD(SceneView sv)
        {
            Handles.BeginGUI();
            string paintMat = _paintMatIndex < _matNames.Count ? _matNames[_paintMatIndex] : "—";
            string text     = $"[Icon Painter] {_width}x{_height}  Paint: {paintMat}";
            float  w        = Mathf.Min(sv.position.width - 20f, 420f), h = 26f;
            var    r        = new Rect(10f, sv.position.height - h - 36f, w, h);
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.62f));
            DrawBorder(r, new Color(0.4f, 0.92f, 0.5f, 0.9f), 1f);
            GUI.Label(new Rect(r.x + 8f, r.y + 5f, r.width, r.height), text,
                new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.4f, 0.95f, 0.5f) } });
            Handles.EndGUI();
        }

        // ── Util ───────────────────────────────────────────────────────────
        private static void DrawBorder(Rect r, Color c, float w)
        {
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        r.width, w),  c);
            EditorGUI.DrawRect(new Rect(r.x,        r.yMax - w, r.width, w),  c);
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        w, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y,        w, r.height), c);
        }
    }
}
