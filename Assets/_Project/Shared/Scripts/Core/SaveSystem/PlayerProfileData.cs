using System;

namespace SpieleMarmelade.Core.SaveSystem
{
    [Serializable]
    public class PlayerProfileData
    {
        public string username = "Player";
        public int avatarId;
        public int level = 1;
        public int xp;
        public int coins;
        public int stars;
        public float totalPlaytimeSeconds;
        public int gamesPlayed;
        public int gamesCompleted;
    }
}
