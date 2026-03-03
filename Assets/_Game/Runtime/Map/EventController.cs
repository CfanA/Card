using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Map
{
    /// <summary>
    /// Runtime event UI controller for Event rooms.
    /// </summary>
    public class EventController : MonoBehaviour
    {
        public Canvas EventCanvas;
        public GameObject EventPanel;
        public TMP_Text TitleText;
        public TMP_Text BodyText;
        public TMP_Text ResultText;
        public Transform OptionRoot;
        public Button LeaveButton;
        public Button ContinueButton;

        private EventDefinition _activeEvent;
        private int _nodeId = -1;
        private MapRoomType _roomType;
        private IRoomCompletionSink _sink;
        private MapRunState _runState;

        private Func<int, string, string> _grantRelic;
        private Func<int, string, string> _grantPotion;
        private Action<int, string> _addCardToDeck;

        private readonly List<GameObject> _optionViews = new List<GameObject>();

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void OpenEvent(
            int nodeId,
            MapRoomType roomType,
            EventDefinition definition,
            IRoomCompletionSink sink,
            MapRunState runState,
            Func<int, string, string> grantRelic,
            Func<int, string, string> grantPotion,
            Action<int, string> addCardToDeck)
        {
            EnsureUi();
            _nodeId = nodeId;
            _roomType = roomType;
            _activeEvent = definition;
            _sink = sink;
            _runState = runState;
            _grantRelic = grantRelic;
            _grantPotion = grantPotion;
            _addCardToDeck = addCardToDeck;

            TitleText.text = string.IsNullOrWhiteSpace(definition.title) ? "Event" : definition.title;
            BodyText.text = definition.body;
            ResultText.text = string.Empty;
            LeaveButton.gameObject.SetActive(false);
            ContinueButton.gameObject.SetActive(false);
            EventCanvas.enabled = true;
            EventPanel.SetActive(true);

            RebuildOptions(definition.options);
        }

        public void Hide()
        {
            ClearOptions();
            if (EventPanel != null) EventPanel.SetActive(false);
            if (EventCanvas != null) EventCanvas.enabled = false;
        }

        private void RebuildOptions(List<EventOption> options)
        {
            ClearOptions();
            if (options == null || OptionRoot == null)
            {
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                int idx = i;
                var option = options[i];
                var btn = CreateButton($"Option_{i}", OptionRoot, option.buttonText);
                var rect = btn.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(780f, 64f);
                btn.onClick.AddListener(() => OnOptionClicked(option, idx));
                _optionViews.Add(btn.gameObject);
            }
        }

        private void OnOptionClicked(EventOption option, int optionIndex)
        {
            ApplyEffects(option.effects);
            ResultText.text = option.resultText;
            ClearOptions();

            if (option.endEvent)
            {
                LeaveButton.gameObject.SetActive(true);
                ContinueButton.gameObject.SetActive(false);
                LeaveButton.onClick.RemoveAllListeners();
                LeaveButton.onClick.AddListener(LeaveEvent);
            }
            else
            {
                LeaveButton.gameObject.SetActive(false);
                ContinueButton.gameObject.SetActive(true);
                ContinueButton.onClick.RemoveAllListeners();
                ContinueButton.onClick.AddListener(ContinueEvent);
            }
        }

        private void ContinueEvent()
        {
            ContinueButton.gameObject.SetActive(false);
            ResultText.text = string.Empty;
            RebuildOptions(_activeEvent.options);
        }

        private void LeaveEvent()
        {
            Hide();
            _sink?.CompleteRoom(_nodeId, _roomType, RoomCompletionResult.Cleared, null);
        }

        private void ApplyEffects(List<EventEffect> effects)
        {
            if (effects == null || _runState == null)
            {
                return;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                switch (effect.effectType)
                {
                    case EventEffectType.Heal:
                        _runState.currentPlayerHp = Mathf.Min(_runState.maxPlayerHp, _runState.currentPlayerHp + Mathf.Max(0, effect.value));
                        break;
                    case EventEffectType.LoseHP:
                        _runState.currentPlayerHp = Mathf.Max(0, _runState.currentPlayerHp - Mathf.Max(0, effect.value));
                        break;
                    case EventEffectType.GainRelic:
                        _grantRelic?.Invoke(_nodeId, effect.idParam);
                        break;
                    case EventEffectType.GainPotion:
                        _grantPotion?.Invoke(_nodeId, effect.idParam);
                        break;
                    case EventEffectType.AddCardToDeck:
                        _addCardToDeck?.Invoke(_nodeId, effect.idParam);
                        break;
                }
            }
        }

        private void EnsureUi()
        {
            if (EventCanvas != null && EventPanel != null && TitleText != null && BodyText != null &&
                ResultText != null && OptionRoot != null && LeaveButton != null && ContinueButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (EventCanvas == null)
            {
                var canvasGo = new GameObject("EventCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                EventCanvas = canvasGo.GetComponent<Canvas>();
                EventCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (EventPanel == null)
            {
                var panelGo = new GameObject("EventPanel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(EventCanvas.transform, false);
                var rect = panelGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1100f, 700f);
                panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.84f);
                EventPanel = panelGo;
            }

            if (TitleText == null)
            {
                TitleText = CreateText("Title", EventPanel.transform, new Vector2(0.5f, 0.9f), new Vector2(900f, 70f), 46f);
            }

            if (BodyText == null)
            {
                BodyText = CreateText("Body", EventPanel.transform, new Vector2(0.5f, 0.74f), new Vector2(960f, 180f), 30f);
            }

            if (ResultText == null)
            {
                ResultText = CreateText("Result", EventPanel.transform, new Vector2(0.5f, 0.52f), new Vector2(960f, 120f), 28f);
            }

            if (OptionRoot == null)
            {
                var rootGo = new GameObject("OptionRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
                rootGo.transform.SetParent(EventPanel.transform, false);
                var rect = rootGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.28f);
                rect.anchorMax = new Vector2(0.5f, 0.28f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(900f, 230f);
                var layout = rootGo.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 14f;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                layout.childForceExpandHeight = false;
                layout.childForceExpandWidth = false;
                layout.childAlignment = TextAnchor.MiddleCenter;
                OptionRoot = rootGo.transform;
            }

            if (LeaveButton == null)
            {
                LeaveButton = CreateButton("LeaveButton", EventPanel.transform, "Leave");
                var rect = LeaveButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.08f);
                rect.anchorMax = new Vector2(0.5f, 0.08f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            if (ContinueButton == null)
            {
                ContinueButton = CreateButton("ContinueButton", EventPanel.transform, "Continue");
                var rect = ContinueButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.08f);
                rect.anchorMax = new Vector2(0.5f, 0.08f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            EnsureEventSystem();
        }

        private void ClearOptions()
        {
            for (int i = 0; i < _optionViews.Count; i++)
            {
                Destroy(_optionViews[i]);
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
            var img = go.GetComponent<Image>();
            img.color = new Color(0.2f, 0.42f, 0.22f, 1f);

            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var rect = txt.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26f;
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
