using System;
using GameJamUniverse.Core.Minigames;
using UnityEngine;
using UnityEngine.UI;

namespace GameJamUniverse.Shared.UI
{
    /// <summary>
    /// Visual representation of a single <see cref="MinigameMetadata"/> entry in the Hub's
    /// "Play" list.
    /// </summary>
    public class GameCardView : MonoBehaviour
    {
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Button playButton;

        [Header("Optional decorations")]
        [SerializeField] private Button favoriteButton;
        [SerializeField] private Text favoriteButtonText;
        [SerializeField] private GameObject newBadge;

        private MinigameMetadata _metadata;
        private bool _isFavorite;
        private Action<MinigameMetadata, bool> _onToggleFavorite;

        private static readonly Color FavoriteColor = new Color(1f, 0.85f, 0.2f);
        private static readonly Color NotFavoriteColor = new Color(0.6f, 0.6f, 0.6f);

        public void Bind(MinigameMetadata metadata, Action<MinigameMetadata> onPlay,
            bool isFavorite = false, Action<MinigameMetadata, bool> onToggleFavorite = null,
            bool showNewBadge = false, string statsLine = null)
        {
            _metadata = metadata;
            _isFavorite = isFavorite;
            _onToggleFavorite = onToggleFavorite;

            if (titleText != null) titleText.text = metadata.displayName;
            if (descriptionText != null) descriptionText.text = metadata.description;
            if (difficultyText != null)
            {
                difficultyText.text = string.IsNullOrEmpty(statsLine)
                    ? metadata.difficulty.ToString()
                    : $"{metadata.difficulty}   |   {statsLine}";
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = metadata.thumbnail;
                thumbnailImage.enabled = metadata.thumbnail != null;
            }

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => onPlay?.Invoke(metadata));
            }

            if (favoriteButton != null)
            {
                favoriteButton.onClick.RemoveAllListeners();
                bool enable = onToggleFavorite != null;
                favoriteButton.gameObject.SetActive(enable);
                if (enable)
                {
                    favoriteButton.onClick.AddListener(OnFavoriteClicked);
                }
            }
            RefreshFavoriteVisual();

            if (newBadge != null) newBadge.SetActive(showNewBadge);
        }

        private void OnFavoriteClicked()
        {
            _isFavorite = !_isFavorite;
            RefreshFavoriteVisual();
            _onToggleFavorite?.Invoke(_metadata, _isFavorite);
        }

        private void RefreshFavoriteVisual()
        {
            if (favoriteButtonText == null) return;
            favoriteButtonText.text = _isFavorite ? "FAV" : "fav";
            favoriteButtonText.color = _isFavorite ? FavoriteColor : NotFavoriteColor;
        }
    }
}
