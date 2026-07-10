using GameJamUniverse.Core.Minigames;
using UnityEngine;
using UnityEngine.UI;

namespace GameJamUniverse.Minigames.BrickDungeon
{
    /// <summary>
    /// Minimal demo minigame: shows the current highscore/best time and offers two buttons that
    /// immediately complete the game (win or lose), exercising the full
    /// Hub -> Minigame -> Hub round trip. New minigames are created by duplicating this folder.
    /// </summary>
    public class BrickDungeonMinigameController : MinigameBase
    {
        [SerializeField] private Text statusText;
        [SerializeField] private Button winButton;
        [SerializeField] private Button loseButton;

        protected override void OnInitialize()
        {
            if (statusText != null)
            {
                statusText.text =
                    $"Template Minigame\nHighscore: {Context.Highscore}\nBest Time: {Context.BestTime:F1}s";
            }
        }

        protected override void OnStartGame()
        {
            if (winButton != null) winButton.onClick.AddListener(OnWinClicked);
            if (loseButton != null) loseButton.onClick.AddListener(OnLoseClicked);
        }

        private void OnWinClicked()
        {
            Context.ReportScore(Context.Highscore + 10);
            Context.ReportTime(Time.timeSinceLevelLoad);
            Context.CompleteGame(true);
        }

        private void OnLoseClicked()
        {
            Context.CompleteGame(false);
        }

        // Wired to LevelExitTrigger.OnPlayerReached in the scene — reaching the exit finishes
        // the run (same win path as the template's WinButton, just triggered by gameplay
        // instead of a debug button).
        public void CompleteRun()
        {
            Context.ReportScore(Context.Highscore + 10);
            Context.ReportTime(Time.timeSinceLevelLoad);
            Context.CompleteGame(true);
        }

        protected override void OnUnload()
        {
            if (winButton != null) winButton.onClick.RemoveListener(OnWinClicked);
            if (loseButton != null) loseButton.onClick.RemoveListener(OnLoseClicked);
        }
    }
}
