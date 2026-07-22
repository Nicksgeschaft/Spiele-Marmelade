using System;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    public class TetrisEventsBase : ScriptableObject
    {
        public delegate void BrickSpawnedDelegate(StudColor studColor);
        public event BrickSpawnedDelegate OnBrickSpawned;
        
        public delegate void LevelUpSkillDelegate(StudColor studColor, int numberOfGroups);
        public event LevelUpSkillDelegate OnLevelUpSkill;
        
        public event Action OnGameOver;
    
        protected void InvokeBrickSpawned(StudColor studColor)
        {
            if (OnBrickSpawned != null)
            {
                OnBrickSpawned(studColor);
            }
        }
    
        protected void InvokeLevelUpSkill(StudColor studColor, int numberOfGroups)
        {
            if (OnLevelUpSkill != null)
            {
                OnLevelUpSkill(studColor, numberOfGroups);
            }
        }

        protected void InvokeGameOver()
        {
            if (OnGameOver != null)
            {
                OnGameOver();
            }
        }
    }
}
