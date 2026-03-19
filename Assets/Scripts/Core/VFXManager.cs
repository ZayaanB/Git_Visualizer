using System.Collections;
using UnityEngine;

namespace GitVisualizer.Core
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Spawn Animation")]
        [SerializeField] private float _spawnDuration = 0.4f;
        [SerializeField] private float _elasticOvershoot = 1.2f;
        [SerializeField] private float _staggerDelay = 0.02f;

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

        public void PlaySpawnAnimation(Transform node, Vector3 targetScale, float delay = 0f)
        {
            if (node == null) return;
            StartCoroutine(SpawnAnimationCoroutine(node, targetScale, delay));
        }

        public void PlaySpawnAnimationStaggered(Transform[] nodes, Vector3 targetScale)
        {
            if (nodes == null) return;
            for (int i = 0; i < nodes.Length; i++)
                PlaySpawnAnimation(nodes[i], targetScale, i * _staggerDelay);
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
                float scale = ElasticOut(t, _elasticOvershoot);
                node.localScale = targetScale * scale;
                yield return null;
            }

            node.localScale = targetScale;
        }

        // Bounce overshoot: t + overshoot * sin(t*pi)
        private static float ElasticOut(float t, float overshoot)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return t + (overshoot - 0.5f) * Mathf.Sin(t * Mathf.PI);
        }
    }
}
