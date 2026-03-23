using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;
using GitVisualizer.Core;

namespace GitVisualizer
{
    [RequireComponent(typeof(NetworkObject))]
    public class GameLoopManager : NetworkBehaviour
    {
        public static GameLoopManager Instance { get; private set; }

        private const float TimerDuration = 300f; // 5 minutes
        private const int ConflictCount = 5;

        [Header("UI")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private GameObject _gameOverPanel;

        [Header("Audio")]
        [SerializeField] private AudioClip _resolveSuccessClip;
        [SerializeField] private AudioClip _timerWarningClip;

        private NetworkVariable<float> _remainingTime = new NetworkVariable<float>(TimerDuration);
        private NetworkVariable<int> _resolvedCount = new NetworkVariable<int>(0);
        private NetworkList<int> _conflictNodeIndices;
        private NetworkVariable<bool> _gameEnded = new NetworkVariable<bool>(false);

        private bool _timerStarted;
        private float _localRemainingTime;
        private bool _warningPlayed;

        public int ResolvedCount => _resolvedCount.Value;
        public bool IsConflictNode(int globalIndex)
        {
            for (int i = 0; i < _conflictNodeIndices.Count; i++)
                if (_conflictNodeIndices[i] == globalIndex) return true;
            return false;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _conflictNodeIndices = new NetworkList<int>();

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _remainingTime.OnValueChanged += OnTimerChanged;
            _resolvedCount.OnValueChanged += OnProgressChanged;
            _conflictNodeIndices.OnListChanged += OnConflictListChanged;

            if (IsServer)
            {
                _remainingTime.Value = TimerDuration;
                _localRemainingTime = TimerDuration;
                _resolvedCount.Value = 0;
                _gameEnded.Value = false;
                _timerStarted = true;
                GraphRenderer.OnGraphReady += OnGraphReady;
            }

            BuildGameLoopUIIfNeeded();
            UpdateTimerDisplay(_remainingTime.Value);
            UpdateProgressDisplay(_resolvedCount.Value);
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();
            _remainingTime.OnValueChanged -= OnTimerChanged;
            _resolvedCount.OnValueChanged -= OnProgressChanged;
            if (_conflictNodeIndices != null)
            {
                _conflictNodeIndices.OnListChanged -= OnConflictListChanged;
                _conflictNodeIndices.Dispose();
            }

            if (IsServer)
                GraphRenderer.OnGraphReady -= OnGraphReady;

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (!IsServer || !_timerStarted || _gameEnded.Value) return;

            _localRemainingTime -= Time.deltaTime;
            if (_localRemainingTime <= 0f)
            {
                _remainingTime.Value = 0f;
                EndGame(false);
                return;
            }
            int prevDisplaySec = Mathf.FloorToInt(_remainingTime.Value);
            int displaySec = Mathf.FloorToInt(_localRemainingTime);
            if (displaySec != prevDisplaySec)
                _remainingTime.Value = _localRemainingTime;
        }

        private void OnGraphReady(int totalNodeCount)
        {
            if (!IsServer || _conflictNodeIndices.Count > 0) return;
            if (totalNodeCount < ConflictCount)
            {
                Debug.LogWarning($"[GameLoopManager] Not enough nodes ({totalNodeCount}) for {ConflictCount} conflicts.");
                return;
            }

            var indices = new List<int>();
            for (int i = 0; i < totalNodeCount; i++)
                indices.Add(i);

            var rng = new System.Random();
            for (int i = 0; i < ConflictCount; i++)
            {
                int idx = rng.Next(indices.Count);
                _conflictNodeIndices.Add(indices[idx]);
                indices.RemoveAt(idx);
            }

            NotifyConflictNodesChangedClientRpc();
        }

        [ClientRpc]
        private void NotifyConflictNodesChangedClientRpc()
        {
            foreach (var node in FindObjectsByType<NodeInteractable>())
                node.RefreshConflictState();
        }

        [ServerRpc(RequireOwnership = false)]
        public void ResolveConflictServerRpc(int nodeIndex)
        {
            if (_gameEnded.Value) return;
            if (!_conflictNodeIndices.Contains(nodeIndex)) return;

            _conflictNodeIndices.Remove(nodeIndex);
            _resolvedCount.Value += 1;

            MarkNodeResolvedClientRpc(nodeIndex);

            if (_resolvedCount.Value >= ConflictCount)
                EndGame(true);
        }

        [ClientRpc]
        private void MarkNodeResolvedClientRpc(int nodeIndex)
        {
            if (_resolveSuccessClip != null)
                AudioManager.Instance?.PlaySFX(_resolveSuccessClip);
            foreach (var node in FindObjectsByType<NodeInteractable>())
            {
                if (node.GlobalIndex == nodeIndex)
                {
                    node.SetResolved();
                    break;
                }
            }
        }

        private void EndGame(bool win)
        {
            _gameEnded.Value = true;
            ShowEndGameClientRpc(win);
        }

        [ClientRpc]
        private void ShowEndGameClientRpc(bool win)
        {
            ShowEndGameUI(win);
        }

        private void OnTimerChanged(float prev, float current)
        {
            if (!_warningPlayed && prev > 10f && current <= 10f && _timerWarningClip != null)
            {
                _warningPlayed = true;
                AudioManager.Instance?.PlaySFX(_timerWarningClip);
            }
            UpdateTimerDisplay(current);
        }

        private void OnProgressChanged(int prev, int current)
        {
            UpdateProgressDisplay(current);
        }

        private void OnConflictListChanged(NetworkListEvent<int> change)
        {
            foreach (var node in FindObjectsByType<NodeInteractable>())
                node.RefreshConflictState();
        }

        private void UpdateTimerDisplay(float seconds)
        {
            if (_timerText == null) return;
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            _timerText.text = $"{m:D2}:{s:D2}";
        }

        private void UpdateProgressDisplay(int resolved)
        {
            if (_progressText == null) return;
            _progressText.text = $"Conflicts: {resolved}/{ConflictCount}";
        }

        private void ShowEndGameUI(bool win)
        {
            if (_winPanel != null) _winPanel.SetActive(win);
            if (_gameOverPanel != null) _gameOverPanel.SetActive(!win);
        }

        private void BuildGameLoopUIIfNeeded()
        {
            if (_timerText != null && _progressText != null) return;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("GameLoopCanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }
            var panel = new GameObject("GameLoopPanel");
            panel.transform.SetParent(canvas.transform, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -20);
            rect.sizeDelta = new Vector2(200, 80);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childAlignment = TextAnchor.MiddleCenter;

            _timerText = CreateTMPLabel(panel.transform, "05:00", 32);
            _progressText = CreateTMPLabel(panel.transform, $"Conflicts: 0/{ConflictCount}", 18);

            _winPanel = CreateEndPanel(canvas.transform, "YOU WIN!", new Color(0.2f, 0.8f, 0.3f));
            _gameOverPanel = CreateEndPanel(canvas.transform, "GAME OVER", new Color(0.9f, 0.2f, 0.2f));
        }

        private static TMP_Text CreateTMPLabel(Transform parent, string text, int fontSize)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static GameObject CreateEndPanel(Transform parent, string title, Color bgColor)
        {
            var panel = new GameObject("EndPanel");
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var img = panel.AddComponent<Image>();
            img.color = new Color(bgColor.r, bgColor.g, bgColor.b, 0.9f);

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(panel.transform, false);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.2f, 0.4f);
            titleRect.anchorMax = new Vector2(0.8f, 0.6f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            titleTmp.fontSize = 48;
            titleTmp.color = Color.white;
            titleTmp.alignment = TextAlignmentOptions.Center;

            return panel;
        }
    }
}
