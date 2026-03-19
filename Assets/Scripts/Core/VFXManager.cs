using System.Collections;
using UnityEngine;

namespace GitVisualizer.Core
{
    /// <summary>
    /// Handles visual polish: spawn animations, particle effects, and other VFX.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Spawn Animation")]
        [SerializeField]
        private float _spawnDuration = 0.4f;

        [SerializeField]
        private float _elasticOvershoot = 1.2f;

        [SerializeField]
        private float _elasticPeriod = 0.3f;

        [SerializeField]
        private float _staggerDelay = 0.02f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Spawns a node with elastic scale-up animation. Node should start at scale zero.
        /// </summary>
        /// <param name="node">The node transform to animate.</param>
        /// <param name="targetScale">Final scale (e.g. Vector3.one * 0.3).</param>
        /// <param name="delay">Optional delay before starting.</param>
        public void PlaySpawnAnimation(Transform node, Vector3 targetScale, float delay = 0f)
        {
            if (node == null)
                return;

            StartCoroutine(SpawnAnimationCoroutine(node, targetScale, delay));
        }

        /// <summary>
        /// Spawns multiple nodes with staggered elastic animation.
        /// </summary>
        public void PlaySpawnAnimationStaggered(Transform[] nodes, Vector3 targetScale)
        {
            if (nodes == null)
                return;

            for (int i = 0; i < nodes.Length; i++)
            {
                PlaySpawnAnimation(nodes[i], targetScale, i * _staggerDelay);
            }
        }

        private IEnumerator SpawnAnimationCoroutine(Transform node, Vector3 targetScale, float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            node.localScale = Vector3.zero;
            float elapsed = 0f;

            while (elapsed < _spawnDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _spawnDuration);

                float scale = ElasticOut(t, _elasticOvershoot, _elasticPeriod);
                node.localScale = targetScale * scale;

                yield return null;
            }

            node.localScale = targetScale;
        }

        /// <summary>
        /// Elastic ease-out: overshoots then settles. t in [0,1], returns ~1 at t=1.
        /// </summary>
        private static float ElasticOut(float t, float overshoot, float period)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;

            float bounce = Mathf.Sin(t * Mathf.PI);
            return t + (overshoot - 0.5f) * bounce;
        }
    }
}
