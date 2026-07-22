using SpieleMarmelade.Shared;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.DevTools.Editor
{
    // One-click repair for the Brickrot player after the AssetJam port churn.
    //
    // The shared Player_TopDownFree prefab is a complete top-down actor (CharacterController +
    // PlayerInputReader + TopDownFreeMovement + PlayerController). Two things break it in Brickrot:
    //   1. PlayerController got removed from the scene instance → nothing ticks TopDownFreeMovement,
    //      so WASD does nothing at all.
    //   2. A leftover Rigidbody (from the now-deleted ported movement controller's RequireComponent)
    //      sits on the player next to the CharacterController and fights it.
    // This restores the first and strips the second so the player just moves.
    [InitializeOnLoad]
    public static class BrickrotPlayerRepair
    {
        static BrickrotPlayerRepair() { }

        [MenuItem("Tools/Game Creation/Repair Brickrot Player (open scene)")]
        public static void Repair()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Player reparieren", "Keine Szene geöffnet.", "OK");
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Player reparieren",
                    "Kein Objekt mit Tag 'Player' in der Szene gefunden. Zieh Player_TopDownFree " +
                    "aus Shared/Prefabs/Player/ hinein und versuch es erneut.", "OK");
                return;
            }

            int changes = 0;

            // 1. Restore PlayerController (the movement orchestrator).
            if (player.GetComponent<PlayerController>() == null)
            {
                Undo.AddComponent<PlayerController>(player);
                changes++;
            }

            // 2. Strip leftover Rigidbodies from the player and its children — they conflict with
            //    the CharacterController that actually drives movement.
            foreach (Rigidbody rb in player.GetComponentsInChildren<Rigidbody>(true))
            {
                Undo.DestroyObjectImmediate(rb);
                changes++;
            }

            // 3. Sanity: the prefab should already carry these, but say so if it somehow doesn't.
            string missing = "";
            if (player.GetComponent<CharacterController>() == null) missing += "\n  • CharacterController";
            if (player.GetComponent<PlayerInputReader>() == null) missing += "\n  • PlayerInputReader";
            if (player.GetComponent<IPlayerMovement>() == null) missing += "\n  • ein IPlayerMovement (z.B. TopDownFreeMovement)";

            EditorSceneManager.MarkSceneDirty(scene);
            EditorUtility.SetDirty(player);

            string msg = changes == 0
                ? $"'{player.name}' sah schon in Ordnung aus — nichts geändert."
                : $"'{player.name}' repariert ({changes} Änderung(en)): PlayerController sichergestellt, " +
                  "störende Rigidbodies entfernt.";

            if (missing.Length > 0)
            {
                msg += "\n\nEs fehlen aber noch Komponenten, die das Prefab eigentlich mitbringt:" + missing +
                       "\n\nAm sichersten: Player löschen und Player_TopDownFree frisch reinziehen.";
            }
            else
            {
                msg += "\n\nNoch speichern (Strg+S) und Play drücken.";
            }

            EditorUtility.DisplayDialog("Player reparieren", msg, "OK");
            Selection.activeGameObject = player;
        }
    }
}
