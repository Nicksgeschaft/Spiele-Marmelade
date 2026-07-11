using System.Text;
using SpieleMarmelade.Shared.Audio;
using SpieleMarmelade.Shared.Combat;
using SpieleMarmelade.Shared.Items;
using SpieleMarmelade.Shared.Stats;
using UnityEngine;
using UnityEngine.UI;

namespace SpieleMarmelade.Shared.UI
{
    // Builds its own Canvas + three independently-toggleable overlay panels (Inventory/
    // Character/Map) at runtime, in the same plain-UnityEngine.UI code style as
    // MenuFlowGenerator (that one runs at editor-time; this runs in Awake so no manual scene
    // setup is needed — drop the component on the player and it works). Unlike
    // MenuFlowController's screens, these don't stop time and can be open independently of
    // each other and of Pause.
    public class PlayerHudScreensController : MonoBehaviour
    {
        private static readonly Color PanelBg = new(0.09f, 0.09f, 0.14f, 0.92f);
        private static readonly Color TitleCol = Color.white;
        private static readonly Color BodyCol = new(0.85f, 0.85f, 0.92f);

        [SerializeField] private string toggleSfxId;

        private PlayerInputReader _input;
        private Inventory _inventory;
        private Health _health;
        private CharacterStats _stats;

        private GameObject _inventoryPanel;
        private GameObject _characterPanel;
        private GameObject _mapPanel;
        private Text _inventoryBodyText;
        private Text _characterBodyText;

        private void Awake()
        {
            _input = GetComponent<PlayerInputReader>();
            _inventory = GetComponent<Inventory>();
            _health = GetComponent<Health>();
            _stats = GetComponent<CharacterStats>();

            BuildUI();
        }

        private void OnEnable()
        {
            if (_input == null) return;
            _input.ToggleInventoryPerformed += ToggleInventory;
            _input.ToggleCharacterPerformed += ToggleCharacter;
            _input.ToggleMapPerformed += ToggleMap;
            if (_inventory != null) _inventory.OnChanged += OnInventoryChanged;
        }

        private void OnDisable()
        {
            if (_input == null) return;
            _input.ToggleInventoryPerformed -= ToggleInventory;
            _input.ToggleCharacterPerformed -= ToggleCharacter;
            _input.ToggleMapPerformed -= ToggleMap;
            if (_inventory != null) _inventory.OnChanged -= OnInventoryChanged;
        }

        private void ToggleInventory()
        {
            bool nowActive = !_inventoryPanel.activeSelf;
            _inventoryPanel.SetActive(nowActive);
            if (nowActive)
            {
                RefreshInventoryPanel();
                SfxPlayer.PlayUi(toggleSfxId);
            }
        }

        private void ToggleCharacter()
        {
            bool nowActive = !_characterPanel.activeSelf;
            _characterPanel.SetActive(nowActive);
            if (nowActive)
            {
                RefreshCharacterPanel();
                SfxPlayer.PlayUi(toggleSfxId);
            }
        }

        private void ToggleMap()
        {
            bool nowActive = !_mapPanel.activeSelf;
            _mapPanel.SetActive(nowActive);
            if (nowActive) SfxPlayer.PlayUi(toggleSfxId);
        }

        private void OnInventoryChanged()
        {
            if (_inventoryPanel.activeSelf) RefreshInventoryPanel();
        }

        private void RefreshInventoryPanel()
        {
            if (_inventory == null || _inventory.Slots.Count == 0)
            {
                _inventoryBodyText.text = "Inventar leer";
                return;
            }

            var sb = new StringBuilder();
            foreach (InventorySlot slot in _inventory.Slots)
            {
                if (slot.item == null) continue;
                sb.AppendLine($"{slot.item.displayName} x{slot.count}");
            }
            _inventoryBodyText.text = sb.ToString();
        }

        private void RefreshCharacterPanel()
        {
            var sb = new StringBuilder();

            if (_health != null)
                sb.AppendLine($"Health: {_health.CurrentHealth:F0} / {_health.MaxHealth:F0}");
            else
                sb.AppendLine("Keine Health-Komponente.");

            if (_stats != null)
            {
                foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                {
                    if (_stats.HasStat(type))
                        sb.AppendLine($"{type}: {_stats.GetStat(type):F2}");
                }
            }

            _characterBodyText.text = sb.ToString();
        }

        // ── UI construction (same idiom as MenuFlowGenerator, kept runtime-safe) ──────────

        private void BuildUI()
        {
            var canvasGo = new GameObject("PlayerHudCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            _inventoryPanel = BuildPanel(canvasGo.transform, "Inventar", out _inventoryBodyText);
            _characterPanel = BuildPanel(canvasGo.transform, "Charakter", out _characterBodyText);
            _mapPanel = BuildPanel(canvasGo.transform, "Karte", out Text mapBody);
            mapBody.text = "Karte kommt, sobald es mehrere Räume gibt.";

            _inventoryPanel.SetActive(false);
            _characterPanel.SetActive(false);
            _mapPanel.SetActive(false);
        }

        private static GameObject BuildPanel(Transform parent, string title, out Text bodyText)
        {
            var panelGo = new GameObject($"Panel_{title}", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            StretchFull(panelGo.GetComponent<RectTransform>());
            panelGo.GetComponent<Image>().color = PanelBg;

            BuildText(panelGo.transform, "Title", title, 40, TitleCol, new Vector2(0, -60), new Vector2(900, 80));

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Text));
            bodyGo.transform.SetParent(panelGo.transform, false);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0.1f, 0.1f);
            bodyRt.anchorMax = new Vector2(0.9f, 0.8f);
            bodyRt.offsetMin = Vector2.zero;
            bodyRt.offsetMax = Vector2.zero;

            bodyText = bodyGo.GetComponent<Text>();
            bodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyText.fontSize = 26;
            bodyText.color = BodyCol;
            bodyText.alignment = TextAnchor.UpperLeft;
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;

            return panelGo;
        }

        private static void BuildText(Transform parent, string name, string text, int fontSize, Color color,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var t = go.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
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
