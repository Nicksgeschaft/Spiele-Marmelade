using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    [RequireComponent(typeof(Collider))]
    public class ScytheProjectile : MonoBehaviour
    {
        float speed;
        float damage;
        float hitCooldown;
        int pierce;

        float lifetime;
        float maxRange;

        float startScale;
        float endScale;
        AnimationCurve growthCurve;

        Vector3 origin;
        Vector3 dir;

        // Anti-Maschinengewehr: pro Gegner Cooldown
        readonly HitCooldownTracker hits = new();

        // optional: bisschen Spin fürs “Sichel-Feeling”
        public float spinDegreesPerSecond = 720f;

        public void Init(
            Vector3 origin,
            Vector3 direction,
            float speed,
            float damage,
            float hitCooldown,
            int pierce,
            float lifetime,
            float maxRange,
            float startScale,
            float endScale,
            AnimationCurve growthCurve
        )
        {
            this.origin = origin;
            this.dir = direction.normalized;

            this.speed = speed;
            this.damage = damage;
            this.hitCooldown = hitCooldown;
            this.pierce = pierce;

            this.lifetime = lifetime;
            this.maxRange = Mathf.Max(0.01f, maxRange);

            this.startScale = startScale;
            this.endScale = endScale;
            this.growthCurve = growthCurve ?? AnimationCurve.Linear(0, 0, 1, 1);

            transform.localScale = Vector3.one * startScale;

            StartCoroutine(LifeTimer());
        }

        void Update()
        {
            // Bewegung
            transform.position += dir * (speed * Time.deltaTime);

            // Spin (optional)
            transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);

            // Wachstum nach Distanz
            float dist = Vector3.Distance(origin, transform.position);
            float t = Mathf.Clamp01(dist / maxRange);
            float eased = Mathf.Clamp01(growthCurve.Evaluate(t));
            float s = Mathf.Lerp(startScale, endScale, eased);
            transform.localScale = Vector3.one * s;

            // Range hard stop (falls lifetime größer ist)
            if (dist >= maxRange)
                Destroy(gameObject);
        }

        IEnumerator LifeTimer()
        {
            yield return new WaitForSeconds(lifetime);
            Destroy(gameObject);
        }

        void OnTriggerStay(Collider other)
        {
            if (!hits.TryHit(other, hitCooldown)) return;

            pierce--;
            if (pierce <= 0)
                Destroy(gameObject);
        }
    }
}
