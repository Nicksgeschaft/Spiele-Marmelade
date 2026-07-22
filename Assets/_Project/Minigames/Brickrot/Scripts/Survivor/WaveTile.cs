using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // One brick of an expanding shockwave ring. Wobbles along its radial direction and damages
    // anything that stays inside it, on a per-target cooldown.
    //
    // Ported from the 2D original: rings now lie flat on the XZ ground plane, so the radial
    // direction is flattened on Y (was Z) and physics uses the 3D trigger callbacks. See
    // DamageTile for the kinematic Rigidbody it brings.
    public class WaveTile : DamageTile
    {
        float damage;
        float hitCooldown;
        float lifetime;

        float waveAmp;
        float waveFreq;
        float outwardPush;

        Vector3 center;
        Vector3 basePos;
        Vector3 radialDir;
        float phase;

        public void Init(Vector3 center, float damage, float hitCooldown, float lifetime,
                         float waveAmp, float waveFreq, float outwardPush)
        {
            this.center = center;
            this.damage = damage;
            this.hitCooldown = hitCooldown;
            this.lifetime = lifetime;
            this.waveAmp = waveAmp;
            this.waveFreq = waveFreq;
            this.outwardPush = outwardPush;

            basePos = transform.position;
            radialDir = basePos - center;
            radialDir.y = 0f; // keep the ring flat on the ground plane
            radialDir = radialDir.sqrMagnitude < 0.0001f ? Vector3.forward : radialDir.normalized;

            // Random phase so neighbouring tiles don't wobble in lockstep.
            phase = Random.Range(0f, 1000f);

            StartCoroutine(Life());
        }

        void Update()
        {
            float wobble = Mathf.Sin((Time.time * waveFreq) + phase) * waveAmp;
            float push = outwardPush * Time.deltaTime;

            basePos += radialDir * push;
            transform.position = basePos + radialDir * wobble;
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
        }
    }
}
