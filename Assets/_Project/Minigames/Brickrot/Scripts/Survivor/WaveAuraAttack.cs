using System.Collections;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Expanding shockwave: spawns concentric rings of WaveTiles outward from the player.
    //
    // Ported from the 2D original. The rings were laid out in the XY plane at a constant Z depth;
    // they now lie flat in the XZ ground plane at a constant Y height. Ring geometry, timing and
    // all upgrade stats are unchanged.
    public class WaveAuraAttack : MonoBehaviour
    {
        [Header("Prefab / Grid")]
        public WaveTile tilePrefab;

        // Grid step / spacing between tiles
        public float tileSize = 0.079f;

        // =========================
        // Upgrade Stats
        // =========================
        [Header("Upgrade Stats")]
        [Min(1)] public int frequency = 1;          // how often per trigger
        public float damage = 10f;
        [Tooltip("Knockback (outward push) für Gegner, wenn WaveTile das unterstützt.")]
        public float knockback = 0f;                // mapped to outwardPush

        [Header("Ring Settings")]
        public int minRadius = 3;

        [Tooltip("Max Radius der Welle (upgradebar). Default soll 8 sein.")]
        public int maxRadius = 8;

        [Tooltip("Wie schnell die Ringe nacheinander spawnen.")]
        public float ringStepInterval = 0.08f;

        public float tileLifetime = 0.5f;

        [Header("Upgrade Caps")]
        public int maxMaxRadius = 20;               // cap for maxRadius
        public int maxFrequency = 4;

        [Header("Damage")]
        public float perEnemyHitCooldown = 0.15f;

        [Header("Wave Motion")]
        public float waveAmplitude = 0.12f;
        public float waveFrequency = 10f;

        [Header("Spawn Tolerance")]
        [Tooltip("Wie dick ist der Ring in Tile-Einheiten.")]
        public float ringThickness = 0.5f;

        [Header("Ground Plane")]
        [Tooltip("Höhen-Offset relativ zum Spieler — hebt die Ringe leicht über den Boden.")]
        public float spawnHeightOffset = 0f;

        [Header("Juice")]
        [Tooltip("Kamera-Ruckler wenn die Welle losgeht.")]
        public float fireShake = 0.3f;

        Coroutine running;

        // =========================
        // External trigger (Tetris)
        // =========================
        public void TriggerFromTetris()
        {
            int n = Mathf.Clamp(frequency, 1, maxFrequency);
            for (int i = 0; i < n; i++)
            {
                Fire();
            }
        }

        // =========================
        // Upgrade API (Buttons call these)
        // =========================
        public void UpgradeRadius(int amount = 1)
        {
            maxRadius = Mathf.Clamp(maxRadius + amount, minRadius, maxMaxRadius);
        }

        public void UpgradeKnockback(float amount = 1f)
        {
            knockback = Mathf.Max(0f, knockback + amount);
        }

        public void UpgradeDamage(float amount = 1f)
        {
            damage = Mathf.Max(0f, damage + amount);
        }

        public void UpgradeFrequency(int amount = 1)
        {
            frequency = Mathf.Clamp(frequency + amount, 1, maxFrequency);
        }

        // =========================
        // Fire logic
        // =========================
        public void Fire()
        {
            if (tilePrefab == null) return;

            // Restarts the wave rather than stacking a second one — drop the StopCoroutine to stack.
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(DoWave());

            CameraShake.Add(fireShake);
        }

        IEnumerator DoWave()
        {
            // Ring centre in plain world space: X = width, Y = height, Z = depth.
            Vector3 center = transform.position;
            center.y += spawnHeightOffset;

            int rMin = Mathf.Max(0, minRadius);
            int rMax = Mathf.Max(rMin, maxRadius);

            for (int r = rMin; r <= rMax; r++)
            {
                SpawnRing(center, r);
                yield return new WaitForSeconds(ringStepInterval);
            }

            running = null;
        }

        void SpawnRing(Vector3 center, int radius)
        {
            int bound = radius + 1;

            // Ring lies flat on the ground plane: stepped out in X and Z, height held at center.y.
            for (int x = -bound; x <= bound; x++)
            {
                for (int z = -bound; z <= bound; z++)
                {
                    float dist = Mathf.Sqrt(x * x + z * z);

                    if (Mathf.Abs(dist - radius) <= ringThickness)
                    {
                        Vector3 worldPos = center + new Vector3(x * tileSize, 0f, z * tileSize);

                        WaveTile tile = Instantiate(
                            tilePrefab,
                            worldPos,
                            tilePrefab.transform.rotation
                        );

                        tile.Init(
                            center: center,
                            damage: damage,
                            hitCooldown: perEnemyHitCooldown,
                            lifetime: tileLifetime,
                            waveAmp: waveAmplitude,
                            waveFreq: waveFrequency,
                            outwardPush: knockback
                        );
                    }
                }
            }
        }

    }
}
