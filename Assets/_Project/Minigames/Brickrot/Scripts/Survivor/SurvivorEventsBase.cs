using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    public class SurvivorEventsBase : ScriptableObject
    {
        public delegate void TakeDamageDelegate(int damage);
        public event TakeDamageDelegate OnTakeDamage;
    
        protected void InvokeTakeDamage(int damage)
        {
            if (OnTakeDamage != null)
            {
                OnTakeDamage(damage);
            }
        }
    }
}
