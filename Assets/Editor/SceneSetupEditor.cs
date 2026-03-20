using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace GitVisualizer.Editor
{
    public static class SceneSetupEditor
    {
        private const string MainMenuScenePath = "Assets/Scenes/Scene_MainMenu.unity";
        private const string MainGameScenePath = "Assets/Scenes/Scene_MainGame.unity";
        private const string OriginalScenePath = "Assets/Scenes/MainScene.unity";

        [MenuItem("Git Visualizer/Setup Networking (Scene_MainGame)")]
        public static void SetupNetworking()
        {
            var scene = EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Single);
            EnsureNetworkManager();
            EnsureNetworkBootstrap();
            EnsureAvatarPrefabAndNetworkObject();
            EnsureGraphRendererWithNetworkObject();
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Git Visualizer] Networking setup complete.");
        }

        [MenuItem("Git Visualizer/Setup Scenes and Build Settings")]
        public static void SetupScenesAndBuildSettings()
        {
            EnsureScene_MainGame();
            EnsureScene_MainMenu();
            UpdateBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Git Visualizer] Scene setup complete. Main Menu: index 0, Main Game: index 1.");
        }

        private static void EnsureScene_MainGame()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainGameScenePath) != null)
                return;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(OriginalScenePath) == null)
            {
                Debug.LogWarning($"[Git Visualizer] {OriginalScenePath} not found. Create Scene_MainGame manually.");
                return;
            }

            AssetDatabase.CopyAsset(OriginalScenePath, MainGameScenePath);
            Debug.Log($"[Git Visualizer] Created {MainGameScenePath}");
        }

        private static void EnsureScene_MainMenu()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath) != null)
                return;

            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 1, -10);
            }

            var canvasObj = new GameObject("MainMenuCanvas");
            var rect = canvasObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var scaler = canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            canvasObj.AddComponent<GitVisualizer.UI.MainMenuController>();

            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esObj = new GameObject("EventSystem");
                esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), MainMenuScenePath);
            Debug.Log($"[Git Visualizer] Created {MainMenuScenePath}");
        }

        private static void UpdateBuildSettings()
        {
            var scenes = new[] {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(MainGameScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        private static void EnsureNetworkManager()
        {
            var nm = Object.FindObjectOfType<NetworkManager>();
            if (nm != null)
            {
                if (nm.GetComponent<UnityTransport>() == null)
                    nm.gameObject.AddComponent<UnityTransport>();
                return;
            }

            var go = new GameObject("NetworkManager");
            go.AddComponent<NetworkManager>();
            go.AddComponent<UnityTransport>();
            Debug.Log("[Git Visualizer] Added NetworkManager with UnityTransport.");
        }

        private static void EnsureNetworkBootstrap()
        {
            if (Object.FindObjectOfType<GitVisualizer.NetworkBootstrap>() != null) return;

            var go = new GameObject("NetworkBootstrap");
            go.AddComponent<GitVisualizer.NetworkBootstrap>();
            Debug.Log("[Git Visualizer] Added NetworkBootstrap.");
        }

        private const string AvatarPrefabPath = "Assets/Prefabs/AvatarPlayer.prefab";

        private static void EnsureAvatarPrefabAndNetworkObject()
        {
            var sceneAvatar = GameObject.Find("Avatar");
            if (sceneAvatar == null)
            {
                Debug.LogWarning("[Git Visualizer] Scene Avatar not found.");
                return;
            }

            if (sceneAvatar.GetComponent<NetworkObject>() == null)
                sceneAvatar.AddComponent<NetworkObject>();

            var nm = Object.FindObjectOfType<NetworkManager>();
            if (nm == null) return;

            GameObject prefab;
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                prefab = PrefabUtility.SaveAsPrefabAsset(sceneAvatar, AvatarPrefabPath);
                Debug.Log($"[Git Visualizer] Created Avatar prefab at {AvatarPrefabPath}");
            }
            else
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabPath);
            }

            var prefabNo = prefab.GetComponent<NetworkObject>();
            if (prefabNo == null)
            {
                var prefabRoot = PrefabUtility.LoadPrefabContents(AvatarPrefabPath);
                if (prefabRoot.GetComponent<NetworkObject>() == null)
                    prefabRoot.AddComponent<NetworkObject>();
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, AvatarPrefabPath);
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AvatarPrefabPath);
            }

            nm.AddNetworkPrefab(prefab);
            nm.NetworkConfig.PlayerPrefab = prefab;
            EditorUtility.SetDirty(nm);
            Debug.Log("[Git Visualizer] Assigned Avatar prefab to NetworkManager PlayerPrefab.");
        }

        private static void EnsureGraphRendererWithNetworkObject()
        {
            var graphRenderer = Object.FindObjectOfType<GitVisualizer.Core.GraphRenderer>();
            if (graphRenderer == null)
            {
                var go = new GameObject("GraphRenderer");
                graphRenderer = go.AddComponent<GitVisualizer.Core.GraphRenderer>();
                Debug.Log("[Git Visualizer] Created GraphRenderer GameObject.");
            }
            if (graphRenderer.GetComponent<NetworkObject>() == null)
            {
                graphRenderer.gameObject.AddComponent<NetworkObject>();
                Debug.Log("[Git Visualizer] Added NetworkObject to GraphRenderer.");
            }
        }
    }
}
