using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Map
{
    /// <summary>
    /// Handles Shop room UI and purchase flow.
    /// </summary>
    public class ShopController : MonoBehaviour
    {
        public Canvas ShopCanvas;
        public GameObject ShopPanel;
        public TMP_Text GoldText;
        public TMP_Text InfoText;
        public Transform GoodsRoot;
        public Button RemoveCardButton;
        public Button LeaveButton;
        public GameObject RemovePanel;
        public Transform RemoveRoot;
        public Button CloseRemoveButton;

        private MapRunState _runState;
        private int _nodeId;
        private MapRoomType _roomType;
        private IRoomCompletionSink _sink;
        private Action<string> _onBuyCard;
        private Action<string> _onBuyRelic;
        private Func<string, bool> _onBuyPotion;
        private readonly List<GameObject> _goodViews = new List<GameObject>();
        private readonly List<GameObject> _removeViews = new List<GameObject>();
        private readonly List<ShopGood> _goods = new List<ShopGood>();
        private bool _removeUsed;

        public void OpenShop(
            int nodeId,
            MapRoomType roomType,
            MapRunState runState,
            IRoomCompletionSink sink,
            Action<string> onBuyCard,
            Action<string> onBuyRelic,
            Func<string, bool> onBuyPotion,
            int shopSeed)
        {
            EnsureUi();
            _nodeId = nodeId;
            _roomType = roomType;
            _runState = runState;
            _sink = sink;
            _onBuyCard = onBuyCard;
            _onBuyRelic = onBuyRelic;
            _onBuyPotion = onBuyPotion;
            _removeUsed = false;
            BuildGoods(shopSeed);
            RebuildGoodsUi();
            RefreshGold();
            InfoText.text = "Welcome to the shop.";
            ShopCanvas.enabled = true;
            ShopPanel.SetActive(true);
            RemovePanel.SetActive(false);
        }

        public void Hide()
        {
            ClearList(_goodViews);
            ClearList(_removeViews);
            if (RemovePanel != null) RemovePanel.SetActive(false);
            if (ShopPanel != null) ShopPanel.SetActive(false);
            if (ShopCanvas != null) ShopCanvas.enabled = false;
        }

        private void BuildGoods(int shopSeed)
        {
            _goods.Clear();
            var rng = new System.Random(shopSeed);

            var cards = CardLibrary.GetRewardPool();
            PickDistinct(cards, 3, rng, picked =>
            {
                int price = rng.Next(40, 81);
                _goods.Add(ShopGood.ForCard(picked.id, picked.displayName, picked.description, price));
            });

            var relics = RelicLibrary.AllOrdered();
            if (relics.Count > 0)
            {
                var relic = relics[rng.Next(0, relics.Count)];
                int price = rng.Next(150, 251);
                _goods.Add(ShopGood.ForRelic(relic.id, relic.displayName, relic.description, price));
            }

            var potions = PotionLibrary.AllOrdered();
            if (potions.Count > 0)
            {
                var potion = potions[rng.Next(0, potions.Count)];
                int price = rng.Next(50, 81);
                _goods.Add(ShopGood.ForPotion(potion.id, potion.displayName, potion.description, price));
            }
        }

        private void RebuildGoodsUi()
        {
            ClearList(_goodViews);
            for (int i = 0; i < _goods.Count; i++)
            {
                int idx = i;
                var good = _goods[i];
                var btn = CreateButton($"Good_{i}", GoodsRoot, BuildGoodLabel(good));
                var rect = btn.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(980f, 86f);
                btn.onClick.AddListener(() => TryBuy(idx));
                _goodViews.Add(btn.gameObject);
            }

            RemoveCardButton.onClick.RemoveAllListeners();
            RemoveCardButton.onClick.AddListener(OpenRemoveCardPanel);
            RemoveCardButton.interactable = !_removeUsed;
        }

        private void TryBuy(int index)
        {
            if (index < 0 || index >= _goods.Count)
            {
                return;
            }

            var good = _goods[index];
            if (good.Sold)
            {
                return;
            }

            if (!_runState.SpendGold(good.Price))
            {
                InfoText.text = "Not enough gold.";
                return;
            }

            bool success = true;
            switch (good.Kind)
            {
                case ShopGoodKind.Card:
                    _onBuyCard?.Invoke(good.Id);
                    break;
                case ShopGoodKind.Relic:
                    _onBuyRelic?.Invoke(good.Id);
                    break;
                case ShopGoodKind.Potion:
                    success = _onBuyPotion == null || _onBuyPotion.Invoke(good.Id);
                    break;
            }

            if (!success)
            {
                _runState.AddGold(good.Price);
                InfoText.text = "Purchase failed.";
                return;
            }

            good.Sold = true;
            Debug.Log($"[Shop] Purchased {good.Kind}:{good.Id} for {good.Price}g");
            InfoText.text = $"Bought {good.Name} for {good.Price}g.";
            RefreshGold();
            RebuildGoodsUi();
        }

        private void OpenRemoveCardPanel()
        {
            if (_removeUsed)
            {
                return;
            }

            if (_runState.deck.cardIds.Count == 0)
            {
                InfoText.text = "Deck is empty.";
                return;
            }

            RemovePanel.SetActive(true);
            RebuildRemoveList();
            InfoText.text = "Choose a card to remove (75g).";
        }

        private void RebuildRemoveList()
        {
            ClearList(_removeViews);
            if (_runState.deck.cardIds.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _runState.deck.cardIds.Count; i++)
            {
                int idx = i;
                string cardId = _runState.deck.cardIds[i];
                string name = CardLibrary.TryGet(cardId, out var def) ? def.displayName : cardId;
                var btn = CreateButton($"Remove_{i}", RemoveRoot, $"Remove {name}");
                var rect = btn.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(620f, 62f);
                btn.onClick.AddListener(() => RemoveCardAt(idx));
                _removeViews.Add(btn.gameObject);
            }
        }

        private void RemoveCardAt(int index)
        {
            if (index < 0 || index >= _runState.deck.cardIds.Count)
            {
                return;
            }

            const int price = 75;
            if (!_runState.SpendGold(price))
            {
                InfoText.text = "Not enough gold for Remove Card.";
                return;
            }

            string removed = _runState.deck.cardIds[index];
            _runState.deck.cardIds.RemoveAt(index);
            _removeUsed = true;
            RemovePanel.SetActive(false);
            Debug.Log($"[Shop] Removed card {removed} for {price}g");
            InfoText.text = $"Removed {removed} for {price}g.";
            RefreshGold();
            RebuildGoodsUi();
        }

        private void RefreshGold()
        {
            if (GoldText != null)
            {
                GoldText.text = $"Gold: {_runState.gold}";
            }
        }

        private void OnLeave()
        {
            Hide();
            _sink?.CompleteRoom(_nodeId, _roomType, RoomCompletionResult.Cleared, null);
        }

        private void EnsureUi()
        {
            if (ShopCanvas != null && ShopPanel != null && GoldText != null && InfoText != null && GoodsRoot != null &&
                RemoveCardButton != null && LeaveButton != null && RemovePanel != null && RemoveRoot != null && CloseRemoveButton != null)
            {
                BindStaticButtons();
                EnsureEventSystem();
                return;
            }

            if (ShopCanvas == null)
            {
                var canvasGo = new GameObject("ShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                ShopCanvas = canvasGo.GetComponent<Canvas>();
                ShopCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (ShopPanel == null)
            {
                var panelGo = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(ShopCanvas.transform, false);
                var rect = panelGo.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(1220f, 760f);
                panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.84f);
                ShopPanel = panelGo;
            }

            if (GoldText == null) GoldText = CreateText("GoldText", ShopPanel.transform, new Vector2(0.2f, 0.92f), new Vector2(360f, 60f), 38f);
            if (InfoText == null) InfoText = CreateText("InfoText", ShopPanel.transform, new Vector2(0.65f, 0.92f), new Vector2(760f, 60f), 28f);

            if (GoodsRoot == null)
            {
                var root = new GameObject("GoodsRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
                root.transform.SetParent(ShopPanel.transform, false);
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.58f);
                rect.anchorMax = new Vector2(0.5f, 0.58f);
                rect.sizeDelta = new Vector2(1040f, 430f);
                var layout = root.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 12f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                GoodsRoot = root.transform;
            }

            if (RemoveCardButton == null)
            {
                RemoveCardButton = CreateButton("RemoveCardButton", ShopPanel.transform, "Remove Card (75g)");
                var rect = RemoveCardButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.35f, 0.08f);
                rect.anchorMax = new Vector2(0.35f, 0.08f);
                rect.sizeDelta = new Vector2(300f, 70f);
            }

            if (LeaveButton == null)
            {
                LeaveButton = CreateButton("LeaveButton", ShopPanel.transform, "Leave");
                var rect = LeaveButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.7f, 0.08f);
                rect.anchorMax = new Vector2(0.7f, 0.08f);
                rect.sizeDelta = new Vector2(220f, 70f);
            }

            if (RemovePanel == null)
            {
                var panel = new GameObject("RemovePanel", typeof(RectTransform), typeof(Image));
                panel.transform.SetParent(ShopPanel.transform, false);
                var rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.42f);
                rect.anchorMax = new Vector2(0.5f, 0.42f);
                rect.sizeDelta = new Vector2(760f, 420f);
                panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.92f);
                RemovePanel = panel;
            }

            if (RemoveRoot == null)
            {
                var root = new GameObject("RemoveRoot", typeof(RectTransform), typeof(VerticalLayoutGroup));
                root.transform.SetParent(RemovePanel.transform, false);
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.56f);
                rect.anchorMax = new Vector2(0.5f, 0.56f);
                rect.sizeDelta = new Vector2(680f, 290f);
                var layout = root.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 10f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlHeight = false;
                layout.childControlWidth = false;
                RemoveRoot = root.transform;
            }

            if (CloseRemoveButton == null)
            {
                CloseRemoveButton = CreateButton("CloseRemoveButton", RemovePanel.transform, "Cancel");
                var rect = CloseRemoveButton.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.08f);
                rect.anchorMax = new Vector2(0.5f, 0.08f);
                rect.sizeDelta = new Vector2(220f, 64f);
            }

            BindStaticButtons();
            EnsureEventSystem();
        }

        private void BindStaticButtons()
        {
            LeaveButton.onClick.RemoveAllListeners();
            LeaveButton.onClick.AddListener(OnLeave);
            CloseRemoveButton.onClick.RemoveAllListeners();
            CloseRemoveButton.onClick.AddListener(() => RemovePanel.SetActive(false));
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

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.18f, 0.4f, 0.2f, 1f);
            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var rect = txt.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24f;
            tmp.color = Color.white;
            return go.GetComponent<Button>();
        }

        private static void PickDistinct<T>(List<T> source, int count, System.Random rng, Action<T> onPick)
        {
            var copy = new List<T>(source);
            int pickCount = Mathf.Min(count, copy.Count);
            for (int i = 0; i < pickCount; i++)
            {
                int idx = rng.Next(0, copy.Count);
                onPick?.Invoke(copy[idx]);
                copy.RemoveAt(idx);
            }
        }

        private static void ClearList(List<GameObject> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null) Destroy(list[i]);
            }

            list.Clear();
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

        private static string BuildGoodLabel(ShopGood good)
        {
            string sold = good.Sold ? " [SOLD]" : string.Empty;
            return $"{good.Name} ({good.Price}g){sold}\n{good.Description}";
        }
    }

    public enum ShopGoodKind
    {
        Card,
        Relic,
        Potion
    }

    public class ShopGood
    {
        public ShopGoodKind Kind;
        public string Id;
        public string Name;
        public string Description;
        public int Price;
        public bool Sold;

        public static ShopGood ForCard(string id, string name, string desc, int price)
        {
            return new ShopGood { Kind = ShopGoodKind.Card, Id = id, Name = name, Description = desc, Price = price };
        }

        public static ShopGood ForRelic(string id, string name, string desc, int price)
        {
            return new ShopGood { Kind = ShopGoodKind.Relic, Id = id, Name = name, Description = desc, Price = price };
        }

        public static ShopGood ForPotion(string id, string name, string desc, int price)
        {
            return new ShopGood { Kind = ShopGoodKind.Potion, Id = id, Name = name, Description = desc, Price = price };
        }
    }
}
