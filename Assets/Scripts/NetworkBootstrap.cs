using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using GitVisualizer.Core;

namespace GitVisualizer
{
    public class NetworkBootstrap : MonoBehaviour
    {
        [Header("Join UI (Co-op Client)")]
        [SerializeField] private GameObject _joinPanel;
        [SerializeField] private TMP_InputField _ipInputField;
        [SerializeField] private Button _joinButton;
        [SerializeField] private ushort _port = 7777;

        [Header("Co-op Mode UI")]
        [SerializeField] private GameObject _coopModePanel;
        [SerializeField] private Button _hostButton;

        [Header("Scene References")]
        [SerializeField] private GameObject _sceneAvatar;
        [SerializeField] private OrbitCamera _orbitCamera;

        private NetworkManager _networkManager;

        private void Awake()
        {
            _networkManager = NetworkManager.Singleton;
            ResolveSceneReferences();

            if (_networkManager == null)
            {
                Debug.LogWarning("[NetworkBootstrap] NetworkManager not found. Run Git Visualizer > Setup Networking (Scene_MainGame) in the Editor.");
                HideCoopUI();
                EnsureSoloAvatar();
                return;
            }

            if (!GameStateManager.IsCoopMode)
            {
                HideCoopUI();
                EnsureSoloAvatar();
                return;
            }

            BuildCoopUIIfNeeded();
            ShowCoopUI();
            BindCoopButtons();
        }

        private void ResolveSceneReferences()
        {
            if (_sceneAvatar == null)
                _sceneAvatar = GameObject.Find("Avatar");
            if (_orbitCamera == null)
                _orbitCamera = FindFirstObjectByType<OrbitCamera>();
        }

        private void Start()
        {
            if (GameStateManager.IsCoopMode && _networkManager != null)
            {
                _networkManager.OnClientConnectedCallback += OnClientConnected;
                _networkManager.OnServerStarted += OnServerStarted;
            }
        }

        private void OnDestroy()
        {
            if (_networkManager != null)
            {
                _networkManager.OnClientConnectedCallback -= OnClientConnected;
                _networkManager.OnServerStarted -= OnServerStarted;
            }
        }

        private void HideCoopUI()
        {
            if (_coopModePanel != null) _coopModePanel.SetActive(false);
            if (_joinPanel != null) _joinPanel.SetActive(false);
        }

        private void ShowCoopUI()
        {
            if (_coopModePanel != null) _coopModePanel.SetActive(true);
        }

        private void BindCoopButtons()
        {
            if (_hostButton != null)
                _hostButton.onClick.AddListener(OnHostClicked);
            if (_joinButton != null)
                _joinButton.onClick.AddListener(OnJoinClicked);
        }

        private void OnHostClicked()
        {
            if (_networkManager == null) return;
            if (_coopModePanel != null) _coopModePanel.SetActive(false);
            if (_sceneAvatar != null) _sceneAvatar.SetActive(false);
            _networkManager.StartHost();
        }

        private void OnJoinClicked()
        {
            if (_networkManager == null) return;

            var ip = _ipInputField != null ? _ipInputField.text.Trim() : "";
            if (string.IsNullOrEmpty(ip))
            {
                Debug.LogWarning("[NetworkBootstrap] Please enter the Host's IP address.");
                return;
            }

            var transport = _networkManager.GetComponent<UnityTransport>();
            if (transport != null)
                transport.SetConnectionData(ip, _port);

            if (_coopModePanel != null) _coopModePanel.SetActive(false);
            if (_sceneAvatar != null) _sceneAvatar.SetActive(false);
            _networkManager.StartClient();
        }

        private void OnServerStarted()
        {
            HideCoopUI();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (clientId == _networkManager.LocalClientId)
                HideCoopUI();
        }

        private void EnsureSoloAvatar()
        {
            if (_sceneAvatar != null)
                _sceneAvatar.SetActive(true);
        }

        public void BuildCoopUIIfNeeded()
        {
            if (_coopModePanel != null && _joinPanel != null) return;

            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("CoopUICanvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>().uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<UnityEngine.UI.CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var es = new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>();
                    es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            if (_coopModePanel == null)
                _coopModePanel = CreateCoopPanel(canvas.transform);
            if (_joinPanel == null)
                _joinPanel = CreateJoinPanel(_coopModePanel.transform);
        }

        private GameObject CreateCoopPanel(Transform parent)
        {
            var panel = new GameObject("CoopModePanel");
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.25f, 0.3f);
            rect.anchorMax = new Vector2(0.75f, 0.7f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 20;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var title = new GameObject("Title");
            title.transform.SetParent(panel.transform, false);
            var titleTmp = title.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CO-OP (LAN)";
            titleTmp.fontSize = 28;
            titleTmp.color = new Color(0f, 0.95f, 1f, 1f);
            titleTmp.alignment = TextAlignmentOptions.Center;

            _hostButton = CreateButton(panel.transform, "Host Game");
            return panel;
        }

        private GameObject CreateJoinPanel(Transform parent)
        {
            var panel = new GameObject("JoinPanel");
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.2f);
            rect.anchorMax = new Vector2(0.8f, 0.5f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleCenter;

            var label = new GameObject("Label");
            label.transform.SetParent(panel.transform, false);
            var labelTmp = label.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "Host IP Address:";
            labelTmp.fontSize = 18;
            labelTmp.color = Color.white;

            var inputObj = new GameObject("IPInput");
            inputObj.transform.SetParent(panel.transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(300, 40);
            inputObj.AddComponent<LayoutElement>().preferredHeight = 40;
            inputObj.AddComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            _ipInputField = inputObj.AddComponent<TMP_InputField>();

            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 6);
            textAreaRect.offsetMax = new Vector2(-10, -6);

            var placeholder = new GameObject("Placeholder");
            placeholder.transform.SetParent(textArea.transform, false);
            var placeholderRect = placeholder.AddComponent<RectTransform>();
            placeholderRect.anchorMin = placeholderRect.anchorMax = Vector2.zero;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = new Vector2(280, 28);
            var placeholderTmp = placeholder.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "192.168.1.100";
            placeholderTmp.fontSize = 16;
            placeholderTmp.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            _ipInputField.placeholder = placeholderTmp;

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(textArea.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = new Vector2(280, 28);
            var textTmp = textObj.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 16;
            textTmp.color = Color.white;
            _ipInputField.textComponent = textTmp;

            _joinButton = CreateButton(panel.transform, "Join Game");
            return panel;
        }

        private static Button CreateButton(Transform parent, string label)
        {
            var btnObj = new GameObject($"Button_{label.Replace(" ", "")}");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 48);
            var img = btnObj.AddComponent<Image>();
            img.color = new Color(0.12f, 0.2f, 0.3f, 0.9f);
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            var textObj = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            textObj.transform.SetParent(btnObj.transform, false);
            textObj.text = label;
            textObj.fontSize = 20;
            textObj.color = new Color(0f, 0.95f, 1f, 1f);
            textObj.alignment = TextAlignmentOptions.Center;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
