using SpieleMarmelade.Core.Minigames;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Jump'n'Run: no in-game UI, win/lose are wired from level triggers instead of buttons.
    // Hook a LevelExitTrigger at the goal to OnLevelGoalReached(), and one on a fall-out volume
    // below the level to OnFellOff(). RoundTimerBar.OnTimeUp goes to OnTimeUp().
    //
    // Context is null when the scene is played directly (no Boot/GameManager), which is the normal
    // way to iterate on a level - hence the null-conditional calls.
    public class JumpBrickScaleMinigameController : MinigameBase
    {
        public void OnLevelGoalReached()
        {
            Context?.ReportScore(Context.Highscore + 10);
            Context?.ReportTime(Time.timeSinceLevelLoad);
            Context?.CompleteGame(true);
        }

        public void OnFellOff()
        {
            Context?.CompleteGame(false);
        }

        // Round timer ran out: the run counts as finished, scored by how many points were collected.
        public void OnTimeUp()
        {
            Context?.ReportScore(CollectedPoints);
            Context?.ReportTime(Time.timeSinceLevelLoad);
            Context?.CompleteGame(true);
        }

        /// <summary>Points picked up this run, read off the on-screen stack.</summary>
        public int CollectedPoints => PointStack.Instance != null ? PointStack.Instance.Count : 0;
    }
}
