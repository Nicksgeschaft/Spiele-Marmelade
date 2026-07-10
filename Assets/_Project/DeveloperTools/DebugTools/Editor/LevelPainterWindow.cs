using System.Collections.Generic;
using System.Linq;
using GameJamUniverse.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace GameJamUniverse.DevTools.Editor
{
    public class LevelPainterWindow : EditorWindow
    {
        // ── Enums ──────────────────────────────────────────────────────────
        private enum BrushMode  { Paint, Erase }
        private enum StackMode  { Stack, Replace, ReplaceOnly, ReplaceStack }
        private enum BrushShape { Single, Rect, Circle }

        // ── Tile footprint at raw OBJ scale (step 0.0795 confirmed by user) ─
        // All 4 brick shapes share the same 1x1 footprint — only height differs,
        // and that's driven by BrickShapeInfo so it never has to be hand-kept in sync.
        private const float TileWidth = WorldConstants.PlateWidth;

        // ── State ──────────────────────────────────────────────────────────
        private BrickType _tileType   = BrickType.Plate;
        private BrushMode  _brushMode  = BrushMode.Paint;
        private StackMode  _stackMode  = StackMode.Stack;
        private BrushShape _brushShape = BrushShape.Single;
        private int        _brushRadius  = 0;
        private int        _replaceDepth = 3;
        private float      _gridStep    = 0.0795f;
        private int        _selMat      = 0;
        private bool       _active      = false;
        private Transform  _root;
        private Vector2    _scroll;
        private Vector3    _lastCenter  = Vector3.positiveInfinity;

        // ── Bake settings ──────────────────────────────────────────────────
        private bool _bakeHideOriginal = true;
        private bool _bakeSetStatic    = false;

        // ── Spatial index: grid key → top-of-stack world Y ─────────────────
        // O(1) lookup during hover instead of O(n) iteration
        private readonly Dictionary<Vector2Int, float> _topYIndex = new();
        private bool      _indexDirty  = true;
        private Transform _lastRoot;
        private float     _lastStep    = -1f;

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
        [MenuItem("Tools/GameJam/Level Painter")]
        public static void Open()
        {
            var w = GetWindow<LevelPainterWindow>("Level Painter");
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
                DrawSectionTitle("STACKING");      DrawStackModeRow();    GUILayout.Space(8);
                DrawSectionTitle("BRUSH SHAPE");   DrawBrushShapeRow();   GUILayout.Space(8);
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
            GUI.Label(r, "Level Painter", new GUIStyle(EditorStyles.boldLabel)
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
                ModeBtn("✏  Paint", BrushMode.Paint, GreenC);
                GUILayout.Space(4);
                ModeBtn("✕  Erase", BrushMode.Erase, RedC);
            }
        }

        void ModeBtn(string label, BrushMode m, Color col)
        {
            bool sel = _brushMode == m;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _brushMode = m;
            GUI.backgroundColor = old;
        }

        void DrawStackModeRow()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                StackBtn("↕ Stack",        StackMode.Stack,        YellowC);
                GUILayout.Space(3);
                StackBtn("↔ Replace",      StackMode.Replace,      PurpleC);
            }
            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                StackBtn("→ Only",         StackMode.ReplaceOnly,  new Color(0.6f, 0.4f, 1.0f));
                GUILayout.Space(3);
                StackBtn("⬡ Stack N",      StackMode.ReplaceStack, new Color(1.0f, 0.5f, 0.8f));
            }

            GUILayout.Space(3);
            string hint = _stackMode switch
            {
                StackMode.Stack        => "Baut auf – 3× Platte = 1× Baustein",
                StackMode.Replace      => "Ersetzt oben – platziert neu wenn leer",
                StackMode.ReplaceOnly  => "Ersetzt oben – überspringt leere Zellen",
                StackMode.ReplaceStack => "Ersetzt die obersten N Tiles",
                _                     => ""
            };
            GUILayout.Label(hint, new GUIStyle(EditorStyles.centeredGreyMiniLabel)
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

        void StackBtn(string label, StackMode m, Color col)
        {
            bool sel = _stackMode == m;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _stackMode = m;
            GUI.backgroundColor = old;
        }

        void DrawBrushShapeRow()
        {
            GUILayout.Space(2);
            using (new GUILayout.HorizontalScope())
            {
                ShapeBtn("■ Single", BrushShape.Single);
                GUILayout.Space(3);
                ShapeBtn("▦ Rect",   BrushShape.Rect);
                GUILayout.Space(3);
                ShapeBtn("● Circle", BrushShape.Circle);
            }
            if (_brushShape != BrushShape.Single)
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
        }

        void ShapeBtn(string label, BrushShape shape)
        {
            bool sel = _brushShape == shape;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? new Color(0.55f, 0.55f, 0.85f) : new Color(0.20f, 0.20f, 0.30f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _brushShape = shape;
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

            Event e      = Event.current;
            var   center = GetBaseCell(e);

            if (center.HasValue && _brushMode == BrushMode.Paint)
            {
                var cells = GetBrushCells(center.Value);
                float hw = TileWidth;
                float hh = TileHeightFor(_tileType);
                Handles.color = new Color(0.4f, 0.9f, 1f, 0.55f);
                foreach (var c in cells)
                    Handles.DrawWireCube(c + new Vector3(0f, hh * 0.5f, 0f), new Vector3(hw, hh, hw));
            }

            DrawSceneHUD(sv);

            if (e.type == EventType.MouseUp && e.button == 0)
                _lastCenter = Vector3.positiveInfinity;

            bool lmb = (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                       && e.button == 0 && !e.alt;

            if (lmb && center.HasValue && center.Value != _lastCenter)
            {
                _lastCenter = center.Value;
                foreach (var c in GetBrushCells(center.Value))
                {
                    if (_brushMode == BrushMode.Paint) PlaceTile(c);
                    else                               EraseAt(c.x, c.z);
                }
                e.Use();
            }
            sv.Repaint();
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
            string mode  = _brushMode == BrushMode.Paint ? "Paint"  : "Erase";
            string stack = _stackMode == StackMode.Stack  ? "Stack"  : "Replace";
            string shape = _brushShape == BrushShape.Single ? "1×1"
                         : _brushShape == BrushShape.Rect   ? $"Rect r{_brushRadius}"
                         :                                    $"Circle r{_brushRadius}";
            string text  = $"[{mode}] {tile}  {stack}  {shape}  |  {mat}";
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
        Vector2Int ToKey(float x, float z)
        {
            float s = Mathf.Max(0.001f, _gridStep);
            return new Vector2Int(Mathf.RoundToInt(x / s), Mathf.RoundToInt(z / s));
        }

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
            var   key  = ToKey(x, z);
            float snap = Mathf.Max(0.001f, _gridStep) * 0.5f;
            float best = float.MinValue;
            bool  any  = false;
            foreach (var go in AllTiles())
            {
                if (Mathf.Abs(go.transform.position.x - x) > snap) continue;
                if (Mathf.Abs(go.transform.position.z - z) > snap) continue;
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
            float step = Mathf.Max(0.001f, _gridStep);
            var   plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float t)) return null;
            var p = ray.GetPoint(t);
            return new Vector3(Mathf.Round(p.x / step) * step, 0f, Mathf.Round(p.z / step) * step);
        }

        List<Vector3> GetBrushCells(Vector3 center)
        {
            var   result = new List<Vector3>();
            float step   = Mathf.Max(0.001f, _gridStep);
            int   r      = _brushShape == BrushShape.Single ? 0 : _brushRadius;

            for (int dx = -r; dx <= r; dx++)
            for (int dz = -r; dz <= r; dz++)
            {
                if (_brushShape == BrushShape.Circle && dx * dx + dz * dz > r * r) continue;
                float cx = center.x + dx * step;
                float cz = center.z + dz * step;
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
            float snap = Mathf.Max(0.001f, _gridStep) * 0.5f;
            if (_root != null)
            {
                foreach (Transform child in _root)
                {
                    if (Mathf.Abs(child.position.x - x) < snap &&
                        Mathf.Abs(child.position.z - z) < snap)
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
                    if (Mathf.Abs(go.transform.position.x - x) < snap &&
                        Mathf.Abs(go.transform.position.z - z) < snap &&
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
            float snap    = Mathf.Max(0.001f, _gridStep) * 0.5f;
            float bestTop = float.MinValue;
            GameObject result = null;
            foreach (var go in AllTiles())
            {
                if (Mathf.Abs(go.transform.position.x - x) > snap) continue;
                if (Mathf.Abs(go.transform.position.z - z) > snap) continue;
                float top = go.transform.position.y + TileHeight(go);
                if (top > bestTop) { bestTop = top; result = go; }
            }
            return result;
        }

        // All tiles at XZ sorted top → bottom
        List<GameObject> FindAllTilesAt(float x, float z)
        {
            float snap = Mathf.Max(0.001f, _gridStep) * 0.5f;
            return AllTiles()
                .Where(go => Mathf.Abs(go.transform.position.x - x) < snap &&
                             Mathf.Abs(go.transform.position.z - z) < snap)
                .OrderByDescending(go => go.transform.position.y + TileHeight(go))
                .ToList();
        }

        bool TileExistsAt(Vector3 cell)
        {
            float xzSnap = Mathf.Max(0.001f, _gridStep) * 0.5f;
            float ySnap  = TileHeightFor(BrickType.Plate) * 0.1f;
            foreach (var go in AllTiles())
                if (Mathf.Abs(go.transform.position.x - cell.x) < xzSnap &&
                    Mathf.Abs(go.transform.position.z - cell.z) < xzSnap &&
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

                // Convert world position to plate-unit global coords
                const float pw = WorldConstants.PlateWidth;
                const float ph = WorldConstants.PlateHeight;
                int gx = Mathf.RoundToInt(pos.x / pw);
                int gy = Mathf.RoundToInt(pos.y / ph);
                int gz = Mathf.RoundToInt(pos.z / pw);

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
