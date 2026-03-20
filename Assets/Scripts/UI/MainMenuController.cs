using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace GitVisualizer.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button _playSoloButton;
        [SerializeField] private Button _playCoopButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _learnMoreButton;
        [SerializeField] private Button _exitButton;

        [Header("Overlay Panels")]
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _learnMorePanel;

        [Header("Scene Names")]
        [SerializeField] private string _mainGameSceneName = "Scene_MainGame";

        private void Awake()
        {
            BuildUIIfNeeded();
            BindButtons();
            HideOverlays();
        }

        private void BuildUIIfNeeded()
        {
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                var scaler = GetComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                gameObject.AddComponent<GraphicRaycaster>();
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var es = new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>();
                    es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            if (_playSoloButton == null || _playCoopButton == null || _settingsPanel == null || _learnMorePanel == null)
                BuildMainMenuUI();
        }

        private void BuildMainMenuUI()
        {
            var bgPanel = CreateBackgroundPanel();
            CreateTitle(bgPanel.transform);
            var buttonContainer = CreateButtonContainer(bgPanel.transform);
            CreateButtons(buttonContainer);
            CreateOverlayPanels(bgPanel.transform);
        }

        private void CreateTitle(Transform parent)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);
            var rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.82f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "GIT VISUALIZER";
            tmp.fontSize = 42;
            tmp.color = new Color(0f, 0.95f, 1f, 1f);
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private GameObject CreateBackgroundPanel()
        {
            var panel = new GameObject("BackgroundPanel");
            panel.transform.SetParent(transform, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.15f);
            rect.anchorMax = new Vector2(0.8f, 0.85f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.05f, 0.08f, 0.12f, 0.95f);

            var outline = panel.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.8f, 1f, 0.6f);
            outline.effectDistance = new Vector2(2, 2);

            return panel;
        }

        private GameObject CreateButtonContainer(Transform parent)
        {
            var container = new GameObject("ButtonContainer");
            container.transform.SetParent(parent, false);

            var rect = container.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.3f);
            rect.anchorMax = new Vector2(0.8f, 0.8f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(40, 40, 40, 40);

            return container;
        }

        private void CreateButtons(GameObject container)
        {
            _playSoloButton = CreateMenuButton(container.transform, "Play Solo");
            _playCoopButton = CreateMenuButton(container.transform, "Play Co-op (LAN)");
            _settingsButton = CreateMenuButton(container.transform, "Settings");
            _learnMoreButton = CreateMenuButton(container.transform, "Learn More");
            _exitButton = CreateMenuButton(container.transform, "Exit");
        }

        private Button CreateMenuButton(Transform parent, string label)
        {
            var btnObj = new GameObject($"Button_{label.Replace(" ", "")}");
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 56);

            var layoutElem = btnObj.AddComponent<LayoutElement>();
            layoutElem.minHeight = 56;
            layoutElem.preferredHeight = 56;

            var image = btnObj.AddComponent<Image>();
            image.color = new Color(0.12f, 0.2f, 0.3f, 0.9f);

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = image;
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.35f, 0.5f, 1f);
            colors.pressedColor = new Color(0.08f, 0.15f, 0.25f, 1f);
            btn.colors = colors;

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

        private void CreateOverlayPanels(Transform parent)
        {
            _settingsPanel = CreateOverlayPanel(parent, "SettingsPanel", "Settings",
                "Volume, graphics, and controls.\n(Placeholder - to be implemented)");
            _learnMorePanel = CreateOverlayPanel(parent, "LearnMorePanel", "Learn More",
                "Git Visualizer - Explore GitHub repositories in 3D.\n\nNavigate with right-drag (orbit), WASD (pan), scroll (zoom).\nClick commit nodes to view details.");
        }

        private GameObject CreateOverlayPanel(Transform parent, string name, string title, string body)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.SetActive(false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.2f);
            rect.anchorMax = new Vector2(0.9f, 0.8f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.02f, 0.05f, 0.1f, 0.98f);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperLeft;

            var titleText = CreateTMPLabel(panel.transform, title, 24, true);
            var bodyText = CreateTMPLabel(panel.transform, body, 16, false);
            bodyText.GetComponent<LayoutElement>().flexibleHeight = 1;

            var closeBtn = CreateMenuButton(panel.transform, "Close");
            closeBtn.onClick.AddListener(() => panel.SetActive(false));
            closeBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Close";
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.8f, 0f);
            closeRect.anchorMax = new Vector2(1f, 0.12f);
            closeRect.offsetMin = new Vector2(10, 10);
            closeRect.offsetMax = new Vector2(-10, -10);
            closeBtn.GetComponent<LayoutElement>().ignoreLayout = true;

            return panel;
        }

        private static GameObject CreateTMPLabel(Transform parent, string text, int fontSize, bool isTitle)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = new Color(0.7f, 0.9f, 1f, 1f);
            tmp.enableWordWrapping = true;
            go.AddComponent<LayoutElement>().minHeight = fontSize + 8;
            return go;
        }

        private void BindButtons()
        {
            if (_playSoloButton != null) _playSoloButton.onClick.AddListener(OnPlaySolo);
            if (_playCoopButton != null) _playCoopButton.onClick.AddListener(OnPlayCoop);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettings);
            if (_learnMoreButton != null) _learnMoreButton.onClick.AddListener(OnLearnMore);
            if (_exitButton != null) _exitButton.onClick.AddListener(OnExit);
        }

        private void HideOverlays()
        {
            if (_settingsPanel != null)
                _settingsPanel.SetActive(false);
            if (_learnMorePanel != null)
                _learnMorePanel.SetActive(false);
        }

        private void OnPlaySolo()
        {
            GameStateManager.SetSoloMode();
            SceneManager.LoadScene(_mainGameSceneName);
        }

        private void OnPlayCoop()
        {
            GameStateManager.SetCoopMode();
            SceneManager.LoadScene(_mainGameSceneName);
        }

        private void OnSettings()
        {
            if (_settingsPanel == null) return;
            bool isActive = _settingsPanel.activeSelf;
            if (_learnMorePanel != null && _learnMorePanel.activeSelf)
                _learnMorePanel.SetActive(false);
            _settingsPanel.SetActive(!isActive);
        }

        private void OnLearnMore()
        {
            if (_learnMorePanel == null) return;
            bool isActive = _learnMorePanel.activeSelf;
            if (_settingsPanel != null && _settingsPanel.activeSelf)
                _settingsPanel.SetActive(false);
            _learnMorePanel.SetActive(!isActive);
        }

        private void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
