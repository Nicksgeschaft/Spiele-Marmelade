using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    [CreateAssetMenu(fileName = "TetrisEvents", menuName = "ScriptableObjects/TetrisEvents", order = 1)]
    public class TetrisEvents : TetrisEventsBase
    {
        public new void InvokeBrickSpawned(StudColor studColor)
        {
            base.InvokeBrickSpawned(studColor);
        }
        
        public new void InvokeLevelUpSkill(StudColor studColor, int numberOfGroups)
        {
            base.InvokeLevelUpSkill(studColor, numberOfGroups);
        }
        
        public new void InvokeGameOver()
        {
            base.InvokeGameOver();
        }
    }
}
