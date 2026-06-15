using UnityEngine;

namespace GameJamUniverse.Core.Minigames
{
    /// <summary>
    /// The "game card" data for a single minigame. One asset per minigame, stored in
    /// Minigames/&lt;Name&gt;/Config/. Collected into a <see cref="MinigameRegistry"/> for the Hub.
    /// </summary>
    [CreateAssetMenu(fileName = "MinigameMetadata_", menuName = "GameJam Universe/Minigame Metadata")]
    public class MinigameMetadata : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id, e.g. '001_BrickRunner'. Never reuse or change once shipped.")]
        public string gameId;
        public string displayName;
        [TextArea] public string description;
        public string author = "GameJam Universe Team";
        public Genre genre = Genre.Other;
        public string version = "0.1.0";

        [Header("Presentation")]
        public Sprite thumbnail;
        public Difficulty difficulty = Difficulty.Normal;
        [Tooltip("Estimated time to complete/enjoy one session, in minutes.")]
        public float estimatedPlaytimeMinutes = 5f;

        [Header("Capabilities")]
        public bool supportsMultiplayer;
        public bool supportsController;

        [Header("Scene")]
        [Tooltip("Name of the scene to load additively when this minigame is started.")]
        public string sceneName;

        [Header("Meta")]
        [Tooltip("ISO 8601 date, e.g. 2026-06-15.")]
        public string releaseDate;
        public string jamTheme;
    }
}
