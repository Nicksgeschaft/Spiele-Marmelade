using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot
{
    // Per-target damage cooldown for lingering damage volumes.
    //
    // Every damage tile (shot, scythe, wave) sits still and damages on OnTriggerStay, which fires
    // every physics frame — without this an enemy standing in a wave would take dozens of ticks a
    // second. Each tile keeps its own tracker, so overlapping effects still stack properly.
    public class HitCooldownTracker
    {
        private readonly Dictionary<EntityId, float> _lastHitTime = new();

        /// <summary>
        /// True if <paramref name="target"/> may be damaged now, recording the hit as it does so.
        /// False while the target is still on cooldown.
        /// </summary>
        public bool TryHit(Object target, float cooldown)
        {
            if (target == null) return false;

            EntityId id = target.GetEntityId();
            float now = Time.time;

            if (_lastHitTime.TryGetValue(id, out float last) && now - last < cooldown)
            {
                return false;
            }

            _lastHitTime[id] = now;
            return true;
        }
    }
}
