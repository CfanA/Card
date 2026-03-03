using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Run
{
    /// <summary>
    /// Handles boss relic reward selection UI.
    /// </summary>
    public class BossRewardController : MonoBehaviour
    {
        public Canvas RewardCanvas;
        public GameObject RewardPanel;
        public TMP_Text TitleText;
        public Transform OptionRoot;
        public Button SkipButton;

        private readonly List<GameObject> _optionViews = new List<GameObject>();
        private Action<string> _onSelect;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Open(List<RelicDefinitionRuntime> options, Action<string> onSelect)
        {
            EnsureUi();
            _onSelect = onSelect;
            TitleText.text = "Boss Relic Reward";
            Rebuild(options);
            RewardCanvas.enabled = true;
            RewardPanel.SetActive(true);
        }

        public void Hide()
        {
            ClearOptions();
            if (RewardPanel != null) RewardPanel.SetActive(false);
            if (RewardCanvas != null) RewardCanvas.enabled = false;
        }

        private void Rebuild(List<RelicDefinitionRuntime> options)
        {
            ClearOptions();
            if (options == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                var relic = options[i];
                var btn = CreateButton($"Relic_{i}", OptionRoot, $"{relic.displayName}\n{relic.description}");
                var rect = btn.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(980f, 96f);
                string id = relic.id;
                btn.onClick.AddListener(() => Select(id));
                _optionViews.Add(btn.gameObject);
            }

            SkipButton.onClick.RemoveAllListeners();
            SkipButton.onClick.AddListener(() => Select(null));
        }

        private void Select(string relicId)
        {
            var callback = _onSelect;
            Hide();
            _onSelect = null;
            callback?.Invoke(relicId);
        }

        private void EnsureUi()
        {
            if (RewardCanvas != null && RewardPanel != null && TitleText != null && OptionRoot != null && SkipButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (RewardCanvas == null)
            {
                var go = new GameObject("BossRewardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(transform, false);
                RewardCanvas = go.GetComponent<Canvas>();
                RewardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (RewardPanel == null)
            {
                var go = new GameObject("BossRewardPanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(RewardCanvas.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1200f, 700f);
                go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
                RewardPanel = go;
            }

            if (TitleText == null) TitleText = CreateText("Title", RewardPanel.transform, new Vector2(0.5f, 0.9f), new Vector2(700f, 70f), 44f);

            if (OptionRoot == null)
            {
                var go = new GameObject("OptionRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
                go.transform.SetParent(RewardPanel.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.54f);
                rect.anchorMax = new Vector2(0.5f, 0.54f);
                rect.sizeDelta = new Vector2(1040f, 420f);
                var layout = go.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                layout.childAlignment = TextAnchor.MiddleCenter;
                OptionRoot = go.transform;
            }

            if (SkipButton == null)
            {
                SkipButton = CreateButton("Skip", RewardPanel.transform, "Skip");
                var rect = SkipButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.1f);
                rect.anchorMax = new Vector2(0.5f, 0.1f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            EnsureEventSystem();
        }

        private void ClearOptions()
        {
            for (int i = 0; i < _optionViews.Count; i++)
            {
                if (_optionViews[i] != null) Destroy(_optionViews[i]);
            }

            _optionViews.Clear();
        }

        private static TMP_Text CreateText(string name, Transform parent, Vector2 anchor, Vector2 size, float fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.22f, 0.35f, 0.20f, 1f);
            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var rect = txt.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go.GetComponent<Button>();
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(es);
        }
    }
}


