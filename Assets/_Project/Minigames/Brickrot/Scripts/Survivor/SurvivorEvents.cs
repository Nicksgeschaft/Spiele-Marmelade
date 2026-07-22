using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    [CreateAssetMenu(fileName = "SurvivorEvents", menuName = "ScriptableObjects/SurvivorEvents", order = 1)]
    public class SurvivorEvents : SurvivorEventsBase
    {
        public new void InvokeTakeDamage(int damage)
        {
            base.InvokeTakeDamage(damage);
        }
    }
}
