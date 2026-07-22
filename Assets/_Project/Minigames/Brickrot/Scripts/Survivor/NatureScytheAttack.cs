using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Sweeping arc of ScytheTiles that flies outward from the player.
    //
    // Ported from the 2D original: the arc was built in the XY plane at the player's Z; it is now
    // built in the XZ ground plane at the player's Y. Arc shape, growth and damage are unchanged.
    public class NatureScytheAttack : MonoBehaviour
    {
        [Header("Tile Prefab (Brick)")]
        public ScytheTile tilePrefab;

        [Header("Grid")]
        public float tileSize = 0.079f;

        [Header("Upgrade Stats")]
        [Min(1)] public int frequency = 1; // wie oft pro Trigger (Spawn-Event) gefeuert wird

        [Header("Upgrade Caps")]
        public int maxRadiusTiles = 12;
        public float maxLifetime = 3.0f;
        public int maxFrequency = 4;

        [Header("Scythe Shape")]
        public int radiusTiles = 6;
        public float thicknessTiles = 1.2f;
        public float arcDegrees = 120f;

        [Header("Spawn")]
        [Tooltip("Spawn-Offset relativ zum Spieler (X/Y/Z).")]
        public Vector3 spawnDelta = Vector3.zero;

        [Tooltip("Wie viele Tiles vor dem Spieler spawnen (in Forward-Richtung).")]
        public float forwardSpawnTiles = 2f;

        [Header("Motion")]
        public float speed = 6f;
        public float lifetime = 1.2f;

        [Header("Growth (by distance)")]
        public float startScale = 0.7f;
        public float endScale = 1.4f;
        public float growRangeTiles = 12f;

        [Header("Damage")]
        public float damage = 10f;
        public float perEnemyHitCooldown = 0.15f;

        [Header("Tile Wobble")]
        public float waveAmplitude = 0.03f;
        public float waveFrequency = 12f;

        [Header("Juice")]
        [Tooltip("Kamera-Ruckler wenn die Sichel losfliegt.")]
        public float fireShake = 0.18f;

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
        public void UpgradeSize(int amount = 1)
        {
            radiusTiles = Mathf.Clamp(radiusTiles + amount, 1, maxRadiusTiles);
        }

        public void UpgradeRange(float lifetimeIncrease = 0.2f)
        {
            lifetime = Mathf.Clamp(lifetime + lifetimeIncrease, 0.05f, maxLifetime);
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

        /// <summary>
        /// Feuert eine Scythe in eine zufällige Richtung auf der Bodenebene.
        /// </summary>
        public void Fire()
        {
            Vector3 origin = transform.position + spawnDelta;

            Vector2 disc = Random.insideUnitCircle.normalized;
            Vector3 dir = new Vector3(disc.x, 0f, disc.y);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.right;

            SpawnScythe(origin, dir);
        }

        /// <summary>
        /// Optional: feuert nach vorne (z.B. Player FacingDirection).
        /// </summary>
        public void FireForward(Vector3 dir)
        {
            Vector3 origin = transform.position + spawnDelta;

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            SpawnScythe(origin, dir);
        }

        void SpawnScythe(Vector3 origin, Vector3 dir)
        {
            if (tilePrefab == null) return;

            // Plain world space: X = width, Y = height, Z = depth. The arc is laid out along the
            // forward/right pair on the ground plane, so the height of every tile is just origin.y.
            dir.y = 0f;
            Vector3 forward = dir.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            Vector3 center = origin + forward * (forwardSpawnTiles * tileSize);

            CameraShake.Add(fireShake);

            int r = Mathf.Max(1, radiusTiles);
            int bound = r + 2;

            // gRight/gForward index the arc in tile steps sideways and along the travel direction.
            for (int gRight = -bound; gRight <= bound; gRight++)
            {
                for (int gForward = -bound; gForward <= bound; gForward++)
                {
                    float distTiles = Mathf.Sqrt(gRight * gRight + gForward * gForward);

                    if (Mathf.Abs(distTiles - r) > thicknessTiles)
                        continue;

                    float ang = Mathf.Rad2Deg * Mathf.Atan2(gRight, gForward);
                    if (Mathf.Abs(ang) > arcDegrees * 0.5f)
                        continue;

                    Vector3 worldPos = center
                                     + right * (gRight * tileSize)
                                     + forward * (gForward * tileSize);

                    var tile = Instantiate(tilePrefab, worldPos, Quaternion.identity);

                    tile.Init(
                        moveDir: forward,
                        speed: speed,
                        damage: damage,
                        hitCooldown: perEnemyHitCooldown,
                        lifetime: lifetime,
                        waveAmp: waveAmplitude,
                        waveFreq: waveFrequency,
                        startScale: startScale,
                        endScale: endScale,
                        growRange: growRangeTiles * tileSize
                    );
                }
            }
        }

    }
}
