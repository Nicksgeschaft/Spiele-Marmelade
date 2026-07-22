using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    public class FireShotAttack : MonoBehaviour
    {
        [Header("Projectile made of tiles")]
        [SerializeField] private ShotTile shotTilePrefab;

        [Tooltip("Wie viele Segmente in Flugrichtung (z.B. 5).")]
        [SerializeField] private int segmentsForward = 5;

        [Tooltip("Wie viele Reihen quer zur Flugrichtung (2 = zweireihig).")]
        [SerializeField] private int rows = 2;

        [SerializeField] private float tileSpacing = 0.079f;

        [Header("Movement")]
        [SerializeField] private float projectileSpeed = 8f;

        [Tooltip("Wie weit vor dem Spieler der Schuss startet (in Tiles).")]
        [SerializeField] private float forwardSpawnTiles = 1f;

        [Header("Ground Plane")]
        [Tooltip("Höhen-Offset relativ zum Spieler — hebt den Schuss leicht über den Boden.")]
        [SerializeField] private float spawnHeightOffset = 0f;

        // =========================
        // Upgrade Stats
        // =========================
        [Header("Upgrade Stats")]
        [Min(1)] [SerializeField] private int frequency = 1;          // wie oft pro Trigger
        [SerializeField] private float damage = 1f;                   // Default 1
        [SerializeField] private float projectileLifetime = 0.5f;     // Default 0.5 = Range
        [Min(0)] [SerializeField] private int explosionRadiusTiles = 3; // Default 3

        [Header("Upgrade Caps")]
        [SerializeField] private int maxFrequency = 4;
        [SerializeField] private float maxLifetime = 2.5f;
        [SerializeField] private int maxExplosionRadiusTiles = 10;

        [Header("Hit Cooldown")]
        [SerializeField] private float perEnemyHitCooldown = 0.15f;

        // =========================
        // Explosion / Mini Wave
        // =========================
        [Header("Explosion (Mini Wave on First Hit)")]
        [SerializeField] private WaveTile miniWaveTilePrefab;
        [SerializeField] private float miniRingThickness = 0.5f;
        [SerializeField] private float miniTileLifetime = 0.35f;
        [SerializeField] private float miniPerEnemyHitCooldown = 0.12f;
        [SerializeField] private float miniWaveAmplitude = 0.08f;
        [SerializeField] private float miniWaveFrequency = 12f;
        [SerializeField] private float miniOutwardPush = 0f;

        [Header("Targeting")]
        [SerializeField] private float maxRange = 20f;

        [Header("Juice")]
        [SerializeField] private float fireShake = 0.12f;
        [Tooltip("Kamera-Ruckler bei der Explosion. Skaliert mit dem Explosionsradius.")]
        [SerializeField] private float explosionShake = 0.5f;
        [Tooltip("Kurzer Zeitstopp bei der Explosion, in Sekunden Echtzeit. 0 = aus.")]
        [SerializeField] private float explosionHitStop = 0.06f;

        private bool miniWaveTriggered;

        // =========================
        // External trigger (Tetris)
        // =========================
        public void TriggerFromTetris()
        {
            int shots = Mathf.Clamp(frequency, 1, maxFrequency);
            for (int i = 0; i < shots; i++)
            {
                Fire();
            }
        }

        // =========================
        // Upgrade API (Buttons call these)
        // =========================
        public void UpgradeRange(float lifetimeIncrease = 0.1f)
        {
            projectileLifetime = Mathf.Clamp(projectileLifetime + lifetimeIncrease, 0.05f, maxLifetime);
        }

        public void UpgradeDamage(float amount = 1f)
        {
            damage = Mathf.Max(0f, damage + amount);
        }

        public void UpgradeExplosion(int radiusIncrease = 1)
        {
            explosionRadiusTiles = Mathf.Clamp(explosionRadiusTiles + radiusIncrease, 0, maxExplosionRadiusTiles);
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
            var target = FindClosestEnemy(transform.position, maxRange);
            if (target == null || shotTilePrefab == null) return;

            // Straight world-space maths: X = width, Y = height, Z = depth. Directions are
            // flattened to the ground plane (y = 0) rather than being packed into a Vector2.
            Vector3 origin = transform.position;
            origin.y += spawnHeightOffset;

            Vector3 dir = target.position - origin;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector3.forward;
            dir.Normalize();

            // Sideways on the ground plane = 90° about the up axis.
            Vector3 right = Vector3.Cross(Vector3.up, dir);

            miniWaveTriggered = false;

            var rootGO = new GameObject("ShotProjectileRoot");
            rootGO.transform.position = origin + dir * (forwardSpawnTiles * tileSpacing);

            var mover = rootGO.AddComponent<ShotProjectileMover>();
            mover.Init(moveDir: dir, speed: projectileSpeed, lifetime: projectileLifetime);

            float rowCenter = (rows - 1) * 0.5f;
            Quaternion shotRot = shotTilePrefab.transform.rotation;

            for (int f = 0; f < segmentsForward; f++)
            {
                for (int r = 0; r < rows; r++)
                {
                    float rowOffset = (r - rowCenter) * tileSpacing;

                    Vector3 worldPos = rootGO.transform.position
                                     + dir * (f * tileSpacing)
                                     + right * rowOffset;

                    var tile = Instantiate(shotTilePrefab, worldPos, shotRot, rootGO.transform);

                    tile.Init(
                        damage: damage,
                        hitCooldown: perEnemyHitCooldown,
                        lifetime: projectileLifetime,
                        onEnemyHit: (hitPos) =>
                        {
                            if (miniWaveTriggered) return;
                            miniWaveTriggered = true;
                            SpawnExplosion(hitPos);
                        }
                    );

                    tile.BeginLife();
                }
            }

            CameraShake.Add(fireShake);

            Destroy(rootGO, projectileLifetime);
        }

        private void SpawnExplosion(Vector3 center)
        {
            if (miniWaveTilePrefab == null) return;

            int radius = Mathf.Max(0, explosionRadiusTiles);
            if (radius <= 0) return;

            // The bigger the blast, the harder it hits — scaled against the upgrade cap so a
            // maxed-out explosion is the one that really shakes the screen.
            CameraShake.Add(explosionShake * Mathf.Clamp01((float)radius / Mathf.Max(1, maxExplosionRadiusTiles)));
            HitStop.Freeze(explosionHitStop);

            float explosionDamage = damage; // gekoppelt an Schuss-Schaden

            // AOE als mehrere Ringe 1..radius
            for (int rr = 1; rr <= radius; rr++)
            {
                SpawnMiniRing(center, rr, explosionDamage);
            }
        }

        private void SpawnMiniRing(Vector3 center, int radius, float explosionDamage)
        {
            int bound = radius + 1;
            Quaternion miniRot = miniWaveTilePrefab.transform.rotation;

            // Ring lies flat on the ground plane: stepped out in X and Z, height held at center.y.
            for (int x = -bound; x <= bound; x++)
            {
                for (int z = -bound; z <= bound; z++)
                {
                    float dist = Mathf.Sqrt(x * x + z * z);

                    if (Mathf.Abs(dist - radius) <= miniRingThickness)
                    {
                        Vector3 worldPos = center + new Vector3(x * tileSpacing, 0f, z * tileSpacing);

                        var tile = Instantiate(miniWaveTilePrefab, worldPos, miniRot);
                        tile.Init(
                            center: center,
                            damage: explosionDamage,
                            hitCooldown: miniPerEnemyHitCooldown,
                            lifetime: miniTileLifetime,
                            waveAmp: miniWaveAmplitude,
                            waveFreq: miniWaveFrequency,
                            outwardPush: miniOutwardPush
                        );
                    }
                }
            }
        }

        // Distance is measured on the ground plane only, so an enemy's height never affects
        // targeting.
        private static Transform FindClosestEnemy(Vector3 fromPosition, float maxRange = Mathf.Infinity)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

            Transform best = null;
            float bestDistSq = maxRange * maxRange;

            for (int i = 0; i < enemies.Length; i++)
            {
                Vector3 toEnemy = enemies[i].transform.position - fromPosition;
                toEnemy.y = 0f;
                float dSq = toEnemy.sqrMagnitude;

                if (dSq < bestDistSq)
                {
                    bestDistSq = dSq;
                    best = enemies[i].transform;
                }
            }

            return best;
        }
    }

    /// <summary>
    /// Bewegt den Projektil-Root wie ein klassisches Projektil über die Bodenebene.
    /// </summary>
    public class ShotProjectileMover : MonoBehaviour
    {
        private Vector3 dir;
        private float speed;
        private float lifetime;
        private float t;

        public void Init(Vector3 moveDir, float speed, float lifetime)
        {
            moveDir.y = 0f;
            this.dir = moveDir.sqrMagnitude < 0.0001f ? Vector3.forward : moveDir.normalized;
            this.speed = speed;
            this.lifetime = lifetime;
        }

        private void Update()
        {
            transform.position += dir * (speed * Time.deltaTime);

            t += Time.deltaTime;
            if (t >= lifetime)
                Destroy(gameObject);
        }
    }
}
