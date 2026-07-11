using System;
using System.Collections.Generic;
using SpieleMarmelade.Shared.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

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

        // Filled in by the Menu Flow Editor's Generate step: screenId -> its panel/sign-group GameObject.
        private readonly Dictionary<string, GameObject> _panels = new();
        private readonly Dictionary<string, GameObject> _brickSigns = new();

        private Camera _gameplayCamera;
        private string _pauseScreenId;
        private bool   _isPaused;
        private bool   _gameActive;

        public void RegisterPanel(string screenId, GameObject panel) => _panels[screenId] = panel;
        public void RegisterBrickSigns(string screenId, GameObject signRoot) => _brickSigns[screenId] = signRoot;

        private void Start()
        {
            foreach (var panel in _panels.Values)
                if (panel != null) panel.SetActive(false);
            foreach (var signs in _brickSigns.Values)
                if (signs != null) signs.SetActive(false);

            _pauseScreenId = FindFirst(MenuScreenKind.Pause)?.id;
            _gameActive = false;
            _isPaused = false;

            if (gameplayRoot != null) gameplayRoot.SetActive(false);

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

            foreach (var panel in _panels.Values)
                if (panel != null) panel.SetActive(false);
            foreach (var signs in _brickSigns.Values)
                if (signs != null) signs.SetActive(false);

            if (_panels.TryGetValue(screenId, out var target) && target != null)
                target.SetActive(true);
            if (_brickSigns.TryGetValue(screenId, out var signGroup) && signGroup != null)
                signGroup.SetActive(true);

            SetMenuCameraActive(true);
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
                    _isPaused = false;
                    Time.timeScale = 1f;
                    EnterGame();
                    break;
            }
        }

        private void EnterGame()
        {
            foreach (var panel in _panels.Values)
                if (panel != null) panel.SetActive(false);
            foreach (var signs in _brickSigns.Values)
                if (signs != null) signs.SetActive(false);

            if (gameplayRoot != null)
            {
                gameplayRoot.SetActive(true);
                if (_gameplayCamera == null) _gameplayCamera = gameplayRoot.GetComponentInChildren<Camera>(true);
            }

            SetMenuCameraActive(false);
            _gameActive = true;
            _isPaused = false;
            Time.timeScale = 1f;
        }

        private void Resume()
        {
            if (_pauseScreenId != null)
            {
                if (_panels.TryGetValue(_pauseScreenId, out var pausePanel) && pausePanel != null)
                    pausePanel.SetActive(false);
                if (_brickSigns.TryGetValue(_pauseScreenId, out var pauseSigns) && pauseSigns != null)
                    pauseSigns.SetActive(false);
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
            if (_panels.TryGetValue(_pauseScreenId, out var pausePanel) && pausePanel != null)
                pausePanel.SetActive(true);
            if (_brickSigns.TryGetValue(_pauseScreenId, out var pauseSigns) && pauseSigns != null)
                pauseSigns.SetActive(true);
            SetMenuCameraActive(true);
            SfxPlayer.PlayUi(pauseSfxId);
        }

        private MenuScreenNode FindFirst(MenuScreenKind kind) => graph?.screens.Find(s => s.kind == kind);
    }
}
