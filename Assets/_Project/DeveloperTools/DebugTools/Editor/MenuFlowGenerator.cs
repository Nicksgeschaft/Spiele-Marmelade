using SpieleMarmelade.Shared.UI;
using SpieleMarmelade.Shared.UI.MenuFlow;
using SpieleMarmelade.Shared.World;
using SpieleMarmelade.World;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpieleMarmelade.DevTools.Editor
{
    // Turns a MenuFlowGraph into an actual Canvas (titles/body text/Options sliders) + a
    // separate 3D brick-text button stage (see MenuStageRoot/MenuCamera) in the currently open
    // scene, and wires a MenuFlowController to drive navigation between them at runtime. Safe
    // to re-run: it removes any previously generated "MenuCanvas"/"MenuStageRoot"/"MenuCamera"/
    // "MenuFlowController" first.
    public static class MenuFlowGenerator
    {
        private const string GameplayRootName = "GameplayRoot";
        private const string BrickPrefabPath = "Assets/_Project/Shared/Prefabs/Bricks/Brick.prefab";
        private const float StageHeight = 500f; // far above any gameplay geometry — dedicated "menu stage"
        private const float ButtonSpacing = 0.6f;
        // The camera's starting orthographicSize (16:9 baseline — MenuStageResizer recalculates
        // this at runtime for the actual aspect ratio, see MenuStageResizer.UnitsPerReferencePixel
        // — must match what that constant produces at 16:9, currently 3.6). Also doubles as the
        // fixed reference point for converting a stage-space Y into a Canvas Scaler reference-
        // pixel Y (see StageYToCanvasY) — the two must share this constant or the Options
        // screen's title (positioned via StageAlignedElement) will drift at generate time.
        // Sized to comfortably fit the Options screen's title + 4 rows + buttons at
        // OptionsRowScale — the tallest content any screen sharing this one camera needs to fit.
        private const float BaselineOrthographicSize = 3.6f;

        // Replace the Options screen's Master/Music/SFX text labels with these brick-built icons
        // (Icon Painter), in this fixed top-to-bottom order. Fullscreen has its own two-icon
        // toggle instead (see FullscreenIconPath/MinimizeIconPath, IconToggleButton).
        private static readonly string[] OptionsSliderIconPaths =
        {
            "Assets/_Project/Shared/Prefabs/UI/IconRoot_SoundIcon.prefab",
            "Assets/_Project/Shared/Prefabs/UI/IconRoot_MusicIcon.prefab",
            "Assets/_Project/Shared/Prefabs/UI/IconRoot_SFXIcon.prefab",
        };

        // The Fullscreen row is a single clickable icon that swaps between these two, instead of
        // an icon next to a separate uGUI Toggle — shows FullscreenIconPath (click → go
        // fullscreen) while windowed, swaps to MinimizeIconPath (click → restore window) once
        // fullscreen. Removes the whole "keep a 3D icon aligned with a 2D checkbox" problem.
        private const string FullscreenIconPath = "Assets/_Project/Shared/Prefabs/UI/IconRoot_Fullscreen.prefab";
        private const string MinimizeIconPath = "Assets/_Project/Shared/Prefabs/UI/IconRoot_Minimize.prefab";

        private static readonly Color PanelBg  = new(0.09f, 0.09f, 0.14f, 0.96f);
        private static readonly Color BodyCol  = new(0.75f, 0.75f, 0.85f);

        public static void Generate(MenuFlowGraph graph)
        {
            if (graph == null) return;

            // GameObject.Find only searches ACTIVE objects — MenuCamera gets deactivated right
            // after being built (see below), so Find silently stops seeing it after the first
            // Generate() run, leaving an orphaned inactive copy every time. Walking the scene's
            // root objects directly finds inactive ones too. MenuFlowController previously wasn't
            // in this list at all, so every re-run left a stale extra controller behind.
            var namesToClean = new[] { "MenuCanvas", "MenuStageRoot", "MenuCamera", "MenuFlowController" };
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (System.Array.IndexOf(namesToClean, root.name) >= 0)
                    Object.DestroyImmediate(root);

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
            menuCam.orthographicSize = BaselineOrthographicSize;
            menuCam.clearFlags = CameraClearFlags.SolidColor;
            menuCam.backgroundColor = PanelBg;
            menuCam.nearClipPlane = 0.3f;
            menuCam.farClipPlane = 20f;

            // Keeps orthographicSize correct for whatever the actual screen aspect ratio turns
            // out to be at runtime — the fixed 3f above is just the 16:9 starting point.
            var resizer = menuCamGo.AddComponent<MenuStageResizer>();
            var resizerSo = new SerializedObject(resizer);
            resizerSo.FindProperty("stageCamera").objectReferenceValue = menuCam;
            resizerSo.ApplyModifiedPropertiesWithoutUndo();

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

                // Title/button stage-Y positions only depend on the button count, so they're
                // computed once here and shared by BuildBrickButtonGroup (title/buttons/Options
                // widgets) — computing them twice risked the two drifting apart, which was the
                // root of the original overlap bug.
                float topButtonY = node.buttons.Count > 0 ? (node.buttons.Count - 1) * ButtonSpacing * 0.5f : 0f;
                // The Options rows stack upward from the button row, so with the buttons centred on
                // stage-Y 0 the whole screen ends up crowded into the top half with dead space below.
                // Drop the anchor to re-centre the block; the title stays pinned to the top edge and
                // is nudged separately via OptionsTitleTopMargin.
                if (node.kind == MenuScreenKind.Options) topButtonY += OptionsContentOffsetY;
                // The Creator's middle is taken up by the character preview and its arrows, so its
                // buttons are pinned near the bottom edge instead of centred like every other screen.
                else if (node.kind == MenuScreenKind.CharacterCreator)
                    topButtonY = CreatorButtonY + (node.buttons.Count - 1) * ButtonSpacing * 0.5f;
                // Only meaningful for non-Options screens — the Options title is pinned near the
                // top of the screen independently (see BuildBrickButtonGroup), since it needs to
                // clear 4 extra rows below it rather than just sitting above the button stack.
                float titleY = topButtonY + ButtonSpacing * (node.buttons.Count > 0 ? 1.8f : 0.5f);

                var panel = BuildPanel(canvasGo.transform, node, out var optionsController);
                controller.RegisterPanel(node.id, panel);

                var signGroup = BuildBrickButtonGroup(stageRoot.transform, node, graph, brickPrefab, menuCam,
                    controller, titleY, topButtonY, optionsController);
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

        private static GameObject BuildPanel(Transform parent, MenuScreenNode node,
            out OptionsPanelController optionsController)
        {
            optionsController = null;

            var panelGo = new GameObject($"Panel_{node.title}", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            StretchFull(panelGo.GetComponent<RectTransform>());
            // Fully transparent: MenuCamera's SolidColor clear provides the backdrop now, so
            // the brick-text buttons it renders (behind this Overlay Canvas in compositing
            // terms) aren't hidden behind an opaque panel image.
            panelGo.GetComponent<Image>().color = new Color(PanelBg.r, PanelBg.g, PanelBg.b, 0f);

            // Title is brick-text now (built in BuildBrickButtonGroup, above the button stack on
            // the MenuStageRoot) — no uGUI Text title here anymore, keeping normal UI text out of
            // the menu entirely except body paragraphs (impractical as brick-text). Master/Music/
            // SFX/Fullscreen have no uGUI widget at all anymore — icons, sliders and the
            // Fullscreen toggle are all brick-built on the MenuStageRoot (see BuildOptionsWidgets)
            // — this panel exists only for optional body text and to host OptionsPanelController.
            float nextY = -220f;
            if (!string.IsNullOrEmpty(node.bodyText))
            {
                BuildText(panelGo.transform, "Body", node.bodyText, 24, BodyCol, new Vector2(0, nextY), new Vector2(900, 160));
                nextY -= 200f;
            }

            if (node.kind == MenuScreenKind.Options)
                optionsController = panelGo.AddComponent<OptionsPanelController>();

            return panelGo;
        }

        // Builds the screen's title AND one brick-text sign per button (see BrickTextBuilder),
        // stacked vertically and centered on the MenuStageRoot's local origin — title on top,
        // buttons below. For the Options screen, also places Master/Music/SFX/Fullscreen rows
        // between the title and the buttons (pure stage-space, see BuildOptionsWidgets). Wires
        // each button sign's click straight to the same ShowScreen/TriggerSpecialAction targets
        // the old uGUI buttons used.
        private static GameObject BuildBrickButtonGroup(Transform stageParent, MenuScreenNode node,
            MenuFlowGraph graph, GameObject brickPrefab, Camera menuCam, MenuFlowController controller,
            float titleY, float topButtonY, OptionsPanelController optionsController)
        {
            var groupGo = new GameObject($"Signs_{node.title}");
            groupGo.transform.SetParent(stageParent, false);

            if (brickPrefab == null) return groupGo;

            Material[] letterMats = ResolveLetterMaterials(graph);
            Material   bgMat      = ResolveBackgroundMaterial(graph);

            // A big logo reads best with every brick its own colour; normal screen titles stay one
            // colour per letter so they're still legible.
            var titleColorMode = node.titleRandomBrickColors
                ? BrickTextBuilder.ColorMode.RandomPerBrick
                : BrickTextBuilder.ColorMode.RandomPerLetter;

            var titleResult = BrickTextBuilder.Build(brickPrefab, node.title, letterMats, bgMat, "Title",
                graph.buttonHasBackground, titleColorMode);
            titleResult.Root.transform.SetParent(groupGo.transform, false);

            // Guarded rather than used raw: graph assets written before titleScale existed deserialize
            // without it, and a 0 there would collapse the title to nothing.
            float titleScale = node.titleScale <= 0f ? 1f : node.titleScale;
            titleResult.Root.transform.localScale = Vector3.one * titleScale;
            float titleWidth  = titleResult.Width * titleScale;
            float titleHeight = titleResult.Height * titleScale;

            if (node.titleBricksFall)
            {
                var faller = titleResult.Root.AddComponent<FallingTitleBricks>();
                var fallerSo = new SerializedObject(faller);
                fallerSo.FindProperty("raycastCamera").objectReferenceValue = menuCam;
                fallerSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Both of these fill their middle with content, so their title hangs from the top edge
            // instead of sitting just above a centred button stack.
            bool pinTitleToTop = node.kind == MenuScreenKind.Options || node.kind == MenuScreenKind.CharacterCreator;

            if (pinTitleToTop)
            {
                // The Options screen has enough extra content (4 rows) that centering the title
                // just above them (like every other screen) reads as cramped — instead pin it a
                // fixed distance below the actual top edge of the screen via StageAlignedElement,
                // so it "hangs" near the top with breathing room at any aspect ratio instead of
                // sitting right on top of the rows.
                //
                // That fixed target is only safe as long as the 4 rows stay shorter than it —
                // tuning OptionsIconScale/OptionsRowMargin up later (as happened here) can grow
                // the rows enough that the top-pinned title would end up BELOW row 0 instead of
                // above it. Taking the max against the rows' own minimum-clearance requirement
                // means the title always sits above the rows no matter how those get tuned, only
                // "wasting" some of its top margin on tall content instead of overlapping it.
                float titleTopPinnedY = BaselineOrthographicSize - OptionsTitleTopMargin;
                // Only the Options screen stacks rows under its title; the Creator's preview sits
                // well below, so it just takes the top-pinned position as-is.
                float titleCenterY = node.kind == MenuScreenKind.Options
                    ? Mathf.Max(titleTopPinnedY, OptionsRowStageY(topButtonY, 0, OptionsRowNames.Length) + OptionsEdgeGap)
                    : titleTopPinnedY;

                float titleAnchoredY = StageYToCanvasY(titleCenterY - titleHeight * 0.5f);
                float titleAnchoredX = -titleWidth * 0.5f / MenuStageResizer.UnitsPerReferencePixel;
                titleResult.Root.AddComponent<StageAlignedElement>().SetAnchored(titleAnchoredX, titleAnchoredY, menuCam);
            }
            else
            {
                titleResult.Root.transform.localPosition =
                    new Vector3(-titleWidth * 0.5f, titleY - titleHeight * 0.5f, 0f);
            }

            if (node.kind == MenuScreenKind.Options)
                BuildOptionsWidgets(groupGo.transform, topButtonY, menuCam, brickPrefab, graph, optionsController);
            else if (node.kind == MenuScreenKind.CharacterCreator)
                BuildCharacterCreator(groupGo, menuCam);

            if (node.buttons.Count == 0) return groupGo;

            float y = topButtonY;
            foreach (var btn in node.buttons)
            {
                var result = BrickTextBuilder.Build(brickPrefab, btn.label, letterMats, bgMat, $"Btn_{btn.label}",
                    graph.buttonHasBackground, BrickTextBuilder.ColorMode.RandomPerLetter);
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

        // Converts a fixed MenuStageRoot-local Y into a Canvas Scaler reference-pixel Y (measured
        // downward from the top, same convention as AnchorTopCenter elsewhere in this file) —
        // the exact inverse of StageAlignedElement.Reposition at BaselineOrthographicSize. Used
        // once, at generate time, to place the Options title via StageAlignedElement so it hangs
        // a fixed distance below the screen's actual top edge at any aspect ratio.
        private static float StageYToCanvasY(float stageY) =>
            (stageY - BaselineOrthographicSize) / MenuStageResizer.UnitsPerReferencePixel;

        // Icon Painter icons are hand-painted on a fixed square grid (see IconPainterWindow) —
        // the Options icons all happen to be 15x15 bricks. Half-heights below are computed from
        // real brick/font dimensions (not guessed pixel offsets) so row spacing always has enough
        // clearance to avoid overlap, however OptionsIconScale/GridSize are tuned later.
        private const int OptionsIconGridSize = 15;
        private const float OptionsIconScale = 0.2f;
        // Extra multiplier applied to each whole row (icon+slider together, or the Fullscreen
        // icon alone) on top of their own individual scales above — makes every row bigger as a
        // unit instead of tuning icon/slider scale separately.
        private const float OptionsRowScale = 1.5f;
        private const float OptionsRowMargin = 0.15f; // breathing room between adjacent row/title/button bounds
        // How far below the screen's actual top edge the Options title hangs (world units at
        // BaselineOrthographicSize — converted to a Canvas Y via StageYToCanvasY so it tracks
        // correctly via StageAlignedElement at any aspect ratio). Only takes effect when there's
        // room for it — see the Mathf.Max against the rows' own minimum clearance requirement in
        // BuildBrickButtonGroup.
        private const float OptionsTitleTopMargin = 0.75f;

        // Shifts the whole Options block (rows + buttons) down the stage. Negative = lower. Tune this
        // single value to re-balance the screen vertically rather than nudging rows and buttons apart.
        private const float OptionsContentOffsetY = -0.75f;

        private static float BrickRowStep => BrickShapeInfo.HeightInPlates(BrickType.Brick) * WorldConstants.PlateHeight;
        private static float BrickTextHalfHeight => BrickFont.GlyphHeight * BrickRowStep * 0.5f; // title/button half-height
        // Includes OptionsRowScale so every clearance/spacing formula below anticipates the
        // rows' real (row-scaled) size instead of just their own individual icon scale.
        private static float OptionsIconHalfHeight => OptionsIconGridSize * BrickRowStep * OptionsIconScale * OptionsRowScale * 0.5f;

        // Vertical distance between two adjacent Options rows (Master/Music/SFX/Fullscreen).
        private static float OptionsRowSpacing => OptionsIconHalfHeight * 2f + OptionsRowMargin;
        // Vertical distance from the bottom row to the button stack — bigger than
        // OptionsRowSpacing since buttons are taller than an icon.
        private static float OptionsEdgeGap => BrickTextHalfHeight + OptionsIconHalfHeight + OptionsRowMargin;

        // Stage-local Y of Options row `rowIndex` (0 = Master, at the top; rowCount-1 =
        // Fullscreen, closest to the buttons) — built outward from the button stack using real
        // measured half-heights, so it can never overlap the buttons or each other at any aspect
        // ratio. The title is positioned independently (see BuildBrickButtonGroup), pinned near
        // the top of the screen rather than chained off this stack, so it always has headroom.
        private static float OptionsRowStageY(float topButtonY, int rowIndex, int rowCount)
        {
            float bottomRowY = topButtonY + OptionsEdgeGap;
            return bottomRowY + (rowCount - 1 - rowIndex) * OptionsRowSpacing;
        }

        // Places the Options rows (Master/Music/SFX icon+slider, plus Fullscreen's icon-toggle)
        // on the MenuStageRoot. Each row lives under its own row parent, and all 4 rows live
        // under one outer parent — see BrickStackLayout, the 3D-world equivalent of a uGUI
        // Horizontal/VerticalLayoutGroup (real Unity layout groups only arrange RectTransforms,
        // so they can't lay out these brick objects). The inner, per-row stack keeps each slider
        // vertically centered on its icon automatically instead of by hand-picked offsets; the
        // outer stack spaces the rows using their real measured heights. Fullscreen used to need
        // a separate StageAlignedElement/uGUI-Toggle alignment trick here — now that it's just a
        // clickable icon (IconToggleButton) with no 2D counterpart, it's a perfectly normal row.
        private const int SliderSegmentCount = 20;
        private const float OptionsSliderScale = 1.4f; // scales the whole track+handle up — a single unscaled brick row reads as a thin line
        private const float OptionsIconStageX = -2.3f; // shared left edge for every icon's row
        private const float OptionsIconSliderGap = 0.3f; // gap between the icon's right edge and the slider's left edge
        // Fullscreen sits noticeably closer under SFX than the uniform gap between the other
        // rows (see BrickStackLayoutSpacing) — its own row has no slider, so the usual row-to-row
        // margin reads as an oversized empty gap above just an icon.
        private const float OptionsFullscreenGap = 0.05f;

        private static readonly string[] OptionsRowNames = { "Master", "Music", "SFX", "Fullscreen" };

        private static void BuildOptionsWidgets(Transform groupTransform, float topButtonY,
            Camera menuCam, GameObject brickPrefab, MenuFlowGraph graph, OptionsPanelController optionsController)
        {
            Material filledMat   = CreateColorMaterial(graph.sliderFilledColor);
            Material unfilledMat = CreateColorMaterial(graph.sliderUnfilledColor);
            Material handleMat   = CreateColorMaterial(graph.sliderHandleColor);

            BrickSliderTrack masterSlider = null, musicSlider = null, sfxSlider = null;
            IconToggleButton fullscreenButton = null;
            int rowCount = OptionsRowNames.Length;

            var rowsGo = new GameObject("OptionsRows");
            rowsGo.transform.SetParent(groupTransform, false);
            // Anchored a safe distance above the button stack (same measured half-height math
            // the rows are sized from); the outer stack below then spaces the rows precisely.
            rowsGo.transform.localPosition =
                new Vector3(0f, OptionsRowStageY(topButtonY, 0, rowCount) + OptionsIconHalfHeight, 0f);
            var rowsStack = rowsGo.AddComponent<BrickStackLayout>();
            // Rows keep their own shared X (set below) — the outer stack should only ever move
            // them vertically, not re-center them horizontally against each other.
            rowsStack.Configure(BrickStackLayout.Axis.Vertical, OptionsRowMargin, centerCrossAxis: false);

            for (int i = 0; i < rowCount; i++)
            {
                bool isFullscreenRow = i == rowCount - 1;

                var rowGo = new GameObject($"Row_{OptionsRowNames[i]}");
                rowGo.transform.SetParent(rowsGo.transform, false);
                rowGo.transform.localPosition = new Vector3(OptionsIconStageX, 0f, 0f);
                rowGo.transform.localScale = Vector3.one * OptionsRowScale;

                if (isFullscreenRow)
                {
                    fullscreenButton = BuildFullscreenToggle(rowGo.transform, menuCam);
                    rowGo.AddComponent<BrickStackLayoutSpacing>().spacingBefore = OptionsFullscreenGap;
                    continue;
                }

                var rowStack = rowGo.AddComponent<BrickStackLayout>();
                rowStack.Configure(BrickStackLayout.Axis.Horizontal, OptionsIconSliderGap);

                var iconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OptionsSliderIconPaths[i]);
                if (iconPrefab == null)
                    Debug.LogWarning($"[MenuFlowGenerator] Options-Icon fehlt: {OptionsSliderIconPaths[i]}");
                else
                {
                    var icon = (GameObject)PrefabUtility.InstantiatePrefab(iconPrefab, rowGo.transform);
                    icon.transform.localScale = Vector3.one * OptionsIconScale;
                }

                if (brickPrefab != null)
                {
                    var slider = BuildBrickSlider(rowGo.transform, brickPrefab, filledMat, unfilledMat, handleMat, menuCam);
                    if (i == 0) masterSlider = slider;
                    else if (i == 1) musicSlider = slider;
                    else sfxSlider = slider;
                }

                rowStack.Arrange();
            }

            rowsStack.Arrange();

            if (optionsController == null) return;
            var so = new SerializedObject(optionsController);
            so.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
            so.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("fullscreenButton").objectReferenceValue = fullscreenButton;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Builds the Fullscreen row's clickable icon directly on `parent` (no extra wrapper
        // GameObject needed — IconToggleButton's own raycast just needs a Collider somewhere
        // among its children, same as the icon-only rows): shows FullscreenIconPath while
        // windowed (click → go fullscreen), swaps to MinimizeIconPath once fullscreen (click →
        // restore window). Replaces the old uGUI Toggle entirely.
        private static IconToggleButton BuildFullscreenToggle(Transform parent, Camera menuCam)
        {
            var fullscreenIconPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FullscreenIconPath);
            var minimizeIconPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(MinimizeIconPath);

            GameObject offIcon = null, onIcon = null;
            if (fullscreenIconPrefab == null)
                Debug.LogWarning($"[MenuFlowGenerator] Fullscreen-Icon fehlt: {FullscreenIconPath}");
            else
            {
                offIcon = (GameObject)PrefabUtility.InstantiatePrefab(fullscreenIconPrefab, parent);
                offIcon.transform.localScale = Vector3.one * OptionsIconScale;
            }

            if (minimizeIconPrefab == null)
                Debug.LogWarning($"[MenuFlowGenerator] Minimize-Icon fehlt: {MinimizeIconPath}");
            else
            {
                onIcon = (GameObject)PrefabUtility.InstantiatePrefab(minimizeIconPrefab, parent);
                onIcon.transform.localScale = Vector3.one * OptionsIconScale;
            }

            var button = parent.gameObject.AddComponent<IconToggleButton>();
            var so = new SerializedObject(button);
            so.FindProperty("raycastCamera").objectReferenceValue = menuCam;
            so.FindProperty("offIcon").objectReferenceValue = offIcon;
            so.FindProperty("onIcon").objectReferenceValue = onIcon;
            so.ApplyModifiedPropertiesWithoutUndo();

            // AddComponent() already ran Awake() with both icon fields still null (they're only
            // wired above, afterward), so its initial ApplyIcons() call was a no-op and both
            // icons were left in their prefab-default active state — both visible/overlapping
            // instead of just one. Explicitly apply the real initial state now that the
            // references exist, so the generated scene looks correct immediately.
            button.SetIsOn(false, notify: false);

            return button;
        }

        // A row of SliderSegmentCount bricks laid out flat + one dark "handle" brick on top,
        // draggable along the row (see BrickSliderTrack) — segments from the left up to the
        // handle turn light (filled), the rest stay dark (unfilled), like a classic filled
        // slider track built out of bricks instead of a 2D Image fill. Built at local (0,0,0) —
        // its parent's BrickStackLayout (see BuildOptionsWidgets) positions it alongside the icon.
        private static BrickSliderTrack BuildBrickSlider(Transform parent, GameObject brickPrefab,
            Material filledMat, Material unfilledMat, Material handleMat, Camera menuCam)
        {
            var root = new GameObject("Slider");
            root.transform.SetParent(parent, false);
            root.transform.localScale = Vector3.one * OptionsSliderScale;

            float spacing = WorldConstants.PlateWidth;
            var segments = new GameObject[SliderSegmentCount];
            for (int i = 0; i < SliderSegmentCount; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(brickPrefab, root.transform);
                go.transform.localPosition = new Vector3(i * spacing, 0f, 0f);
                segments[i] = go;

                // Track segments are purely visual (the fill color), only the handle should ever
                // be clickable — leaving the Brick prefab's default collider enabled on all 20 of
                // them was extra, unnecessary raycast-hittable geometry sitting right next to (and
                // between) sliders, which made it easy to hit a neighboring row by accident.
                var segCollider = go.GetComponent<Collider>();
                if (segCollider != null) segCollider.enabled = false;
            }

            var handleGo = (GameObject)PrefabUtility.InstantiatePrefab(brickPrefab, root.transform);
            handleGo.name = "Handle";
            // Offset back a touch so it doesn't z-fight with whichever track brick it's over.
            handleGo.transform.localPosition = new Vector3(0f, 0f, -WorldConstants.PlateDepth);
            // Its own colour, not the track's: sharing unfilledMat made the handle vanish into the
            // unfilled part of the track it sits on.
            ApplyMaterial(handleGo, handleMat);

            // The Brick prefab's default collider is sized for grid-stacking (includes the stud
            // bump on top, taller than the visible brick body) — tighten it to just the handle's
            // own footprint so a click clearly meant for this handle can't also register from
            // just outside it (or bleed into a tightly-packed neighboring row).
            var handleCollider = handleGo.GetComponent<BoxCollider>();
            if (handleCollider == null) handleCollider = handleGo.AddComponent<BoxCollider>();
            handleCollider.center = Vector3.zero;
            handleCollider.size = new Vector3(WorldConstants.PlateWidth * 0.9f, WorldConstants.PlateHeight * 2.5f, WorldConstants.PlateDepth * 0.9f);

            var track = root.AddComponent<BrickSliderTrack>();
            var so = new SerializedObject(track);
            so.FindProperty("handle").objectReferenceValue = handleGo.transform;
            so.FindProperty("axis").vector3Value = Vector3.right;
            // Plain local length — no scale factors folded in. BrickSliderTrack measures the real
            // track from the segments below and works in local space, so it handles OptionsSliderScale
            // and the parent row's OptionsRowScale by itself.
            so.FindProperty("trackLength").floatValue = (SliderSegmentCount - 1) * spacing;
            so.FindProperty("raycastCamera").objectReferenceValue = menuCam;
            so.FindProperty("filledMaterial").objectReferenceValue = filledMat;
            so.FindProperty("unfilledMaterial").objectReferenceValue = unfilledMat;
            var segProp = so.FindProperty("segments");
            segProp.arraySize = segments.Length;
            for (int i = 0; i < segments.Length; i++)
                segProp.GetArrayElementAtIndex(i).objectReferenceValue = segments[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            return track;
        }

        // ── Character Creator ────────────────────────────────────────────────

        private const string PlayerVisualPath = "Assets/_Project/Shared/Prefabs/Player/Player_Platformer.prefab";
        private const string ArrowIconPath = "Assets/_Project/Shared/Prefabs/UI/IconRoot_Arrow.prefab";

        // Stage-Y the Creator's button stack is centred on — low enough to clear the preview above it.
        private const float CreatorButtonY = -2.5f;
        // Overrides the prefab's own scale (10) rather than multiplying it: the character is authored
        // at gameplay size, and the Creator wants it filling the middle of the screen instead.
        private const float CreatorCharacterScale = 25f;
        // One row per part. At CreatorCharacterScale the character's own parts happen to sit exactly
        // this far apart, so aligning its Body with the middle row lines Head and Feet up with theirs.
        private static readonly float[] CreatorRowY = { 0.8f, 0f, -0.8f }; // Head, Body, Feet
        private const float CreatorArrowX = 1.75f;
        // Same on-screen size as the Options icons (their own scale times their row scale).
        private const float CreatorArrowScale = OptionsIconScale * OptionsRowScale;
        // Z-rotation that mirrors the arrow horizontally. If the arrows end up pointing the wrong
        // way, swap these two values - the icon prefab's own direction decides which is which.
        private const float CreatorRightArrowZ = 0f;
        private const float CreatorLeftArrowZ = 180f;

        // Builds the middle of a Character Creator screen: the player preview, plus three pairs of
        // arrows (head/body/feet) that recolour it. Everything lands under the screen's sign group, so
        // it shows and hides with the screen like any other content.
        private static void BuildCharacterCreator(GameObject groupGo, Camera menuCam)
        {
            var visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVisualPath);
            if (visualPrefab == null)
            {
                Debug.LogWarning($"[MenuFlowGenerator] Charakter-Prefab fehlt: {PlayerVisualPath} — " +
                                  "Character Creator bleibt leer.");
                return;
            }

            var preview = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, groupGo.transform);
            preview.name = "CharacterPreview";
            preview.transform.localScale = Vector3.one * CreatorCharacterScale;

            // Player_Platformer's root carries the "Player" tag. Left on, this preview becomes a second
            // object answering to it - and SideScrollCameraRig picks its target with
            // FindGameObjectWithTag, whose order is undefined. It would sometimes latch onto this copy
            // sitting on the menu stage 500 units up and hang at the level's top edge instead of
            // following the real player. Nothing here needs the tag.
            UntagRecursively(preview);
            // Aligned by the Body part, not by overall bounds: the googly eyes stick out well past the
            // bricks, so a bounds-based centre would sit noticeably off its arrow row.
            PlaceCentered(preview, groupGo.transform, new Vector3(0f, CreatorRowY[1], 0f),
                CharacterLook.FindPart(preview.transform, CharacterPart.Body));

            var customizer = groupGo.AddComponent<CharacterCustomizer>();
            var customizerSo = new SerializedObject(customizer);
            customizerSo.FindProperty("preview").objectReferenceValue = preview.transform;
            customizerSo.ApplyModifiedPropertiesWithoutUndo();

            var arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ArrowIconPath);
            if (arrowPrefab == null)
            {
                Debug.LogWarning($"[MenuFlowGenerator] Pfeil-Prefab fehlt: {ArrowIconPath} — " +
                                  "Character Creator bekommt keine Knöpfe.");
                return;
            }

            // Index matches CreatorRowY / CharacterPart order: 0 Head, 1 Body, 2 Feet.
            UnityAction[] previousActions = { customizer.PrevHead, customizer.PrevBody, customizer.PrevFeet };
            UnityAction[] nextActions     = { customizer.NextHead, customizer.NextBody, customizer.NextFeet };
            string[] rowNames = { "Head", "Body", "Feet" };

            for (int row = 0; row < CreatorRowY.Length; row++)
            {
                BuildCreatorArrow(groupGo.transform, arrowPrefab, menuCam,
                    $"Arrow_{rowNames[row]}_Prev", -CreatorArrowX, CreatorRowY[row], CreatorLeftArrowZ, previousActions[row]);
                BuildCreatorArrow(groupGo.transform, arrowPrefab, menuCam,
                    $"Arrow_{rowNames[row]}_Next", CreatorArrowX, CreatorRowY[row], CreatorRightArrowZ, nextActions[row]);
            }
        }

        private static void BuildCreatorArrow(Transform parent, GameObject arrowPrefab, Camera menuCam,
            string name, float x, float y, float rotationZ, UnityAction action)
        {
            var arrow = (GameObject)PrefabUtility.InstantiatePrefab(arrowPrefab, parent);
            arrow.name = name;
            arrow.transform.localScale = Vector3.one * CreatorArrowScale;
            arrow.transform.localRotation = Quaternion.Euler(0f, 0f, rotationZ);
            PlaceCentered(arrow, parent, new Vector3(x, y, 0f));

            var button = arrow.AddComponent<IconClickButton>();
            var so = new SerializedObject(button);
            so.FindProperty("raycastCamera").objectReferenceValue = menuCam;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddVoidPersistentListener(button.OnClicked, action);
        }

        private static void UntagRecursively(GameObject root)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.tag = "Untagged";
            }
        }

        // Positions an object so its VISUAL centre lands on localTarget. Icon Painter icons and the
        // character prefab both have their pivot at a corner of their content rather than in the
        // middle, so setting localPosition directly would leave them visibly off-centre.
        //
        // Pass `alignTo` to centre on one specific renderer instead of everything combined - useful
        // when part of the object (googly eyes) sticks out and would skew the overall bounds.
        private static void PlaceCentered(GameObject go, Transform parent, Vector3 localTarget,
            Renderer alignTo = null)
        {
            go.transform.localPosition = Vector3.zero;

            var renderers = alignTo != null ? new[] { alignTo } : go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                go.transform.localPosition = localTarget;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

            // With localPosition at zero this is exactly how far the content sits from the pivot.
            Vector3 centerOffset = parent.InverseTransformPoint(bounds.center);
            go.transform.localPosition = localTarget - centerOffset;
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (mat == null) return;
            foreach (var mr in go.GetComponentsInChildren<MeshRenderer>())
                mr.sharedMaterial = mat;
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

        // ── Graph styling (colors set on the MenuFlowGraph asset before Generate) ───────────

        // An explicit Material always wins (full control, e.g. a custom shader/texture); with
        // none set, one color per cycling entry in buttonLetterColors is baked into its own
        // instanced material (see CreateColorMaterial); with neither set, falls back to the
        // original hardcoded default so existing graphs keep looking the same after upgrading.
        private static Material[] ResolveLetterMaterials(MenuFlowGraph graph)
        {
            if (graph.buttonLetterMaterial != null) return new[] { graph.buttonLetterMaterial };

            if (graph.buttonLetterColors != null && graph.buttonLetterColors.Count > 0)
            {
                var mats = new Material[graph.buttonLetterColors.Count];
                for (int i = 0; i < mats.Length; i++) mats[i] = CreateColorMaterial(graph.buttonLetterColors[i]);
                return mats;
            }

            return new[] { FindMaterial("M_Special_GlowWhite") };
        }

        private static Material ResolveBackgroundMaterial(MenuFlowGraph graph)
        {
            if (!graph.buttonHasBackground) return null; // BrickTextBuilder skips background bricks entirely
            if (graph.buttonBackgroundMaterial != null) return graph.buttonBackgroundMaterial;
            return CreateColorMaterial(graph.buttonBackgroundColor);
        }

        // Clones a base brick material and recolors it — the same override pattern
        // BrickShatterEffect uses for its fragment colors — so custom-colored bricks always
        // match whatever shader the project's brick materials actually use, instead of requiring
        // one specific shader/property name to be hardcoded here.
        private static Material CreateColorMaterial(Color color)
        {
            var baseMat = FindMaterial("M_Brick_White");
            if (baseMat == null) return null;

            var mat = new Material(baseMat);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            return mat;
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
