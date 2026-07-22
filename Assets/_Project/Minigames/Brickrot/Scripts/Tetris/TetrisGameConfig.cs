using System;
using System.Collections.Generic;
using SpieleMarmelade.Minigames.Brickrot.Survivor;
using UnityEngine;

namespace SpieleMarmelade.Minigames.Brickrot.Tetris
{
    [Serializable]
    public class TetrisGameConfig : ITetrisGameConfig
    {
        [SerializeField]
        private Vector2Int gridSizeInStuds = new Vector2Int(12, 20);

        [SerializeField]
        private float moveSpeed = 5.0f;

        [SerializeField]
        private float fallSpeedInStudsPerSecond = 1.0f;

        [SerializeField]
        private TetrisEvents tetrisEvents;

        [SerializeField]
        private SurvivorEventsBase survivorEvents;

        [SerializeField]
        private GameObject borderStudPrefab;

        [SerializeField]
        private GameObject[] damagePrefabs;

        [SerializeField]
        private GameObject redPrefab;
        [SerializeField]
        private GameObject greenPrefab;
        [SerializeField]
        private GameObject bluePrefab;
        [SerializeField]
        private GameObject whitePrefab;

        [SerializeField, Tooltip("if set, pieces spawned always use this color prefab so that it is easier to test")]
        private GameObject forcedPrefab;

        [SerializeField]
        private Camera tetrisCamera;

        public Vector2Int GridSizeInStuds => gridSizeInStuds;
        public float MoveSpeed => moveSpeed;
        public float FallSpeedInStudsPerSecond => fallSpeedInStudsPerSecond;

        public TetrisEvents TetrisEvents => tetrisEvents;
        public SurvivorEventsBase SurvivorEvents => survivorEvents;

        public GameObject BorderStudPrefab => borderStudPrefab;
        public IEnumerable<GameObject> DamagePrefabs => damagePrefabs;

        public GameObject RedPrefab => redPrefab;
        public GameObject GreenPrefab => greenPrefab;
        public GameObject BluePrefab => bluePrefab;
        public GameObject WhitePrefab => whitePrefab;
        public GameObject ForcedPrefab => forcedPrefab;

        public IEnumerable<GameObject> PiecePrefabs
        {
            get
            {
                yield return RedPrefab;
                yield return GreenPrefab;
                yield return BluePrefab;
            }
        }

        public Camera TetrisCamera => tetrisCamera;
    }
}
