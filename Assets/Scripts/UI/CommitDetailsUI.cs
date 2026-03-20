using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GitVisualizer.Models;

namespace GitVisualizer.UI
{
    public class CommitDetailsUI : MonoBehaviour
    {
        public static CommitDetailsUI Instance { get; private set; }

        [Header("References")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _commitMessageText;
        [SerializeField] private TMP_Text _authorText;
        [SerializeField] private TMP_Text _dateText;

        [Header("Tween")]
        [SerializeField] private float _tweenDuration = 0.25f;
        [SerializeField] private AnimationCurve _tweenCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private Vector2 _hiddenAnchorMin = new Vector2(1.1f, 0.5f);
        [SerializeField] private Vector2 _visibleAnchorMin = new Vector2(0.75f, 0.5f);
        [SerializeField] private Vector2 _anchorMax = new Vector2(1f, 0.7f);

        private CanvasGroup _canvasGroup;
        private Coroutine _tweenCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_panel == null)
                BuildUIIfNeeded();

            if (_panel != null)
            {
                _canvasGroup = _panel.GetComponent<CanvasGroup>() ?? _panel.gameObject.AddComponent<CanvasGroup>();
                _panel.anchorMin = _hiddenAnchorMin;
                _panel.anchorMax = _anchorMax;
                _panel.pivot = new Vector2(1f, 0.5f);
                _canvasGroup.alpha = 0f;
                _panel.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void BuildUIIfNeeded()
        {
            if (GetComponent<Canvas>() == null)
            {
                gameObject.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                gameObject.AddComponent<CanvasScaler>();
                gameObject.AddComponent<GraphicRaycaster>();
                if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
                {
                    var es = new GameObject("EventSystem").AddComponent<UnityEngine.EventSystems.EventSystem>();
                    es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }
            }

            _panel = new GameObject("CommitDetailsPanel").AddComponent<RectTransform>();
            _panel.SetParent(transform, false);
            _panel.anchorMin = _hiddenAnchorMin;
            _panel.anchorMax = _anchorMax;
            _panel.pivot = new Vector2(1f, 0.5f);
            _panel.anchoredPosition = Vector2.zero;
            _panel.sizeDelta = Vector2.zero;

            _panel.gameObject.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            var layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _commitMessageText = CreateTMPLabel(_panel, "Commit Message", 18, true);
            _authorText = CreateTMPLabel(_panel, "Author", 14, false);
            _dateText = CreateTMPLabel(_panel, "Date", 14, false);

            var closeBtn = new GameObject("CloseButton").AddComponent<Button>();
            closeBtn.transform.SetParent(_panel, false);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
            closeRect.pivot = new Vector2(1f, 1f);
            closeRect.anchoredPosition = new Vector2(-8, -8);
            closeRect.sizeDelta = new Vector2(32, 32);
            closeBtn.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            closeBtn.gameObject.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var closeText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            closeText.transform.SetParent(closeBtn.transform, false);
            closeText.text = "×";
            closeText.fontSize = 24;
            closeText.color = Color.white;
            closeText.alignment = TextAlignmentOptions.Center;
            var textRect = closeText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            closeBtn.onClick.AddListener(Hide);
        }

        private static TMP_Text CreateTMPLabel(Transform parent, string label, int fontSize, bool isTitle)
        {
            var go = new GameObject(label);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = Color.white;
            text.text = "-";
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            var layout = go.AddComponent<LayoutElement>();
            layout.minHeight = fontSize + 8;
            layout.preferredHeight = isTitle ? 60 : 24;
            return text;
        }

        public void ShowCommit(Commit commit)
        {
            if (commit == null) return;

            _commitMessageText.text = Truncate(commit.commit?.message?.Trim() ?? "(No message)", 200);
            _authorText.text = commit.commit?.author?.name ?? commit.author?.login ?? "(Unknown)";
            _dateText.text = FormatDate(commit.commit?.committer?.date ?? commit.commit?.author?.date ?? "");

            Show();
        }

        public void Hide()
        {
            if (_tweenCoroutine != null) StopCoroutine(_tweenCoroutine);
            _tweenCoroutine = StartCoroutine(TweenToHidden());
        }

        private void Show()
        {
            if (_panel == null) return;
            if (_tweenCoroutine != null) StopCoroutine(_tweenCoroutine);
            _panel.gameObject.SetActive(true);
            _tweenCoroutine = StartCoroutine(TweenToVisible());
        }

        private IEnumerator TweenToVisible()
        {
            var elapsed = 0f;
            while (elapsed < _tweenDuration)
            {
                elapsed += Time.deltaTime;
                var t = _tweenCurve.Evaluate(elapsed / _tweenDuration);
                _panel.anchorMin = Vector2.Lerp(_hiddenAnchorMin, _visibleAnchorMin, t);
                if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            _panel.anchorMin = _visibleAnchorMin;
            if (_canvasGroup != null) _canvasGroup.alpha = 1f;
            _tweenCoroutine = null;
        }

        private IEnumerator TweenToHidden()
        {
            var elapsed = 0f;
            while (elapsed < _tweenDuration)
            {
                elapsed += Time.deltaTime;
                var t = _tweenCurve.Evaluate(elapsed / _tweenDuration);
                _panel.anchorMin = Vector2.Lerp(_visibleAnchorMin, _hiddenAnchorMin, t);
                if (_canvasGroup != null) _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            _panel.anchorMin = _hiddenAnchorMin;
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
            _panel.gameObject.SetActive(false);
            _tweenCoroutine = null;
        }

        private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) ? "(No message)" : s.Length <= max ? s : s.Substring(0, max) + "...";

        private static string FormatDate(string dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return "(No date)";
            return DateTime.TryParse(dateStr, out var dt) ? dt.ToString("yyyy-MM-dd HH:mm") : dateStr;
        }
    }
}
