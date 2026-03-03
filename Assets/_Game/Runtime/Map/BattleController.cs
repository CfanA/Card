using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Map
{
    /// <summary>
    /// Controls a simple turn-based battle flow for map combat rooms.
    /// </summary>
    public class BattleController : MonoBehaviour, IRelicBattleApi
    {
        /// <summary>
        /// Battle phase state.
        /// </summary>
        public enum BattleState
        {
            None,
            StartBattle,
            PlayerTurn,
            EnemyTurn,
            Reward,
            Defeat
        }

        [Header("Config")]
        public int MaxPlayerHp = 80;
        public int EnergyPerTurn = 3;
        public int DrawPerTurn = 5;
        public int EnemyIntentBaseDamage = 5;
        public int BossBaseHp = 150;
        public int BossAttackDamage = 12;
        public int BossBuffStrength = 2;
        public int MonsterEnemyStrength = 0;
        public int EliteEnemyStrength = 1;
        public int BossEnemyStrength = 2;

        [Header("UI References")]
        public Canvas BattleCanvas;
        public GameObject BattlePanel;
        public TMP_Text HudText;
        public TMP_Text IntentText;
        public TMP_Text PlayerStatusText;
        public TMP_Text EnemyStatusText;
        public TMP_Text RelicListText;
        public TMP_Text PotionHintText;
        public Button EnemyTargetButton;
        public Button PotionSlot0Button;
        public Button PotionSlot1Button;
        public Button PotionSlot2Button;
        public TMP_Text HandTitleText;
        public Transform HandRoot;
        public Button EndTurnButton;
        public GameObject RewardPanel;
        public TMP_Text RewardTitleText;
        public Transform RewardCardRoot;
        public Button RewardSkipButton;
        public GameObject DefeatPanel;
        public Button RestartBattleButton;
        public TMP_Text DefeatText;

        private BattleState _state = BattleState.None;
        private int _activeNodeId = -1;
        private MapRoomType _activeRoomType;
        private IRoomCompletionSink _completionSink;

        private BattleActor _player;
        private BattleActor _enemy;
        private DeckState _deckState;
        private List<string> _relicIds;
        private MapRunState _runState;
        private System.Random _battleRng;
        private System.Random _rewardRng;
        private BattleEventBus _eventBus;
        private RelicSystem _relicSystem;

        private readonly List<BattleCardInstance> _drawPile = new List<BattleCardInstance>();
        private readonly List<BattleCardInstance> _hand = new List<BattleCardInstance>();
        private readonly List<BattleCardInstance> _discardPile = new List<BattleCardInstance>();
        private readonly List<GameObject> _handCardViews = new List<GameObject>();
        private readonly List<GameObject> _rewardCardViews = new List<GameObject>();
        private readonly List<CardDefinitionRuntime> _currentRewards = new List<CardDefinitionRuntime>();
        private List<string> _potionSlots;
        private bool _awaitingPotionTarget;
        private int _pendingPotionSlot = -1;

        /// <summary>
        /// True while battle panel is active.
        /// </summary>
        public bool IsInBattle { get; private set; }

        private void Awake()
        {
            _eventBus = new BattleEventBus();
            _relicSystem = new RelicSystem(this);
            EnsureUi();
            BindButtons();
            CloseBattle();
        }

        /// <summary>
        /// Closes battle UI and clears active battle state.
        /// </summary>
        public void CloseBattle()
        {
            HideBattle();
        }

        /// <summary>
        /// Starts a new battle for selected map node.
        /// </summary>
        public void StartBattle(
            int nodeId,
            MapRoomType roomType,
            IRoomCompletionSink completionSink,
            DeckState deckState,
            List<string> relicIds,
            MapRunState runState,
            List<string> potionSlots,
            int battleSeed,
            int rewardSeed)
        {
            EnsureUi();
            BindButtons();
            _activeNodeId = nodeId;
            _activeRoomType = roomType;
            _completionSink = completionSink;
            _deckState = deckState;
            _relicIds = relicIds;
            _runState = runState;
            _potionSlots = potionSlots;
            _awaitingPotionTarget = false;
            _pendingPotionSlot = -1;
            _battleRng = new System.Random(battleSeed);
            _rewardRng = new System.Random(rewardSeed);
            IsInBattle = true;
            _state = BattleState.StartBattle;

            if (_player == null)
            {
                _player = new BattleActor("Player", MaxPlayerHp);
            }
            else
            {
                _player.MaxHp = MaxPlayerHp;
            }

            int runHp = _runState != null ? _runState.currentPlayerHp : _player.CurrentHp;
            if (runHp <= 0 || runHp > _player.MaxHp)
            {
                runHp = _player.MaxHp;
            }
            _player.CurrentHp = runHp;
            _player.Block = 0;
            _player.Energy = 0;
            _player.Statuses = new StatusSet();

            _enemy = BuildEnemy(roomType);

            _drawPile.Clear();
            _hand.Clear();
            _discardPile.Clear();
            BuildDeckFromState(_deckState, _drawPile);
            Shuffle(_drawPile, _battleRng);
            _relicSystem.Initialize(relicIds, _eventBus);

            ShowBattle();
            Debug.Log($"EnterBattle nodeId={_activeNodeId} roomType={_activeRoomType}", this);
            StartPlayerTurn();
            _eventBus.RaiseBattleStart();
            RefreshUi();
        }

        private void StartPlayerTurn()
        {
            if (!IsInBattle)
            {
                return;
            }

            _state = BattleState.PlayerTurn;
            _player.Block = 0;
            _player.Energy = EnergyPerTurn;
            _eventBus.RaiseTurnStart();
            DrawCards(DrawPerTurn);
            RefreshUi();
        }

        private void EndPlayerTurn()
        {
            if (_state != BattleState.PlayerTurn)
            {
                return;
            }

            DiscardHand();
            _player.Statuses.TickOwnerTurnEnd();
            _state = BattleState.EnemyTurn;
            ResolveEnemyTurn();
        }

        private void ResolveEnemyTurn()
        {
            _enemy.Block = 0;
            if (_enemy.CurrentIntentType == EnemyIntentType.Attack)
            {
                int finalDamage = BattleResolver.CalculateFinalDamage(_enemy.IntentBaseDamage, _enemy, _player);
                BattleResolver.DealDamage(finalDamage, _enemy, _player);
                SyncPlayerHpToRunState();
                Debug.Log($"[Battle] Enemy dealt {finalDamage} damage.", this);
            }
            else
            {
                BattleResolver.ApplyStatus(StatusType.Strength, _enemy.IntentBuffValue, _enemy);
                Debug.Log($"[Battle] Enemy buffed Strength +{_enemy.IntentBuffValue}.", this);
            }

            _enemy.Statuses.TickOwnerTurnEnd();
            AdvanceEnemyIntent(_enemy);
            if (_player.CurrentHp <= 0)
            {
                EnterDefeat();
                return;
            }

            StartPlayerTurn();
        }

        private void PlayCard(BattleCardInstance card)
        {
            if (_state != BattleState.PlayerTurn)
            {
                return;
            }

            if (_player.Energy < card.Definition.cost)
            {
                return;
            }

            _player.Energy -= card.Definition.cost;
            BattleResolver.ResolveCard(card.Definition, _player, _enemy, DrawCards);
            _eventBus.RaisePlayCard(card);
            _hand.Remove(card);
            _discardPile.Add(card);
            SyncPlayerHpToRunState();

            if (_enemy.CurrentHp <= 0)
            {
                _eventBus.RaiseVictory();
                if (_activeRoomType == MapRoomType.Boss)
                {
                    CompleteWithReward(null);
                }
                else
                {
                    EnterReward();
                }

                return;
            }

            RefreshUi();
        }

        private void EnterReward()
        {
            _state = BattleState.Reward;
            BuildRewardChoices();
            ShowReward();
            RefreshUi();
        }

        private void EnterDefeat()
        {
            _state = BattleState.Defeat;
            ShowDefeat();
            RefreshUi();
        }

        private void CompleteWithReward(string rewardedCardId)
        {
            int nodeId = _activeNodeId;
            MapRoomType roomType = _activeRoomType;
            var sink = _completionSink;
            HideBattle();
            Debug.Log($"Victory Continue clicked nodeId={nodeId}", this);
            sink?.CompleteRoom(nodeId, roomType, RoomCompletionResult.Cleared, rewardedCardId);
        }

        private void RestartAfterDefeat()
        {
            if (_state != BattleState.Defeat)
            {
                return;
            }

            _player.CurrentHp = _player.MaxHp;
            StartBattle(_activeNodeId, _activeRoomType, _completionSink, _deckState, _relicIds, _runState, _potionSlots, _battleRng.Next(), _rewardRng.Next());
        }

        private void OnRewardSkipClicked()
        {
            if (_state != BattleState.Reward)
            {
                return;
            }

            CompleteWithReward(null);
        }

        private void OnRewardCardClicked(CardDefinitionRuntime card)
        {
            if (_state != BattleState.Reward)
            {
                return;
            }

            CompleteWithReward(card.id);
        }

        private void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (_drawPile.Count == 0)
                {
                    if (_discardPile.Count == 0)
                    {
                        return;
                    }

                    _drawPile.AddRange(_discardPile);
                    _discardPile.Clear();
                    Shuffle(_drawPile, _battleRng);
                }

                int last = _drawPile.Count - 1;
                var card = _drawPile[last];
                _drawPile.RemoveAt(last);
                _hand.Add(card);
            }
        }

        private void DiscardHand()
        {
            _discardPile.AddRange(_hand);
            _hand.Clear();
        }

        private void RefreshUi()
        {
            if (!IsInBattle)
            {
                return;
            }

            if (HudText != null)
            {
                HudText.text =
                    $"Player HP {_player.CurrentHp}/{_player.MaxHp}  Block {_player.Block}  Energy {_player.Energy}\n" +
                    $"Enemy HP {_enemy.CurrentHp}/{_enemy.MaxHp}  Block {_enemy.Block}\n" +
                    $"Draw {_drawPile.Count}  Discard {_discardPile.Count}";
            }

            if (IntentText != null)
            {
                if (_enemy.CurrentIntentType == EnemyIntentType.Attack)
                {
                    int preview = BattleResolver.CalculateFinalDamage(_enemy.IntentBaseDamage, _enemy, _player);
                    IntentText.text = $"Intent: Attack {preview}";
                }
                else
                {
                    IntentText.text = $"Intent: Buff Strength +{_enemy.IntentBuffValue}";
                }
            }

            if (PlayerStatusText != null)
            {
                PlayerStatusText.text = $"Player Status: {_player.Statuses.ToDisplayString()}";
            }

            if (EnemyStatusText != null)
            {
                EnemyStatusText.text = $"Enemy Status: {_enemy.Statuses.ToDisplayString()}";
            }

            if (RelicListText != null)
            {
                var names = new List<string>();
                var relics = _relicSystem.ActiveRelics;
                for (int i = 0; i < relics.Count; i++)
                {
                    names.Add(relics[i].displayName);
                }

                RelicListText.text = names.Count == 0 ? "Relics: None" : $"Relics: {string.Join(", ", names)}";
            }

            if (PotionHintText != null)
            {
                PotionHintText.text = _awaitingPotionTarget ? "Potion Targeting: Click Enemy" : "Potions";
            }

            RefreshPotionButtons();

            if (HandTitleText != null)
            {
                HandTitleText.text = "Hand";
            }

            if (EndTurnButton != null)
            {
                EndTurnButton.interactable = _state == BattleState.PlayerTurn;
            }

            RebuildHandUi();
        }

        private void RebuildHandUi()
        {
            for (int i = 0; i < _handCardViews.Count; i++)
            {
                Destroy(_handCardViews[i]);
            }

            _handCardViews.Clear();
            if (HandRoot == null || _state == BattleState.Reward || _state == BattleState.Defeat)
            {
                return;
            }

            for (int i = 0; i < _hand.Count; i++)
            {
                var card = _hand[i];
                var cardGo = new GameObject($"Card_{i}_{card.Definition.displayName}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGo.transform.SetParent(HandRoot, false);
                var cardRect = cardGo.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(170f, 200f);

                var image = cardGo.GetComponent<Image>();
                image.color = new Color(0.2f, 0.2f, 0.25f, 0.95f);
                var btn = cardGo.GetComponent<Button>();
                btn.interactable = _state == BattleState.PlayerTurn && _player.Energy >= card.Definition.cost;
                BattleCardInstance localCard = card;
                btn.onClick.AddListener(() => PlayCard(localCard));

                var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(cardGo.transform, false);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;

                var tmp = label.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 22f;
                tmp.color = Color.white;
                tmp.text = $"{card.Definition.displayName}\nCost {card.Definition.cost}\n{card.Definition.description}";

                _handCardViews.Add(cardGo);
            }
        }

        private void BuildRewardChoices()
        {
            _currentRewards.Clear();
            var pool = CardLibrary.GetRewardPool();
            if (pool.Count == 0)
            {
                return;
            }

            var available = new List<CardDefinitionRuntime>(pool);
            int pick = Mathf.Min(3, available.Count);
            for (int i = 0; i < pick; i++)
            {
                int idx = _rewardRng.Next(0, available.Count);
                _currentRewards.Add(available[idx]);
                available.RemoveAt(idx);
            }

            RebuildRewardUi();
        }

        private void RebuildRewardUi()
        {
            for (int i = 0; i < _rewardCardViews.Count; i++)
            {
                Destroy(_rewardCardViews[i]);
            }

            _rewardCardViews.Clear();
            if (RewardCardRoot == null)
            {
                return;
            }

            for (int i = 0; i < _currentRewards.Count; i++)
            {
                var card = _currentRewards[i];
                var cardGo = new GameObject($"Reward_{card.displayName}", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGo.transform.SetParent(RewardCardRoot, false);
                var cardRect = cardGo.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(220f, 260f);
                cardGo.GetComponent<Image>().color = new Color(0.18f, 0.22f, 0.30f, 0.96f);
                var btn = cardGo.GetComponent<Button>();
                CardDefinitionRuntime localCard = card;
                btn.onClick.AddListener(() => OnRewardCardClicked(localCard));

                var label = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                label.transform.SetParent(cardGo.transform, false);
                var labelRect = label.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(8f, 8f);
                labelRect.offsetMax = new Vector2(-8f, -8f);
                var tmp = label.GetComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 21f;
                tmp.color = Color.white;
                tmp.text = $"{card.displayName}\nCost {card.cost}\n{card.description}";

                _rewardCardViews.Add(cardGo);
            }
        }

        private void ShowBattle()
        {
            if (BattleCanvas != null)
            {
                BattleCanvas.enabled = true;
            }

            if (BattlePanel != null)
            {
                BattlePanel.SetActive(true);
            }

            HideReward();
            HideDefeat();
        }

        private void HideBattle()
        {
            IsInBattle = false;
            _state = BattleState.None;
            _activeNodeId = -1;
            _completionSink = null;
            _deckState = null;
            _relicIds = null;
            _runState = null;
            _potionSlots = null;
            _battleRng = null;
            _rewardRng = null;
            _relicSystem.Dispose();

            ClearCardViews(_handCardViews);
            ClearCardViews(_rewardCardViews);
            _currentRewards.Clear();

            if (BattlePanel != null)
            {
                BattlePanel.SetActive(false);
            }

            if (BattleCanvas != null)
            {
                BattleCanvas.enabled = false;
            }
        }

        private void ShowReward()
        {
            if (RewardPanel != null)
            {
                RewardPanel.SetActive(true);
            }

            if (RewardTitleText != null)
            {
                RewardTitleText.text = "Choose A Card Reward";
            }
        }

        private void HideReward()
        {
            if (RewardPanel != null)
            {
                RewardPanel.SetActive(false);
            }
        }

        private void ShowDefeat()
        {
            if (DefeatPanel != null)
            {
                DefeatPanel.SetActive(true);
            }

            if (DefeatText != null)
            {
                DefeatText.text = "Defeat";
            }
        }

        private void HideDefeat()
        {
            if (DefeatPanel != null)
            {
                DefeatPanel.SetActive(false);
            }
        }

        private void BindButtons()
        {
            if (EndTurnButton != null)
            {
                EndTurnButton.onClick.RemoveListener(EndPlayerTurn);
                EndTurnButton.onClick.AddListener(EndPlayerTurn);
            }

            if (EnemyTargetButton != null)
            {
                EnemyTargetButton.onClick.RemoveAllListeners();
                EnemyTargetButton.onClick.AddListener(OnEnemyTargetClicked);
            }

            BindPotionButton(PotionSlot0Button, 0);
            BindPotionButton(PotionSlot1Button, 1);
            BindPotionButton(PotionSlot2Button, 2);

            if (RewardSkipButton != null)
            {
                RewardSkipButton.onClick.RemoveListener(OnRewardSkipClicked);
                RewardSkipButton.onClick.AddListener(OnRewardSkipClicked);
            }

            if (RestartBattleButton != null)
            {
                RestartBattleButton.onClick.RemoveListener(RestartAfterDefeat);
                RestartBattleButton.onClick.AddListener(RestartAfterDefeat);
            }
        }

        private BattleActor BuildEnemy(MapRoomType roomType)
        {
            int hp = roomType switch
            {
                MapRoomType.Boss => BossBaseHp,
                MapRoomType.Elite => 40,
                _ => 30
            };

            int str = roomType switch
            {
                MapRoomType.Boss => BossEnemyStrength,
                MapRoomType.Elite => EliteEnemyStrength,
                _ => MonsterEnemyStrength
            };

            var enemy = new BattleActor("Enemy", hp)
            {
                IntentBaseDamage = EnemyIntentBaseDamage
            };
            enemy.Statuses.Add(StatusType.Strength, str);
            enemy.CurrentIntentType = EnemyIntentType.Attack;
            if (roomType == MapRoomType.Boss)
            {
                enemy.IntentBaseDamage = BossAttackDamage;
                enemy.IntentBuffValue = BossBuffStrength;
            }

            return enemy;
        }

        private static void BuildDeckFromState(DeckState deckState, List<BattleCardInstance> deck)
        {
            deck.Clear();
            var defs = CardLibrary.GetAll();
            if (deckState != null)
            {
                for (int i = 0; i < deckState.cardIds.Count; i++)
                {
                    string cardId = deckState.cardIds[i];
                    if (defs.TryGetValue(cardId, out var def))
                    {
                        deck.Add(new BattleCardInstance(def));
                    }
                }
            }

            if (deck.Count == 0)
            {
                // Safety fallback.
                if (defs.TryGetValue("strike", out var strike))
                {
                    for (int i = 0; i < 5; i++) deck.Add(new BattleCardInstance(strike));
                }

                if (defs.TryGetValue("defend", out var defend))
                {
                    for (int i = 0; i < 5; i++) deck.Add(new BattleCardInstance(defend));
                }
            }
        }

        private static void Shuffle<T>(List<T> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void EnsureUi()
        {
            if (BattleCanvas != null && BattlePanel != null && HudText != null && IntentText != null &&
                PlayerStatusText != null && EnemyStatusText != null &&
                RelicListText != null &&
                PotionHintText != null && EnemyTargetButton != null &&
                PotionSlot0Button != null && PotionSlot1Button != null && PotionSlot2Button != null &&
                HandRoot != null && EndTurnButton != null && RewardPanel != null && RewardCardRoot != null &&
                RewardSkipButton != null && DefeatPanel != null && RestartBattleButton != null)
            {
                EnsureEventSystem();
                return;
            }

            if (BattleCanvas == null)
            {
                var canvasGo = new GameObject("BattleCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGo.transform.SetParent(transform, false);
                BattleCanvas = canvasGo.GetComponent<Canvas>();
                BattleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (BattlePanel == null)
            {
                var panelGo = new GameObject("BattlePanel", typeof(RectTransform), typeof(Image));
                panelGo.transform.SetParent(BattleCanvas.transform, false);
                var rect = panelGo.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                panelGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
                BattlePanel = panelGo;
            }

            if (HudText == null)
            {
                HudText = CreateText("HudText", BattlePanel.transform, new Vector2(0.5f, 0.92f), new Vector2(900f, 160f), 30f);
            }

            if (IntentText == null)
            {
                IntentText = CreateText("IntentText", BattlePanel.transform, new Vector2(0.5f, 0.74f), new Vector2(500f, 70f), 34f);
            }

            if (PlayerStatusText == null)
            {
                PlayerStatusText = CreateText("PlayerStatusText", BattlePanel.transform, new Vector2(0.3f, 0.62f), new Vector2(560f, 60f), 24f);
            }

            if (EnemyStatusText == null)
            {
                EnemyStatusText = CreateText("EnemyStatusText", BattlePanel.transform, new Vector2(0.7f, 0.62f), new Vector2(560f, 60f), 24f);
            }

            if (RelicListText == null)
            {
                RelicListText = CreateText("RelicListText", BattlePanel.transform, new Vector2(0.5f, 0.54f), new Vector2(1200f, 60f), 24f);
            }

            if (PotionHintText == null)
            {
                PotionHintText = CreateText("PotionHintText", BattlePanel.transform, new Vector2(0.5f, 0.44f), new Vector2(1000f, 56f), 24f);
            }

            if (EnemyTargetButton == null)
            {
                EnemyTargetButton = CreateButton("EnemyTargetButton", BattlePanel.transform, new Vector2(0.5f, 0.66f), new Vector2(260f, 62f), "Enemy Target");
                var img = EnemyTargetButton.GetComponent<Image>();
                if (img != null)
                {
                    img.color = new Color(0.45f, 0.2f, 0.2f, 1f);
                }
            }

            if (PotionSlot0Button == null)
            {
                PotionSlot0Button = CreateButton("PotionSlot0", BattlePanel.transform, new Vector2(0.2f, 0.08f), new Vector2(220f, 64f), "Empty");
            }

            if (PotionSlot1Button == null)
            {
                PotionSlot1Button = CreateButton("PotionSlot1", BattlePanel.transform, new Vector2(0.5f, 0.08f), new Vector2(220f, 64f), "Empty");
            }

            if (PotionSlot2Button == null)
            {
                PotionSlot2Button = CreateButton("PotionSlot2", BattlePanel.transform, new Vector2(0.8f, 0.08f), new Vector2(220f, 64f), "Empty");
            }

            if (HandTitleText == null)
            {
                HandTitleText = CreateText("HandTitle", BattlePanel.transform, new Vector2(0.5f, 0.47f), new Vector2(220f, 60f), 30f);
            }

            if (HandRoot == null)
            {
                HandRoot = CreateHorizontalRoot("HandRoot", BattlePanel.transform, new Vector2(0.5f, 0.18f), new Vector2(1200f, 220f), 14f);
            }

            if (EndTurnButton == null)
            {
                EndTurnButton = CreateButton("EndTurnButton", BattlePanel.transform, new Vector2(0.87f, 0.78f), new Vector2(220f, 72f), "End Turn");
            }

            if (RewardPanel == null)
            {
                RewardPanel = CreateOverlayPanel("RewardPanel", BattlePanel.transform, new Vector2(0.5f, 0.5f), new Vector2(1320f, 560f));
                RewardTitleText = CreateText("RewardTitle", RewardPanel.transform, new Vector2(0.5f, 0.86f), new Vector2(560f, 80f), 42f);
                RewardCardRoot = CreateHorizontalRoot("RewardCardRoot", RewardPanel.transform, new Vector2(0.5f, 0.52f), new Vector2(1140f, 300f), 18f);
                RewardSkipButton = CreateButton("SkipButton", RewardPanel.transform, new Vector2(0.5f, 0.14f), new Vector2(220f, 72f), "Skip");
                RewardPanel.SetActive(false);
            }

            if (DefeatPanel == null)
            {
                DefeatPanel = CreateResultPanel("DefeatPanel", BattlePanel.transform, out DefeatText, out RestartBattleButton, "Restart");
            }

            EnsureEventSystem();
        }

        private static Transform CreateHorizontalRoot(string name, Transform parent, Vector2 anchor, Vector2 size, float spacing)
        {
            var handGo = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            handGo.transform.SetParent(parent, false);
            var rect = handGo.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            var layout = handGo.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            return handGo.transform;
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
            rect.anchoredPosition = Vector2.zero;
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
            rect.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.15f, 0.5f, 0.22f, 1f);

            var txt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txt.transform.SetParent(go.transform, false);
            var txtRect = txt.GetComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;
            var tmp = txt.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 30f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return go.GetComponent<Button>();
        }

        private static GameObject CreateOverlayPanel(string name, Transform parent, Vector2 anchor, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.82f);
            return go;
        }

        private static GameObject CreateResultPanel(string name, Transform parent, out TMP_Text titleText, out Button actionButton, string buttonLabel)
        {
            var go = CreateOverlayPanel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(520f, 240f));
            titleText = CreateText("Title", go.transform, new Vector2(0.5f, 0.68f), new Vector2(380f, 80f), 54f);
            actionButton = CreateButton("ActionButton", go.transform, new Vector2(0.5f, 0.28f), new Vector2(260f, 72f), buttonLabel);
            go.SetActive(false);
            return go;
        }

        private static void ClearCardViews(List<GameObject> views)
        {
            for (int i = 0; i < views.Count; i++)
            {
                Destroy(views[i]);
            }

            views.Clear();
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

        public void GainPlayerBlock(int amount)
        {
            BattleResolver.GainBlock(amount, _player);
        }

        public void DrawPlayerCards(int amount)
        {
            DrawCards(amount);
        }

        public void GainPlayerEnergy(int amount)
        {
            BattleResolver.GainEnergy(amount, _player);
        }

        public int HealPlayer(int amount)
        {
            int healed = BattleResolver.Heal(_player, amount);
            SyncPlayerHpToRunState();
            return healed;
        }

        public void NotifyRelicTriggered(string message)
        {
            Debug.Log(message, this);
        }

        private void BindPotionButton(Button button, int index)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => TryUsePotion(index));
        }

        private void RefreshPotionButtons()
        {
            RefreshPotionButton(PotionSlot0Button, 0);
            RefreshPotionButton(PotionSlot1Button, 1);
            RefreshPotionButton(PotionSlot2Button, 2);
        }

        private void RefreshPotionButton(Button button, int index)
        {
            if (button == null)
            {
                return;
            }

            string potionId = GetPotionId(index);
            bool hasPotion = !string.IsNullOrWhiteSpace(potionId);
            string label = "Empty";
            if (hasPotion && PotionLibrary.TryGet(potionId, out var potion))
            {
                label = potion.displayName;
            }
            else if (hasPotion)
            {
                label = potionId;
            }

            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
            }

            button.interactable = hasPotion && _state == BattleState.PlayerTurn;
        }

        private string GetPotionId(int index)
        {
            if (_potionSlots == null || index < 0 || index >= _potionSlots.Count)
            {
                return string.Empty;
            }

            return _potionSlots[index];
        }

        private void TryUsePotion(int slotIndex)
        {
            if (_state != BattleState.PlayerTurn)
            {
                return;
            }

            string potionId = GetPotionId(slotIndex);
            if (string.IsNullOrWhiteSpace(potionId) || !PotionLibrary.TryGet(potionId, out var potion))
            {
                return;
            }

            if (potion.targeting == PotionTargeting.Enemy)
            {
                _awaitingPotionTarget = true;
                _pendingPotionSlot = slotIndex;
                if (PotionHintText != null)
                {
                    PotionHintText.text = "Potion Targeting: Click Enemy";
                }

                return;
            }

            ApplyPotion(slotIndex, potion);
        }

        private void OnEnemyTargetClicked()
        {
            if (!_awaitingPotionTarget || _pendingPotionSlot < 0)
            {
                return;
            }

            string potionId = GetPotionId(_pendingPotionSlot);
            if (!string.IsNullOrWhiteSpace(potionId) && PotionLibrary.TryGet(potionId, out var potion))
            {
                ApplyPotion(_pendingPotionSlot, potion);
            }
        }

        private void ApplyPotion(int slotIndex, PotionDefinitionRuntime potion)
        {
            switch (potion.effectType)
            {
                case PotionEffectType.Heal:
                    BattleResolver.Heal(_player, potion.effectValue);
                    break;
                case PotionEffectType.GainStrength:
                    BattleResolver.ApplyStatus(StatusType.Strength, potion.effectValue, _player);
                    break;
                case PotionEffectType.ApplyWeak:
                    BattleResolver.ApplyStatus(StatusType.Weak, potion.effectValue, _enemy);
                    break;
            }

            _potionSlots[slotIndex] = string.Empty;
            _awaitingPotionTarget = false;
            _pendingPotionSlot = -1;
            SyncPlayerHpToRunState();
            Debug.Log($"[Battle] Potion used: {potion.displayName}", this);
            RefreshUi();
        }

        private void SyncPlayerHpToRunState()
        {
            if (_runState == null || _player == null)
            {
                return;
            }

            _runState.currentPlayerHp = Mathf.Clamp(_player.CurrentHp, 0, _runState.maxPlayerHp);
        }

        private void AdvanceEnemyIntent(BattleActor enemy)
        {
            if (enemy == null)
            {
                return;
            }

            if (_activeRoomType != MapRoomType.Boss)
            {
                enemy.CurrentIntentType = EnemyIntentType.Attack;
                enemy.IntentBaseDamage = EnemyIntentBaseDamage;
                return;
            }

            if (enemy.CurrentIntentType == EnemyIntentType.Attack)
            {
                enemy.CurrentIntentType = EnemyIntentType.BuffStrength;
                enemy.IntentBuffValue = BossBuffStrength;
            }
            else
            {
                enemy.CurrentIntentType = EnemyIntentType.Attack;
                enemy.IntentBaseDamage = BossAttackDamage;
            }
        }
    }

    /// <summary>
    /// Runtime actor stats used in battle.
    /// </summary>
    [Serializable]
    public class BattleActor
    {
        public string Name;
        public int MaxHp;
        public int CurrentHp;
        public int Block;
        public int Energy;
        public int IntentBaseDamage;
        public int IntentBuffValue;
        public EnemyIntentType CurrentIntentType;
        public StatusSet Statuses = new StatusSet();

        public BattleActor(string name, int hp)
        {
            Name = name;
            MaxHp = hp;
            CurrentHp = hp;
            Block = 0;
            Energy = 0;
            IntentBaseDamage = 0;
            IntentBuffValue = 0;
            CurrentIntentType = EnemyIntentType.Attack;
        }
    }

    public enum EnemyIntentType
    {
        Attack,
        BuffStrength
    }

    /// <summary>
    /// Runtime card instance in battle piles.
    /// </summary>
    [Serializable]
    public class BattleCardInstance
    {
        public CardDefinitionRuntime Definition;

        public BattleCardInstance(CardDefinitionRuntime definition)
        {
            Definition = definition;
        }
    }

    /// <summary>
    /// Centralized combat value resolver.
    /// </summary>
    public static class BattleResolver
    {
        public static void ResolveCard(CardDefinitionRuntime card, BattleActor player, BattleActor enemy, Action<int> drawCards)
        {
            switch (card.effectType)
            {
                case CardEffectType.Damage:
                    int finalDamage = CalculateFinalDamage(card.effectValue, player, enemy);
                    DealDamage(finalDamage, player, enemy);
                    break;
                case CardEffectType.Block:
                    GainBlock(card.effectValue, player);
                    break;
                case CardEffectType.Draw:
                    drawCards?.Invoke(Mathf.Max(0, card.effectValue));
                    break;
                case CardEffectType.GainEnergy:
                    GainEnergy(card.effectValue, player);
                    break;
                case CardEffectType.ApplyWeak:
                    ApplyStatus(StatusType.Weak, card.effectValue, enemy);
                    break;
                case CardEffectType.ApplyVulnerable:
                    ApplyStatus(StatusType.Vulnerable, card.effectValue, enemy);
                    break;
                case CardEffectType.GainStrength:
                    ApplyStatus(StatusType.Strength, card.effectValue, player);
                    break;
            }
        }

        public static int CalculateFinalDamage(int baseDamage, BattleActor attacker, BattleActor defender)
        {
            int value = Mathf.Max(0, baseDamage);
            value += Mathf.Max(0, attacker.Statuses.Get(StatusType.Strength));

            if (attacker.Statuses.Get(StatusType.Weak) > 0)
            {
                value = Mathf.FloorToInt(value * 0.75f);
            }

            if (defender.Statuses.Get(StatusType.Vulnerable) > 0)
            {
                value = Mathf.FloorToInt(value * 1.5f);
            }

            return Mathf.Max(0, value);
        }

        public static void DealDamage(int amount, BattleActor source, BattleActor target)
        {
            int remaining = amount;
            if (target.Block > 0)
            {
                int blocked = Mathf.Min(target.Block, remaining);
                target.Block -= blocked;
                remaining -= blocked;
            }

            if (remaining > 0)
            {
                target.CurrentHp = Mathf.Max(0, target.CurrentHp - remaining);
            }
        }

        public static void GainBlock(int amount, BattleActor target)
        {
            target.Block += Mathf.Max(0, amount);
        }

        public static void GainEnergy(int amount, BattleActor target)
        {
            target.Energy += Mathf.Max(0, amount);
        }

        public static void ApplyStatus(StatusType statusType, int amount, BattleActor target)
        {
            target.Statuses.Add(statusType, Mathf.Max(0, amount));
        }

        public static int Heal(BattleActor target, int amount)
        {
            int before = target.CurrentHp;
            target.CurrentHp = Mathf.Min(target.MaxHp, target.CurrentHp + Mathf.Max(0, amount));
            return target.CurrentHp - before;
        }
    }
}
