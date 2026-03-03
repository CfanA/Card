using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Map
{
    /// <summary>
    /// Final run summary UI with restart action.
    /// </summary>
    public class VictorySummaryController : MonoBehaviour
    {
        public Canvas SummaryCanvas;
        public GameObject SummaryPanel;
        public TMP_Text TitleText;
        public TMP_Text BodyText;
        public Button RestartButton;

        private Action _onRestart;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void Open(MapRunState runState, int floorsCleared, Action onRestart)
        {
            EnsureUi();
            _onRestart = onRestart;
            TitleText.text = "Victory";
            BodyText.text =
                $"Seed: {runState.seed}\n" +
                $"HP: {runState.currentPlayerHp}/{runState.maxPlayerHp}\n" +
                $"Gold: {runState.gold}\n" +
                $"Deck Count: {runState.deck.cardIds.Count}\n" +
                $"Relic Count: {runState.relicIds.Count}\n" +
                $"Floors Cleared: {floorsCleared}";
            SummaryCanvas.enabled = true;
            SummaryPanel.SetActive(true);
            RestartButton.onClick.RemoveAllListeners();
            RestartButton.onClick.AddListener(RestartRun);
        }

        public void Hide()
        {
            if (SummaryPanel != null) SummaryPanel.SetActive(false);
            if (SummaryCanvas != null) SummaryCanvas.enabled = false;
        }

        private void RestartRun()
        {
            Hide();
            var cb = _onRestart;
            _onRestart = null;
            cb?.Invoke();
        }

        private void EnsureUi()
        {
            if (SummaryCanvas != null && SummaryPanel != null && TitleText != null && BodyText != null && RestartButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (SummaryCanvas == null)
            {
                var go = new GameObject("VictorySummaryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(transform, false);
                SummaryCanvas = go.GetComponent<Canvas>();
                SummaryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (SummaryPanel == null)
            {
                var go = new GameObject("VictorySummaryPanel", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(SummaryCanvas.transform, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(900f, 640f);
                go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.9f);
                SummaryPanel = go;
            }

            if (TitleText == null) TitleText = CreateText("Title", SummaryPanel.transform, new Vector2(0.5f, 0.86f), new Vector2(500f, 80f), 56f);
            if (BodyText == null) BodyText = CreateText("Body", SummaryPanel.transform, new Vector2(0.5f, 0.53f), new Vector2(700f, 340f), 30f);
            if (RestartButton == null)
            {
                RestartButton = CreateButton("RestartButton", SummaryPanel.transform, "Restart Run");
                var rect = RestartButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.12f);
                rect.anchorMax = new Vector2(0.5f, 0.12f);
                rect.sizeDelta = new Vector2(280f, 72f);
            }

            EnsureEventSystem();
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
            go.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.2f, 1f);
            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var rect = txt.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28f;
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
