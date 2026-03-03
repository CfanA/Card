using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Map
{
    /// <summary>
    /// Handles room entry UI and completion callback.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("UI References")]
        public Canvas RoomCanvas;
        public GameObject RoomPanel;
        public TMP_Text RoomTypeText;
        public Button CompleteRoomButton;

        private int _activeNodeId = -1;
        private MapRoomType _activeRoomType;
        private IRoomCompletionSink _completionSink;

        /// <summary>
        /// True while room panel is open and waiting for completion.
        /// </summary>
        public bool IsRoomOpen { get; private set; }

        private void Awake()
        {
            EnsureUi();
            BindButton();
            CloseRoom();
        }

        /// <summary>
        /// Opens room UI for a selected node.
        /// </summary>
        public void EnterRoom(int nodeId, MapRoomType roomType, IRoomCompletionSink completionSink, string messageOverride = null)
        {
            EnsureUi();
            BindButton();
            _activeNodeId = nodeId;
            _activeRoomType = roomType;
            _completionSink = completionSink;
            IsRoomOpen = true;

            if (RoomTypeText != null)
            {
                RoomTypeText.text = string.IsNullOrWhiteSpace(messageOverride)
                    ? $"Room: {roomType}"
                    : messageOverride;
            }

            if (RoomCanvas != null)
            {
                RoomCanvas.enabled = true;
            }

            if (RoomPanel != null)
            {
                RoomPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Closes current room UI without changing run progress.
        /// </summary>
        public void CloseRoom()
        {
            IsRoomOpen = false;
            _activeNodeId = -1;
            _completionSink = null;

            if (RoomPanel != null)
            {
                RoomPanel.SetActive(false);
            }

            if (RoomCanvas != null)
            {
                RoomCanvas.enabled = false;
            }
        }

        private void CompleteCurrentRoom()
        {
            if (!IsRoomOpen)
            {
                return;
            }

            int nodeId = _activeNodeId;
            MapRoomType roomType = _activeRoomType;
            var sink = _completionSink;
            CloseRoom();
            sink?.CompleteRoom(nodeId, roomType, RoomCompletionResult.Cleared, null);
        }

        private void BindButton()
        {
            if (CompleteRoomButton == null)
            {
                return;
            }

            CompleteRoomButton.onClick.RemoveListener(CompleteCurrentRoom);
            CompleteRoomButton.onClick.AddListener(CompleteCurrentRoom);
        }

        private void EnsureUi()
        {
            if (RoomCanvas != null && RoomPanel != null && RoomTypeText != null && CompleteRoomButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (RoomCanvas == null)
            {
                var canvasGo = new GameObject("RoomCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                RoomCanvas = canvasGo.GetComponent<Canvas>();
                RoomCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (RoomPanel == null)
            {
                var panelGo = new GameObject("RoomPanel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(RoomCanvas.transform, false);
                var panelRect = panelGo.GetComponent<RectTransform>();
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.sizeDelta = new Vector2(520f, 240f);
                panelRect.anchoredPosition = Vector2.zero;
                var img = panelGo.GetComponent<Image>();
                img.color = new Color(0f, 0f, 0f, 0.72f);
                RoomPanel = panelGo;
            }

            if (RoomTypeText == null)
            {
                var textGo = new GameObject("RoomTypeText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(RoomPanel.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = new Vector2(0.5f, 0.72f);
                textRect.anchorMax = new Vector2(0.5f, 0.72f);
                textRect.pivot = new Vector2(0.5f, 0.5f);
                textRect.sizeDelta = new Vector2(420f, 80f);
                textRect.anchoredPosition = Vector2.zero;
                var tmp = textGo.GetComponent<TextMeshProUGUI>();
                tmp.fontSize = 44f;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
                RoomTypeText = tmp;
            }

            if (CompleteRoomButton == null)
            {
                var btnGo = new GameObject("CompleteRoomButton", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(RoomPanel.transform, false);
                var btnRect = btnGo.GetComponent<RectTransform>();
                btnRect.anchorMin = new Vector2(0.5f, 0.3f);
                btnRect.anchorMax = new Vector2(0.5f, 0.3f);
                btnRect.pivot = new Vector2(0.5f, 0.5f);
                btnRect.sizeDelta = new Vector2(280f, 72f);
                btnRect.anchoredPosition = Vector2.zero;
                var btnImg = btnGo.GetComponent<Image>();
                btnImg.color = new Color(0.15f, 0.55f, 0.24f, 1f);
                CompleteRoomButton = btnGo.GetComponent<Button>();

                var labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(btnGo.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                var labelText = labelGo.GetComponent<TextMeshProUGUI>();
                labelText.text = "Complete Room";
                labelText.fontSize = 34f;
                labelText.alignment = TextAlignmentOptions.Center;
                labelText.color = Color.white;
            }

            EnsureEventSystem();
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
