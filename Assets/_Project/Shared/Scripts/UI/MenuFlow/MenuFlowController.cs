using System;
using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.Shared.UI.MenuFlow
{
    // Lives in the minigame scene. Shows/hides the generated screen panels according to a
    // MenuFlowGraph and handles the behaviour every screen shares: navigating between panels,
    // the Game screen's special meaning (hide the menu, run the game), and Escape toggling the
    // Pause screen while the game is running.
    //
    // Project uses the new Input System exclusively (Active Input Handling = Input System
    // Package), so Escape is polled via Keyboard.current rather than the legacy Input class.
    public class MenuFlowController : MonoBehaviour
    {
        [SerializeField] private MenuFlowGraph graph;
        [SerializeField] private GameObject gameplayRoot;

        // Renders the brick-text button signs (see MenuFlowGenerator) — separate from whatever
        // camera the game itself uses, since menu buttons need to be reliably visible/clickable
        // regardless of where the gameplay camera happens to be looking (especially at Pause,
        // triggered mid-action). Swapped in/out against the gameplay camera in lockstep with
        // showing/hiding a screen.
        [SerializeField] private Camera menuCamera;

        [SerializeField] private string pauseSfxId;
        [SerializeField] private string resumeSfxId;

        [Header("Music")]
        // Ids from the AudioLibrary's Music list. Playback runs on the AudioManager's dedicated music
        // channel, so the Options music slider already controls it. Leave an id empty to not switch
        // music for that state. Pausing deliberately does NOT change track - the pause panel is a
        // panel swap, and cutting the music for it would be jarring.
        [Tooltip("Music id played on every menu screen (main menu, options, ...).")]
        [SerializeField] private string menuMusicId;
        [Tooltip("Music id played while the game itself is running.")]
        [SerializeField] private string gameMusicId;

        // Filled in by the Menu Flow Editor's Generate step: screenId -> its panel/sign-group
        // GameObject. Must be a serialized list, not a plain Dictionary — Generate() calls
        // RegisterPanel/RegisterBrickSigns on the live component at EDITOR time, and a
        // non-[SerializeField] Dictionary never survives into the saved scene or a Play-mode
        // domain reload, so at runtime it would just be empty and every screen (main menu,
        // options, pause, ...) would stay in whatever active state Generate() last left it in
        // — i.e. all visible/overlapping at once instead of only the current screen.
        [Serializable]
        private struct ScreenBinding
        {
            public string screenId;
            public GameObject panel;
            public GameObject brickSigns;
        }

        [SerializeField] private List<ScreenBinding> bindings = new();

        private Camera _gameplayCamera;
        private string _pauseScreenId;
        private bool   _isPaused;
        private bool   _gameActive;

        // Survives the scene reload used to restart a level (statics persist across LoadScene within
        // a play session). Lets "Restart" drop straight back into a fresh game, while "Main Menu"
        // reloads and stops at the menu.
        private static bool _enterGameAfterReload;

        public void RegisterPanel(string screenId, GameObject panel) =>
            SetBinding(screenId, panel: panel);
        public void RegisterBrickSigns(string screenId, GameObject signRoot) =>
            SetBinding(screenId, brickSigns: signRoot);

        private void SetBinding(string screenId, GameObject panel = null, GameObject brickSigns = null)
        {
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].screenId != screenId) continue;
                var b = bindings[i];
                if (panel != null) b.panel = panel;
                if (brickSigns != null) b.brickSigns = brickSigns;
                bindings[i] = b;
                return;
            }
            bindings.Add(new ScreenBinding { screenId = screenId, panel = panel, brickSigns = brickSigns });
        }

        private GameObject FindPanel(string screenId)
        {
            foreach (var b in bindings) if (b.screenId == screenId) return b.panel;
            return null;
        }

        private GameObject FindBrickSigns(string screenId)
        {
            foreach (var b in bindings) if (b.screenId == screenId) return b.brickSigns;
            return null;
        }

        private void HideAllScreens()
        {
            foreach (var b in bindings)
            {
                if (b.panel != null) b.panel.SetActive(false);
                if (b.brickSigns != null) b.brickSigns.SetActive(false);
            }
        }

        private void Start()
        {
            HideAllScreens();

            _pauseScreenId = FindFirst(MenuScreenKind.Pause)?.id;
            _gameActive = false;
            _isPaused = false;

            if (gameplayRoot != null) gameplayRoot.SetActive(false);

            // Came back from a "Restart" scene reload - skip the menu and start playing immediately.
            if (_enterGameAfterReload)
            {
                _enterGameAfterReload = false;
                EnterGame();
                return;
            }

            if (graph != null && !string.IsNullOrEmpty(graph.startScreenId))
                ShowScreen(graph.startScreenId);
        }

        public void ShowScreen(string screenId)
        {
            var node = graph != null ? graph.FindScreen(screenId) : null;
            if (node == null) return;

            if (node.kind == MenuScreenKind.Game)
            {
                EnterGame();
                return;
            }

            // Leaving a running game back to the start screen ends the run: reload the scene so the
            // next Play begins a fresh level instead of resuming the half-played one. Panels alone
            // can't do this - the gameplay root keeps its full state (player position, attached
            // bricks, collected pickups). Other screens (Pause, Options) are just panel swaps and
            // deliberately keep the session alive.
            if (_gameActive && graph != null && screenId == graph.startScreenId)
            {
                ReloadScene(enterGameAfterwards: false);
                return;
            }

            HideAllScreens();

            var target = FindPanel(screenId);
            if (target != null) target.SetActive(true);
            var signGroup = FindBrickSigns(screenId);
            if (signGroup != null) signGroup.SetActive(true);

            SetMenuCameraActive(true);

            // Not restarted if it's already the current track, so moving between menu screens
            // (main menu -> options -> back) keeps one continuous piece playing.
            SfxPlayer.PlayMusic(menuMusicId);
        }

        // Bound as a persistent BrickTextButton.OnClicked listener by MenuFlowGenerator, with
        // the enum name baked in as the argument (OnClicked has no parameters of its own, but
        // UnityEventTools.AddStringPersistentListener lets a persistent call carry one fixed
        // string argument — see MenuFlowGenerator.WireBrickButton).
        public void TriggerSpecialAction(string actionName)
        {
            if (!Enum.TryParse(actionName, out MenuSpecialAction action)) return;

            switch (action)
            {
                case MenuSpecialAction.QuitApp:
                    Application.Quit();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#endif
                    break;

                case MenuSpecialAction.ResumeGame:
                    Resume();
                    break;

                case MenuSpecialAction.RestartGame:
                    // Re-entering without a reload would just un-pause the same half-played level,
                    // so reload and jump straight back into a fresh one.
                    ReloadScene(enterGameAfterwards: true);
                    break;
            }
        }

        // Restarts the level by reloading the active scene - the only reliable way to reset all
        // gameplay state (player, attached bricks, collected pickups, timers) without every minigame
        // having to implement its own teardown.
        private void ReloadScene(bool enterGameAfterwards)
        {
            _isPaused = false;
            _gameActive = false;
            Time.timeScale = 1f;

            Scene active = SceneManager.GetActiveScene();
            if (active.buildIndex < 0)
            {
                Debug.LogWarning($"[MenuFlowController] Scene '{active.name}' is not in the Build Settings, " +
                                 "so the level can't be reloaded to restart it. Add it under File > Build Settings.", this);
                return;
            }

            _enterGameAfterReload = enterGameAfterwards;
            SceneManager.LoadScene(active.buildIndex);
        }

        private void EnterGame()
        {
            HideAllScreens();

            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(true);
                if (_gameplayCamera == null) _gameplayCamera = gameplayRoot.GetComponentInChildren<Camera>(true);
            }

            SetMenuCameraActive(false);
            _gameActive = true;
            _isPaused = false;
            Time.timeScale = 1f;

            SfxPlayer.PlayMusic(gameMusicId);
        }

        private void Resume()
        {
            if (_pauseScreenId != null)
            {
                var pausePanel = FindPanel(_pauseScreenId);
                if (pausePanel != null) pausePanel.SetActive(false);
                var pauseSigns = FindBrickSigns(_pauseScreenId);
                if (pauseSigns != null) pauseSigns.SetActive(false);
            }

            SetMenuCameraActive(false);
            _isPaused = false;
            Time.timeScale = 1f;
            SfxPlayer.PlayUi(resumeSfxId);
        }

        // Swaps rendering between the menu-button stage and the game's own camera — Screen
        // Space Overlay UI (panel titles, Options sliders) renders independently of either and
        // is unaffected. Safe to call before the gameplay camera has ever been found (e.g. on
        // the very first pre-game menu, where gameplayRoot — and the camera inside it — is
        // still inactive anyway).
        private void SetMenuCameraActive(bool active)
        {
            if (menuCamera != null) menuCamera.gameObject.SetActive(active);
            if (_gameplayCamera != null) _gameplayCamera.enabled = !active;
        }

        private void Update()
        {
            if (!_gameActive || _pauseScreenId == null) return;
            if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

            if (_isPaused)
            {
                Resume();
                return;
            }

            _isPaused = true;
            Time.timeScale = 0f;
            var pausePanel = FindPanel(_pauseScreenId);
            if (pausePanel != null) pausePanel.SetActive(true);
            var pauseSigns = FindBrickSigns(_pauseScreenId);
            if (pauseSigns != null) pauseSigns.SetActive(true);
            SetMenuCameraActive(true);
            SfxPlayer.PlayUi(pauseSfxId);
        }

        private MenuScreenNode FindFirst(MenuScreenKind kind) => graph?.screens.Find(s => s.kind == kind);
    }
}
