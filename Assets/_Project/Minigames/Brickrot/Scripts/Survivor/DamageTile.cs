using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Shared base for the damage bricks (shot / wave / scythe). They damage anything with an
    // IDamageable that stays inside their trigger, on a per-target cooldown.
    //
    // The one thing they all need and can't skip: a kinematic Rigidbody. Enemies are pure
    // CharacterControllers (no Rigidbody), and Unity only raises OnTrigger callbacks when at least
    // one side of the overlap carries a Rigidbody. The tile is the natural place for it — it's the
    // moving "projectile" — so every damage tile brings its own, set kinematic so it never actually
    // does physics.
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class DamageTile : MonoBehaviour
    {
        protected readonly HitCooldownTracker Hits = new();

        protected virtual void Awake()
        {
            var rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;   // never driven by physics — position is set by the attack/Update
            rb.useGravity = false;
        }

        // Resolves the damageable an overlapping collider belongs to (on it, a parent, or the root),
        // applies the cooldown, and returns it so the concrete tile can deal its own damage/effects.
        protected bool TryHit(Collider other, float cooldown, out IDamageable damageable)
        {
            damageable = other.GetComponentInParent<IDamageable>();
            if (damageable == null) return false;
            return Hits.TryHit(other, cooldown);
        }
    }
}
