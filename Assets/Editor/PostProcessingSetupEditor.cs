#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GitVisualizer.Editor
{
    public static class PostProcessingSetupEditor
    {
        private const string MainGameScenePath = "Assets/Scenes/Scene_MainGame.unity";
        private const string ProfilePath = "Assets/Settings/GraphVisualizerPostProcess.asset";

        private const string URPAssetPath = "Assets/Settings/GraphVisualizerURP.asset";

        [MenuItem("Git Visualizer/Setup Post-Processing (Scene_MainGame)")]
        public static void SetupPostProcessing()
        {
            EditorSceneManager.OpenScene(MainGameScenePath, OpenSceneMode.Single);
            SetupPostProcessingInCurrentScene();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Git Visualizer] Post-processing setup complete.");
        }

        public static void SetupPostProcessingInCurrentScene()
        {
            EnsureURPPipeline();
            EnsureGlobalVolume();
            EnsureCameraPostProcessing();
        }

        private static void EnsureURPPipeline()
        {
            if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath) != null)
                return;
            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            var pipeline = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            AssetDatabase.CreateAsset(pipeline, URPAssetPath);
            GraphicsSettings.defaultRenderPipeline = pipeline;
            Debug.Log("[Git Visualizer] Created URP Pipeline Asset.");
        }

        private static void EnsureGlobalVolume()
        {
            var existing = Object.FindFirstObjectByType<Volume>();
            if (existing != null)
            {
                EnsureVolumeProfile(existing);
                return;
            }

            var go = new GameObject("Global Volume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.blendDistance = 0f;
            volume.weight = 1f;
            EnsureVolumeProfile(volume);
            Debug.Log("[Git Visualizer] Created Global Volume.");
        }

        private static void EnsureVolumeProfile(Volume volume)
        {
            if (volume.profile == null)
            {
                if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                    AssetDatabase.CreateFolder("Assets", "Settings");
                volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(volume.profile, ProfilePath);
            }

            var profile = volume.profile;

            if (!profile.TryGet<Bloom>(out var bloom))
            {
                bloom = profile.Add<Bloom>(true);
            }
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);
            bloom.threshold.Override(0.9f);

            if (!profile.TryGet<Vignette>(out var vignette))
            {
                vignette = profile.Add<Vignette>(true);
            }
            vignette.intensity.Override(0.45f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(new Color(0f, 0f, 0f, 1f));

            if (!profile.TryGet<ColorAdjustments>(out var colorGrading))
            {
                colorGrading = profile.Add<ColorAdjustments>(true);
            }
            colorGrading.contrast.Override(15f);
            colorGrading.saturation.Override(5f);
            colorGrading.postExposure.Override(0.1f);

            if (!profile.TryGet<Tonemapping>(out var tonemapping))
            {
                tonemapping = profile.Add<Tonemapping>(true);
            }
            tonemapping.mode.Override(TonemappingMode.ACES);

            EditorUtility.SetDirty(volume);
        }

        private static void EnsureCameraPostProcessing()
        {
            var cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;

            var additionalCameraData = cam.GetComponent<UniversalAdditionalCameraData>();
            if (additionalCameraData == null)
            {
                additionalCameraData = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
            }
            additionalCameraData.renderPostProcessing = true;
            EditorUtility.SetDirty(cam);
        }
    }
}
#endif
