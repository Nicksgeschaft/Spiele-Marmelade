using System.Collections.Generic;
using System.Linq;
using SpieleMarmelade.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.DevTools.Editor
{
    // Ground-grid painting tool for floors/walls (and anything else — small assemblies like
    // characters work fine too via Parent Object + Stack/Replace/Line/Mauer erhöhen).
    public class AssetBuilderWindow : EditorWindow
    {
        // ── Enums ──────────────────────────────────────────────────────────
        private enum BrushMode  { Paint, Erase, RaiseWall }
        private enum StackMode  { Stack, Replace, ReplaceOnly, ReplaceStack }
        private enum BrushShape { Single, Rect, Circle, Line }
        private enum LineAxis   { X, Y, Z }

        // ── Tile footprint at raw OBJ scale (measured from the mesh bounds) ─
        // All 4 brick shapes share the same footprint — only height differs, and that's
        // driven by BrickShapeInfo so it never has to be hand-kept in sync. The footprint
        // itself is NOT a perfect square (X=0.0795, Z=0.079), so X and Z need separate
        // constants or adjacent tiles leave a hairline gap/overlap on one axis.
        private const float TileWidthX = WorldConstants.PlateWidth;
        private const float TileWidthZ = WorldConstants.PlateDepth;

        // ── State ──────────────────────────────────────────────────────────
        private BrickType _tileType   = BrickType.Plate;
        private BrushMode  _brushMode  = BrushMode.Paint;
        private StackMode  _stackMode  = StackMode.Stack;
        private BrushShape _brushShape = BrushShape.Single;
        private int        _brushRadius  = 0;
        private int        _replaceDepth = 3;
        private LineAxis   _lineAxis     = LineAxis.X;
        private int        _lineLength   = 10;
        private float      _gridStep    = 0.0795f;
        private int        _selMat      = 0;
        private bool       _active      = false;
        private Transform  _root;
        private Vector2    _scroll;
        private Vector3    _lastCenter  = Vector3.positiveInfinity;

        // Z always uses a slightly different step than X (see TileWidthX/TileWidthZ) —
        // both scale together from the single "Step" field so the aspect ratio stays correct
        // even if the user dials the grid step up or down.
        private float StepX => Mathf.Max(0.001f, _gridStep);
        private float StepZ => Mathf.Max(0.001f, _gridStep * (TileWidthZ / TileWidthX));

        // ── Bake settings ──────────────────────────────────────────────────
        private bool _bakeHideOriginal = true;
        private bool _bakeSetStatic    = false;
        private bool _bakeAddCollider  = true;

        // ── Spatial index: grid key → top-of-stack world Y ─────────────────
        // O(1) lookup during hover instead of O(n) iteration
        private readonly Dictionary<Vector2Int, float> _topYIndex = new();
        private bool      _indexDirty  = true;
        private Transform _lastRoot;
        private float     _lastStep    = -1f;

        // Cached result of the last PickGameObject call for precision Replace/Erase — only
        // re-picked on Mouse* events (see HandlePrecisionTarget) and reused on Layout/
        // Repaint, since PickGameObject does a real render pass and calling it on every
        // single event (incl. Layout) corrupts Unity's GUI window stack.
        private GameObject _replaceHoverTile;

        // Raise Wall: same event-guarded pick as _replaceHoverTile, but the flood-fill result
        // (the whole connected same-height group) is only recomputed when the hovered tile
        // actually changes — it's an O(n) scan per cell, too expensive to redo every repaint.
        private GameObject       _raiseWallHoverTile;
        private List<GameObject> _raiseWallGroup;
        private bool             _raiseWallBlocked;

        // ── Assets ─────────────────────────────────────────────────────────
        private readonly Dictionary<BrickType, GameObject> _prefabs   = new();
        private List<Material> _mats      = new();
        private List<Color>    _matColors = new();
        private List<string>   _matNames  = new();

        // ── Styles ─────────────────────────────────────────────────────────
        private GUIStyle _segBtn;
        private GUIStyle _segBtnSel;
        private GUIStyle _paintBtn;
        private GUIStyle _warnBtn;
        private bool     _stylesReady;

        // ── Colours ────────────────────────────────────────────────────────
        private static readonly Color BG      = new(0.13f, 0.13f, 0.19f);
        private static readonly Color Accent  = new(0.45f, 0.75f, 1.00f);
        private static readonly Color AccentB = new(1.00f, 0.60f, 0.25f);
        private static readonly Color GreenC  = new(0.30f, 0.88f, 0.50f);
        private static readonly Color RedC    = new(1.00f, 0.35f, 0.35f);
        private static readonly Color YellowC = new(1.00f, 0.85f, 0.20f);
        private static readonly Color PurpleC = new(0.70f, 0.45f, 1.00f);
        private static readonly Color OrangeC = new(1.00f, 0.55f, 0.15f);

        // ── Menu ───────────────────────────────────────────────────────────
        [MenuItem("Tools/Level Creation/Asset Builder")]
        public static void Open()
        {
            var w = GetWindow<AssetBuilderWindow>("Asset Builder");
            w.minSize = new Vector2(285, 460);
        }

        // ── Lifecycle ──────────────────────────────────────────────────────
        void OnEnable()
        {
            LoadAssets();
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed   += OnUndoRedo;
            _indexDirty = true;
        }

        void OnDisable()
        {
            _active = false;
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed   -= OnUndoRedo;
        }

        void OnUndoRedo() => _indexDirty = true;

        // ── Asset loading ──────────────────────────────────────────────────
        void LoadAssets()
        {
            _prefabs.Clear();
            LoadPrefab(BrickType.Plate,      "Assets/_Project/Shared/Prefabs/Bricks/Plate.prefab");
            LoadPrefab(BrickType.Brick,      "Assets/_Project/Shared/Prefabs/Bricks/Brick.prefab");
            LoadPrefab(BrickType.PlateRound, "Assets/_Project/Shared/Prefabs/Bricks/PlateRound.prefab");
            LoadPrefab(BrickType.BrickRound, "Assets/_Project/Shared/Prefabs/Bricks/BrickRound.prefab");

            _mats.Clear(); _matColors.Clear(); _matNames.Clear();
            foreach (var guid in AssetDatabase.FindAssets("t:Material",
                         new[] { "Assets/_Project/Shared/Materials" }))
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

        void LoadPrefab(BrickType type, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Debug.LogWarning($"[LevelPainter] Brick-Prefab fehlt: {path}");
            else _prefabs[type] = prefab;
        }

        // ── Styles ─────────────────────────────────────────────────────────
        void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _segBtn = new GUIStyle(EditorStyles.miniButton)
            {
                fixedHeight = 34, fontSize = 12, fontStyle = FontStyle.Bold,
                normal      = { textColor = new Color(0.70f, 0.70f, 0.70f) }
            };
            _segBtnSel = new GUIStyle(_segBtn) { normal = { textColor = Color.black } };
            _paintBtn  = new GUIStyle(GUI.skin.button)
            { fixedHeight = 46, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _warnBtn   = new GUIStyle(_paintBtn) { fontSize = 13 };
        }

        // ── OnGUI ──────────────────────────────────────────────────────────
        void OnGUI()
        {
            EnsureStyles();

            // Detect changes that invalidate the spatial index
            if (_root != _lastRoot || !Mathf.Approximately(_gridStep, _lastStep))
            {
                _lastRoot  = _root;
                _lastStep  = _gridStep;
                _indexDirty = true;
            }

            EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), BG);
            DrawHeader();
            DrawHelpText();

            _scroll = GUILayout.BeginScrollView(_scroll);
            GUILayout.Space(8);

            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
            using (new GUILayout.VerticalScope())
            {
                DrawSectionTitle("BAUSTEIN");       DrawTileTypeBar();     GUILayout.Space(8);
                DrawSectionTitle("BRUSH");         DrawBrushBar();        GUILayout.Space(8);
                if (_brushMode == BrushMode.RaiseWall)
                {
                    GUILayout.Label(
                        "Mauer erhöhen ignoriert Stacking-Modus und Brush Shape — es wird immer " +
                        "die ganze zusammenhängende Mauer auf der angeklickten Höhe um eine " +
                        "Reihe erhöht (aktueller Baustein/Farbe wird verwendet).",
                        new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                        { normal = { textColor = new Color(0.50f, 0.50f, 0.65f) } });
                    GUILayout.Space(8);
                }
                else
                {
                    DrawSectionTitle("STACKING");      DrawStackModeRow();    GUILayout.Space(8);
                    DrawSectionTitle("BRUSH SHAPE");   DrawBrushShapeRow();   GUILayout.Space(8);
                }
                DrawSectionTitle("GRID STEP");     DrawGridStepRow();     GUILayout.Space(8);
                DrawSectionTitle("PARENT OBJECT"); DrawParentRow();       GUILayout.Space(8);
                DrawSectionTitle($"MATERIAL  ({_mats.Count})");
            } GUILayout.Space(10); }

            DrawPalette();
            GUILayout.Space(4);

            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
            using (new GUILayout.VerticalScope())
            {
                DrawActivateBtn();
                GUILayout.Space(12);
                DrawSectionTitle("BAKE TO MESH");
                DrawBakeSection();
            } GUILayout.Space(10); }
            GUILayout.Space(12);

            GUILayout.EndScrollView();
        }

        // ── GUI sections ───────────────────────────────────────────────────
        void DrawHeader()
        {
            var r = GUILayoutUtility.GetRect(0, 52, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r, new Color(0.09f, 0.09f, 0.14f));
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 3, r.width, 3), Accent);
            GUI.Label(r, "Asset Builder", new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        void DrawHelpText()
        {
            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
                GUILayout.Label(
                    "So geht's: Baustein wählen → Farbe wählen → \"Start Painting\" → im Scene-View klicken.\n" +
                    "Boden-Platte & Wand-Baustein sind stapelbar und werden ins Spiel exportiert. " +
                    "Die runden Teile sind reine Deko und bleiben nur in der Szene.",
                    new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    { normal = { textColor = new Color(0.60f, 0.60f, 0.75f) } });
            GUILayout.Space(10); }
            GUILayout.Space(4);
        }

        void DrawSectionTitle(string t) =>
            GUILayout.Label(t, new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 10, normal = { textColor = new Color(0.55f, 0.55f, 0.75f) } });

        void DrawTileTypeBar()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                TileBtn("▭  Boden-Platte", BrickType.Plate, Accent,
                    "Flach, eckig. Strukturell: stapelbar und wird ins Spiel exportiert.");
                GUILayout.Space(4);
                TileBtn("▮  Wand-Baustein", BrickType.Brick, AccentB,
                    "Hoch, eckig. Strukturell: stapelbar und wird ins Spiel exportiert.");
            }
            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                TileBtn("●  Runde Platte", BrickType.PlateRound, PurpleC,
                    "Flach, rund. Nur Deko — bleibt in der Szene, wird nicht ins Spiel exportiert.");
                GUILayout.Space(4);
                TileBtn("⬤  Runder Baustein", BrickType.BrickRound, OrangeC,
                    "Hoch, rund. Nur Deko — bleibt in der Szene, wird nicht ins Spiel exportiert.");
            }
        }

        void TileBtn(string label, BrickType t, Color col, string tooltip)
        {
            bool sel = _tileType == t;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(new GUIContent(label, tooltip), sel ? _segBtnSel : _segBtn)) _tileType = t;
            GUI.backgroundColor = old;
        }

        void DrawBrushBar()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                ModeBtn("✏  Paint", BrushMode.Paint, GreenC,
                    "Baustein setzen (per Brush-Shape/Stacking-Modus konfigurierbar).");
                GUILayout.Space(4);
                ModeBtn("✕  Erase", BrushMode.Erase, RedC,
                    "Baustein unter der Maus entfernen (highlighted rot).");
            }
            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                ModeBtn("⬆  Mauer erhöhen", BrushMode.RaiseWall, new Color(0.35f, 0.75f, 1.0f),
                    "Klick auf eine Mauer: findet alle direkt verbundenen Bausteine auf derselben " +
                    "Höhe und packt auf jeden davon eine weitere Reihe obendrauf. Ignoriert Brush " +
                    "Shape/Stacking-Modus.");
            }
        }

        void ModeBtn(string label, BrushMode m, Color col, string tooltip)
        {
            bool sel = _brushMode == m;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(new GUIContent(label, tooltip), sel ? _segBtnSel : _segBtn)) _brushMode = m;
            GUI.backgroundColor = old;
        }

        void DrawStackModeRow()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                StackBtn("↕ Stack",        StackMode.Stack,        YellowC,
                    "Baut einfach oben drauf, ohne etwas zu löschen. 3 flache Platten sind " +
                    "genauso hoch wie 1 hoher Baustein — 'aufbauen' heißt hier stapeln, nicht ersetzen.");
                GUILayout.Space(3);
                StackBtn("↔ Replace",      StackMode.Replace,      PurpleC,
                    "Ersetzt genau den Baustein unter deiner Maus (wird pink markiert, egal ob " +
                    "er oben liegt oder unter einem anderen hervorschaut). Steht dort noch " +
                    "nichts, wird stattdessen neu platziert.");
            }
            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                StackBtn("→ Only",         StackMode.ReplaceOnly,  new Color(0.6f, 0.4f, 1.0f),
                    "Wie Replace, aber nur wenn dort schon ein Baustein steht — leere Zellen " +
                    "werden komplett übersprungen (nichts Neues wird platziert).");
                GUILayout.Space(3);
                StackBtn("⬡ Stack N",      StackMode.ReplaceStack, new Color(1.0f, 0.5f, 0.8f),
                    "Ersetzt die obersten N Bausteine EINES bestehenden Stapels an dieser Stelle " +
                    "(N über den Tiefe-Regler unten). Braucht einen echten Stapel — auf einer " +
                    "einzelnen flachen Fliese ersetzt es nur die eine, sieht also wie normales " +
                    "Replace aus.");
            }

            GUILayout.Space(3);
            string hint = _brushMode == BrushMode.Erase
                ? "Erase zielt immer exakt auf den Baustein unter der Maus — Stacking-Modus hat beim Löschen keine Wirkung"
                : _stackMode switch
                {
                    StackMode.Stack        => "Baut oben drauf, ohne zu löschen (3 Platten = 1 Baustein in der Höhe)",
                    StackMode.Replace      => "Ersetzt genau den Baustein unter der Maus (pink markiert)",
                    StackMode.ReplaceOnly  => "Wie Replace, aber überspringt leere Zellen komplett",
                    StackMode.ReplaceStack => "Ersetzt die obersten N Bausteine eines Stapels (braucht einen echten Stapel!)",
                    _                     => ""
                };
            GUILayout.Label(hint, new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            { normal = { textColor = new Color(0.50f, 0.50f, 0.65f) } });

            if (_stackMode == StackMode.ReplaceStack)
            {
                GUILayout.Space(3);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Tiefe:", GUILayout.Width(42));
                    _replaceDepth = EditorGUILayout.IntSlider(_replaceDepth, 1, 20);
                    GUILayout.Label($"({_replaceDepth})", new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.7f, 0.6f, 0.9f) } }, GUILayout.Width(32));
                }
            }
        }

        void StackBtn(string label, StackMode m, Color col, string tooltip)
        {
            bool sel = _stackMode == m;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(new GUIContent(label, tooltip), sel ? _segBtnSel : _segBtn)) _stackMode = m;
            GUI.backgroundColor = old;
        }

        void DrawBrushShapeRow()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                ShapeBtn("■ Single", BrushShape.Single,
                    "Ein einzelner Baustein pro Klick.");
                GUILayout.Space(3);
                ShapeBtn("▦ Rect",   BrushShape.Rect,
                    "Füllt ein Quadrat um den Klickpunkt (Größe über Radius).");
                GUILayout.Space(3);
                ShapeBtn("● Circle", BrushShape.Circle,
                    "Füllt einen Kreis um den Klickpunkt (Größe über Radius).");
                GUILayout.Space(3);
                ShapeBtn("▬ Line",   BrushShape.Line,
                    "Baut eine gerade Reihe aus N Bausteinen in eine Richtung (X, Y oder Z) — " +
                    "z.B. eine Mauer aus 10 Steinen mit einem Klick.");
            }
            if (_brushShape == BrushShape.Rect || _brushShape == BrushShape.Circle)
            {
                GUILayout.Space(4);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Radius:", GUILayout.Width(50));
                    _brushRadius = EditorGUILayout.IntSlider(_brushRadius, 1, 8);
                    int s = _brushRadius * 2 + 1;
                    GUILayout.Label($"({s}×{s})", new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.6f, 0.6f, 0.8f) } }, GUILayout.Width(44));
                }
            }
            else if (_brushShape == BrushShape.Line)
            {
                GUILayout.Space(4);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Achse:", GUILayout.Width(50));
                    LineAxisBtn("X", LineAxis.X);
                    GUILayout.Space(3);
                    LineAxisBtn("Y ↑", LineAxis.Y);
                    GUILayout.Space(3);
                    LineAxisBtn("Z", LineAxis.Z);
                }
                GUILayout.Space(3);
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.Label("Länge:", GUILayout.Width(50));
                    _lineLength = EditorGUILayout.IntSlider(_lineLength, 2, 40);
                    GUILayout.Label($"({_lineLength})", new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = new Color(0.6f, 0.6f, 0.8f) } }, GUILayout.Width(32));
                }
                GUILayout.Label(
                    _lineAxis == LineAxis.Y
                        ? "Stapelt N Bausteine übereinander am Klickpunkt."
                        : "Reiht N Bausteine ab dem Klickpunkt entlang der Achse auf.",
                    new GUIStyle(EditorStyles.wordWrappedMiniLabel)
                    { normal = { textColor = new Color(0.50f, 0.50f, 0.65f) } });
            }
        }

        void ShapeBtn(string label, BrushShape shape, string tooltip)
        {
            bool sel = _brushShape == shape;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? new Color(0.55f, 0.55f, 0.85f) : new Color(0.20f, 0.20f, 0.30f);
            if (GUILayout.Button(new GUIContent(label, tooltip), sel ? _segBtnSel : _segBtn)) _brushShape = shape;
            GUI.backgroundColor = old;
        }

        void LineAxisBtn(string label, LineAxis axis)
        {
            bool sel = _lineAxis == axis;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? new Color(0.55f, 0.55f, 0.85f) : new Color(0.20f, 0.20f, 0.30f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _lineAxis = axis;
            GUI.backgroundColor = old;
        }

        void DrawGridStepRow()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Step:", GUILayout.Width(36));
                _gridStep = EditorGUILayout.FloatField(_gridStep, GUILayout.Width(58));
                GUILayout.Space(4);
                if (GUILayout.Button("0.0795", GUILayout.Width(50), GUILayout.Height(18)))
                { _gridStep = 0.0795f; GUI.FocusControl(null); }
                GUILayout.FlexibleSpace();
            }
        }

        void DrawParentRow()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                _root = (Transform)EditorGUILayout.ObjectField(
                    _root, typeof(Transform), true, GUILayout.Height(20));
                if (_root == null)
                    if (GUILayout.Button("Create Root", GUILayout.Width(86), GUILayout.Height(20)))
                    {
                        var go = new GameObject("LevelRoot");
                        Undo.RegisterCreatedObjectUndo(go, "Create LevelRoot");
                        _root = go.transform;
                    }
            }
        }

        void DrawPalette()
        {
            if (_mats.Count == 0) return;
            float availW = position.width - 20f;
            int   cols   = Mathf.Max(1, Mathf.FloorToInt(availW / 44f));
            int   rows   = Mathf.CeilToInt(_mats.Count / (float)cols);
            var   prect  = GUILayoutUtility.GetRect(availW, rows * 44f + 8f);

            float dx = 10f, dy = 4f;
            for (int i = 0; i < _mats.Count; i++)
            {
                var r = new Rect(prect.x + dx, prect.y + dy, 40f, 40f);
                EditorGUI.DrawRect(r, _matColors[i]);
                DrawBorder(r, i == _selMat ? Color.white : new Color(0f, 0f, 0f, 0.35f),
                              i == _selMat ? 2f : 1f);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                { _selMat = i; Repaint(); Event.current.Use(); }
                GUI.Label(r, new GUIContent("", _matNames[i]));
                dx += 44f;
                if (i < _mats.Count - 1 && dx + 40f > availW - 4f) { dx = 10f; dy += 44f; }
            }
            if (_selMat >= 0 && _selMat < _matNames.Count)
                GUILayout.Label(_matNames[_selMat], new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                { normal = { textColor = new Color(0.75f, 0.75f, 0.90f) } });
            GUILayout.Space(4);
        }

        void DrawActivateBtn()
        {
            var old = GUI.backgroundColor;
            GUI.backgroundColor = _active ? GreenC : new Color(0.30f, 0.30f, 0.50f);
            if (GUILayout.Button(_active ? "● PAINTING ACTIVE  —  Click to Stop" : "Start Painting", _paintBtn))
            { _active = !_active; SceneView.RepaintAll(); }
            GUI.backgroundColor = old;
        }

        void DrawBakeSection()
        {
            GUILayout.Space(4);

            // Info line
            int tileCount = CountTiles();
            var infoStyle = new GUIStyle(EditorStyles.miniLabel)
            { normal = { textColor = new Color(0.6f, 0.6f, 0.75f) } };
            GUILayout.Label($"{tileCount} tiles painted  |  spatial index: {(_indexDirty ? "dirty" : $"{_topYIndex.Count} cells")}", infoStyle);
            GUILayout.Space(4);

            _bakeHideOriginal = EditorGUILayout.ToggleLeft("Hide original tiles after bake", _bakeHideOriginal);
            _bakeSetStatic    = EditorGUILayout.ToggleLeft("Mark baked mesh as Static",      _bakeSetStatic);
            _bakeAddCollider  = EditorGUILayout.ToggleLeft("Add MeshCollider to baked mesh", _bakeAddCollider);
            if (_bakeHideOriginal && !_bakeAddCollider)
                EditorGUILayout.HelpBox(
                    "Hide Original ist an, Add MeshCollider aus — das Level hätte danach gar keine " +
                    "Kollision mehr (die Original-Bricks mit ihren Collidern werden ja versteckt).",
                    MessageType.Warning);
            GUILayout.Space(6);

            var old = GUI.backgroundColor;
            GUI.backgroundColor = OrangeC;
            if (GUILayout.Button("⬡  Bake Level to Mesh", _warnBtn))
            {
                if (_root == null && !EditorUtility.DisplayDialog("No Root set",
                    "No Parent Root is assigned. Bake will collect all scene MeshRenderers. Continue?", "Bake", "Cancel"))
                    goto done;
                BakeToMesh();
            }
            done:
            GUI.backgroundColor = old;

            GUILayout.Space(8);
            DrawSectionTitle("SIMPLIFY COLLISION (BOX MERGE)");
            GUILayout.Label(
                "Alternative zum MeshCollider oben: führt benachbarte Bricks auf derselben Höhe " +
                "zu möglichst wenigen, komplett flachen Box-Collidern zusammen (keine Noppen, " +
                "keine hunderte Einzel-Collider) — läuft sich glatter. Erzeugt ein eigenes " +
                "'Colliders_Baked'-Objekt; Original-Bricks (Renderer und/oder ganzes Objekt) " +
                "kannst du danach ausblenden.",
                new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.55f, 0.55f, 0.70f) } });
            GUILayout.Space(4);
            var old4 = GUI.backgroundColor;
            GUI.backgroundColor = GreenC;
            if (GUILayout.Button("⬡  Generate Merged Box Colliders", _warnBtn))
                GenerateMergedBoxColliders();
            GUI.backgroundColor = old4;

            GUILayout.Space(8);
            DrawSectionTitle("EXPORT AS WORLD DATA");
            GUILayout.Label("Nur Boden-Platte & Wand-Baustein werden exportiert — Rundteile bleiben Deko.",
                new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.55f, 0.55f, 0.70f) } });
            GUILayout.Space(4);
            var old3 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.25f, 0.55f, 0.85f);
            if (GUILayout.Button("⬡  Export to WorldData Asset", _warnBtn))
            {
                if (_root == null && !EditorUtility.DisplayDialog("No Root set",
                    "No Parent Root assigned. Export will scan all scene tiles. Continue?", "Export", "Cancel"))
                    goto doneExport;
                ExportAsWorldData();
            }
            doneExport:
            GUI.backgroundColor = old3;

            GUILayout.Space(4);
            var old2 = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.45f);
            if (GUILayout.Button("Rebuild Spatial Index", GUILayout.Height(22)))
                _indexDirty = true;
            GUI.backgroundColor = old2;
        }

        // ── Scene GUI ──────────────────────────────────────────────────────
        void OnSceneGUI(SceneView sv)
        {
            if (!_active) return;
            EnsureIndex();
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Event e = Event.current;

            if (_brushMode == BrushMode.RaiseWall) { HandleRaiseWall(e, sv); return; }

            // Replace (single tile) and Erase (single tile) both target whatever brick is
            // actually under the cursor — including a lower tile whose edge peeks out from
            // under a shorter/narrower one on top — instead of always the topmost tile in the
            // XZ column (Erase never used Stack-Mode anyway, so it always gets this precision
            // targeting regardless of which Stack-Mode button happens to be selected).
            // Everything else keeps the ground-plane/column-based flow (brush shapes paint
            // multiple new cells at once and have no single "picked object" to target).
            bool precisionTarget = _brushShape == BrushShape.Single &&
                                    (_brushMode == BrushMode.Erase ||
                                     (_brushMode == BrushMode.Paint && _stackMode == StackMode.Replace));

            if (precisionTarget) { HandlePrecisionTarget(e, sv); return; }

            var center = GetBaseCell(e);
            bool verticalLine = _brushShape == BrushShape.Line && _lineAxis == LineAxis.Y;

            if (center.HasValue && _brushMode == BrushMode.Paint)
            {
                float hh = TileHeightFor(_tileType);
                Handles.color = new Color(0.4f, 0.9f, 1f, 0.55f);
                if (verticalLine)
                {
                    // The flat GetBrushCells list only has the base cell for a Y line (its
                    // height depends on placement order, see PlaceVerticalLine) — preview the
                    // whole future stack here instead by walking upward in fixed hh steps.
                    float baseY = GetTopY(center.Value.x, center.Value.z);
                    for (int i = 0; i < _lineLength; i++)
                    {
                        var c = new Vector3(center.Value.x, baseY + i * hh, center.Value.z);
                        Handles.DrawWireCube(c + new Vector3(0f, hh * 0.5f, 0f), new Vector3(TileWidthX, hh, TileWidthZ));
                    }
                }
                else
                {
                    foreach (var c in GetBrushCells(center.Value))
                        Handles.DrawWireCube(c + new Vector3(0f, hh * 0.5f, 0f), new Vector3(TileWidthX, hh, TileWidthZ));
                }
            }

            DrawSceneHUD(sv);

            if (e.type == EventType.MouseUp && e.button == 0)
                _lastCenter = Vector3.positiveInfinity;

            bool lmb = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                       && e.button == 0 && !e.alt;

            if (lmb && center.HasValue && center.Value != _lastCenter)
            {
                _lastCenter = center.Value;
                if (verticalLine)
                {
                    if (_brushMode == BrushMode.Paint) PlaceVerticalLine(center.Value.x, center.Value.z, _lineLength);
                    else                               EraseVerticalLine(center.Value.x, center.Value.z, _lineLength);
                }
                else
                {
                    foreach (var c in GetBrushCells(center.Value))
                    {
                        if (_brushMode == BrushMode.Paint) PlaceTile(c);
                        else                               EraseAt(c.x, c.z);
                    }
                }
                e.Use();
            }
            sv.Repaint();
        }

        // Picks the exact brick GameObject under the mouse (real geometry, not the XZ column's
        // topmost tile) and highlights it — pink for Replace (destroys + reinstates exactly
        // that one with the currently selected type/material), red for Erase (just destroys
        // it, nothing is placed back). Replace still falls back to the normal empty-cell ghost
        // preview and places a new tile when nothing is under the cursor; Erase does nothing
        // when there's no tile to target.
        void HandlePrecisionTarget(Event e, SceneView sv)
        {
            bool erasing = _brushMode == BrushMode.Erase;

            // PickGameObject triggers a real render pass — only safe to call on Mouse*
            // events. Calling it on every event (incl. Layout) corrupts Unity's GUI window
            // stack ("GUI Window tried to begin rendering while something else had not
            // finished rendering"). Layout/Repaint/etc. just reuse the last pick result.
            bool wantsPick = e.type == EventType.MouseMove || e.type == EventType.MouseDown ||
                              e.type == EventType.MouseDrag;
            if (wantsPick)
            {
                GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
                _replaceHoverTile = TryResolveTile(picked, out GameObject resolved) ? resolved : null;
            }

            GameObject tile = _replaceHoverTile;
            bool validPick = tile != null;

            if (validPick)
            {
                Bounds b = TileBounds(tile);
                Handles.color = erasing ? new Color(1f, 0.25f, 0.25f, 0.9f) : new Color(1f, 0.35f, 0.95f, 0.9f);
                Handles.DrawWireCube(b.center, b.size * 1.03f);
            }
            else if (!erasing)
            {
                var center = GetBaseCell(e);
                if (center.HasValue)
                {
                    float hh = TileHeightFor(_tileType);
                    Handles.color = new Color(0.4f, 0.9f, 1f, 0.55f);
                    Handles.DrawWireCube(center.Value + new Vector3(0f, hh * 0.5f, 0f), new Vector3(TileWidthX, hh, TileWidthZ));
                }
            }

            DrawSceneHUD(sv);

            if (e.type == EventType.MouseUp && e.button == 0)
                _lastCenter = Vector3.positiveInfinity;

            bool lmb = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                       && e.button == 0 && !e.alt;

            if (lmb)
            {
                if (erasing)
                {
                    if (validPick && tile.transform.position != _lastCenter)
                    {
                        Vector3 pos = tile.transform.position;
                        _lastCenter = pos;
                        Undo.DestroyObjectImmediate(tile);
                        _replaceHoverTile = null;
                        IndexRemove(pos.x, pos.z);
                        e.Use();
                    }
                }
                else
                {
                    Vector3? targetCenter = validPick ? tile.transform.position : GetBaseCell(e);
                    if (targetCenter.HasValue && targetCenter.Value != _lastCenter)
                    {
                        _lastCenter = targetCenter.Value;
                        if (validPick)
                        {
                            Vector3 pos = tile.transform.position;
                            Undo.DestroyObjectImmediate(tile);
                            _replaceHoverTile = null;
                            IndexRemove(pos.x, pos.z);
                            InstantiateTile(pos);
                        }
                        else
                        {
                            InstantiateTile(targetCenter.Value);
                        }
                        e.Use();
                    }
                }
            }
            sv.Repaint();
        }

        // Hover a brick that's part of a wall → highlights the whole connected group of tiles
        // at that exact height (4-directional XZ adjacency) in green, click adds one more layer
        // on top of every tile in the group at once. Ignores Brush Shape/Stacking-Modus
        // entirely — this is its own tool, ala Precision Replace/Erase.
        void HandleRaiseWall(Event e, SceneView sv)
        {
            bool wantsPick = e.type == EventType.MouseMove || e.type == EventType.MouseDown ||
                              e.type == EventType.MouseDrag;
            if (wantsPick)
            {
                GameObject picked = HandleUtility.PickGameObject(e.mousePosition, false);
                bool resolved = TryResolveTile(picked, out GameObject tile);
                GameObject newHover = resolved ? tile : null;

                // The flood-fill is an O(n) scan per connected tile — only worth redoing when
                // the hovered brick actually changed, not on every Layout/Repaint.
                if (newHover != _raiseWallHoverTile)
                {
                    _raiseWallHoverTile = newHover;
                    var group = newHover != null ? FloodFillSameHeight(newHover.transform.position) : null;

                    // A "wall" is at most 1 brick thick. If any tile in the group has a
                    // same-height neighbor on all 4 sides (fully enclosed — part of a solid
                    // filled area, not a thin wall), refuse to offer raising it at all.
                    _raiseWallBlocked = group != null && HasEnclosedTile(group);
                    _raiseWallGroup   = _raiseWallBlocked ? null : group;
                }
            }

            if (_raiseWallGroup is { Count: > 0 })
            {
                float addH = TileHeightFor(_tileType);
                Handles.color = new Color(0.3f, 1f, 0.5f, 0.85f);
                foreach (var go in _raiseWallGroup)
                {
                    float topY = go.transform.position.y + TileHeight(go);
                    var   c    = new Vector3(go.transform.position.x, topY + addH * 0.5f, go.transform.position.z);
                    Handles.DrawWireCube(c, new Vector3(TileWidthX, addH, TileWidthZ));
                }
            }

            DrawSceneHUD(sv);

            // Single-shot on MouseDown only (no MouseDrag repeat) — one click = one new row,
            // dragging across an already-raised wall shouldn't spam additional layers.
            if (e.type == EventType.MouseDown && e.button == 0 && !e.alt &&
                _raiseWallGroup is { Count: > 0 })
            {
                foreach (var go in _raiseWallGroup.ToList())
                {
                    Vector3 pos  = go.transform.position;
                    float   topY = pos.y + TileHeight(go);
                    InstantiateTile(new Vector3(pos.x, topY, pos.z));
                }
                // Force re-detection next frame — the group just grew a layer taller.
                _raiseWallHoverTile = null;
                _raiseWallGroup = null;
                e.Use();
            }
            sv.Repaint();
        }

        // Flood fill in grid-key space starting at startPos's cell, following 4-directional XZ
        // adjacency, collecting every tile whose Y matches startPos.y (within tolerance) — i.e.
        // "this whole wall segment", not just the one brick under the cursor.
        List<GameObject> FloodFillSameHeight(Vector3 startPos)
        {
            float targetY = startPos.y;
            float ySnap   = TileHeightFor(BrickType.Plate) * 0.25f;
            var   visited = new HashSet<Vector2Int>();
            var   frontier = new Queue<Vector2Int>();
            var   result  = new List<GameObject>();

            var startKey = ToKey(startPos.x, startPos.z);
            frontier.Enqueue(startKey);
            visited.Add(startKey);

            while (frontier.Count > 0)
            {
                var key = frontier.Dequeue();
                float x = key.x * StepX;
                float z = key.y * StepZ;

                var tile = FindTileAtExactY(x, z, targetY, ySnap);
                if (tile == null) continue; // dead end — nothing here at this height

                result.Add(tile);

                foreach (var d in NeighborOffsets)
                {
                    var nk = key + d;
                    if (visited.Contains(nk)) continue;
                    visited.Add(nk);
                    frontier.Enqueue(nk);
                }
            }
            return result;
        }

        // True if any tile in the group has a same-height neighbor on all 4 XZ sides — i.e.
        // it's fully enclosed, part of a solid filled area rather than a thin (1-brick) wall.
        bool HasEnclosedTile(List<GameObject> group)
        {
            var keys = new HashSet<Vector2Int>();
            foreach (var go in group)
                keys.Add(ToKey(go.transform.position.x, go.transform.position.z));

            foreach (var key in keys)
            {
                bool allSidesPresent = true;
                foreach (var d in NeighborOffsets)
                    if (!keys.Contains(key + d)) { allSidesPresent = false; break; }
                if (allSidesPresent) return true;
            }
            return false;
        }

        static readonly Vector2Int[] NeighborOffsets =
            { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        GameObject FindTileAtExactY(float x, float z, float y, float ySnap)
        {
            float snapX = StepX * 0.5f;
            float snapZ = StepZ * 0.5f;
            foreach (var go in AllTiles())
            {
                if (Mathf.Abs(go.transform.position.x - x) > snapX) continue;
                if (Mathf.Abs(go.transform.position.z - z) > snapZ) continue;
                if (Mathf.Abs(go.transform.position.y - y) > ySnap) continue;
                return go;
            }
            return null;
        }

        // Resolves a Scene-View pick to the tile GameObject the painter actually operates on
        // (the PlacedBrick marker's own object, in case picking ever returns a nested child).
        bool TryResolveTile(GameObject picked, out GameObject tile)
        {
            tile = null;
            if (picked == null) return false;

            var marker = picked.GetComponentInParent<PlacedBrick>();
            if (marker != null) { tile = marker.gameObject; return true; }

            // Fallback for stray tiles without the marker component (matches AllTiles()).
            if (AllTiles().Contains(picked)) { tile = picked; return true; }
            return false;
        }

        Bounds TileBounds(GameObject go)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return new Bounds(go.transform.position, new Vector3(TileWidthX, TileWidthX, TileWidthZ));
            var b = rends[0].bounds;
            foreach (var r in rends) b.Encapsulate(r.bounds);
            return b;
        }

        void DrawSceneHUD(SceneView sv)
        {
            Handles.BeginGUI();
            string mat   = _selMat < _matNames.Count ? _matNames[_selMat] : "—";
            string tile  = _tileType switch
            {
                BrickType.Plate      => "Boden-Platte",
                BrickType.Brick      => "Wand-Baustein",
                BrickType.PlateRound => "Runde Platte",
                BrickType.BrickRound => "Runder Baustein",
                _                    => _tileType.ToString(),
            };
            string text;
            if (_brushMode == BrushMode.RaiseWall)
            {
                text = _raiseWallBlocked
                    ? "[Mauer erhöhen] Blockiert — Bereich ist an einer Stelle voll umschlossen (keine dünne Mauer)"
                    : $"[Mauer erhöhen] {tile}  Gruppe: {_raiseWallGroup?.Count ?? 0}  |  {mat}";
            }
            else
            {
                string mode  = _brushMode == BrushMode.Paint ? "Paint"  : "Erase";
                string stack = _stackMode == StackMode.Stack  ? "Stack"  : "Replace";
                string shape = _brushShape == BrushShape.Single ? "1×1"
                             : _brushShape == BrushShape.Rect   ? $"Rect r{_brushRadius}"
                             : _brushShape == BrushShape.Circle ? $"Circle r{_brushRadius}"
                             :                                    $"Line {_lineAxis} x{_lineLength}";
                text = $"[{mode}] {tile}  {stack}  {shape}  |  {mat}";
            }
            float  w     = Mathf.Min(sv.position.width - 20f, 420f), h = 26f;
            var    r     = new Rect(10f, sv.position.height - h - 36f, w, h);
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.62f));
            DrawBorder(r, new Color(0.4f, 0.92f, 0.5f, 0.9f), 1f);
            GUI.Label(new Rect(r.x + 8f, r.y + 5f, r.width, r.height), text,
                new GUIStyle(EditorStyles.miniLabel)
                { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.40f, 0.95f, 0.50f) } });
            Handles.EndGUI();
        }

        // ── Spatial index ──────────────────────────────────────────────────
        Vector2Int ToKey(float x, float z) =>
            new(Mathf.RoundToInt(x / StepX), Mathf.RoundToInt(z / StepZ));

        void EnsureIndex()
        {
            if (!_indexDirty) return;
            RebuildIndex();
        }

        void RebuildIndex()
        {
            _topYIndex.Clear();
            foreach (var go in AllTiles())
            {
                var  key = ToKey(go.transform.position.x, go.transform.position.z);
                float top = go.transform.position.y + TileHeight(go);
                if (!_topYIndex.TryGetValue(key, out float cur) || top > cur)
                    _topYIndex[key] = top;
            }
            _indexDirty = false;
        }

        void IndexAdd(Vector3 pos, float height)
        {
            var   key = ToKey(pos.x, pos.z);
            float top = pos.y + height;
            if (!_topYIndex.TryGetValue(key, out float cur) || top > cur)
                _topYIndex[key] = top;
        }

        void IndexRemove(float x, float z)
        {
            // Recompute top for this cell from remaining tiles
            var   key    = ToKey(x, z);
            float snapX  = StepX * 0.5f;
            float snapZ  = StepZ * 0.5f;
            float best = float.MinValue;
            bool  any  = false;
            foreach (var go in AllTiles())
            {
                if (Mathf.Abs(go.transform.position.x - x) > snapX) continue;
                if (Mathf.Abs(go.transform.position.z - z) > snapZ) continue;
                float top = go.transform.position.y + TileHeight(go);
                if (top > best) { best = top; any = true; }
            }
            if (any) _topYIndex[key] = best;
            else     _topYIndex.Remove(key);
        }

        // ── Grid / brush ───────────────────────────────────────────────────
        Vector3? GetBaseCell(Event e)
        {
            var   ray  = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var   plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float t)) return null;
            var p = ray.GetPoint(t);
            return new Vector3(Mathf.Round(p.x / StepX) * StepX, 0f, Mathf.Round(p.z / StepZ) * StepZ);
        }

        List<Vector3> GetBrushCells(Vector3 center)
        {
            var result = new List<Vector3>();

            if (_brushShape == BrushShape.Line)
            {
                // Y lines stack N tiles on top of each other at one XZ spot — each tile's
                // height depends on the one placed before it, so that has to happen one
                // PlaceTile call at a time (see PlaceVerticalLine) instead of a static cell
                // list. Just hand back the base cell here for the preview/click-count check.
                if (_lineAxis == LineAxis.Y)
                {
                    result.Add(new Vector3(center.x, GetTopY(center.x, center.z), center.z));
                    return result;
                }

                for (int i = 0; i < _lineLength; i++)
                {
                    float cx = _lineAxis == LineAxis.X ? center.x + i * StepX : center.x;
                    float cz = _lineAxis == LineAxis.Z ? center.z + i * StepZ : center.z;
                    result.Add(new Vector3(cx, GetTopY(cx, cz), cz));
                }
                return result;
            }

            int r = _brushShape == BrushShape.Single ? 0 : _brushRadius;
            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (_brushShape == BrushShape.Circle && dx * dx + dz * dz > r * r) continue;
                float cx = center.x + dx * StepX;
                float cz = center.z + dz * StepZ;
                result.Add(new Vector3(cx, GetTopY(cx, cz), cz));
            }
            return result;
        }

        // ── Stack logic — O(1) via index ───────────────────────────────────
        float GetTopY(float x, float z)
        {
            if (_stackMode == StackMode.Replace) return 0f;
            var key = ToKey(x, z);
            return _topYIndex.TryGetValue(key, out float top) ? top : 0f;
        }

        float TileHeightFor(BrickType t) => BrickShapeInfo.HeightInPlates(t) * WorldConstants.PlateHeight;

        float TileHeight(GameObject go)
        {
            var marker = go.GetComponent<PlacedBrick>();
            if (marker != null) return TileHeightFor(marker.shape);

            // Fallback for stray objects without the marker component
            var rends = go.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) return TileHeightFor(_tileType);
            var b = rends[0].bounds;
            foreach (var rr in rends) b.Encapsulate(rr.bounds);
            return b.size.y;
        }

        // ── Tile ops ───────────────────────────────────────────────────────
        void PlaceTile(Vector3 cell)
        {
            if (!_prefabs.ContainsKey(_tileType) || _prefabs[_tileType] == null)
            { Debug.LogWarning("[LevelPainter] Prefab not found."); return; }

            switch (_stackMode)
            {
                case StackMode.Stack:
                    if (TileExistsAt(cell)) return;
                    InstantiateTile(cell);
                    break;

                case StackMode.Replace:
                {
                    var top = FindTopTile(cell.x, cell.z);
                    float y = top != null ? top.transform.position.y : 0f;
                    if (top != null) { Undo.DestroyObjectImmediate(top); IndexRemove(cell.x, cell.z); }
                    InstantiateTile(new Vector3(cell.x, y, cell.z));
                    break;
                }

                case StackMode.ReplaceOnly:
                {
                    var top = FindTopTile(cell.x, cell.z);
                    if (top == null) return;
                    float y = top.transform.position.y;
                    Undo.DestroyObjectImmediate(top);
                    IndexRemove(cell.x, cell.z);
                    InstantiateTile(new Vector3(cell.x, y, cell.z));
                    break;
                }

                case StackMode.ReplaceStack:
                {
                    var all = FindAllTilesAt(cell.x, cell.z);
                    if (all.Count == 0) return;
                    var targets   = all.Take(_replaceDepth).ToList();
                    var positions = targets.Select(go => go.transform.position).ToList();
                    foreach (var go in targets) Undo.DestroyObjectImmediate(go);
                    _topYIndex.Remove(ToKey(cell.x, cell.z));
                    foreach (var pos in positions) InstantiateTile(pos);
                    break;
                }
            }
        }

        // Line brush, Y axis: stacks _lineLength tiles on top of each other at one XZ spot.
        // Has to place one at a time and re-read GetTopY between each — unlike the flat
        // GetBrushCells lists, every tile's height depends on the one placed right before it.
        void PlaceVerticalLine(float x, float z, int count)
        {
            for (int i = 0; i < count; i++)
                PlaceTile(new Vector3(x, GetTopY(x, z), z));
        }

        // Mirror of PlaceVerticalLine for Erase — removes the top tile at (x,z), which then
        // exposes the next one down for the following iteration (top-to-bottom removal).
        void EraseVerticalLine(float x, float z, int count)
        {
            for (int i = 0; i < count; i++) EraseAt(x, z);
        }

        void InstantiateTile(Vector3 position)
        {
            if (!_prefabs.TryGetValue(_tileType, out var prefab) || prefab == null)
            { Debug.LogWarning("[LevelPainter] Prefab not found."); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = position;
            if (_root != null) go.transform.SetParent(_root, true);
            if (_selMat >= 0 && _selMat < _mats.Count)
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                    mr.sharedMaterial = _mats[_selMat];

            var marker = go.GetComponent<PlacedBrick>();
            if (marker != null) marker.materialIndex = (byte)Mathf.Clamp(_selMat, 0, 255);

            IndexAdd(position, TileHeightFor(_tileType));
            Undo.RegisterCreatedObjectUndo(go, "Paint Tile");
        }

        void EraseAt(float x, float z)
        {
            float snapX = StepX * 0.5f;
            float snapZ = StepZ * 0.5f;
            if (_root != null)
            {
                foreach (Transform child in _root)
                {
                    if (Mathf.Abs(child.position.x - x) < snapX &&
                        Mathf.Abs(child.position.z - z) < snapZ)
                    {
                        Undo.DestroyObjectImmediate(child.gameObject);
                        IndexRemove(x, z);
                        return;
                    }
                }
            }
            else
            {
                foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    if (Mathf.Abs(go.transform.position.x - x) < snapX &&
                        Mathf.Abs(go.transform.position.z - z) < snapZ &&
                        go.GetComponentInChildren<MeshRenderer>() != null)
                    {
                        Undo.DestroyObjectImmediate(go);
                        IndexRemove(x, z);
                        return;
                    }
                }
            }
        }

        // Returns the tile whose (y + height) is highest at the given XZ cell
        GameObject FindTopTile(float x, float z)
        {
            float snapX   = StepX * 0.5f;
            float snapZ   = StepZ * 0.5f;
            float bestTop = float.MinValue;
            GameObject result = null;
            foreach (var go in AllTiles())
            {
                if (Mathf.Abs(go.transform.position.x - x) > snapX) continue;
                if (Mathf.Abs(go.transform.position.z - z) > snapZ) continue;
                float top = go.transform.position.y + TileHeight(go);
                if (top > bestTop) { bestTop = top; result = go; }
            }
            return result;
        }

        // All tiles at XZ sorted top → bottom
        List<GameObject> FindAllTilesAt(float x, float z)
        {
            float snapX = StepX * 0.5f;
            float snapZ = StepZ * 0.5f;
            return AllTiles()
                .Where(go => Mathf.Abs(go.transform.position.x - x) < snapX &&
                             Mathf.Abs(go.transform.position.z - z) < snapZ)
                .OrderByDescending(go => go.transform.position.y + TileHeight(go))
                .ToList();
        }

        bool TileExistsAt(Vector3 cell)
        {
            float snapX = StepX * 0.5f;
            float snapZ = StepZ * 0.5f;
            float ySnap = TileHeightFor(BrickType.Plate) * 0.1f;
            foreach (var go in AllTiles())
                if (Mathf.Abs(go.transform.position.x - cell.x) < snapX &&
                    Mathf.Abs(go.transform.position.z - cell.z) < snapZ &&
                    Mathf.Abs(go.transform.position.y - cell.y) < ySnap) return true;
            return false;
        }

        IEnumerable<GameObject> AllTiles()
        {
            if (_root != null)
                return _root.Cast<Transform>().Select(tf => tf.gameObject);
            return SceneManager.GetActiveScene().GetRootGameObjects()
                .Where(go => go.GetComponentInChildren<MeshRenderer>() != null);
        }

        int CountTiles() => _root != null ? _root.childCount
            : SceneManager.GetActiveScene().GetRootGameObjects()
                .Count(go => go.GetComponentInChildren<MeshRenderer>() != null);

        // ── Mesh baker ─────────────────────────────────────────────────────
        void BakeToMesh()
        {
            var filters = (_root != null
                ? _root.GetComponentsInChildren<MeshFilter>()
                : SceneManager.GetActiveScene().GetRootGameObjects()
                    .SelectMany(go => go.GetComponentsInChildren<MeshFilter>())).ToList();

            if (filters.Count == 0)
            { EditorUtility.DisplayDialog("Bake", "No meshes found to bake.", "OK"); return; }

            // Group CombineInstances by material
            var groups = new Dictionary<Material, List<CombineInstance>>();
            foreach (var mf in filters)
            {
                if (mf.sharedMesh == null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || mr.sharedMaterial == null) continue;
                var mat = mr.sharedMaterial;
                if (!groups.ContainsKey(mat)) groups[mat] = new List<CombineInstance>();
                groups[mat].Add(new CombineInstance
                {
                    mesh      = mf.sharedMesh,
                    transform = mf.transform.localToWorldMatrix
                });
            }

            // Ensure output folder
            const string outFolder = "Assets/_Project/GeneratedMeshes";
            if (!AssetDatabase.IsValidFolder(outFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "GeneratedMeshes");

            // Create baked root
            var bakedRoot = new GameObject("LevelRoot_Baked");
            Undo.RegisterCreatedObjectUndo(bakedRoot, "Bake Level Mesh");

            int groupIdx = 0;
            foreach (var kvp in groups)
            {
                var combined = new Mesh { name = $"BakedMesh_{kvp.Key.name}" };
                combined.indexFormat = IndexFormat.UInt32;
                combined.CombineMeshes(kvp.Value.ToArray(), mergeSubMeshes: true, useMatrices: true);
                combined.RecalculateNormals();
                combined.RecalculateBounds();
                combined.Optimize();

                // Save mesh asset
                string assetPath = AssetDatabase.GenerateUniqueAssetPath(
                    $"{outFolder}/BakedLevel_{kvp.Key.name}.asset");
                AssetDatabase.CreateAsset(combined, assetPath);

                // Create child GO
                var child = new GameObject($"Baked_{kvp.Key.name}");
                Undo.RegisterCreatedObjectUndo(child, "Bake Level Mesh");
                child.transform.SetParent(bakedRoot.transform, true);
                child.AddComponent<MeshFilter>().sharedMesh  = combined;
                child.AddComponent<MeshRenderer>().sharedMaterial = kvp.Key;
                // Static level geometry (no Rigidbody) — non-convex MeshCollider is fine and
                // gives exact collision matching the visual. Without this, hiding the original
                // per-brick tiles (each with its own BoxCollider) leaves the level with no
                // collision at all — the player falls straight through.
                if (_bakeAddCollider) child.AddComponent<MeshCollider>().sharedMesh = combined;
                if (_bakeSetStatic) child.isStatic = true;

                groupIdx++;
            }

            AssetDatabase.SaveAssets();

            if (_bakeHideOriginal && _root != null)
                _root.gameObject.SetActive(false);

            int totalVerts = bakedRoot.GetComponentsInChildren<MeshFilter>()
                .Sum(mf => mf.sharedMesh != null ? mf.sharedMesh.vertexCount : 0);

            EditorUtility.DisplayDialog("Bake Complete",
                $"Baked {filters.Count} tiles into {groups.Count} mesh(es).\n" +
                $"Total vertices: {totalVerts:N0}\n" +
                $"Saved to: {outFolder}", "OK");

            Selection.activeGameObject = bakedRoot;
        }

        // Merges adjacent structural tiles at the same height into the fewest possible flat
        // BoxColliders — no stud bumps (unlike a MeshCollider copy of the visual mesh) and far
        // fewer colliders than one per brick. Classic greedy-rectangle-merge: repeatedly grow
        // the largest available rectangle from an arbitrary still-unclaimed cell, claim it,
        // repeat. Not necessarily the mathematically fewest possible rectangles, but a simple,
        // reliable heuristic that works well for typical floor/wall layouts.
        Vector2Int MergeKey(float x, float z) =>
            new(Mathf.RoundToInt(x / TileWidthX), Mathf.RoundToInt(z / TileWidthZ));

        void GenerateMergedBoxColliders()
        {
            var tiles = AllTiles()
                .Where(go =>
                {
                    var marker = go.GetComponent<PlacedBrick>();
                    return marker != null && BrickShapeInfo.IsStructural(marker.shape);
                })
                .ToList();

            if (tiles.Count == 0)
            { EditorUtility.DisplayDialog("Collider vereinfachen", "Keine strukturellen Bricks (Plate/Brick) gefunden.", "OK"); return; }

            // Group by (Y, height) — only tiles that start at the same height AND share the
            // same brick height can safely merge into one flat box.
            var layers = new Dictionary<(int y, int h), List<GameObject>>();
            foreach (var go in tiles)
            {
                float height = TileHeight(go);
                var key = (Mathf.RoundToInt(go.transform.position.y * 10000f), Mathf.RoundToInt(height * 10000f));
                if (!layers.TryGetValue(key, out var list)) layers[key] = list = new List<GameObject>();
                list.Add(go);
            }

            // Safe to re-run: clears out a previous pass's boxes instead of stacking a second,
            // redundant set of colliders on top.
            var stale = GameObject.Find("Colliders_Baked");
            if (stale != null) Undo.DestroyObjectImmediate(stale);

            var collRoot = new GameObject("Colliders_Baked");
            Undo.RegisterCreatedObjectUndo(collRoot, "Generate Merged Box Colliders");
            int boxCount = 0;

            foreach (var layer in layers.Values)
            {
                float y      = layer[0].transform.position.y;
                float height = TileHeight(layer[0]);

                // Uses the fixed brick footprint (TileWidthX/Z), not the shared ToKey/StepX/StepZ
                // tied to the adjustable "Grid Step" field — that field may well be sitting at
                // something other than its 0.0795 default (e.g. left over from a Line-brush
                // pass), which would silently scramble this grouping if reused here.
                var remaining = new HashSet<Vector2Int>(
                    layer.Select(go => MergeKey(go.transform.position.x, go.transform.position.z)));

                while (remaining.Count > 0)
                {
                    var start = remaining.First();
                    GrowRectangle(remaining, start, out int x1, out int z1);

                    for (int x = start.x; x <= x1; x++)
                    for (int z = start.y; z <= z1; z++)
                        remaining.Remove(new Vector2Int(x, z));

                    float minX = start.x * TileWidthX, maxX = x1 * TileWidthX;
                    float minZ = start.y * TileWidthZ, maxZ = z1 * TileWidthZ;

                    var boxGo = new GameObject($"Box_{boxCount++}");
                    Undo.RegisterCreatedObjectUndo(boxGo, "Generate Merged Box Colliders");
                    boxGo.transform.SetParent(collRoot.transform, true);
                    boxGo.transform.position = new Vector3((minX + maxX) * 0.5f, y + height * 0.5f, (minZ + maxZ) * 0.5f);
                    var box = boxGo.AddComponent<BoxCollider>();
                    box.size = new Vector3(maxX - minX + TileWidthX, height, maxZ - minZ + TileWidthZ);
                }
            }

            EditorUtility.DisplayDialog("Collider vereinfachen",
                $"{boxCount} zusammengeführte Box-Collider erzeugt (aus {tiles.Count} Bricks) unter " +
                "'Colliders_Baked'.\n\nDie Original-Bricks kannst du jetzt gefahrlos ausblenden " +
                "(Renderer deaktivieren oder das ganze Level-Objekt verstecken) — die Kollision " +
                "übernimmt jetzt dieses Objekt.", "OK");
            Selection.activeGameObject = collRoot;
        }

        // Tries growing the rectangle from `start` both X-first-then-Z and Z-first-then-X, and
        // keeps whichever comes out bigger. A single fixed growth order (e.g. always X-first)
        // tends to shred straight walls/rings into lots of thin strips depending on which cell
        // happened to be picked first — trying both per step gives noticeably cleaner, more
        // "expected" rectangles for typical floor/wall layouts without a full (NP-hard) optimal
        // rectangle decomposition.
        void GrowRectangle(HashSet<Vector2Int> remaining, Vector2Int start, out int x1, out int z1)
        {
            // X-first
            int x1A = start.x;
            while (remaining.Contains(new Vector2Int(x1A + 1, start.y))) x1A++;
            int z1A = start.y;
            bool canGrowA = true;
            while (canGrowA)
            {
                int nz = z1A + 1;
                for (int x = start.x; x <= x1A; x++)
                    if (!remaining.Contains(new Vector2Int(x, nz))) { canGrowA = false; break; }
                if (canGrowA) z1A = nz;
            }

            // Z-first
            int z1B = start.y;
            while (remaining.Contains(new Vector2Int(start.x, z1B + 1))) z1B++;
            int x1B = start.x;
            bool canGrowB = true;
            while (canGrowB)
            {
                int nx = x1B + 1;
                for (int z = start.y; z <= z1B; z++)
                    if (!remaining.Contains(new Vector2Int(nx, z))) { canGrowB = false; break; }
                if (canGrowB) x1B = nx;
            }

            int areaA = (x1A - start.x + 1) * (z1A - start.y + 1);
            int areaB = (x1B - start.x + 1) * (z1B - start.y + 1);
            if (areaA >= areaB) { x1 = x1A; z1 = z1A; }
            else                { x1 = x1B; z1 = z1B; }
        }

        // ── World Data Export ──────────────────────────────────────────────
        // Only structural bricks (Plate/Brick) are written to the voxel world —
        // round decoration bricks stay scene-only (see BrickShapeInfo).
        void ExportAsWorldData()
        {
            const string outFolder = "Assets/_Project/GeneratedMeshes";
            if (!AssetDatabase.IsValidFolder(outFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "GeneratedMeshes");

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{outFolder}/WorldData.asset");
            var worldData = ScriptableObject.CreateInstance<WorldData>();

            // Build a local material list so we can map Material → index
            var matIndex = new Dictionary<Material, byte>();
            byte nextIdx = 0;

            int tileCount = 0;
            int skippedDecoration = 0;
            foreach (var go in AllTiles())
            {
                var marker = go.GetComponent<PlacedBrick>();
                BrickType shape = marker != null ? marker.shape : BrickType.Plate;

                if (!BrickShapeInfo.IsStructural(shape))
                {
                    skippedDecoration++;
                    continue;
                }

                var pos = go.transform.position;

                // Determine material index
                var mr  = go.GetComponentInChildren<MeshRenderer>();
                byte mi = 0;
                if (mr != null && mr.sharedMaterial != null)
                {
                    if (!matIndex.TryGetValue(mr.sharedMaterial, out mi))
                    {
                        mi = nextIdx++;
                        matIndex[mr.sharedMaterial] = mi;
                    }
                }

                // Convert world position to plate-unit global coords — must use the same
                // per-axis constants as ChunkData.LocalToWorld/WorldToChunkLocal for a
                // consistent round trip (the brick footprint isn't a perfect square).
                const float pw = WorldConstants.PlateWidth;
                const float pd = WorldConstants.PlateDepth;
                const float ph = WorldConstants.PlateHeight;
                int gx = Mathf.RoundToInt(pos.x / pw);
                int gy = Mathf.RoundToInt(pos.y / ph);
                int gz = Mathf.RoundToInt(pos.z / pd);

                // Chunk + local
                int cx = Mathf.FloorToInt((float)gx / ChunkData.SizeX);
                int cz = Mathf.FloorToInt((float)gz / ChunkData.SizeZ);
                int lx = gx - cx * ChunkData.SizeX;
                int ly = gy;
                int lz = gz - cz * ChunkData.SizeZ;

                var coord = new UnityEngine.Vector3Int(cx, 0, cz);
                var chunk = worldData.GetOrCreateChunk(coord);

                var cell = new BrickCell(shape, mi);
                int slots = BrickShapeInfo.HeightInPlates(shape);
                for (int dy = 0; dy < slots && ly + dy < ChunkData.SizeY; dy++)
                    chunk.Set(lx, ly + dy, lz, dy == 0 ? cell : new BrickCell(shape, mi));

                tileCount++;
            }

            AssetDatabase.CreateAsset(worldData, assetPath);
            AssetDatabase.SaveAssets();

            int matCount = matIndex.Count;
            EditorUtility.DisplayDialog("Export Complete",
                $"Exportiert: {tileCount} Bausteine.\n" +
                $"Übersprungen (Deko/rund): {skippedDecoration}.\n" +
                $"{matCount} unique materials → assign them to BrickWorld.MaterialPalette in the same index order.\n" +
                $"Saved: {assetPath}", "OK");

            Selection.activeObject = worldData;
        }

        // ── Util ───────────────────────────────────────────────────────────
        static void DrawBorder(Rect r, Color c, float w)
        {
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        r.width, w),    c);
            EditorGUI.DrawRect(new Rect(r.x,        r.yMax - w, r.width, w),    c);
            EditorGUI.DrawRect(new Rect(r.x,        r.y,        w, r.height),   c);
            EditorGUI.DrawRect(new Rect(r.xMax - w, r.y,        w, r.height),   c);
        }
    }
}
