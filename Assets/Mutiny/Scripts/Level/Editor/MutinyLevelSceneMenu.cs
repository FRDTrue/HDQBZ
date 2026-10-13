using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mutiny.Levels.Editor
{
    public static class MutinyLevelSceneMenu
    {
        [MenuItem("Mutiny/Levels/Load Level 1 to Active Scene")]
        public static void LoadLevel1ToScene()
        {
            try
            {
                ClearLoadedLevel();

                string xmlPath = Path.Combine(Application.dataPath, "Mutiny", "Data", "Levels", "level_01.xml");
                if (!File.Exists(xmlPath))
                {
                    Debug.LogError($"[MutinyLevelSceneMenu] level_01.xml not found at: {xmlPath}");
                    return;
                }

                string xmlContent = File.ReadAllText(xmlPath);
                MutinyLevelData levelData = MutinyLevelXmlParser.Parse(xmlContent, "level_01.xml");

                // Keep the controller on a stable host. The generated level is its child,
                // so restart/next-level can safely destroy and replace that child.
                GameObject host = new GameObject("MutinyGame");
                var controller = host.AddComponent<MutinyLevelController>();
                controller.CurrentLevelIndex = 1;
                string relAssetPath = "Assets/Mutiny/Data/Levels/level_01.xml";
                controller.LevelXml = AssetDatabase.LoadAssetAtPath<TextAsset>(relAssetPath);
                controller.BuildLevel();
                if (controller.CurrentLevel != null)
                    controller.CurrentLevel.gameObject.name = "Level_01";
                Undo.RegisterCreatedObjectUndo(host, "Load Level 1");

                // Configure Camera
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Undo.RecordObject(cam, "Configure Camera for Level 1");
                    Undo.RecordObject(cam.transform, "Position Camera for Level 1");

                    cam.orthographic = true;
                    // Flash viewport: 550x400 at 32 PPU -> 12.5 height -> orthographicSize = 6.25
                    cam.orthographicSize = 6.25f;
                    cam.transform.position = new Vector3(8.6f, -6.25f, -10f);
                    cam.backgroundColor = new Color(0.38f, 0.65f, 0.88f); // Flash Sky Blue
                }

                Selection.activeGameObject = host;
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }

                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                Debug.Log($"[MutinyLevelSceneMenu] Successfully loaded Level 1: {levelData.Width}x{levelData.Height}, {levelData.Objects.Count} objects into scene.");
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MenuItem("Mutiny/Levels/Clear Loaded Level")]
        public static void ClearLoadedLevel()
        {
            var existingRoots = UnityEngine.Object.FindObjectsByType<MutinyLevelRoot>(FindObjectsSortMode.None);
            foreach (var root in existingRoots)
            {
                Undo.DestroyObjectImmediate(root.gameObject);
            }

            GameObject level01 = GameObject.Find("Level_01");
            if (level01 != null)
            {
                Undo.DestroyObjectImmediate(level01);
            }

            GameObject host = GameObject.Find("MutinyGame");
            if (host != null)
            {
                Undo.DestroyObjectImmediate(host);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
    }
}
