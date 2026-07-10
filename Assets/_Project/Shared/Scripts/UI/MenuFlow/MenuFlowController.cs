using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameJamUniverse.Shared.UI.MenuFlow
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

        // Filled in by the Menu Flow Editor's Generate step: screenId -> its panel GameObject.
        private readonly Dictionary<string, GameObject> _panels = new();

        private string _pauseScreenId;
        private bool   _isPaused;
        private bool   _gameActive;

        public void RegisterPanel(string screenId, GameObject panel) => _panels[screenId] = panel;

        private void Start()
        {
            foreach (var panel in _panels.Values)
                if (panel != null) panel.SetActive(false);

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

            if (_panels.TryGetValue(screenId, out var target) && target != null)
                target.SetActive(true);
        }

        // Bound as a persistent Button.onClick listener by MenuFlowGenerator, with the enum
        // name baked in as the argument (Button.onClick has no parameters of its own, but
        // UnityEventTools.AddStringPersistentListener lets a persistent call carry one fixed
        // string argument — see MenuFlowGenerator.WireButton).
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

            if (gameplayRoot != null) gameplayRoot.SetActive(true);
            _gameActive = true;
            _isPaused = false;
            Time.timeScale = 1f;
        }

        private void Resume()
        {
            if (_pauseScreenId != null && _panels.TryGetValue(_pauseScreenId, out var pausePanel) && pausePanel != null)
                pausePanel.SetActive(false);

            _isPaused = false;
            Time.timeScale = 1f;
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
        }

        private MenuScreenNode FindFirst(MenuScreenKind kind) => graph?.screens.Find(s => s.kind == kind);
    }
}
