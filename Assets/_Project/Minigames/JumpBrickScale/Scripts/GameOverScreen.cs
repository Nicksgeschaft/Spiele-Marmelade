using System;
using System.Collections;
using System.Collections.Generic;
using SpieleMarmelade.Shared.UI.MenuFlow;
using SpieleMarmelade.World;
using UnityEngine;

namespace SpieleMarmelade.Minigames.JumpBrickScale
{
    // End-of-round screen. Switches the menu to a "Time is Over" screen, then flies the bricks the
    // player actually collected in one by one and stamps a rating underneath, based on how many
    // there were.
    //
    // The screen itself (title + "Back to Menu" button) is an ordinary Generic screen authored in the
    // Menu Flow Editor - only the parts that depend on the run's result are built here at runtime.
    public class GameOverScreen : MonoBehaviour
    {
        [Serializable]
        public class RatingTier
        {
            [Tooltip("Applies when the collected count is at most this. Leave the highest tier at a big " +
                     "number so it always catches the rest.")]
            public int maxPoints = 5;
            public string text = "BRICKLESS";
        }

        [Header("Wiring")]
        [SerializeField] private MenuFlowController menuFlow;
        [Tooltip("Title of the Generic screen as typed in the Menu Flow Editor - must match exactly. " +
                 "Screens are referenced by title because their ids are auto-generated GUIDs the editor never shows.")]
        [SerializeField] private string gameOverScreenTitle = "Time is Over";

        [Header("Collected bricks fly-in")]
        [Tooltip("Where the collected bricks gather, relative to the screen's sign group.")]
        [SerializeField] private Vector3 bricksAnchor = new(-1.5f, 0.4f, 0f);
        [SerializeField] private int bricksPerRow = 10;
        [SerializeField] private float brickSpacing = 0.32f;
        [SerializeField] private float rowSpacing = 0.36f;
        [SerializeField] private float flightDuration = 0.35f;
        [Tooltip("Delay between two bricks starting their flight - this is what makes them arrive one by one.")]
        [SerializeField] private float delayBetweenBricks = 0.08f;
        [SerializeField] private AnimationCurve flightEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Rating text")]
        [SerializeField] private GameObject brickPrefab;
        [SerializeField] private Material[] letterMaterials;
        [SerializeField] private Vector3 ratingAnchor = new(0f, -0.9f, 0f);
        [SerializeField] private float ratingScale = 0.5f;
        [Tooltip("Checked from top to bottom; the first tier whose Max Points the score fits into wins.")]
        [SerializeField]
        private List<RatingTier> ratingTiers = new()
        {
            new RatingTier { maxPoints = 4, text = "BRICKLESS" },
            new RatingTier { maxPoints = 15, text = "SOLID" },
            new RatingTier { maxPoints = int.MaxValue, text = "BRICKPOT" },
        };

        private GameObject _runtimeContent;

        /// <summary>Wire RoundTimerBar.OnTimeUp here instead of straight to ReturnToMainMenu.</summary>
        public void Show()
        {
            if (menuFlow == null)
            {
                Debug.LogWarning($"[GameOverScreen] '{name}' has no MenuFlowController assigned, so the end " +
                                 "screen can't be shown.", this);
                return;
            }

            string screenId = menuFlow.FindScreenIdByTitle(gameOverScreenTitle);
            if (string.IsNullOrEmpty(screenId))
            {
                Debug.LogWarning($"[GameOverScreen] No screen titled '{gameOverScreenTitle}' in the menu graph. " +
                                 "Check the exact spelling in the Menu Flow Editor.", this);
                return;
            }

            menuFlow.ShowScreen(screenId);

            GameObject signs = menuFlow.GetBrickSigns(screenId);
            if (signs == null)
            {
                Debug.LogWarning($"[GameOverScreen] Screen '{gameOverScreenTitle}' has no brick signs - " +
                                 "run Generate in the Menu Flow Editor after adding it.", this);
                return;
            }

            // Everything built here goes under one child of the screen, so re-showing simply rebuilds
            // it and it hides together with the screen.
            if (_runtimeContent != null) Destroy(_runtimeContent);
            _runtimeContent = new GameObject("RunResult");
            _runtimeContent.transform.SetParent(signs.transform, false);

            int points = PointStack.Instance != null ? PointStack.Instance.Count : 0;
            BuildRating(points);
            StartCoroutine(FlyCollectedBricksIn());
        }

        private void BuildRating(int points)
        {
            string text = ResolveRatingText(points);
            if (string.IsNullOrEmpty(text) || brickPrefab == null) return;

            BrickTextBuilder.Result result = BrickTextBuilder.Build(
                brickPrefab, text, letterMaterials, null, "Rating", includeBackground: false);
            if (result.Root == null) return;

            Transform root = result.Root.transform;
            root.SetParent(_runtimeContent.transform, false);
            root.localScale = Vector3.one * ratingScale;
            // Built from its left edge, so shift by half its width to centre it on the anchor.
            root.localPosition = ratingAnchor - new Vector3(result.Width * ratingScale * 0.5f, 0f, 0f);
        }

        private string ResolveRatingText(int points)
        {
            foreach (RatingTier tier in ratingTiers)
            {
                if (points <= tier.maxPoints) return tier.text;
            }
            return ratingTiers.Count > 0 ? ratingTiers[^1].text : string.Empty;
        }

        private IEnumerator FlyCollectedBricksIn()
        {
            PointStack stack = PointStack.Instance;
            if (stack == null) yield break;

            IReadOnlyList<Transform> bricks = stack.CollectedBricks;
            for (int i = 0; i < bricks.Count; i++)
            {
                Transform brick = bricks[i];
                if (brick == null) continue;

                // Reparent first so the target can be expressed in the screen's own local space - the
                // menu stage and the gameplay camera are in completely different places.
                brick.SetParent(_runtimeContent.transform, worldPositionStays: true);
                StartCoroutine(FlyBrick(brick, SlotFor(i)));

                if (delayBetweenBricks > 0f) yield return new WaitForSeconds(delayBetweenBricks);
            }
        }

        private Vector3 SlotFor(int index)
        {
            int row = bricksPerRow > 0 ? index / bricksPerRow : 0;
            int column = bricksPerRow > 0 ? index % bricksPerRow : index;
            return bricksAnchor + new Vector3(column * brickSpacing, -row * rowSpacing, 0f);
        }

        private IEnumerator FlyBrick(Transform brick, Vector3 targetLocalPosition)
        {
            Vector3 startPosition = brick.localPosition;
            Quaternion startRotation = brick.localRotation;

            float elapsed = 0f;
            while (elapsed < flightDuration && brick != null)
            {
                elapsed += Time.deltaTime;
                float eased = flightEase.Evaluate(Mathf.Clamp01(elapsed / flightDuration));

                brick.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, eased);
                brick.localRotation = Quaternion.Slerp(startRotation, Quaternion.identity, eased);
                yield return null;
            }

            if (brick == null) yield break;
            brick.localPosition = targetLocalPosition;
            brick.localRotation = Quaternion.identity;
        }
    }
}
