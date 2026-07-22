using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Survivor
{
    // Spawns enemies in a ring around the player, unlocking tougher types as the run goes on and
    // (optionally) tightening the spawn interval over time.
    //
    // Ported from the 2D original: the spawn ring was laid out in XY, it now sits on the XZ ground
    // plane. Timing, weighting and the population cap are unchanged.
    public class SurvivorManager : MonoBehaviour
    {
        [Header("Spawn Area")]
        [SerializeField] private float spawndistance = 15f;

        [Header("Spawn Timing")]
        [SerializeField] private float baseSpawnTime = 1f;

        [Tooltip("Optional: Spawnrate wird über Zeit schneller. 0 = aus.")]
        [SerializeField] private float spawnTimeDecreasePerMinute = 0f;

        [Tooltip("Untergrenze für Spawnzeit, damit es nicht komplett eskaliert.")]
        [SerializeField] private float minSpawnTime = 0.2f;

        [Header("Population Cap")]
        [SerializeField] private int maxAliveEnemies = 30;

        [Header("Enemy Unlocks (time since game start)")]
        [SerializeField]
        private List<EnemyUnlock> enemyUnlocks = new List<EnemyUnlock>()
        {
            new EnemyUnlock(){ prefab = null, unlockAfterSeconds = 0f, weight = 1f },
            new EnemyUnlock(){ prefab = null, unlockAfterSeconds = 120f, weight = 1f },  // 2 min
            new EnemyUnlock(){ prefab = null, unlockAfterSeconds = 300f, weight = 1f },  // 5 min
            new EnemyUnlock(){ prefab = null, unlockAfterSeconds = 600f, weight = 1f },  // 10 min
        };

        // internal
        private float _spawnTimer;
        private float _startTime;
        private Transform _playerTransform;

        private readonly List<GameObject> _alive = new List<GameObject>(256);

        [Serializable]
        public class EnemyUnlock
        {
            public GameObject prefab;

            [Tooltip("Ab wann dieser Gegner gespawnt werden darf (Sekunden seit Start).")]
            public float unlockAfterSeconds;

            [Tooltip("Wie häufig dieser Gegner im Vergleich zu den anderen kommt.")]
            public float weight;
        }

        private void Start()
        {
            _startTime = Time.time;

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;

            _spawnTimer = baseSpawnTime;
        }

        private void Update()
        {
            if (!_playerTransform) return;

            CleanupDeadEnemies();

            // Cap check
            if (_alive.Count >= maxAliveEnemies) return;

            // Spawn timer
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer > 0f) return;

            // Spawn
            GameObject prefab = PickEnemyPrefab();
            if (prefab != null)
            {
                Vector3 spawnPos = GetSpawnPositionOnPlane(_playerTransform.position, spawndistance);
                GameObject e = Instantiate(prefab, spawnPos, Quaternion.identity);
                _alive.Add(e);
            }

            // next spawn time (optional scaling)
            _spawnTimer = GetCurrentSpawnInterval();
        }

        private float GetCurrentSpawnInterval()
        {
            float elapsedMinutes = (Time.time - _startTime) / 60f;
            float interval = baseSpawnTime - elapsedMinutes * spawnTimeDecreasePerMinute;
            interval = Mathf.Max(minSpawnTime, interval);
            return interval;
        }

        private GameObject PickEnemyPrefab()
        {
            float elapsed = Time.time - _startTime;

            // Collect everything unlocked so far
            float totalWeight = 0f;
            for (int i = 0; i < enemyUnlocks.Count; i++)
            {
                var u = enemyUnlocks[i];
                if (u.prefab == null) continue;
                if (elapsed < u.unlockAfterSeconds) continue;
                if (u.weight <= 0f) continue;
                totalWeight += u.weight;
            }

            if (totalWeight <= 0f) return null;

            // Weighted random
            float r = UnityEngine.Random.value * totalWeight;
            float acc = 0f;

            for (int i = 0; i < enemyUnlocks.Count; i++)
            {
                var u = enemyUnlocks[i];
                if (u.prefab == null) continue;
                if (elapsed < u.unlockAfterSeconds) continue;
                if (u.weight <= 0f) continue;

                acc += u.weight;
                if (r <= acc) return u.prefab;
            }

            // fallback
            for (int i = enemyUnlocks.Count - 1; i >= 0; i--)
            {
                if (enemyUnlocks[i].prefab != null && elapsed >= enemyUnlocks[i].unlockAfterSeconds)
                    return enemyUnlocks[i].prefab;
            }

            return null;
        }

        private void CleanupDeadEnemies()
        {
            // drop destroyed references
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                if (_alive[i] == null) _alive.RemoveAt(i);
            }
        }

        // Picks a point on the diamond (L1 ring) of the given radius around the player, on the
        // ground plane — so enemies always arrive from off-screen, never on top of the player.
        private static Vector3 GetSpawnPositionOnPlane(Vector3 playerPos, float distance)
        {
            float x = UnityEngine.Random.Range(-distance, distance);
            float z = UnityEngine.Random.value < 0.5f ? 1f : -1f;
            z *= (distance - Mathf.Abs(x));
            return playerPos + new Vector3(x, 0f, z);
        }
    }
}
