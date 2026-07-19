using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // Round timer drawn as a row of flat round bricks along the top of the screen. Put this on a child
    // of the gameplay camera so it rides with the view, same idea as PointStack.
    //
    // The row is anchored at its RIGHT end and grows left, and bricks are removed from the left end
    // first - so the bar visibly retreats toward its anchor as time runs out. One brick is worth
    // secondsPerBrick; when the last one goes, OnTimeUp fires.
    public class RoundTimerBar : MonoBehaviour
    {
        [Header("Brick")]
        [Tooltip("Flat round brick prefab used for one time step (e.g. Shared/Prefabs/Bricks/PlateRound).")]
        [SerializeField] private GameObject brickPrefab;
        [Tooltip("Scale each brick is spawned at. Spacing follows the measured brick size, so this alone " +
                 "controls how big the bar reads on screen.")]
        [SerializeField] private float brickScale = 0.3f;
        [Tooltip("Rotation applied to every brick. Default tips the plate onto its side so its studs point " +
                 "along the bar - the bricks then plug into each other like a roll of coins instead of " +
                 "lying side by side. Flip the sign if they face the wrong way.")]
        [SerializeField] private Vector3 brickRotation = new(0f, 0f, -90f);
        [Tooltip("How deep a plate's stud sits inside the next one, at full brick size. The step is the " +
                 "measured plate height minus this, which is what makes them interlock.")]
        [SerializeField] private float studOverlap = 0.18f;
        [Tooltip("Extra gap between bricks, as a fraction of one step. 0 = fully interlocked.")]
        [SerializeField] private float spacingPadding;

        [Header("Timing")]
        [Tooltip("How long a full round lasts, in seconds. 300 = 5 minutes.")]
        [SerializeField] private float roundDuration = 300f;
        [Tooltip("How much time one brick represents. Brick count = round duration / this.")]
        [SerializeField] private float secondsPerBrick = 5f;
        [Tooltip("Safety cap on how many bricks the bar may contain. Lowering Seconds Per Brick to speed up " +
                 "testing otherwise multiplies the brick count - a bar of thousands runs far off screen, so " +
                 "the end being removed isn't even visible. When capped, the step is stretched so the bar " +
                 "still empties exactly at the end of the round.")]
        [SerializeField] private int maxBricks = 120;
        [Tooltip("Start counting down as soon as this object is enabled. Turn off to start it yourself via StartTimer().")]
        [SerializeField] private bool startAutomatically = true;

        [Header("When time is up")]
        // Direct typed references instead of relying on UnityEvent dropdowns: those need a function
        // picked in a second step that is easy to leave on "No Function", in which case nothing happens
        // and there is no error explaining why.
        [Tooltip("End screen to show. Left empty, one is looked up in the scene automatically.")]
        [SerializeField] private GameOverScreen gameOverScreen;
        [Tooltip("Reports the run's score. Left empty, one is looked up in the scene automatically.")]
        [SerializeField] private JumpBrickScaleMinigameController minigameController;

        [Tooltip("Extra hooks (VFX, sound, ...). The two references above already run on their own.")]
        public UnityEvent OnTimeUp;

        private readonly List<GameObject> _bricks = new();
        private float _timeUntilNextRemoval;
        private bool _running;

        // May differ from secondsPerBrick when the brick count hits maxBricks - the round still has to
        // last roundDuration, so each remaining brick simply covers more time.
        private float _secondsPerBrickEffective = 5f;

        /// <summary>Seconds left, derived from the bricks still standing.</summary>
        public float TimeRemaining =>
            (_bricks.Count - 1) * _secondsPerBrickEffective + Mathf.Max(0f, _timeUntilNextRemoval);

        public int BricksRemaining => _bricks.Count;

        private void Start()
        {
            if (startAutomatically) StartTimer();
        }

        public void StartTimer()
        {
            BuildBar();
            _timeUntilNextRemoval = _secondsPerBrickEffective;
            _running = _bricks.Count > 0;
        }

        public void StopTimer() => _running = false;

        private void Update()
        {
            if (!_running) return;

            // Time.deltaTime is already scaled, so a paused game (timeScale 0) pauses the timer too.
            _timeUntilNextRemoval -= Time.deltaTime;
            if (_timeUntilNextRemoval > 0f) return;

            // Catch up if a frame hitch swallowed more than one interval.
            while (_timeUntilNextRemoval <= 0f && _bricks.Count > 0)
            {
                RemoveLeftmostBrick();
                _timeUntilNextRemoval += _secondsPerBrickEffective;
            }

            if (_bricks.Count == 0)
            {
                _running = false;
                TimeIsUp();
            }
        }

        private void TimeIsUp()
        {
            if (minigameController == null) minigameController = FindFirstObjectByType<JumpBrickScaleMinigameController>();
            if (gameOverScreen == null) gameOverScreen = FindFirstObjectByType<GameOverScreen>();

            minigameController?.OnTimeUp();

            if (gameOverScreen != null)
            {
                gameOverScreen.Show();
            }
            else
            {
                Debug.LogWarning($"[RoundTimerBar] Time is up, but no GameOverScreen exists in the scene - " +
                                 "add the component and assign its Menu Flow / screen title, or the run just " +
                                 "ends with nothing shown.", this);
            }

            OnTimeUp?.Invoke();
        }

        private void BuildBar()
        {
            ClearBar();

            if (brickPrefab == null)
            {
                Debug.LogWarning($"[RoundTimerBar] No brick prefab assigned on '{name}', so no timer is shown.", this);
                return;
            }
            if (secondsPerBrick <= 0f)
            {
                Debug.LogWarning($"[RoundTimerBar] secondsPerBrick must be greater than 0 on '{name}'.", this);
                return;
            }

            int count = ResolveBrickCount();
            _secondsPerBrickEffective = roundDuration / count;
            float step = StackStep();

            // Index 0 sits at the anchor on the right; later ones extend to the left.
            for (int i = 0; i < count; i++)
            {
                _bricks.Add(SpawnBrick(i, step));
            }
        }

        /// <summary>Extends the round by the given number of seconds, appending that many extra bricks
        /// to the far end of the bar. Both TimeRemaining and BricksRemaining are derived straight from
        /// the brick list, so this alone keeps the actual time and the visual bar in sync - call it from
        /// a pickup that should grant time immediately (e.g. a Time Brick attaching).</summary>
        public void AddSeconds(float seconds)
        {
            if (seconds <= 0f || _secondsPerBrickEffective <= 0f) return;

            int bricksToAdd = Mathf.Max(1, Mathf.RoundToInt(seconds / _secondsPerBrickEffective));
            float step = StackStep();

            for (int i = 0; i < bricksToAdd; i++)
            {
                _bricks.Add(SpawnBrick(_bricks.Count, step));
            }
        }

        private GameObject SpawnBrick(int index, float step)
        {
            GameObject brick = Instantiate(brickPrefab, transform);
            brick.transform.localPosition = new Vector3(-index * step, 0f, 0f);
            brick.transform.localRotation = Quaternion.Euler(brickRotation);
            brick.transform.localScale = Vector3.one * brickScale;
            return brick;
        }

        private int ResolveBrickCount()
        {
            int wanted = Mathf.Max(1, Mathf.CeilToInt(roundDuration / secondsPerBrick));
            int capped = Mathf.Clamp(wanted, 1, Mathf.Max(1, maxBricks));

            if (capped < wanted)
            {
                Debug.LogWarning($"[RoundTimerBar] {roundDuration}s / {secondsPerBrick}s would need {wanted} bricks, " +
                                 $"capped at {maxBricks}. Each brick now covers {roundDuration / capped:0.##}s so the " +
                                 "round still ends on time. Raise Seconds Per Brick (or Max Bricks) to change that.", this);
            }

            return capped;
        }

        // Distance between two bricks along the bar. The bricks are tipped onto their side, so the axis
        // they stack along is the plate's own height - minus the stud, so each one seats into the next
        // instead of leaving a visible gap.
        private float StackStep()
        {
            float plateHeight = MeasureBrickSize().y;
            return Mathf.Max(0.0001f, (plateHeight - studOverlap) * brickScale * (1f + spacingPadding));
        }

        // Removes the far (left) end of the row, so the bar shrinks back toward its right anchor.
        private void RemoveLeftmostBrick()
        {
            int last = _bricks.Count - 1;
            if (last < 0) return;

            GameObject brick = _bricks[last];
            _bricks.RemoveAt(last);
            if (brick != null) Destroy(brick);
        }

        private void ClearBar()
        {
            foreach (GameObject brick in _bricks)
            {
                if (brick != null) Destroy(brick);
            }
            _bricks.Clear();
        }

        // Size of one brick at scale 1, read off the prefab's meshes so the spacing can't drift out of
        // sync with the actual art (a hand-typed number here is invisible when wrong - the bricks just
        // overlap or gap).
        private Vector3 MeasureBrickSize()
        {
            // One flat plate at this project's x10 scale, mesh height including the stud.
            Vector3 fallbackSize = new(0.795f, 0.5f, 0.79f);

            MeshFilter[] filters = brickPrefab.GetComponentsInChildren<MeshFilter>(true);
            Bounds combined = default;
            bool any = false;

            foreach (MeshFilter filter in filters)
            {
                if (filter.sharedMesh == null) continue;

                Bounds meshBounds = filter.sharedMesh.bounds;
                // Prefab-local: fold in the child's own offset/scale relative to the prefab root.
                Vector3 scale = filter.transform.lossyScale;
                Vector3 center = filter.transform.localPosition + Vector3.Scale(meshBounds.center, scale);
                Vector3 size = Vector3.Scale(meshBounds.size, scale);

                var local = new Bounds(center, size);
                if (!any)
                {
                    combined = local;
                    any = true;
                }
                else
                {
                    combined.Encapsulate(local);
                }
            }

            return any && combined.size.y > 0.0001f ? combined.size : fallbackSize;
        }

        // Previews the bar's extent in the Scene view so it can be placed without entering play mode.
        private void OnDrawGizmosSelected()
        {
            if (brickPrefab == null || secondsPerBrick <= 0f) return;

            int count = Mathf.Clamp(Mathf.CeilToInt(roundDuration / secondsPerBrick), 1, Mathf.Max(1, maxBricks));
            float step = StackStep();

            Gizmos.color = Color.cyan;
            for (int i = 0; i < count; i++)
            {
                Gizmos.DrawWireCube(transform.TransformPoint(new Vector3(-i * step, 0f, 0f)),
                    Vector3.one * (step * 0.9f));
            }
        }
    }
}
