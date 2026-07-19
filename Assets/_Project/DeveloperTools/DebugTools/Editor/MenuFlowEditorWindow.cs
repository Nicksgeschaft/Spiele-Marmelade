using System.Collections.Generic;
using System.Linq;
using SpieleMarmelade.Shared.UI.MenuFlow;
using UnityEditor;
using UnityEngine;

namespace SpieleMarmelade.DevTools.Editor
{
    /// <summary>
    /// Visual editor for a <see cref="MenuFlowGraph"/>: drag screens around a canvas, see
    /// their button connections as curves, edit each screen's buttons/targets in a side
    /// inspector, then click "Generate" to build the actual Canvas/panels into the currently
    /// open minigame scene (see <see cref="MenuFlowGenerator"/>).
    /// </summary>
    public class MenuFlowEditorWindow : EditorWindow
    {
        private const float NodeWidth = 160f;
        private const float NodeHeight = 76f;
        private const float InspectorWidth = 300f;

        private MenuFlowGraph _graph;
        private string _selectedScreenId;
        private Vector2 _canvasScroll;
        private Vector2 _inspectorScroll;

        [MenuItem("Tools/Game Creation/Menu Flow Editor")]
        public static void Open() => GetWindow<MenuFlowEditorWindow>("Menu Flow Editor");

        private void OnGUI()
        {
            DrawToolbar();

            if (_graph == null)
            {
                EditorGUILayout.HelpBox(
                    "Kein Menu Flow Graph geladen. Oben ein bestehendes Graph-Asset wählen " +
                    "oder über 'Neu...' eins anlegen (mit sinnvollem Standard-Flow vorbefüllt).",
                    MessageType.Info);
                return;
            }

            using (new GUILayout.HorizontalScope())
            {
                DrawCanvas();
                DrawInspector();
            }

            if (Event.current.type == EventType.MouseDrag) Repaint();
        }

        // ── Toolbar ──────────────────────────────────────────────────────────
        private void DrawToolbar()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                var newGraph = (MenuFlowGraph)EditorGUILayout.ObjectField(_graph, typeof(MenuFlowGraph), false, GUILayout.Width(240));
                if (newGraph != _graph)
                {
                    _graph = newGraph;
                    _selectedScreenId = null;
                }

                if (GUILayout.Button("Neu...", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    CreateNewGraph();

                if (GUILayout.Button("+ Screen", EditorStyles.toolbarButton, GUILayout.Width(70)) && _graph != null)
                {
                    var node = new MenuScreenNode { title = "New Screen", editorPosition = new Vector2(40, 40) };
                    _graph.screens.Add(node);
                    _selectedScreenId = node.id;
                    EditorUtility.SetDirty(_graph);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(_graph == null))
                {
                    var old = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.35f, 0.75f, 0.45f);
                    if (GUILayout.Button("Generate", EditorStyles.toolbarButton, GUILayout.Width(90)))
                        MenuFlowGenerator.Generate(_graph);
                    GUI.backgroundColor = old;
                }
            }
        }

        private void CreateNewGraph()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Neuer Menu Flow Graph", "MenuFlow_NewGame", "asset",
                "Wo soll der Graph gespeichert werden? (z.B. Minigames/<Name>/Config/)");
            if (string.IsNullOrEmpty(path)) return;

            var graph = CreateInstance<MenuFlowGraph>();
            SeedDefaultGraph(graph);
            ApplyRememberedAppearance(graph);
            AssetDatabase.CreateAsset(graph, path);
            AssetDatabase.SaveAssets();
            _graph = graph;
            _selectedScreenId = graph.startScreenId;
        }

        // Default flow: Main Menu -> Play/Options/Credits/Quit, Options/Credits have Back,
        // Pause (reached via Escape at runtime, not a button) offers Resume/Options/Main Menu/Quit.
        public static void SeedDefaultGraph(MenuFlowGraph graph)
        {
            var mainMenu = new MenuScreenNode { kind = MenuScreenKind.Generic, title = "Main Menu", editorPosition = new Vector2(40, 40) };
            var options  = new MenuScreenNode { kind = MenuScreenKind.Options, title = "Options", editorPosition = new Vector2(340, 40) };
            var credits  = new MenuScreenNode { kind = MenuScreenKind.Generic, title = "Credits", editorPosition = new Vector2(340, 180),
                bodyText = "Made during a game jam with Spiele Marmelade." };
            var game     = new MenuScreenNode { kind = MenuScreenKind.Game, title = "Game", editorPosition = new Vector2(40, 180) };
            var pause    = new MenuScreenNode { kind = MenuScreenKind.Pause, title = "Pause", editorPosition = new Vector2(40, 320) };

            mainMenu.buttons.Add(new MenuButtonDef { label = "Play", targetScreenId = game.id });
            mainMenu.buttons.Add(new MenuButtonDef { label = "Options", targetScreenId = options.id });
            mainMenu.buttons.Add(new MenuButtonDef { label = "Credits", targetScreenId = credits.id });
            mainMenu.buttons.Add(new MenuButtonDef { label = "Quit", specialAction = MenuSpecialAction.QuitApp });

            options.buttons.Add(new MenuButtonDef { label = "Back", targetScreenId = mainMenu.id });
            credits.buttons.Add(new MenuButtonDef { label = "Back", targetScreenId = mainMenu.id });

            pause.buttons.Add(new MenuButtonDef { label = "Resume", specialAction = MenuSpecialAction.ResumeGame });
            pause.buttons.Add(new MenuButtonDef { label = "Options", targetScreenId = options.id });
            pause.buttons.Add(new MenuButtonDef { label = "Main Menu", targetScreenId = mainMenu.id });
            pause.buttons.Add(new MenuButtonDef { label = "Quit", specialAction = MenuSpecialAction.QuitApp });

            graph.screens.AddRange(new[] { mainMenu, options, credits, game, pause });
            graph.startScreenId = mainMenu.id;
        }

        // ── Canvas ───────────────────────────────────────────────────────────
        private void DrawCanvas()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(position.width - InspectorWidth)))
            {
                var viewRect = GUILayoutUtility.GetRect(
                    position.width - InspectorWidth, position.height - 22, GUILayout.ExpandHeight(true));
                GUI.Box(viewRect, GUIContent.none);

                _canvasScroll = GUI.BeginScrollView(viewRect, _canvasScroll, new Rect(0, 0, 2000, 1400));

                DrawConnections();

                BeginWindows();
                for (int i = 0; i < _graph.screens.Count; i++)
                {
                    var node = _graph.screens[i];
                    var rect = new Rect(node.editorPosition.x, node.editorPosition.y, NodeWidth, NodeHeight);

                    var oldBg = GUI.backgroundColor;
                    GUI.backgroundColor = NodeColor(node.kind, node.id == _selectedScreenId);
                    rect = GUI.Window(i, rect, DrawNodeWindow, node.title);
                    GUI.backgroundColor = oldBg;

                    node.editorPosition = rect.position;
                }
                EndWindows();

                GUI.EndScrollView();
            }
        }

        private void DrawNodeWindow(int windowId)
        {
            var node = _graph.screens[windowId];
            if (Event.current.type == EventType.MouseDown)
                _selectedScreenId = node.id;

            GUILayout.Label(node.kind.ToString(), EditorStyles.miniLabel);
            foreach (var btn in node.buttons)
                GUILayout.Label("• " + btn.label, EditorStyles.wordWrappedMiniLabel);

            GUI.DragWindow();
        }

        private void DrawConnections()
        {
            foreach (var node in _graph.screens)
            {
                var fromRect = new Rect(node.editorPosition.x, node.editorPosition.y, NodeWidth, NodeHeight);
                var fromPoint = new Vector3(fromRect.xMax, fromRect.center.y, 0);

                foreach (var btn in node.buttons)
                {
                    if (string.IsNullOrEmpty(btn.targetScreenId)) continue;
                    var target = _graph.FindScreen(btn.targetScreenId);
                    if (target == null) continue;

                    var toRect = new Rect(target.editorPosition.x, target.editorPosition.y, NodeWidth, NodeHeight);
                    var toPoint = new Vector3(toRect.xMin, toRect.center.y, 0);
                    var tangent = Vector3.right * 60f;

                    Handles.DrawBezier(fromPoint, toPoint, fromPoint + tangent, toPoint - tangent,
                        new Color(0.4f, 0.85f, 1f), null, 3f);
                }
            }
        }

        private static Color NodeColor(MenuScreenKind kind, bool selected)
        {
            Color c = kind switch
            {
                MenuScreenKind.Options          => new Color(0.55f, 0.75f, 1f),
                MenuScreenKind.Pause            => new Color(1f, 0.7f, 0.4f),
                MenuScreenKind.Game             => new Color(0.55f, 0.9f, 0.6f),
                MenuScreenKind.CharacterCreator => new Color(0.85f, 0.6f, 1f),
                _                               => Color.white,
            };
            return selected ? c : Color.Lerp(c, new Color(0.6f, 0.6f, 0.6f), 0.35f);
        }

        // ── Inspector ────────────────────────────────────────────────────────
        private void DrawInspector()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(InspectorWidth)))
            {
                _inspectorScroll = EditorGUILayout.BeginScrollView(_inspectorScroll);

                DrawStylingSection();
                EditorGUILayout.Space();

                var node = _graph.FindScreen(_selectedScreenId);
                if (node == null)
                {
                    EditorGUILayout.HelpBox("Screen anklicken, um ihn zu bearbeiten.", MessageType.None);
                }
                else
                {
                    DrawScreenInspector(node);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // Graph-wide styling (applies to every screen this graph generates), set here before
        // clicking Generate rather than by hunting for the graph .asset in the Project window.
        private bool _showStyling = true;

        private void DrawStylingSection()
        {
            _showStyling = EditorGUILayout.Foldout(_showStyling, "Darstellung (vor Generate einstellen)", true);
            if (!_showStyling) return;

            EditorGUI.BeginChangeCheck();
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Button-Hintergrund", EditorStyles.boldLabel);
                _graph.buttonHasBackground = EditorGUILayout.ToggleLeft("Hintergrund-Bricks", _graph.buttonHasBackground);
                using (new EditorGUI.DisabledScope(!_graph.buttonHasBackground))
                {
                    _graph.buttonBackgroundMaterial = (Material)EditorGUILayout.ObjectField(
                        "Material (optional)", _graph.buttonBackgroundMaterial, typeof(Material), false);
                    using (new EditorGUI.DisabledScope(_graph.buttonBackgroundMaterial != null))
                        _graph.buttonBackgroundColor = EditorGUILayout.ColorField("Farbe", _graph.buttonBackgroundColor);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Button-Schrift", EditorStyles.boldLabel);
                _graph.buttonLetterMaterial = (Material)EditorGUILayout.ObjectField(
                    "Material (optional)", _graph.buttonLetterMaterial, typeof(Material), false);

                using (new EditorGUI.DisabledScope(_graph.buttonLetterMaterial != null))
                {
                    EditorGUILayout.LabelField("Farben (mehrere = wechseln pro Buchstabe)");
                    int colorToRemove = -1;
                    for (int i = 0; i < _graph.buttonLetterColors.Count; i++)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            _graph.buttonLetterColors[i] = EditorGUILayout.ColorField(_graph.buttonLetterColors[i]);
                            if (GUILayout.Button("x", GUILayout.Width(20))) colorToRemove = i;
                        }
                    }
                    if (colorToRemove >= 0) _graph.buttonLetterColors.RemoveAt(colorToRemove);

                    if (GUILayout.Button("+ Farbe hinzufügen"))
                        _graph.buttonLetterColors.Add(Color.white);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Options-Slider", EditorStyles.boldLabel);
                _graph.sliderFilledColor = EditorGUILayout.ColorField("Gefüllt", _graph.sliderFilledColor);
                _graph.sliderUnfilledColor = EditorGUILayout.ColorField("Leer", _graph.sliderUnfilledColor);
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_graph);
                SaveAppearanceAsDefault(_graph);
            }
        }

        // ── Gemerkte Darstellung (über Projekte/Minigames hinweg, pro Rechner) ──
        private const string PrefPrefix = "SpieleMarmelade.MenuFlow.Appearance.";

        private static void SaveAppearanceAsDefault(MenuFlowGraph graph)
        {
            EditorPrefs.SetBool(PrefPrefix + "HasBackground", graph.buttonHasBackground);
            EditorPrefs.SetString(PrefPrefix + "BackgroundMaterialGuid", MaterialToGuid(graph.buttonBackgroundMaterial));
            EditorPrefs.SetString(PrefPrefix + "BackgroundColor", ColorUtility.ToHtmlStringRGBA(graph.buttonBackgroundColor));
            EditorPrefs.SetString(PrefPrefix + "LetterMaterialGuid", MaterialToGuid(graph.buttonLetterMaterial));
            EditorPrefs.SetString(PrefPrefix + "LetterColors",
                string.Join(";", graph.buttonLetterColors.Select(ColorUtility.ToHtmlStringRGBA)));
            EditorPrefs.SetString(PrefPrefix + "SliderFilledColor", ColorUtility.ToHtmlStringRGBA(graph.sliderFilledColor));
            EditorPrefs.SetString(PrefPrefix + "SliderUnfilledColor", ColorUtility.ToHtmlStringRGBA(graph.sliderUnfilledColor));
        }

        private static void ApplyRememberedAppearance(MenuFlowGraph graph)
        {
            if (!EditorPrefs.HasKey(PrefPrefix + "HasBackground")) return; // noch nie gespeichert

            graph.buttonHasBackground = EditorPrefs.GetBool(PrefPrefix + "HasBackground", graph.buttonHasBackground);
            graph.buttonBackgroundMaterial = GuidToMaterial(EditorPrefs.GetString(PrefPrefix + "BackgroundMaterialGuid", ""));
            graph.buttonBackgroundColor = ParseColor(EditorPrefs.GetString(PrefPrefix + "BackgroundColor", ""), graph.buttonBackgroundColor);
            graph.buttonLetterMaterial = GuidToMaterial(EditorPrefs.GetString(PrefPrefix + "LetterMaterialGuid", ""));

            string letterColors = EditorPrefs.GetString(PrefPrefix + "LetterColors", "");
            graph.buttonLetterColors = string.IsNullOrEmpty(letterColors)
                ? new List<Color>()
                : letterColors.Split(';').Select(hex => ParseColor(hex, Color.white)).ToList();

            graph.sliderFilledColor = ParseColor(EditorPrefs.GetString(PrefPrefix + "SliderFilledColor", ""), graph.sliderFilledColor);
            graph.sliderUnfilledColor = ParseColor(EditorPrefs.GetString(PrefPrefix + "SliderUnfilledColor", ""), graph.sliderUnfilledColor);
        }

        private static string MaterialToGuid(Material mat) =>
            mat != null && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mat, out string guid, out long _) ? guid : "";

        private static Material GuidToMaterial(string guid)
        {
            if (string.IsNullOrEmpty(guid)) return null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        private static Color ParseColor(string hex, Color fallback) =>
            !string.IsNullOrEmpty(hex) && ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : fallback;

        private void DrawScreenInspector(MenuScreenNode node)
        {
            EditorGUILayout.LabelField("Screen bearbeiten", EditorStyles.boldLabel);
            node.title = EditorGUILayout.TextField("Titel", node.title);
            // Editable rather than a read-only label: new screens are always created as Generic, so
            // without this there was no way to make one an Options/Pause/Character-Creator screen.
            node.kind = (MenuScreenKind)EditorGUILayout.EnumPopup("Art", node.kind);

            if (node.kind == MenuScreenKind.Generic || node.kind == MenuScreenKind.Pause)
                node.bodyText = EditorGUILayout.TextField("Text (optional)", node.bodyText);

            if (node.kind == MenuScreenKind.Options)
                EditorGUILayout.HelpBox("Options-Screens bekommen automatisch Lautstärke-Regler " +
                                        "+ Vollbild-Schalter beim Generieren.", MessageType.None);

            if (node.kind == MenuScreenKind.CharacterCreator)
                EditorGUILayout.HelpBox("Character-Creator-Screens bekommen automatisch die Charakter-" +
                                        "Vorschau + 6 Pfeil-Knöpfe (Kopf/Körper/Füße) beim Generieren. " +
                                        "Einen Button zum Spiel-Screen selbst hinzufügen.", MessageType.None);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Titel-Darstellung", EditorStyles.boldLabel);
            node.titleScale = EditorGUILayout.Slider("Größe", node.titleScale <= 0f ? 1f : node.titleScale, 0.5f, 4f);
            node.titleRandomBrickColors = EditorGUILayout.ToggleLeft(
                "Jeder Brick eigene Zufallsfarbe (für den Spieltitel)", node.titleRandomBrickColors);
            node.titleBricksFall = EditorGUILayout.ToggleLeft(
                "Bricks lassen sich anklicken und fallen ab", node.titleBricksFall);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Buttons", EditorStyles.boldLabel);

            var screenNames = _graph.screens.Select(s => s.title).ToArray();
            MenuButtonDef toRemove = null;

            foreach (var btn in node.buttons)
            {
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    btn.label = EditorGUILayout.TextField("Label", btn.label);
                    btn.specialAction = (MenuSpecialAction)EditorGUILayout.EnumPopup("Aktion", btn.specialAction);

                    if (btn.specialAction == MenuSpecialAction.None && _graph.screens.Count > 0)
                    {
                        int currentIndex = _graph.screens.FindIndex(s => s.id == btn.targetScreenId);
                        int newIndex = EditorGUILayout.Popup("Ziel-Screen", Mathf.Max(currentIndex, 0), screenNames);
                        btn.targetScreenId = _graph.screens[newIndex].id;
                    }

                    if (GUILayout.Button("Button entfernen"))
                        toRemove = btn;
                }
            }

            if (toRemove != null)
            {
                node.buttons.Remove(toRemove);
                EditorUtility.SetDirty(_graph);
                GUIUtility.ExitGUI();
            }

            if (GUILayout.Button("+ Button hinzufügen"))
            {
                node.buttons.Add(new MenuButtonDef());
                EditorUtility.SetDirty(_graph);
            }

            EditorGUILayout.Space();

            if (node.kind != MenuScreenKind.Game)
            {
                bool isStart = _graph.startScreenId == node.id;
                using (new EditorGUI.DisabledScope(isStart))
                {
                    if (GUILayout.Button(isStart ? "Ist Start-Screen" : "Als Start-Screen setzen"))
                    {
                        _graph.startScreenId = node.id;
                        EditorUtility.SetDirty(_graph);
                    }
                }
            }

            EditorGUILayout.Space();
            var oldColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Screen löschen"))
            {
                _graph.screens.Remove(node);
                _selectedScreenId = null;
                EditorUtility.SetDirty(_graph);
                GUI.backgroundColor = oldColor;
                GUIUtility.ExitGUI();
            }
            GUI.backgroundColor = oldColor;

            if (GUI.changed) EditorUtility.SetDirty(_graph);
        }
    }
}
