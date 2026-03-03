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
    /// Handles Rest room actions: heal or upgrade one card.
    /// </summary>
    public class RestController : MonoBehaviour
    {
        public Canvas RestCanvas;
        public GameObject RestPanel;
        public TMP_Text TitleText;
        public TMP_Text InfoText;
        public Button RestButton;
        public Button UpgradeButton;
        public Button LeaveButton;
        public GameObject UpgradePanel;
        public Transform UpgradeListRoot;
        public Button CloseUpgradePanelButton;

        private int _nodeId = -1;
        private MapRoomType _roomType;
        private IRoomCompletionSink _sink;
        private MapRunState _runState;
        private readonly List<GameObject> _upgradeViews = new List<GameObject>();
        private bool _actionTaken;

        private void Awake()
        {
            EnsureUi();
            Hide();
        }

        public void OpenRest(int nodeId, MapRoomType roomType, IRoomCompletionSink sink, MapRunState runState)
        {
            EnsureUi();
            _nodeId = nodeId;
            _roomType = roomType;
            _sink = sink;
            _runState = runState;
            _actionTaken = false;

            TitleText.text = "Campfire";
            InfoText.text = "Choose one action.";
            RestCanvas.enabled = true;
            RestPanel.SetActive(true);
            UpgradePanel.SetActive(false);
            RestButton.interactable = true;
            UpgradeButton.interactable = true;

            RestButton.onClick.RemoveAllListeners();
            RestButton.onClick.AddListener(DoRest);
            UpgradeButton.onClick.RemoveAllListeners();
            UpgradeButton.onClick.AddListener(OpenUpgradePanel);
            LeaveButton.onClick.RemoveAllListeners();
            LeaveButton.onClick.AddListener(Leave);
            CloseUpgradePanelButton.onClick.RemoveAllListeners();
            CloseUpgradePanelButton.onClick.AddListener(() => UpgradePanel.SetActive(false));
        }

        public void Hide()
        {
            ClearUpgradeViews();
            if (UpgradePanel != null) UpgradePanel.SetActive(false);
            if (RestPanel != null) RestPanel.SetActive(false);
            if (RestCanvas != null) RestCanvas.enabled = false;
        }

        private void DoRest()
        {
            if (_runState == null || _actionTaken)
            {
                return;
            }

            int heal = Mathf.FloorToInt(_runState.maxPlayerHp * 0.3f);
            int before = _runState.currentPlayerHp;
            _runState.currentPlayerHp = Mathf.Min(_runState.maxPlayerHp, _runState.currentPlayerHp + Mathf.Max(0, heal));
            int gained = _runState.currentPlayerHp - before;
            InfoText.text = $"You rest and recover {gained} HP.";
            LockActionsAfterChoice();
        }

        private void OpenUpgradePanel()
        {
            if (_runState == null || _actionTaken)
            {
                return;
            }

            RebuildUpgradeList();
            UpgradePanel.SetActive(true);
        }

        private void RebuildUpgradeList()
        {
            ClearUpgradeViews();
            var indices = _runState.deck.GetUpgradeableIndices();
            if (indices.Count == 0)
            {
                InfoText.text = "No upgradeable cards.";
                return;
            }

            for (int i = 0; i < indices.Count; i++)
            {
                int deckIndex = indices[i];
                string cardId = _runState.deck.cardIds[deckIndex];
                string name = CardLibrary.TryGet(cardId, out var def) ? def.displayName : cardId;
                var btn = CreateButton($"Upgrade_{i}", UpgradeListRoot, $"Upgrade {name}");
                var rect = btn.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(560f, 60f);
                btn.onClick.AddListener(() => UpgradeCard(deckIndex));
                _upgradeViews.Add(btn.gameObject);
            }
        }

        private void UpgradeCard(int deckIndex)
        {
            if (_runState == null || _actionTaken)
            {
                return;
            }

            if (_runState.deck.TryUpgradeAt(deckIndex, out string beforeId, out string afterId))
            {
                string beforeName = CardLibrary.TryGet(beforeId, out var b) ? b.displayName : beforeId;
                string afterName = CardLibrary.TryGet(afterId, out var a) ? a.displayName : afterId;
                InfoText.text = $"Upgraded {beforeName} to {afterName}.";
                UpgradePanel.SetActive(false);
                LockActionsAfterChoice();
            }
            else
            {
                InfoText.text = "Upgrade failed.";
            }
        }

        private void LockActionsAfterChoice()
        {
            _actionTaken = true;
            RestButton.interactable = false;
            UpgradeButton.interactable = false;
        }

        private void Leave()
        {
            Hide();
            _sink?.CompleteRoom(_nodeId, _roomType, RoomCompletionResult.Cleared, null);
        }

        private void EnsureUi()
        {
            if (RestCanvas != null && RestPanel != null && TitleText != null && InfoText != null &&
                RestButton != null && UpgradeButton != null && LeaveButton != null &&
                UpgradePanel != null && UpgradeListRoot != null && CloseUpgradePanelButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (RestCanvas == null)
            {
                var canvasGo = new GameObject("RestCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                RestCanvas = canvasGo.GetComponent<Canvas>();
                RestCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (RestPanel == null)
            {
                var panel = new GameObject("RestPanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(RestCanvas.transform, false);
                var rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(920f, 620f);
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.84f);
                RestPanel = panel;
            }

            if (TitleText == null) TitleText = CreateText("Title", RestPanel.transform, new Vector2(0.5f, 0.85f), new Vector2(600f, 70f), 46f);
            if (InfoText == null) InfoText = CreateText("Info", RestPanel.transform, new Vector2(0.5f, 0.66f), new Vector2(760f, 100f), 28f);

            if (RestButton == null)
            {
                RestButton = CreateButton("RestButton", RestPanel.transform, "Rest");
                var rect = RestButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.3f, 0.35f);
                rect.anchorMax = new Vector2(0.3f, 0.35f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            if (UpgradeButton == null)
            {
                UpgradeButton = CreateButton("UpgradeButton", RestPanel.transform, "Upgrade");
                var rect = UpgradeButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.7f, 0.35f);
                rect.anchorMax = new Vector2(0.7f, 0.35f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            if (LeaveButton == null)
            {
                LeaveButton = CreateButton("LeaveButton", RestPanel.transform, "Leave");
                var rect = LeaveButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.12f);
                rect.anchorMax = new Vector2(0.5f, 0.12f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            if (UpgradePanel == null)
            {
                var panel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(RestPanel.transform, false);
                var rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.54f);
                rect.anchorMax = new Vector2(0.5f, 0.54f);
                rect.sizeDelta = new Vector2(700f, 360f);
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
                UpgradePanel = panel;
            }

            if (UpgradeListRoot == null)
            {
                var root = new GameObject("UpgradeListRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
                root.transform.SetParent(UpgradePanel.transform, false);
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.58f);
                rect.anchorMax = new Vector2(0.5f, 0.58f);
                rect.sizeDelta = new Vector2(620f, 220f);
                var layout = root.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 8f;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                layout.childAlignment = TextAnchor.MiddleCenter;
                UpgradeListRoot = root.transform;
            }

            if (CloseUpgradePanelButton == null)
            {
                CloseUpgradePanelButton = CreateButton("CloseUpgradePanelButton", UpgradePanel.transform, "Close");
                var rect = CloseUpgradePanelButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.1f);
                rect.anchorMax = new Vector2(0.5f, 0.1f);
                rect.sizeDelta = new Vector2(200f, 58f);
            }

            EnsureEventSystem();
        }

        private void ClearUpgradeViews()
        {
            for (int i = 0; i < _upgradeViews.Count; i++)
            {
                if (_upgradeViews[i] != null) Destroy(_upgradeViews[i]);
            }

            _upgradeViews.Clear();
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


