using SpieleMarmelade.Core.Minigames;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Jump'n'Run: no in-game UI, win/lose are wired from level triggers instead of buttons.
    // Hook a LevelExitTrigger at the goal to OnLevelGoalReached(), and one on a fall-out volume
    // below the level to OnFellOff().
    public class JumpBrickScaleMinigameController : MinigameBase
    {
        public void OnLevelGoalReached()
        {
            Context.ReportScore(Context.Highscore + 10);
            Context.ReportTime(Time.timeSinceLevelLoad);
            Context.CompleteGame(true);
        }

        public void OnFellOff()
        {
            Context.CompleteGame(false);
        }
    }
}
