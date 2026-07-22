using System;
using System.Collections;
using System.Collections.Generic;
using SpieleMarmelade.Shared.UI;
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
        [Tooltip("How far above the bottom edge of the screen the stacks stand. Measured from the menu " +
                 "camera at runtime, so it holds at any aspect ratio.")]
        [SerializeField] private float bottomMargin = 0.5f;
        [Tooltip("Bricks per stack before the next stack is started beside it.")]
        [SerializeField] private int bricksPerColumn = 3;
        [Tooltip("Spacing between stacks / stacked bricks. Left at 0 it copies the point stack's own " +
                 "spacing, so the bricks stay packed exactly as they were in the corner.")]
        [SerializeField] private Vector2 spacingOverride = Vector2.zero;
        [SerializeField] private float flightDuration = 0.35f;
        [Tooltip("Delay between two bricks starting their flight - this is what makes them arrive one by one.")]
        [SerializeField] private float delayBetweenBricks = 0.08f;
        [SerializeField] private AnimationCurve flightEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Character")]
        [Tooltip("Player_Platformer-Prefab. Wird links angezeigt, in den Farben die im Character " +
                 "Creator gewählt wurden. Leer lassen = kein Charakter auf dem Endscreen.")]
        [SerializeField] private GameObject characterPrefab;
        [SerializeField] private float characterScale = 25f;
        [Tooltip("Abstand des Charakters vom linken Bildrand. Der Rand wird zur Laufzeit aus der " +
                 "Menü-Kamera gelesen, gilt also bei jedem Seitenverhältnis.")]
        [SerializeField] private float characterLeftMargin = 1.6f;
        [Tooltip("Höhe der Charaktermitte auf dem Screen.")]
        [SerializeField] private float characterY = 0.2f;

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
        private Vector3 _bricksAnchor;

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
            _bricksAnchor = ResolveBricksAnchor(_runtimeContent.transform);

            int points = PointStack.Instance != null ? PointStack.Instance.Count : 0;
            BuildCharacter();
            BuildRating(points);
            StartCoroutine(FlyCollectedBricksIn());
        }

        // Shows the player's own character on the left, in the colours picked in the Character Creator.
        // Read straight from the saved settings rather than cloning the live player: by this point the
        // run's assembly is a pile of physics bricks in some arbitrary pose, and what belongs on a
        // results screen is the character the player built, not the state it happened to end in.
        private void BuildCharacter()
        {
            if (characterPrefab == null) return;

            GameObject character = Instantiate(characterPrefab, _runtimeContent.transform);
            character.name = "Character";
            character.transform.localScale = Vector3.one * characterScale;
            character.transform.localPosition = Vector3.zero;

            // Player_Platformer's root is tagged "Player". A second object answering to that tag is
            // exactly what once sent the gameplay camera chasing a menu prop, so it goes.
            foreach (Transform child in character.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.tag = "Untagged";
            }

            // Pure decoration on a screen with clickable brick buttons - it must never eat their raycasts.
            foreach (Collider characterCollider in character.GetComponentsInChildren<Collider>(true))
            {
                characterCollider.enabled = false;
            }

            CharacterLook.ApplySaved(character.transform);

            // Positioned by its Body part, so characterY means the middle of the character. Its pivot
            // sits at the feet, and its overall bounds are dragged around by the googly eyes - neither
            // makes for a predictable anchor.
            Vector3 anchor = ResolveCharacterAnchor(_runtimeContent.transform);
            Renderer body = CharacterLook.FindPart(character.transform, CharacterPart.Body);
            Vector3 offset = body != null
                ? _runtimeContent.transform.InverseTransformPoint(body.bounds.center)
                : Vector3.zero;
            character.transform.localPosition = anchor - offset;
        }

        // Left edge of the visible area, in the sign group's local space - same reasoning as
        // ResolveBricksAnchor: MenuStageResizer changes the visible width with the aspect ratio.
        private Vector3 ResolveCharacterAnchor(Transform parent)
        {
            Camera cam = menuFlow != null ? menuFlow.MenuCamera : null;
            if (cam == null || !cam.orthographic)
            {
                return new Vector3(-3f, characterY, 0f);
            }

            float worldLeft = cam.transform.position.x - cam.orthographicSize * cam.aspect;
            Vector3 local = parent.InverseTransformPoint(new Vector3(worldLeft, parent.position.y, parent.position.z));
            return new Vector3(local.x + characterLeftMargin, characterY, 0f);
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

        // Stacks of bricksPerColumn, filled bottom-up, starting in the middle of the screen and then
        // alternating outward: centre, right, left, further right, further left, ...
        private Vector3 SlotFor(int index)
        {
            int perColumn = Mathf.Max(1, bricksPerColumn);
            int columnIndex = index / perColumn;
            int rowInColumn = index % perColumn;

            // 0 -> 0, 1 -> +1, 2 -> -1, 3 -> +2, 4 -> -2, ...
            int magnitude = (columnIndex + 1) / 2;
            int columnOffset = columnIndex % 2 == 1 ? magnitude : -magnitude;

            Vector2 step = ResolveSpacing();
            return _bricksAnchor + new Vector3(columnOffset * step.x, rowInColumn * step.y, 0f);
        }

        private Vector2 ResolveSpacing()
        {
            if (spacingOverride.x > 0f && spacingOverride.y > 0f) return spacingOverride;

            PointStack stack = PointStack.Instance;
            if (stack != null)
            {
                Vector3 step = stack.SlotStep;
                if (step.x > 0f && step.y > 0f) return new Vector2(step.x, step.y);
            }

            return new Vector2(0.24f, 0.29f);
        }

        // Bottom-centre of the visible area, expressed in the sign group's local space. Derived from the
        // menu camera rather than a fixed Y, because MenuStageResizer changes the visible height with
        // the aspect ratio - a hard-coded value would sit correctly at one resolution only.
        private Vector3 ResolveBricksAnchor(Transform parent)
        {
            Camera cam = menuFlow != null ? menuFlow.MenuCamera : null;
            if (cam == null || !cam.orthographic)
            {
                return new Vector3(0f, -2.5f, 0f);
            }

            float worldBottom = cam.transform.position.y - cam.orthographicSize;
            Vector3 local = parent.InverseTransformPoint(new Vector3(parent.position.x, worldBottom, parent.position.z));
            return new Vector3(0f, local.y + bottomMargin, 0f);
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
