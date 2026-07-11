using SpieleMarmelade.Shared.UI.MenuFlow;
using SpieleMarmelade.Shared.World;
using SpieleMarmelade.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace SpieleMarmelade.DevTools.Editor
{
    // Turns a MenuFlowGraph into an actual Canvas (titles/body text/Options sliders) + a
    // separate 3D brick-text button stage (see MenuStageRoot/MenuCamera) in the currently open
    // scene, and wires a MenuFlowController to drive navigation between them at runtime. Safe
    // to re-run: it removes any previously generated "MenuCanvas"/"MenuStageRoot"/"MenuCamera" first.
    public static class MenuFlowGenerator
    {
        private const string GameplayRootName = "GameplayRoot";
        private const string BrickPrefabPath = "Assets/_Project/Shared/Prefabs/Bricks/Brick.prefab";
        private const float StageHeight = 500f; // far above any gameplay geometry — dedicated "menu stage"
        private const float ButtonSpacing = 0.6f;

        private static readonly Color PanelBg  = new(0.09f, 0.09f, 0.14f, 0.96f);
        private static readonly Color AccentBg = new(0.22f, 0.47f, 0.78f);
        private static readonly Color TitleCol = Color.white;
        private static readonly Color BodyCol  = new(0.75f, 0.75f, 0.85f);
        private static readonly Color TrackBg  = new(0.20f, 0.20f, 0.28f);

        public static void Generate(MenuFlowGraph graph)
        {
            if (graph == null) return;

            foreach (var name in new[] { "MenuCanvas", "MenuStageRoot", "MenuCamera" })
            {
                var existing = GameObject.Find(name);
                if (existing != null) Object.DestroyImmediate(existing);
            }

            var canvasGo = new GameObject("MenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            EnsureEventSystem();

            var stageRoot = new GameObject("MenuStageRoot");
            stageRoot.transform.position = new Vector3(0f, StageHeight, 0f);

            var menuCamGo = new GameObject("MenuCamera", typeof(Camera));
            menuCamGo.transform.SetPositionAndRotation(new Vector3(0f, StageHeight, -5f), Quaternion.identity);
            var menuCam = menuCamGo.GetComponent<Camera>();
            menuCam.orthographic = true;
            menuCam.orthographicSize = 3f;
            menuCam.clearFlags = CameraClearFlags.SolidColor;
            menuCam.backgroundColor = PanelBg;
            menuCam.nearClipPlane = 0.3f;
            menuCam.farClipPlane = 20f;
            menuCamGo.SetActive(false);

            var brickPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BrickPrefabPath);
            if (brickPrefab == null)
                Debug.LogWarning($"[MenuFlowGenerator] Brick-Prefab fehlt: {BrickPrefabPath} — Buttons bleiben leer.");

            var controllerGo = new GameObject("MenuFlowController", typeof(MenuFlowController));
            var controller = controllerGo.GetComponent<MenuFlowController>();

            var gameplayRoot = GameObject.Find(GameplayRootName);
            if (gameplayRoot == null)
            {
                gameplayRoot = new GameObject(GameplayRootName);
                Debug.LogWarning($"[MenuFlowGenerator] Kein '{GameplayRootName}' in der Szene gefunden — " +
                                  "leeres Objekt angelegt. Gruppiere Spieler/Kamera/Level darunter, " +
                                  "damit der Game-Screen sie ein-/ausblenden kann.");
            }

            var so = new SerializedObject(controller);
            so.FindProperty("graph").objectReferenceValue = graph;
            so.FindProperty("gameplayRoot").objectReferenceValue = gameplayRoot;
            so.FindProperty("menuCamera").objectReferenceValue = menuCam;
            so.ApplyModifiedPropertiesWithoutUndo();

            foreach (var node in graph.screens)
            {
                if (node.kind == MenuScreenKind.Game) continue; // no panel — handled by ShowScreen/EnterGame

                var panel = BuildPanel(canvasGo.transform, node);
                controller.RegisterPanel(node.id, panel);

                var signGroup = BuildBrickButtonGroup(stageRoot.transform, node, graph, brickPrefab, menuCam, controller);
                controller.RegisterBrickSigns(node.id, signGroup);
            }

            EditorUtility.SetDirty(controllerGo);
            Debug.Log($"[MenuFlowGenerator] Generated {graph.screens.Count} screen(s) into 'MenuCanvas'/'MenuStageRoot'.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ── Panel / widget construction ─────────────────────────────────────

        private static GameObject BuildPanel(Transform parent, MenuScreenNode node)
        {
            var panelGo = new GameObject($"Panel_{node.title}", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            StretchFull(panelGo.GetComponent<RectTransform>());
            // Fully transparent: MenuCamera's SolidColor clear provides the backdrop now, so
            // the brick-text buttons it renders (behind this Overlay Canvas in compositing
            // terms) aren't hidden behind an opaque panel image.
            panelGo.GetComponent<Image>().color = new Color(PanelBg.r, PanelBg.g, PanelBg.b, 0f);

            BuildText(panelGo.transform, "Title", node.title, 48, TitleCol, new Vector2(0, -80), new Vector2(900, 100));

            float nextY = -220f;
            if (!string.IsNullOrEmpty(node.bodyText))
            {
                BuildText(panelGo.transform, "Body", node.bodyText, 24, BodyCol, new Vector2(0, nextY), new Vector2(900, 160));
                nextY -= 200f;
            }

            if (node.kind == MenuScreenKind.Options)
            {
                var optionsController = panelGo.AddComponent<OptionsPanelController>();
                var master = BuildSlider(panelGo.transform, "Master", nextY); nextY -= 60f;
                var music  = BuildSlider(panelGo.transform, "Music", nextY);  nextY -= 60f;
                var sfx    = BuildSlider(panelGo.transform, "SFX", nextY);    nextY -= 60f;
                var fs     = BuildToggle(panelGo.transform, "Fullscreen", nextY); nextY -= 70f;

                var optSo = new SerializedObject(optionsController);
                optSo.FindProperty("masterVolumeSlider").objectReferenceValue = master;
                optSo.FindProperty("musicVolumeSlider").objectReferenceValue = music;
                optSo.FindProperty("sfxVolumeSlider").objectReferenceValue = sfx;
                optSo.FindProperty("fullscreenToggle").objectReferenceValue = fs;
                optSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return panelGo;
        }

        // Builds one brick-text sign per button (see BrickTextBuilder), stacked vertically and
        // centered on the MenuStageRoot's local origin, wires each sign's click straight to the
        // same ShowScreen/TriggerSpecialAction targets the old uGUI buttons used.
        private static GameObject BuildBrickButtonGroup(Transform stageParent, MenuScreenNode node,
            MenuFlowGraph graph, GameObject brickPrefab, Camera menuCam, MenuFlowController controller)
        {
            var groupGo = new GameObject($"Signs_{node.title}");
            groupGo.transform.SetParent(stageParent, false);

            if (brickPrefab == null || node.buttons.Count == 0) return groupGo;

            Material letterMat = graph.buttonLetterMaterial != null ? graph.buttonLetterMaterial : FindMaterial("M_Special_GlowWhite");
            Material bgMat     = graph.buttonBackgroundMaterial != null ? graph.buttonBackgroundMaterial : FindMaterial("M_Brick_Black");

            float y = (node.buttons.Count - 1) * ButtonSpacing * 0.5f;
            foreach (var btn in node.buttons)
            {
                var result = BrickTextBuilder.Build(brickPrefab, btn.label, letterMat, bgMat, $"Btn_{btn.label}");
                BrickTextBuilder.MakeClickable(result);

                result.Root.transform.SetParent(groupGo.transform, false);
                result.Root.transform.localPosition = new Vector3(-result.Width * 0.5f, y - result.Height * 0.5f, 0f);
                y -= ButtonSpacing;

                var button = result.Root.GetComponent<BrickTextButton>();
                var btnSo = new SerializedObject(button);
                btnSo.FindProperty("raycastCamera").objectReferenceValue = menuCam;
                btnSo.ApplyModifiedPropertiesWithoutUndo();

                WireBrickButton(button, btn, controller);
            }

            return groupGo;
        }

        private static void WireBrickButton(BrickTextButton button, MenuButtonDef def, MenuFlowController controller)
        {
            if (def.specialAction != MenuSpecialAction.None)
                UnityEventTools.AddStringPersistentListener(button.OnClicked, controller.TriggerSpecialAction, def.specialAction.ToString());
            else if (!string.IsNullOrEmpty(def.targetScreenId))
                UnityEventTools.AddStringPersistentListener(button.OnClicked, controller.ShowScreen, def.targetScreenId);
        }

        private static Material FindMaterial(string name)
        {
            var guids = AssetDatabase.FindAssets($"{name} t:Material", new[] { "Assets/_Project/Shared/Materials" });
            return guids.Length == 0 ? null : AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static Slider BuildSlider(Transform parent, string label, float y)
        {
            var row = new GameObject($"Slider_{label}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            AnchorTopCenter(row.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(560, 40));

            BuildText(row.transform, "Label", label, 22, BodyCol, Vector2.zero, Vector2.zero,
                stretch: true, anchorMinOverride: new Vector2(0, 0), anchorMaxOverride: new Vector2(0.35f, 1f),
                alignment: TextAnchor.MiddleLeft);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.4f, 0.25f);
            sliderRt.anchorMax = new Vector2(1f, 0.75f);
            sliderRt.offsetMin = Vector2.zero;
            sliderRt.offsetMax = Vector2.zero;

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(sliderGo.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = TrackBg;

            var fillAreaGo = new GameObject("Fill Area", typeof(RectTransform));
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            StretchFull(fillAreaGo.GetComponent<RectTransform>());

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            StretchFull(fillGo.GetComponent<RectTransform>());
            fillGo.GetComponent<Image>().color = AccentBg;

            var handleAreaGo = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGo.transform.SetParent(sliderGo.transform, false);
            StretchFull(handleAreaGo.GetComponent<RectTransform>());

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(handleAreaGo.transform, false);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(18, 0);
            handleGo.GetComponent<Image>().color = Color.white;

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillGo.GetComponent<RectTransform>();
            slider.handleRect = handleRt;
            slider.targetGraphic = handleGo.GetComponent<Image>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            return slider;
        }

        private static Toggle BuildToggle(Transform parent, string label, float y)
        {
            var row = new GameObject($"Toggle_{label}", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            AnchorTopCenter(row.GetComponent<RectTransform>(), new Vector2(0, y), new Vector2(560, 40));

            BuildText(row.transform, "Label", label, 22, BodyCol, Vector2.zero, Vector2.zero,
                stretch: true, anchorMinOverride: new Vector2(0, 0), anchorMaxOverride: new Vector2(0.6f, 1f),
                alignment: TextAnchor.MiddleLeft);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(row.transform, false);
            var toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(0.85f, 0.5f);
            toggleRt.anchorMax = new Vector2(0.85f, 0.5f);
            toggleRt.pivot = new Vector2(0.5f, 0.5f);
            toggleRt.sizeDelta = new Vector2(36, 36);

            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(toggleGo.transform, false);
            StretchFull(bgGo.GetComponent<RectTransform>());
            bgGo.GetComponent<Image>().color = TrackBg;

            var checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(bgGo.transform, false);
            var checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = Vector2.zero;
            checkRt.anchorMax = Vector2.one;
            checkRt.offsetMin = new Vector2(6, 6);
            checkRt.offsetMax = new Vector2(-6, -6);
            checkGo.GetComponent<Image>().color = AccentBg;

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bgGo.GetComponent<Image>();
            toggle.graphic = checkGo.GetComponent<Image>();

            return toggle;
        }

        // ── Small layout helpers ─────────────────────────────────────────────

        private static void BuildText(Transform parent, string name, string text, int fontSize, Color color,
            Vector2 anchoredPos, Vector2 size, bool stretch = false,
            Vector2? anchorMinOverride = null, Vector2? anchorMaxOverride = null,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();

            if (stretch)
            {
                rt.anchorMin = anchorMinOverride ?? Vector2.zero;
                rt.anchorMax = anchorMaxOverride ?? Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                AnchorTopCenter(rt, anchoredPos, size);
            }

            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = color;
        }

        private static void AnchorTopCenter(RectTransform rt, Vector2 anchoredPos, Vector2 size)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
