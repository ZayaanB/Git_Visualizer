using System.Collections.Generic;
using UnityEngine;

namespace GitVisualizer.Core
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const int SfxPoolSize = 8;

        [SerializeField] private AudioSource _bgmSource;
        private AudioSource[] _sfxPool;
        private int _sfxIndex;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (_bgmSource == null)
            {
                _bgmSource = gameObject.AddComponent<AudioSource>();
                _bgmSource.loop = true;
                _bgmSource.playOnAwake = false;
            }

            _sfxPool = new AudioSource[SfxPoolSize];
            for (int i = 0; i < SfxPoolSize; i++)
            {
                var sfx = gameObject.AddComponent<AudioSource>();
                sfx.loop = false;
                sfx.playOnAwake = false;
                _sfxPool[i] = sfx;
            }
        }

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null || _bgmSource == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;
            _bgmSource.clip = clip;
            _bgmSource.Play();
        }

        public void PlaySFX(AudioClip clip)
        {
            if (clip == null) return;
            var source = _sfxPool[_sfxIndex];
            _sfxIndex = (_sfxIndex + 1) % SfxPoolSize;
            source.PlayOneShot(clip);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
