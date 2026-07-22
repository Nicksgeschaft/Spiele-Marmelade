using System;
using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // One brick of a fire shot. The shot is a formation of these, parented under a mover root.
    //
    // Ported from the 2D original: physics is now 3D (the whole minigame plays on the XZ ground
    // plane), so the trigger callbacks are the non-2D variants. See DamageTile for why the
    // kinematic Rigidbody it brings is mandatory.
    public class ShotTile : DamageTile
    {
        float damage;
        float hitCooldown;
        float lifetime;

        [SerializeField] private bool destroyOnFirstHit = false;

        Action<Vector3> onEnemyHit;

        bool hasTriggeredHitEvent;
        Coroutine lifeRoutine;

        public void Init(float damage, float hitCooldown, float lifetime, Action<Vector3> onEnemyHit)
        {
            this.damage = damage;
            this.hitCooldown = hitCooldown;
            this.lifetime = lifetime;
            this.onEnemyHit = onEnemyHit;

            // Life is deliberately NOT auto-started so the spawner can place the whole formation
            // first and only then start every tile's clock together — see FireShotAttack.Fire.
        }

        public void BeginLife()
        {
            if (lifeRoutine != null) StopCoroutine(lifeRoutine);
            lifeRoutine = StartCoroutine(Life());
        }

        IEnumerator Life()
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }

        void OnTriggerStay(Collider other)
        {
            if (!TryHit(other, hitCooldown, out IDamageable dmg)) return;

            dmg.TakeDamage(damage);

            if (!hasTriggeredHitEvent)
            {
                hasTriggeredHitEvent = true;
                onEnemyHit?.Invoke(other.transform.position);
            }

            if (destroyOnFirstHit)
                Destroy(gameObject);
        }
    }
}
