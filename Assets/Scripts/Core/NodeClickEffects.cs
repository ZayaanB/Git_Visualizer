using UnityEngine;

namespace GitVisualizer.Core
{
    [RequireComponent(typeof(Collider))]
    public class NodeClickEffects : MonoBehaviour
    {
        private static Material s_sharedParticleMaterial;

        [SerializeField] private ParticleSystem _clickParticles;
        [SerializeField] private int _burstCount = 12;
        [SerializeField] private float _particleLifetime = 0.5f;

        private void Awake()
        {
            if (_clickParticles == null)
                CreateDefaultParticleSystem();
        }

        public void PlayClickEffect()
        {
            if (_clickParticles == null)
                CreateDefaultParticleSystem();
            _clickParticles?.Emit(_burstCount);
        }

        private void CreateDefaultParticleSystem()
        {
            var psObj = new GameObject("ClickParticles");
            psObj.transform.SetParent(transform);
            psObj.transform.localPosition = Vector3.zero;
            psObj.transform.localScale = Vector3.one;

            _clickParticles = psObj.AddComponent<ParticleSystem>();
            var main = _clickParticles.main;
            main.duration = 0.1f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = _particleLifetime;
            main.startSpeed = 0.5f;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 0.9f, 0.6f, 0.8f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;

            var emission = _clickParticles.emission;
            emission.enabled = false;

            var shape = _clickParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.2f;

            var psRenderer = psObj.GetComponent<ParticleSystemRenderer>();
            psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (s_sharedParticleMaterial == null)
            {
                var shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
                s_sharedParticleMaterial = shader != null ? new Material(shader) { color = new Color(1f, 0.95f, 0.7f, 0.9f) } : null;
            }
            if (s_sharedParticleMaterial != null)
                psRenderer.sharedMaterial = s_sharedParticleMaterial;
        }
    }
}
