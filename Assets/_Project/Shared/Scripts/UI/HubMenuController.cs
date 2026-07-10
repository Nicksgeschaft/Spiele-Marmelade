using GameJamUniverse.Core.Managers;
using GameJamUniverse.Shared.World;
using GameJamUniverse.World;
using UnityEngine;

namespace GameJamUniverse.Shared.UI
{
    // Runtime controller for the Hub's brick-built main menu: a 3D world of clickable
    // BrickTextButton signs instead of a uGUI Canvas (menus/buttons in this project are built
    // with the Brick Text Generator). Wire each static sign's BrickTextButton.OnClicked
    // (Play/Settings/Quit) to the matching method here in the Inspector. The game-select list is
    // rebuilt fresh every time it's shown, from whatever is currently in MinigameRegistry — no
    // manual upkeep needed when new games are added.
    public class HubMenuController : MonoBehaviour
    {
        [Header("Static signs (built with the Brick Text Generator)")]
        [SerializeField] private GameObject mainMenuGroup; // parent containing Title/Play/Settings/Quit signs

        [Header("Game Select")]
        [SerializeField] private Transform  gameSelectContainer;
        [SerializeField] private GameObject brickPrefab; // Assign Shared/Prefabs/Bricks/Brick.prefab
        [SerializeField] private Material   gameSelectLetterMaterial;
        [SerializeField] private Material   gameSelectBackgroundMaterial;
        [SerializeField] private float      gameSelectRowSpacing = 0.02f;

        [Header("Options")]
        [SerializeField] private GameObject optionsPanel; // any panel with an OptionsPanelController on it

        private void Start() => ShowMainMenu();

        public void ShowMainMenu()
        {
            SetActive(mainMenuGroup, true);
            SetActive(gameSelectContainer != null ? gameSelectContainer.gameObject : null, false);
            SetActive(optionsPanel, false);
        }

        public void ShowGameSelect()
        {
            SetActive(mainMenuGroup, false);
            SetActive(optionsPanel, false);
            SetActive(gameSelectContainer != null ? gameSelectContainer.gameObject : null, true);
            PopulateGameSelect();
        }

        public void ShowOptions()
        {
            SetActive(mainMenuGroup, false);
            SetActive(gameSelectContainer != null ? gameSelectContainer.gameObject : null, false);
            SetActive(optionsPanel, true);
        }

        public void QuitApp()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void PopulateGameSelect()
        {
            if (gameSelectContainer == null || brickPrefab == null) return;

            for (int i = gameSelectContainer.childCount - 1; i >= 0; i--)
                Destroy(gameSelectContainer.GetChild(i).gameObject);

            var registry = GameManager.Instance != null ? GameManager.Instance.MinigameRegistry : null;
            if (registry == null) return;

            float y = 0f;
            foreach (var metadata in registry.Games)
            {
                if (metadata == null) continue;

                var result = BrickTextBuilder.Build(brickPrefab, metadata.displayName,
                    gameSelectLetterMaterial, gameSelectBackgroundMaterial, $"Game_{metadata.gameId}");
                BrickTextBuilder.MakeClickable(result);

                result.Root.transform.SetParent(gameSelectContainer, false);
                result.Root.transform.localPosition = new Vector3(0f, -y, 0f);
                y += result.Height + gameSelectRowSpacing;

                var button = result.Root.GetComponent<BrickTextButton>();
                button.OnClicked.AddListener(() => GameManager.Instance.RequestStartGame(metadata));
            }
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }
    }
}
