using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpieleMarmelade.DevTools.Editor
{
    // Every minigame created from _Template inherits the template's demo UI: a world-space Canvas
    // holding StatusText / WinButton / LoseButton, which the template controller used to exercise
    // the Hub round trip. As soon as that controller is replaced with real gameplay the objects are
    // dead weight — they still render in the Scene view as two white quads, which reliably confuses
    // whoever opens the scene next.
    //
    // Deleting them by hand in the YAML is a bad idea (SceneRoots entries, component references,
    // and the scene may be open in the editor), so this does it through the normal object API.
    public static class TemplateDemoUiCleaner
    {
        private static readonly string[] DemoChildNames = { "StatusText", "WinButton", "LoseButton" };

        [MenuItem("Tools/Game Creation/Remove Template Demo UI (open scene)")]
        public static void RemoveFromOpenScene()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                EditorUtility.DisplayDialog("Demo-UI entfernen", "Keine Szene geöffnet.", "OK");
                return;
            }

            List<GameObject> targets = FindDemoCanvases(scene);

            if (targets.Count == 0)
            {
                EditorUtility.DisplayDialog("Demo-UI entfernen",
                    "In dieser Szene wurde kein Template-Demo-Canvas gefunden.\n\n" +
                    "Gesucht wird ein Canvas mit den Kindern StatusText, WinButton und LoseButton. " +
                    "MenuCanvas und alles andere bleibt unangetastet.", "OK");
                return;
            }

            string names = string.Join("\n", targets.Select(t => "  • " + PathOf(t)));
            if (!EditorUtility.DisplayDialog("Demo-UI entfernen",
                    $"Folgende Objekte werden gelöscht:\n\n{names}\n\nFortfahren?", "Löschen", "Abbrechen"))
            {
                return;
            }

            foreach (GameObject target in targets)
            {
                Undo.DestroyObjectImmediate(target);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[TemplateDemoUiCleaner] {targets.Count} Demo-Canvas entfernt aus '{scene.name}'. " +
                      "Szene noch speichern (Strg+S).");
        }

        // Matched by shape rather than by name alone: a Canvas whose children include all three
        // template demo objects. That way a Canvas someone added for real UI — and MenuCanvas in
        // particular — can never be caught by accident.
        private static List<GameObject> FindDemoCanvases(Scene scene)
        {
            var result = new List<GameObject>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                {
                    var childNames = new HashSet<string>();
                    foreach (Transform child in canvas.transform)
                    {
                        childNames.Add(child.name);
                    }

                    if (DemoChildNames.All(childNames.Contains))
                    {
                        result.Add(canvas.gameObject);
                    }
                }
            }

            return result;
        }

        private static string PathOf(GameObject go)
        {
            string path = go.name;
            Transform t = go.transform.parent;
            while (t != null)
            {
                path = t.name + "/" + path;
                t = t.parent;
            }
            return path;
        }
    }
}
