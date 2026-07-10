using System.Collections.Generic;
using GameJamUniverse.World;
using UnityEditor;
using UnityEngine;

namespace GameJamUniverse.DevTools.Editor
{
    // Visual tool for assembling small characters (Player/Enemy/NPC bodies) out of the same 4
    // brick prefabs the Level Painter uses — but with rotation and a manual height offset
    // instead of world-grid stacking, since characters need limbs sticking out sideways rather
    // than a flat stacked level. Builds relative to a required "Character Root" so the result
    // can be saved as a relocatable prefab.
    public class CharacterBuilderWindow : EditorWindow
    {
        private enum BrushMode { Place, Erase }
        private enum PlacementMode { Free, Stack }

        private const float GridStep = WorldConstants.PlateWidth;

        private BrickType _brickType = BrickType.Brick;
        private BrushMode _brushMode = BrushMode.Place;
        private PlacementMode _placementMode = PlacementMode.Free;
        private int _rotX, _rotY, _rotZ; // 0/90/180/270
        private float _heightOffset;     // world units, added on top of the clicked ground point (Free mode only)
        private bool _mirrorX;
        private int _selMat;
        private bool _active;
        private Transform _root;
        private string _characterName = "MyCharacter";
        private Vector2 _scroll;

        // Stack mode spatial index: local XZ grid cell → current top-of-stack local Y.
        // Rebuilt lazily whenever the root's children might have changed underneath us.
        private readonly Dictionary<Vector2Int, float> _topIndex = new();
        private bool      _indexDirty = true;
        private Transform _lastRoot;

        private readonly Dictionary<BrickType, GameObject> _prefabs = new();
        private readonly List<Material> _mats = new();
        private readonly List<Color> _matColors = new();
        private readonly List<string> _matNames = new();

        private GUIStyle _segBtn, _segBtnSel, _paintBtn;
        private bool _stylesReady;

        private static readonly Color BG      = new(0.13f, 0.13f, 0.19f);
        private static readonly Color Accent   = new(0.45f, 0.75f, 1.00f);
        private static readonly Color AccentB  = new(1.00f, 0.60f, 0.25f);
        private static readonly Color GreenC   = new(0.30f, 0.88f, 0.50f);
        private static readonly Color RedC     = new(1.00f, 0.35f, 0.35f);
        private static readonly Color YellowC  = new(1.00f, 0.85f, 0.20f);
        private static readonly Color PurpleC  = new(0.70f, 0.45f, 1.00f);
        private static readonly Color OrangeC  = new(1.00f, 0.55f, 0.15f);

        [MenuItem("Tools/GameJam/Character Builder")]
        public static void Open()
        {
            var w = GetWindow<CharacterBuilderWindow>("Character Builder");
            w.minSize = new Vector2(300, 480);
        }

        private void OnEnable()
        {
            LoadAssets();
            SceneView.duringSceneGui += OnSceneGUI;
            Undo.undoRedoPerformed   += OnUndoRedo;
            _indexDirty = true;
        }

        private void OnDisable()
        {
            _active = false;
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed   -= OnUndoRedo;
        }

        private void OnUndoRedo() => _indexDirty = true;

        // ── Asset loading ──────────────────────────────────────────────────
        private void LoadAssets()
        {
            _prefabs.Clear();
            LoadPrefab(BrickType.Plate,      "Assets/_Project/Shared/Prefabs/Bricks/Plate.prefab");
            LoadPrefab(BrickType.Brick,      "Assets/_Project/Shared/Prefabs/Bricks/Brick.prefab");
            LoadPrefab(BrickType.PlateRound, "Assets/_Project/Shared/Prefabs/Bricks/PlateRound.prefab");
            LoadPrefab(BrickType.BrickRound, "Assets/_Project/Shared/Prefabs/Bricks/BrickRound.prefab");

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

        private void LoadPrefab(BrickType type, string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Debug.LogWarning($"[CharacterBuilder] Brick-Prefab fehlt: {path}");
            else _prefabs[type] = prefab;
        }

        // ── Styles ─────────────────────────────────────────────────────────
        private void EnsureStyles()
        {
            if (_stylesReady) return;
            _stylesReady = true;
            _segBtn = new GUIStyle(EditorStyles.miniButton)
            { fixedHeight = 30, fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.7f, 0.7f, 0.7f) } };
            _segBtnSel = new GUIStyle(_segBtn) { normal = { textColor = Color.black } };
            _paintBtn = new GUIStyle(GUI.skin.button)
            { fixedHeight = 42, fontSize = 14, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        }

        // ── OnGUI ──────────────────────────────────────────────────────────
        private void OnGUI()
        {
            EnsureStyles();

            if (_root != _lastRoot)
            {
                _lastRoot   = _root;
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
                DrawSectionTitle("CHARACTER ROOT"); DrawRootRow();       GUILayout.Space(8);
                DrawSectionTitle("BAUSTEIN");       DrawBrickTypeBar();  GUILayout.Space(8);
                DrawSectionTitle("MODUS");          DrawModeBar();       GUILayout.Space(8);
                DrawSectionTitle("PLATZIERUNG");     DrawPlacementModeRow(); GUILayout.Space(8);
                DrawSectionTitle("ROTATION");        DrawRotationRows(); GUILayout.Space(8);
                DrawSectionTitle("HÖHE");            DrawHeightRow();    GUILayout.Space(8);
                DrawSectionTitle("SPIEGELN");        DrawMirrorRow();    GUILayout.Space(8);
                DrawSectionTitle($"MATERIAL  ({_mats.Count})");
            } GUILayout.Space(10); }

            DrawPalette();
            GUILayout.Space(8);

            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
            using (new GUILayout.VerticalScope())
            {
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
            GUI.Label(r, "Character Builder", new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 22, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } });
        }

        private void DrawHelpText()
        {
            GUILayout.Space(4);
            using (new GUILayout.HorizontalScope()) { GUILayout.Space(10);
                GUILayout.Label(
                    "Baue Enemy/Player/NPC-Figuren aus den 4 Bricks: Root anlegen → Baustein + " +
                    "Rotation + Höhe wählen → Start Building → im Scene-View klicken.\n" +
                    "Stapeln = automatisch wie im Level Painter (z. B. Platte auf Platte). " +
                    "Frei = Höhe manuell setzen, für Arme/Kopf/seitliche Teile.\n" +
                    "Spiegeln negiert X-Position und Y-Rotation — bei X/Z-Rotationen ggf. von Hand " +
                    "nachjustieren, da echtes Spiegeln keine reine Rotation ist.",
                    new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.6f, 0.6f, 0.75f) } });
            GUILayout.Space(10); }
            GUILayout.Space(4);
        }

        private void DrawSectionTitle(string t) =>
            GUILayout.Label(t, new GUIStyle(EditorStyles.boldLabel) { fontSize = 10, normal = { textColor = new Color(0.55f, 0.55f, 0.75f) } });

        private void DrawRootRow()
        {
            using (new GUILayout.HorizontalScope())
            {
                _root = (Transform)EditorGUILayout.ObjectField(_root, typeof(Transform), true, GUILayout.Height(20));
                if (_root == null && GUILayout.Button("Create Root", GUILayout.Width(90), GUILayout.Height(20)))
                {
                    var go = new GameObject($"CharacterRoot_{_characterName}");
                    Undo.RegisterCreatedObjectUndo(go, "Create Character Root");
                    _root = go.transform;
                }
            }
            if (_root == null)
                EditorGUILayout.HelpBox("Ohne Root kein Bauen möglich — das Ergebnis muss später als Prefab verschiebbar sein.", MessageType.Warning);
        }

        private void DrawBrickTypeBar()
        {
            using (new GUILayout.HorizontalScope())
            {
                TypeBtn("▭  Boden-Platte", BrickType.Plate, Accent);
                GUILayout.Space(4);
                TypeBtn("▮  Wand-Baustein", BrickType.Brick, AccentB);
            }
            GUILayout.Space(3);
            using (new GUILayout.HorizontalScope())
            {
                TypeBtn("●  Runde Platte", BrickType.PlateRound, PurpleC);
                GUILayout.Space(4);
                TypeBtn("⬤  Runder Baustein", BrickType.BrickRound, OrangeC);
            }
        }

        private void TypeBtn(string label, BrickType t, Color col)
        {
            bool sel = _brickType == t;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _brickType = t;
            GUI.backgroundColor = old;
        }

        private void DrawModeBar()
        {
            using (new GUILayout.HorizontalScope())
            {
                ModeBtn("✏  Place", BrushMode.Place, GreenC);
                GUILayout.Space(4);
                ModeBtn("✕  Erase", BrushMode.Erase, RedC);
            }
        }

        private void ModeBtn(string label, BrushMode m, Color col)
        {
            bool sel = _brushMode == m;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _brushMode = m;
            GUI.backgroundColor = old;
        }

        private void DrawRotationRows()
        {
            RotRow("X", ref _rotX);
            RotRow("Y", ref _rotY);
            RotRow("Z", ref _rotZ);
        }

        private void RotRow(string axis, ref int value)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{axis}:", GUILayout.Width(16));
                if (GUILayout.Button("−90°", GUILayout.Width(50))) value = (value + 270) % 360;
                GUILayout.Label($"{value}°", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { normal = { textColor = Color.white } }, GUILayout.Width(36));
                if (GUILayout.Button("+90°", GUILayout.Width(50))) value = (value + 90) % 360;
            }
        }

        private void DrawPlacementModeRow()
        {
            using (new GUILayout.HorizontalScope())
            {
                PlacementBtn("↕ Stapeln", PlacementMode.Stack, YellowC);
                GUILayout.Space(4);
                PlacementBtn("✥ Frei", PlacementMode.Free, PurpleC);
            }
            GUILayout.Label(
                _placementMode == PlacementMode.Stack
                    ? "Baut automatisch auf dem höchsten Baustein an dieser Stelle auf (z. B. Platte auf Platte)."
                    : "Höhe wird manuell über das Feld unten gesetzt — für Arme/Kopf/seitliche Teile.",
                new GUIStyle(EditorStyles.wordWrappedMiniLabel) { normal = { textColor = new Color(0.55f, 0.55f, 0.7f) } });
        }

        private void PlacementBtn(string label, PlacementMode m, Color col)
        {
            bool sel = _placementMode == m;
            var  old = GUI.backgroundColor;
            GUI.backgroundColor = sel ? col : Color.Lerp(col, Color.black, 0.65f);
            if (GUILayout.Button(label, sel ? _segBtnSel : _segBtn)) _placementMode = m;
            GUI.backgroundColor = old;
        }

        private void DrawHeightRow()
        {
            using (new EditorGUI.DisabledScope(_placementMode == PlacementMode.Stack))
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label("Offset:", GUILayout.Width(50));
                if (GUILayout.Button("−", GUILayout.Width(24))) _heightOffset -= GridStep;
                _heightOffset = EditorGUILayout.FloatField(_heightOffset, GUILayout.Width(60));
                if (GUILayout.Button("+", GUILayout.Width(24))) _heightOffset += GridStep;
                if (GUILayout.Button("Reset", GUILayout.Width(50))) _heightOffset = 0f;
            }
        }

        private void DrawMirrorRow()
        {
            _mirrorX = EditorGUILayout.ToggleLeft("Mirror X (für symmetrische Arme/Beine)", _mirrorX);
        }

        private void DrawPalette()
        {
            if (_mats.Count == 0) return;
            float availW = position.width - 20f;
            int   rows   = Mathf.CeilToInt(_mats.Count / Mathf.Max(1f, Mathf.Floor(availW / 44f)));
            var   prect  = GUILayoutUtility.GetRect(availW, rows * 44f + 8f);

            float dx = 10f, dy = 4f;
            for (int i = 0; i < _mats.Count; i++)
            {
                var r = new Rect(prect.x + dx, prect.y + dy, 40f, 40f);
                EditorGUI.DrawRect(r, _matColors[i]);
                DrawBorder(r, i == _selMat ? Color.white : new Color(0f, 0f, 0f, 0.35f), i == _selMat ? 2f : 1f);
                if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition))
                { _selMat = i; Repaint(); Event.current.Use(); }
                GUI.Label(r, new GUIContent("", _matNames[i]));
                dx += 44f;
                if (i < _mats.Count - 1 && dx + 40f > availW - 4f) { dx = 10f; dy += 44f; }
            }
            if (_selMat >= 0 && _selMat < _matNames.Count)
                GUILayout.Label(_matNames[_selMat], new GUIStyle(EditorStyles.centeredGreyMiniLabel) { normal = { textColor = new Color(0.75f, 0.75f, 0.9f) } });
        }

        private void DrawActivateBtn()
        {
            using (new EditorGUI.DisabledScope(_root == null))
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = _active ? GreenC : new Color(0.3f, 0.3f, 0.5f);
                if (GUILayout.Button(_active ? "● BUILDING ACTIVE — Click to Stop" : "Start Building", _paintBtn))
                { _active = !_active; SceneView.RepaintAll(); }
                GUI.backgroundColor = old;
            }
        }

        private void DrawSaveSection()
        {
            _characterName = EditorGUILayout.TextField("Name", _characterName);
            using (new EditorGUI.DisabledScope(_root == null || _root.childCount == 0 || string.IsNullOrWhiteSpace(_characterName)))
            {
                var old = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.25f, 0.55f, 0.85f);
                if (GUILayout.Button("⬡  Save as Prefab", _paintBtn))
                    SaveAsPrefab();
                GUI.backgroundColor = old;
            }
        }

        private void SaveAsPrefab()
        {
            const string folder = "Assets/_Project/Shared/Prefabs/Characters";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/Shared/Prefabs", "Characters");

            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{_characterName}.prefab");
            var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(_root.gameObject, path, InteractionMode.UserAction);

            EditorUtility.DisplayDialog("Gespeichert",
                $"Charakter gespeichert unter:\n{path}\n\n" +
                "Zieh das Prefab jetzt als 'Body' in dein Player/Enemy-Prefab (alte Platzhalter-Optik entfernen).",
                "OK");
            Selection.activeObject = saved;
        }

        // ── Scene GUI ──────────────────────────────────────────────────────
        private void OnSceneGUI(SceneView sv)
        {
            if (!_active || _root == null) return;
            EnsureIndex();
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            var e   = Event.current;
            var hit = GetLocalPlacementPoint(e);

            if (hit.HasValue)
            {
                var rot      = Quaternion.Euler(_rotX, _rotY, _rotZ);
                var worldPos = _root.TransformPoint(hit.Value);
                var worldRot = _root.rotation * rot;
                DrawPreview(worldPos, worldRot, false);

                if (_mirrorX && !Mathf.Approximately(hit.Value.x, 0f))
                {
                    var mirrorLocal = new Vector3(-hit.Value.x, hit.Value.y, hit.Value.z);
                    var mirrorRot   = Quaternion.Euler(_rotX, -_rotY, _rotZ);
                    DrawPreview(_root.TransformPoint(mirrorLocal), _root.rotation * mirrorRot, true);
                }
            }

            DrawSceneHUD(sv);

            bool clicked = e.type == EventType.MouseDown && e.button == 0 && !e.alt;
            if (clicked && hit.HasValue)
            {
                if (_brushMode == BrushMode.Place) PlaceBrick(hit.Value);
                else EraseNear(hit.Value);
                e.Use();
            }
            sv.Repaint();
        }

        // Raycasts against the root's local XZ ground plane (root assumed axis-aligned/unrotated
        // for simplicity), snaps X/Z to the brick grid. Height comes from the stack index in
        // Stack mode (flat-on-flat auto-stacking), or the manual offset field in Free mode.
        private Vector3? GetLocalPlacementPoint(Event e)
        {
            var ray   = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            var plane = new Plane(Vector3.up, _root.position);
            if (!plane.Raycast(ray, out float t)) return null;

            var worldPoint = ray.GetPoint(t);
            var local       = _root.InverseTransformPoint(worldPoint);
            float x = Mathf.Round(local.x / GridStep) * GridStep;
            float z = Mathf.Round(local.z / GridStep) * GridStep;
            float y = _placementMode == PlacementMode.Stack ? GetStackTopY(x, z) : _heightOffset;
            return new Vector3(x, y, z);
        }

        // ── Stack-mode spatial index (local-space, root-relative) ──────────
        private Vector2Int ToKey(float x, float z)
        {
            float s = Mathf.Max(0.001f, GridStep);
            return new Vector2Int(Mathf.RoundToInt(x / s), Mathf.RoundToInt(z / s));
        }

        private void EnsureIndex()
        {
            if (!_indexDirty) return;
            _topIndex.Clear();
            if (_root != null)
                foreach (Transform child in _root)
                    IndexAdd(child.localPosition, BrickHeightOf(child));
            _indexDirty = false;
        }

        private float BrickHeightOf(Transform child)
        {
            var marker = child.GetComponent<PlacedBrick>();
            var shape  = marker != null ? marker.shape : _brickType;
            return BrickShapeInfo.HeightInPlates(shape) * WorldConstants.PlateHeight;
        }

        private void IndexAdd(Vector3 localPos, float height)
        {
            var   key = ToKey(localPos.x, localPos.z);
            float top = localPos.y + height;
            if (!_topIndex.TryGetValue(key, out float cur) || top > cur)
                _topIndex[key] = top;
        }

        private float GetStackTopY(float x, float z)
        {
            EnsureIndex();
            var key = ToKey(x, z);
            return _topIndex.TryGetValue(key, out float top) ? top : 0f;
        }

        private void DrawPreview(Vector3 worldPos, Quaternion worldRot, bool ghost)
        {
            float h    = BrickShapeInfo.HeightInPlates(_brickType) * WorldConstants.PlateHeight;
            var   size = new Vector3(GridStep, h, GridStep);
            Handles.color = ghost ? new Color(1f, 0.7f, 0.3f, 0.4f) : new Color(0.4f, 0.9f, 1f, 0.55f);
            var m = Handles.matrix;
            Handles.matrix = Matrix4x4.TRS(worldPos, worldRot, Vector3.one);
            Handles.DrawWireCube(new Vector3(0, h * 0.5f, 0), size);
            Handles.matrix = m;
        }

        private void DrawSceneHUD(SceneView sv)
        {
            Handles.BeginGUI();
            string mat   = _selMat < _matNames.Count ? _matNames[_selMat] : "—";
            string mode  = _brushMode == BrushMode.Place ? "Place" : "Erase";
            string place = _placementMode == PlacementMode.Stack ? "Stack" : $"Frei H:{_heightOffset:F3}";
            string text  = $"[{mode}] {_brickType}  {place}  Rot({_rotX},{_rotY},{_rotZ})  |  {mat}{(_mirrorX ? "  [Mirror]" : "")}";
            float  w    = Mathf.Min(sv.position.width - 20f, 460f), h = 26f;
            var    r    = new Rect(10f, sv.position.height - h - 36f, w, h);
            EditorGUI.DrawRect(r, new Color(0f, 0f, 0f, 0.62f));
            DrawBorder(r, new Color(0.4f, 0.92f, 0.5f, 0.9f), 1f);
            GUI.Label(new Rect(r.x + 8f, r.y + 5f, r.width, r.height), text,
                new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.4f, 0.95f, 0.5f) } });
            Handles.EndGUI();
        }

        // ── Placement ops ──────────────────────────────────────────────────
        private void PlaceBrick(Vector3 localPos)
        {
            InstantiateAt(localPos, Quaternion.Euler(_rotX, _rotY, _rotZ));

            if (_mirrorX && !Mathf.Approximately(localPos.x, 0f))
            {
                var mirrorPos = new Vector3(-localPos.x, localPos.y, localPos.z);
                var mirrorRot = Quaternion.Euler(_rotX, -_rotY, _rotZ);
                InstantiateAt(mirrorPos, mirrorRot);
            }
        }

        private void InstantiateAt(Vector3 localPos, Quaternion localRot)
        {
            if (!_prefabs.TryGetValue(_brickType, out var prefab) || prefab == null)
            { Debug.LogWarning("[CharacterBuilder] Prefab not found."); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(_root, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = localRot;

            if (_selMat >= 0 && _selMat < _mats.Count)
                foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                    mr.sharedMaterial = _mats[_selMat];

            var marker = go.GetComponent<PlacedBrick>();
            if (marker != null)
            {
                marker.shape = _brickType;
                marker.materialIndex = (byte)Mathf.Clamp(_selMat, 0, 255);
            }

            IndexAdd(localPos, BrickShapeInfo.HeightInPlates(_brickType) * WorldConstants.PlateHeight);
            Undo.RegisterCreatedObjectUndo(go, "Place Character Brick");
        }

        private void EraseNear(Vector3 localPos)
        {
            if (_root.childCount == 0) return;

            Transform closest = null;
            float     bestDist = float.MaxValue;
            foreach (Transform child in _root)
            {
                float d = Vector3.Distance(child.localPosition, localPos);
                if (d < bestDist) { bestDist = d; closest = child; }
            }
            if (closest != null && bestDist < GridStep)
            {
                Undo.DestroyObjectImmediate(closest.gameObject);
                _indexDirty = true; // simplest correct fix — recompute top-of-stack from scratch
            }
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
