using System;
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
    /// Handles treasure-time potion replacement UI when potion slots are full.
    /// </summary>
    public class PotionRewardController : MonoBehaviour
    {
        public Canvas RewardCanvas;
        public GameObject RewardPanel;
        public TMP_Text TitleText;
        public TMP_Text PotionInfoText;
        public Button Slot0Button;
        public Button Slot1Button;
        public Button Slot2Button;
        public Button SkipButton;

        private Action<int?> _onSelect;
        private string _newPotionName;
        private string[] _slotNames;

        private void Awake()
        {
            EnsureUi();
            BindButtons();
            Hide();
        }

        public void ShowReplacePrompt(string newPotionName, string[] slotNames, Action<int?> onSelect)
        {
            EnsureUi();
            BindButtons();
            _newPotionName = newPotionName;
            _slotNames = slotNames;
            _onSelect = onSelect;

            if (TitleText != null)
            {
                TitleText.text = "Potion Slots Full";
            }

            if (PotionInfoText != null)
            {
                PotionInfoText.text = $"New Potion: {newPotionName}\nChoose slot to replace or skip.";
            }

            SetSlotButton(Slot0Button, 0);
            SetSlotButton(Slot1Button, 1);
            SetSlotButton(Slot2Button, 2);

            if (RewardCanvas != null) RewardCanvas.enabled = true;
            if (RewardPanel != null) RewardPanel.SetActive(true);
        }

        public void Hide()
        {
            if (RewardPanel != null) RewardPanel.SetActive(false);
            if (RewardCanvas != null) RewardCanvas.enabled = false;
        }

        private void SelectSlot(int? slot)
        {
            var callback = _onSelect;
            Hide();
            _onSelect = null;
            callback?.Invoke(slot);
        }

        private void BindButtons()
        {
            BindSlotButton(Slot0Button, 0);
            BindSlotButton(Slot1Button, 1);
            BindSlotButton(Slot2Button, 2);
            if (SkipButton != null)
            {
                SkipButton.onClick.RemoveAllListeners();
                SkipButton.onClick.AddListener(() => SelectSlot(null));
            }
        }

        private void BindSlotButton(Button button, int index)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectSlot(index));
        }

        private void SetSlotButton(Button button, int index)
        {
            if (button == null) return;
            string name = (_slotNames != null && index < _slotNames.Length) ? _slotNames[index] : "Unknown";
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = $"Replace Slot {index + 1}\n{name}";
            }
        }

        private void EnsureUi()
        {
            if (RewardCanvas != null && RewardPanel != null && TitleText != null &&
                PotionInfoText != null && Slot0Button != null && Slot1Button != null &&
                Slot2Button != null && SkipButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (RewardCanvas == null)
            {
                var canvasGo = new GameObject("PotionRewardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                RewardCanvas = canvasGo.GetComponent<Canvas>();
                RewardCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (RewardPanel == null)
            {
                var panelGo = new GameObject("PotionRewardPanel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(RewardCanvas.transform, false);
                var rect = panelGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(900f, 520f);
                panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
                RewardPanel = panelGo;
            }

            if (TitleText == null)
            {
                TitleText = CreateText("Title", RewardPanel.transform, new Vector2(0.5f, 0.84f), new Vector2(600f, 70f), 44f);
            }

            if (PotionInfoText == null)
            {
                PotionInfoText = CreateText("Info", RewardPanel.transform, new Vector2(0.5f, 0.68f), new Vector2(760f, 110f), 28f);
            }

            if (Slot0Button == null)
            {
                Slot0Button = CreateButton("Slot0", RewardPanel.transform, new Vector2(0.5f, 0.46f), new Vector2(460f, 76f), "Slot 1");
            }

            if (Slot1Button == null)
            {
                Slot1Button = CreateButton("Slot1", RewardPanel.transform, new Vector2(0.5f, 0.32f), new Vector2(460f, 76f), "Slot 2");
            }

            if (Slot2Button == null)
            {
                Slot2Button = CreateButton("Slot2", RewardPanel.transform, new Vector2(0.5f, 0.18f), new Vector2(460f, 76f), "Slot 3");
            }

            if (SkipButton == null)
            {
                SkipButton = CreateButton("Skip", RewardPanel.transform, new Vector2(0.5f, 0.06f), new Vector2(220f, 64f), "Skip");
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
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, Vector2 anchor, Vector2 size, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.2f, 0.35f, 0.2f, 1f);

            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
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


