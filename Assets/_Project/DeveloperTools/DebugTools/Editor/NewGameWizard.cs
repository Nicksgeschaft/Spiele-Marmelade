using System.Text.RegularExpressions;
using GameJamUniverse.Shared.Cameras;
using GameJamUniverse.Shared.UI.MenuFlow;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameJamUniverse.DevTools.Editor
{
    /// <summary>
    /// Asks for a movement style + camera perspective, then builds on top of
    /// <see cref="MinigameCreatorWizard"/>'s skeleton duplication to produce a scene that's
    /// ready to press Play on: a configured Player prefab, a matching Camera rig, and
    /// (optionally) a small starter level built from the 4 bricks.
    /// </summary>
    public class NewGameWizard : EditorWindow
    {
        private enum MovementArchetype { Platformer2_5D, TopDownGrid, TopDownFree, Free3rdPerson }

        private const string PlayerPlatformerPath  = "Assets/_Project/Shared/Prefabs/Player/Player_Platformer.prefab";
        private const string PlayerTopDownGridPath = "Assets/_Project/Shared/Prefabs/Player/Player_TopDownGrid.prefab";
        private const string PlayerTopDownFreePath = "Assets/_Project/Shared/Prefabs/Player/Player_TopDownFree.prefab";
        private const string PlayerThirdPersonPath = "Assets/_Project/Shared/Prefabs/Player/Player_ThirdPerson.prefab";
        private const string CameraSideScrollPath  = "Assets/_Project/Shared/Prefabs/Camera/Camera_SideScroll.prefab";
        private const string CameraTopDownPath     = "Assets/_Project/Shared/Prefabs/Camera/Camera_TopDown.prefab";
        private const string CameraThirdPersonPath = "Assets/_Project/Shared/Prefabs/Camera/Camera_ThirdPerson.prefab";
        private const string GameplayRootName      = "GameplayRoot";

        private string _gameName = "MyNewGame";
        private string _displayName = "My New Game";
        private MovementArchetype _archetype = MovementArchetype.Platformer2_5D;
        private bool _includeStarterLevel = true;

        [MenuItem("Tools/GameJam/New Game...")]
        public static void Open() => GetWindow<NewGameWizard>("New Game");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Erstellt ein neues Minigame (wie 'Create New Minigame'), setzt zusätzlich Spieler " +
                "und Kamera passend zum gewählten Bewegungsstil ein und fügt optional ein kleines " +
                "Starter-Level aus den 4 Bricks hinzu. Danach einfach Play drücken.\n\n" +
                "Tipp: Offene Szenen vorher speichern (Strg+S) — der Wizard wechselt die Szene " +
                "und verwirft dabei ungespeicherte Änderungen.", MessageType.Info);

            EditorGUILayout.Space();
            _gameName = EditorGUILayout.TextField("Folder / Class Name", _gameName);
            _displayName = EditorGUILayout.TextField("Display Name", _displayName);

            bool validName = !string.IsNullOrWhiteSpace(_gameName) && Regex.IsMatch(_gameName, "^[A-Za-z][A-Za-z0-9_]*$");
            if (!validName)
                EditorGUILayout.HelpBox("Folder/Class Name must start with a letter and contain only letters, digits or underscores.", MessageType.Error);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bewegungsstil", EditorStyles.boldLabel);
            DrawArchetypeButton(MovementArchetype.Platformer2_5D, "2.5D Platformer (Jump'n'Run)");
            DrawArchetypeButton(MovementArchetype.TopDownGrid, "Top-Down Grid (Bomberman-Style)");
            DrawArchetypeButton(MovementArchetype.TopDownFree, "Top-Down Free (Zelda / Twin-Stick)");
            DrawArchetypeButton(MovementArchetype.Free3rdPerson, "Free 3D / Third-Person");

            EditorGUILayout.Space();
            _includeStarterLevel = EditorGUILayout.ToggleLeft("Starter-Level einfügen (aus den 4 Bricks)", _includeStarterLevel);

            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(!validName))
            {
                if (GUILayout.Button("Create Game", GUILayout.Height(32)))
                {
                    string displayName = string.IsNullOrWhiteSpace(_displayName) ? _gameName : _displayName.Trim();
                    Create(_gameName.Trim(), displayName);
                }
            }
        }

        private void DrawArchetypeButton(MovementArchetype archetype, string label)
        {
            bool selected = _archetype == archetype;
            if (GUILayout.Toggle(selected, label, EditorStyles.radioButton))
                _archetype = archetype;
        }

        private void Create(string gameName, string displayName)
        {
            string targetFolder = MinigameCreatorWizard.CreateMinigameSkeleton(gameName, displayName);
            if (targetFolder == null) return; // error already logged by CreateMinigameSkeleton

            string scenePath = $"{targetFolder}/Scenes/{gameName}.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // Player/Camera/StarterLevel all live under one GameplayRoot so the generated menu
            // can hide the whole game while showing Main Menu/Options/Credits, and reveal it
            // again when the Play button's "Game" screen is reached.
            var gameplayRoot = new GameObject(GameplayRootName);

            switch (_archetype)
            {
                case MovementArchetype.Platformer2_5D: SetUpPlatformer(scene, gameplayRoot.transform);  break;
                case MovementArchetype.TopDownGrid:    SetUpTopDownGrid(scene, gameplayRoot.transform); break;
                case MovementArchetype.TopDownFree:    SetUpTopDownFree(scene, gameplayRoot.transform); break;
                case MovementArchetype.Free3rdPerson:  SetUpThirdPerson(scene, gameplayRoot.transform); break;
            }

            // Left active in the saved scene so it's visible/editable in the Scene view —
            // MenuFlowController.Start() hides it automatically the moment Play is pressed
            // (GameObject.Find, used below by MenuFlowGenerator, wouldn't find it if inactive).
            string graphPath = $"{targetFolder}/Config/MenuFlow_{gameName}.asset";
            var menuGraph = CreateInstance<MenuFlowGraph>();
            MenuFlowEditorWindow.SeedDefaultGraph(menuGraph);
            AssetDatabase.CreateAsset(menuGraph, graphPath);
            AssetDatabase.SaveAssets();
            MenuFlowGenerator.Generate(menuGraph);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var discovered = MinigameRegistryWindow.DiscoverMinigames();
            MinigameRegistryWindow.SyncRegistry(discovered);

            Debug.Log($"[NewGameWizard] '{gameName}' erstellt und registriert. Szene ist bereit für Play.");
        }

        private void SetUpPlatformer(Scene scene, Transform gameplayRoot)
        {
            var player = SpawnPlayerAndCamera<SideScrollCameraRig>(scene, gameplayRoot, PlayerPlatformerPath, CameraSideScrollPath);
            if (player != null && _includeStarterLevel)
                StarterLevelBuilder.BuildPlatformerStarter(gameplayRoot);
        }

        private void SetUpTopDownGrid(Scene scene, Transform gameplayRoot)
        {
            var player = SpawnPlayerAndCamera<TopDownCameraRig>(scene, gameplayRoot, PlayerTopDownGridPath, CameraTopDownPath);
            if (player != null && _includeStarterLevel)
                StarterLevelBuilder.BuildTopDownGridStarter(gameplayRoot);
        }

        private void SetUpTopDownFree(Scene scene, Transform gameplayRoot)
        {
            var player = SpawnPlayerAndCamera<TopDownCameraRig>(scene, gameplayRoot, PlayerTopDownFreePath, CameraTopDownPath);
            if (player != null && _includeStarterLevel)
                StarterLevelBuilder.BuildOpenGroundStarter(gameplayRoot, "StarterLevel_TopDownFree");
        }

        private void SetUpThirdPerson(Scene scene, Transform gameplayRoot)
        {
            var player = SpawnPlayerAndCamera<ThirdPersonOrbitCameraRig>(scene, gameplayRoot, PlayerThirdPersonPath, CameraThirdPersonPath);
            if (player != null && _includeStarterLevel)
                StarterLevelBuilder.BuildOpenGroundStarter(gameplayRoot, "StarterLevel_ThirdPerson");
        }

        // Removes any camera(s) inherited from the duplicated _Template scene (so there's only
        // ever one active camera/audio listener), then instantiates the given Player/Camera
        // prefab pair under gameplayRoot and wires the camera rig to follow the new player.
        private static GameObject SpawnPlayerAndCamera<TRig>(
            Scene scene, Transform gameplayRoot, string playerPrefabPath, string cameraPrefabPath)
            where TRig : Component, ICameraRig
        {
            foreach (var existingCam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                Object.DestroyImmediate(existingCam.gameObject);

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);
            var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(cameraPrefabPath);

            if (playerPrefab == null || cameraPrefab == null)
            {
                Debug.LogError($"[NewGameWizard] Player- oder Kamera-Prefab fehlt: '{playerPrefabPath}' / '{cameraPrefabPath}'.");
                return null;
            }

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetParent(gameplayRoot, false);
            player.transform.localPosition = Vector3.zero;

            var cam = (GameObject)PrefabUtility.InstantiatePrefab(cameraPrefab, scene);
            cam.transform.SetParent(gameplayRoot, true); // keep its world offset from the player
            var rig = cam.GetComponent<TRig>();
            rig?.Init(player.transform);

            return player;
        }
    }
}
