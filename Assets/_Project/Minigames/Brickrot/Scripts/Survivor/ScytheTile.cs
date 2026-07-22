using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // One brick of a scythe arc: flies outward, wobbles sideways and grows with distance.
    //
    // Ported from the 2D original: movement was constrained to XY with Z pinned to 0; it now runs
    // on the XZ ground plane with Y pinned instead, and uses 3D trigger callbacks. See DamageTile
    // for the kinematic Rigidbody it brings.
    public class ScytheTile : DamageTile
    {
        float damage;
        float hitCooldown;
        float lifetime;

        float waveAmp;
        float waveFreq;

        float speed;

        float startScale;
        float endScale;
        float growRange;

        Vector3 basePos;
        Vector3 moveDir;   // ground-plane direction (Y stays 0)
        Vector3 rightDir;  // sideways on the ground plane
        float phase;
        float travelled;

        public void Init(
            Vector3 moveDir,
            float speed,
            float damage,
            float hitCooldown,
            float lifetime,
            float waveAmp,
            float waveFreq,
            float startScale,
            float endScale,
            float growRange
        )
        {
            // Ground plane only — flatten any height component out of the travel direction.
            moveDir.y = 0f;
            this.moveDir = moveDir.sqrMagnitude < 0.0001f ? Vector3.forward : moveDir.normalized;

            this.speed = speed;

            this.damage = damage;
            this.hitCooldown = hitCooldown;
            this.lifetime = lifetime;

            this.waveAmp = waveAmp;
            this.waveFreq = waveFreq;

            this.startScale = startScale;
            this.endScale = endScale;
            this.growRange = Mathf.Max(0.01f, growRange);

            basePos = transform.position;
            phase = Random.Range(0f, 1000f);

            // Sideways on the ground plane: 90° rotation about Y.
            rightDir = new Vector3(-this.moveDir.z, 0f, this.moveDir.x);
            if (rightDir.sqrMagnitude < 0.0001f) rightDir = Vector3.right;

            transform.localScale = Vector3.one * startScale;

            StartCoroutine(Life());
        }

        void Update()
        {
            float step = speed * Time.deltaTime;

            basePos += moveDir * step;
            travelled += step;

            float wobble = Mathf.Sin((Time.time * waveFreq) + phase) * waveAmp;

            float t = Mathf.Clamp01(travelled / growRange);
            float s = Mathf.Lerp(startScale, endScale, t);

            transform.position = basePos + rightDir * wobble;
            transform.localScale = Vector3.one * s;
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
