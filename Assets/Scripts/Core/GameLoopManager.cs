using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
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
        [SerializeField] private GameObject _endGamePanel;
        [SerializeField] private TMP_Text _endGameTitleText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Play Again (Host)")]
        [SerializeField] private GraphRenderer _graphRenderer;
        [SerializeField] private string _repoOwner = "ZayaanB";
        [SerializeField] private string _repoName = "Git_Visualizer";
        [SerializeField] private string _repoToken = "";

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
            BindEndGameButtons();
            UpdateTimerDisplay(_remainingTime.Value);
            UpdateProgressDisplay(_resolvedCount.Value);
        }

        private void BindEndGameButtons()
        {
            if (_mainMenuButton != null)
                _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.AddListener(OnPlayAgainClicked);
                _playAgainButton.gameObject.SetActive(IsServer);
            }
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
            if (_endGamePanel != null)
            {
                _endGamePanel.SetActive(true);
                if (_endGameTitleText != null)
                    _endGameTitleText.text = win ? "YOU WIN!" : "GAME OVER";
            }
        }

        private void OnMainMenuClicked()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null)
                nm.Shutdown();
            SceneManager.LoadScene("Scene_MainMenu");
        }

        private void OnPlayAgainClicked()
        {
            if (!IsServer) return;
            RequestPlayAgainServerRpc();
        }

        [ServerRpc]
        private void RequestPlayAgainServerRpc()
        {
            if (!IsServer) return;
            ResetGameState();
            var graph = _graphRenderer != null ? _graphRenderer : FindFirstObjectByType<GraphRenderer>();
            if (graph != null)
            {
                graph.ClearGraph();
                graph.SpawnGraphFromRepo(_repoOwner, _repoName, _repoToken);
            }
            ClearGraphClientRpc();
        }

        [ClientRpc]
        private void ClearGraphClientRpc()
        {
            var graph = _graphRenderer != null ? _graphRenderer : FindFirstObjectByType<GraphRenderer>();
            graph?.ClearGraph();
        }

        private void ResetGameState()
        {
            _gameEnded.Value = false;
            _timerStarted = true;
            _localRemainingTime = TimerDuration;
            _remainingTime.Value = TimerDuration;
            _resolvedCount.Value = 0;
            _warningPlayed = false;

            _conflictNodeIndices.Clear();

            if (_endGamePanel != null)
                _endGamePanel.SetActive(false);
        }

        private void BuildGameLoopUIIfNeeded()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null && _timerText == null)
                canvas = CreateGameLoopCanvas();

            if (_timerText == null || _progressText == null)
                CreateTimerPanel(canvas != null ? canvas.transform : null);

            if (_endGamePanel == null && canvas != null)
                CreateEndGamePanel(canvas.transform);
        }

        private static Canvas CreateGameLoopCanvas()
        {
            var canvasObj = new GameObject("GameLoopCanvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            return canvas;
        }

        private void CreateTimerPanel(Transform canvasParent)
        {
            if (canvasParent == null) return;

            var panel = new GameObject("GameLoopPanel");
            panel.transform.SetParent(canvasParent, false);
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
        }

        private void CreateEndGamePanel(Transform canvasTransform)
        {
            _endGamePanel = new GameObject("EndGamePanel");
            _endGamePanel.transform.SetParent(canvasTransform, false);
            _endGamePanel.SetActive(false);

            var rect = _endGamePanel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var img = _endGamePanel.AddComponent<Image>();
            img.color = new Color(0.05f, 0.08f, 0.12f, 0.92f);

            var layout = _endGamePanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 24;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(40, 40, 40, 40);

            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(_endGamePanel.transform, false);
            _endGameTitleText = titleObj.AddComponent<TextMeshProUGUI>();
            _endGameTitleText.text = "GAME OVER";
            _endGameTitleText.fontSize = 48;
            _endGameTitleText.color = Color.white;
            _endGameTitleText.alignment = TextAlignmentOptions.Center;
            var titleLe = titleObj.AddComponent<LayoutElement>();
            titleLe.preferredHeight = 60;

            var buttonLayout = new GameObject("ButtonLayout");
            buttonLayout.transform.SetParent(_endGamePanel.transform, false);
            var bl = buttonLayout.AddComponent<VerticalLayoutGroup>();
            bl.spacing = 12;
            bl.childAlignment = TextAnchor.MiddleCenter;

            _playAgainButton = CreateEndGameButton(buttonLayout.transform, "Play Again");
            _mainMenuButton = CreateEndGameButton(buttonLayout.transform, "Main Menu");
        }

        private static Button CreateEndGameButton(Transform parent, string label)
        {
            var btnObj = new GameObject($"Button_{label.Replace(" ", "")}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(220, 52);
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredWidth = 220;
            le.preferredHeight = 52;
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.25f, 0.4f, 0.95f);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.color = new Color(0f, 0.95f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
            return btn;
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
    }
}
