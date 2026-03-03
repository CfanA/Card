using System.Linq;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CardGame.Map
{
    /// <summary>
    /// Scene entry point: generates and visualizes the map automatically on Play.
    /// </summary>
    public class MapSceneBootstrap : MonoBehaviour, IRoomCompletionSink
    {
        [Header("Generation")]
        public int RunSeed = 123456;
        public bool RegenerateOnStart = true;
        public bool RegenerateOnValidate = false;
        public bool SaveSummaryOnGenerate = true;
        public bool DrawGizmosDebug = true;
        public int StartGold = 99;
        public bool ShowGoldHud = true;

        [Header("Gizmos")]
        public float NodeRadius = 0.32f;
        public Color NodeColor = new Color(0.15f, 0.8f, 1f, 1f);
        public Color EdgeColor = new Color(0.95f, 0.95f, 0.95f, 0.9f);
        public Color StartColor = new Color(0.2f, 1f, 0.4f, 1f);
        public Color BossColor = new Color(1f, 0.2f, 0.2f, 1f);
        public Color CurrentNodeColor = new Color(1f, 0.85f, 0.2f, 1f);
        public Color AvailableNodeColor = new Color(1f, 0.95f, 0.35f, 1f);
        public Color SelectedPathEdgeColor = new Color(1f, 0.75f, 0.2f, 1f);
        public Color AvailableEdgeColor = new Color(0.25f, 1f, 0.65f, 1f);
        public Color LockedNodeTint = new Color(0.55f, 0.55f, 0.55f, 1f);

        [SerializeField] private MapGraph _graph;
        [SerializeField] private MapRunState _runState = new MapRunState();
        [SerializeField] private string _lastValidationSummary;
        [SerializeField] private string _lastSummaryPath;
        [SerializeField] private MapRuntimeRenderer _runtimeRenderer;
        [SerializeField] private MapCameraFitter _cameraFitter;
        [SerializeField] private MapCameraPanZoom _cameraPanZoom;
        [SerializeField] private RoomController _roomController;
        [SerializeField] private BattleController _battleController;
        [SerializeField] private PotionRewardController _potionRewardController;
        [SerializeField] private EventController _eventController;
        [SerializeField] private ShopController _shopController;
        [SerializeField] private RestController _restController;
        [SerializeField] private BossRewardController _bossRewardController;
        [SerializeField] private VictorySummaryController _victorySummaryController;

        private readonly System.Collections.Generic.HashSet<int> _availableNodeIds = new System.Collections.Generic.HashSet<int>();

        private void Start()
        {
            if (RegenerateOnStart)
            {
                Generate();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying || _graph == null || _runState.routeLocked || _runState.isInRoom)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TrySelectNodeFromMouse();
            }
        }

        private void OnValidate()
        {
            if (!Application.isPlaying && RegenerateOnValidate)
            {
                Generate();
            }
        }

        [ContextMenu("Generate Map")]
        public void Generate()
        {
            _graph = MapGenerator.Generate(RunSeed);
            var report = MapValidator.Validate(_graph);
            _lastValidationSummary = BuildValidationSummary(report);
            EnsureRoomController();
            EnsureBattleController();
            EnsurePotionRewardController();
            EnsureEventController();
            EnsureShopController();
            EnsureRestController();
            EnsureBossRewardController();
            EnsureVictorySummaryController();
            InitializeRouteSelection();
            _roomController.CloseRoom();
            _battleController.CloseBattle();
            _potionRewardController.Hide();
            _eventController.Hide();
            _shopController.Hide();
            _restController.Hide();
            _bossRewardController.Hide();
            _victorySummaryController.Hide();
            RebuildRuntimeView(forceFitCamera: true, preserveCameraState: false);
            if (SaveSummaryOnGenerate)
            {
                SaveSummary();
            }

            if (!report.IsValid)
            {
                Debug.LogWarning($"[Map] Validation failed. {_lastValidationSummary}", this);
            }
            else
            {
                Debug.Log($"[Map] Generated with seed={RunSeed}. {_lastValidationSummary}", this);
            }
        }

        private void OnDrawGizmos()
        {
            if (!DrawGizmosDebug)
            {
                return;
            }

            if (_graph == null || _graph.Nodes == null || _graph.Nodes.Count == 0)
            {
                return;
            }

            DrawEdges();
            DrawNodes();
        }

        private void DrawEdges()
        {
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                var node = _graph.Nodes[i];
                Vector3 from = ToWorld(node.Position);
                for (int j = 0; j < node.OutgoingNodeIds.Count; j++)
                {
                    int targetId = node.OutgoingNodeIds[j];
                    var target = _graph.GetNodeById(targetId);
                    Vector3 to = ToWorld(target.Position);
                    Gizmos.color = ColorForEdge(node.Id, targetId);
                    Gizmos.DrawLine(from, to);
                }
            }
        }

        private void DrawNodes()
        {
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                var node = _graph.Nodes[i];
                Gizmos.color = ColorForNode(node);
                Gizmos.DrawSphere(ToWorld(node.Position), NodeRadius);

#if UNITY_EDITOR
                Handles.Label(ToWorld(node.Position) + new Vector3(0.25f, 0.2f, 0f), Abbr(node.RoomType));
#endif
            }
        }

        private static Vector3 ToWorld(Vector2 p) => new Vector3(p.x, p.y, 0f);

        private Color ColorForNode(MapNode node)
        {
            if (node.Id == _runState.currentNodeId)
            {
                return CurrentNodeColor;
            }

            if (_availableNodeIds.Contains(node.Id))
            {
                return AvailableNodeColor;
            }

            bool isSelectedPath = _runState.visitedNodeIds.Contains(node.Id);
            if (!isSelectedPath && node.Layer <= CurrentLayer())
            {
                return LockedNodeTint;
            }

            return ColorForRoom(node.RoomType);
        }

        private Color ColorForRoom(MapRoomType type)
        {
            if (type == MapRoomType.Start)
            {
                return StartColor;
            }

            if (type == MapRoomType.Boss)
            {
                return BossColor;
            }

            return NodeColor;
        }

        private Color ColorForEdge(int fromId, int toId)
        {
            if (IsSelectedPathEdge(fromId, toId))
            {
                return SelectedPathEdgeColor;
            }

            if (fromId == _runState.currentNodeId && _availableNodeIds.Contains(toId))
            {
                return AvailableEdgeColor;
            }

            return EdgeColor;
        }

        private static string Abbr(MapRoomType type)
        {
            return type switch
            {
                MapRoomType.Monster => "M",
                MapRoomType.Elite => "E",
                MapRoomType.Event => "EV",
                MapRoomType.Shop => "S",
                MapRoomType.Rest => "R",
                MapRoomType.Treasure => "T",
                MapRoomType.Boss => "B",
                MapRoomType.Start => "ST",
                _ => "?"
            };
        }

        private string BuildValidationSummary(MapValidationReport report)
        {
            string counts = string.Join(", ", report.RoomTypeCounts.Select(kv => $"{kv.Key}:{kv.Value}"));
            return $"Valid={report.IsValid}, Reachable={report.ReachableNodeCount}/{report.TotalNodeCount}, BossReachable={report.IsBossReachable}, AvgOut={report.AverageOutDegree:F2}, Rooms=[{counts}]";
        }

        private void InitializeRouteSelection()
        {
            _availableNodeIds.Clear();

            if (_graph == null || _graph.NodeIdsByLayer.Count == 0 || _graph.NodeIdsByLayer[0].Count == 0)
            {
                _runState.gold = Mathf.Max(0, StartGold);
                _runState.maxPlayerHp = 80;
                _runState.currentPlayerHp = 80;
                _runState.currentNodeId = -1;
                _runState.pendingNodeId = -1;
                _runState.routeLocked = false;
                _runState.isInRoom = false;
                _runState.rewardRollCounter = 0;
                _runState.visitedNodeIds.Clear();
                _runState.availableNodeIds.Clear();
                _runState.deck.ResetStarterDeck();
                _runState.relicIds.Clear();
                _runState.potionSlots.Clear();
                for (int i = 0; i < 3; i++)
                {
                    _runState.potionSlots.Add(string.Empty);
                }
                return;
            }

            EnsureBattleController();
            int startId = _graph.NodeIdsByLayer[0][0];
            int maxHp = _battleController != null ? _battleController.MaxPlayerHp : 80;
            _runState.ResetForNewRun(RunSeed, startId, maxHp, StartGold);
            RefreshAvailableFromCurrent();
        }

        private void TrySelectNodeFromMouse()
        {
            if (Camera.main == null)
            {
                return;
            }

            Vector3 mouse = Input.mousePosition;
            Vector3 world = Camera.main.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, -Camera.main.transform.position.z));
            int clickedNodeId = FindNodeIdAtPoint(new Vector2(world.x, world.y), NodeRadius * 1.5f);
            if (clickedNodeId < 0 || !_availableNodeIds.Contains(clickedNodeId))
            {
                return;
            }

            EnterSelectedNodeRoom(clickedNodeId);
        }

        private int FindNodeIdAtPoint(Vector2 point, float pickRadius)
        {
            int bestId = -1;
            float bestDist = pickRadius;
            for (int i = 0; i < _graph.Nodes.Count; i++)
            {
                var node = _graph.Nodes[i];
                float d = Vector2.Distance(point, node.Position);
                if (d <= bestDist)
                {
                    bestDist = d;
                    bestId = node.Id;
                }
            }

            return bestId;
        }

        private void EnterSelectedNodeRoom(int nodeId)
        {
            var node = _graph.GetNodeById(nodeId);
            var current = _graph.GetNodeById(_runState.currentNodeId);
            if (node.Layer != current.Layer + 1 || !current.OutgoingNodeIds.Contains(nodeId))
            {
                return;
            }

            EnsureRoomController();
            EnsureBattleController();
            EnsureEventController();
            EnsureShopController();
            EnsureRestController();
            EnsureBossRewardController();
            EnsureVictorySummaryController();
            _roomController.CloseRoom();
            _shopController.Hide();
            _restController.Hide();
            _bossRewardController.Hide();
            _victorySummaryController.Hide();
            _runState.pendingNodeId = nodeId;
            _runState.isInRoom = true;
            if (IsCombatRoom(node.RoomType))
            {
                int battleSeed = NextRunSeed("battle", nodeId);
                int rewardSeed = NextRunSeed("reward", nodeId);
                _battleController.StartBattle(nodeId, node.RoomType, this, _runState.deck, _runState.relicIds, _runState, _runState.potionSlots, battleSeed, rewardSeed);
            }
            else
            {
                if (node.RoomType == MapRoomType.Event)
                {
                    var evt = RollEventDefinition(nodeId);
                    _eventController.OpenEvent(
                        nodeId,
                        node.RoomType,
                        evt,
                        this,
                        _runState,
                        GrantRelicFromEffect,
                        GrantPotionFromEffect,
                        AddCardToDeckFromEffect);
                }
                else if (node.RoomType == MapRoomType.Shop)
                {
                    int shopSeed = NextRunSeed("shop_goods", nodeId);
                    _shopController.OpenShop(
                        nodeId,
                        node.RoomType,
                        _runState,
                        this,
                        cardId => AddCardToDeckFromEffect(nodeId, cardId),
                        relicId => GrantRelicFromEffect(nodeId, relicId),
                        potionId => TryGrantPotionIntoSlots(nodeId, potionId, false),
                        shopSeed);
                }
                else if (node.RoomType == MapRoomType.Rest)
                {
                    _restController.OpenRest(nodeId, node.RoomType, this, _runState);
                }
                else if (node.RoomType == MapRoomType.Treasure)
                {
                    ResolveTreasurePotion(nodeId, node.RoomType);
                }
                else
                {
                    _roomController.EnterRoom(nodeId, node.RoomType, this);
                }
            }
        }

        public void CompleteRoom(int nodeId, MapRoomType roomType, RoomCompletionResult result, string rewardedCardId = null)
        {
            if (_graph == null)
            {
                return;
            }

            if (!_runState.isInRoom)
            {
                return;
            }

            if (result != RoomCompletionResult.Cleared)
            {
                _runState.isInRoom = false;
                _runState.pendingNodeId = -1;
                return;
            }

            if (_runState.pendingNodeId != nodeId)
            {
                Debug.LogWarning($"[Map] Completed room node mismatch. pending={_runState.pendingNodeId}, completed={nodeId}", this);
            }

            _runState.currentNodeId = nodeId;
            if (!_runState.visitedNodeIds.Contains(nodeId))
            {
                _runState.visitedNodeIds.Add(nodeId);
            }

            if (!string.IsNullOrWhiteSpace(rewardedCardId))
            {
                _runState.deck.AddCard(rewardedCardId);
                Debug.Log($"[Map] Reward card added to deck: {rewardedCardId}", this);
            }

            GrantGoldForRoom(roomType);

            _runState.pendingNodeId = -1;
            _runState.isInRoom = false;
            if (roomType == MapRoomType.Boss)
            {
                OpenBossRewardFlow(nodeId);
            }
            else
            {
                RecomputeMapProgression();
                RebuildRuntimeView(forceFitCamera: false, preserveCameraState: true);
                Debug.Log($"[Map] Completed room {roomType} at node {nodeId}.", this);
                Debug.Log($"CompleteRoom applied, new current={_runState.currentNodeId}, available=[{string.Join(",", _runState.availableNodeIds)}]", this);
            }
        }

        private void RefreshAvailableFromCurrent()
        {
            _availableNodeIds.Clear();
            _runState.availableNodeIds.Clear();
            if (_runState.currentNodeId < 0)
            {
                return;
            }

            var current = _graph.GetNodeById(_runState.currentNodeId);
            for (int i = 0; i < current.OutgoingNodeIds.Count; i++)
            {
                int nextId = current.OutgoingNodeIds[i];
                _availableNodeIds.Add(nextId);
                _runState.availableNodeIds.Add(nextId);
            }
        }

        private void RecomputeMapProgression()
        {
            RefreshAvailableFromCurrent();
            _runState.routeLocked = _availableNodeIds.Count == 0;
            if (_runState.routeLocked)
            {
                Debug.Log($"[Map] Route locked. Selected {_runState.visitedNodeIds.Count} nodes.", this);
            }
        }

        private bool IsSelectedPathEdge(int fromId, int toId)
        {
            for (int i = 0; i < _runState.visitedNodeIds.Count - 1; i++)
            {
                if (_runState.visitedNodeIds[i] == fromId && _runState.visitedNodeIds[i + 1] == toId)
                {
                    return true;
                }
            }

            return false;
        }

        private int CurrentLayer()
        {
            if (_runState.currentNodeId < 0 || _graph == null)
            {
                return -1;
            }

            return _graph.GetNodeById(_runState.currentNodeId).Layer;
        }

        [ContextMenu("Save Map Summary")]
        public void SaveSummary()
        {
            if (_graph == null)
            {
                Debug.LogWarning("[Map] Cannot save summary because graph is null.", this);
                return;
            }

            string outputDir = System.IO.Path.Combine(Application.persistentDataPath, "MapSummaries");
            string fileName = $"map_summary_seed_{RunSeed}.json";
            _lastSummaryPath = MapSummaryExporter.SaveSummaryToJson(_graph, outputDir, fileName);
            Debug.Log($"[Map] Summary saved: {_lastSummaryPath}", this);
        }

        private void RebuildRuntimeView(bool forceFitCamera, bool preserveCameraState)
        {
            if (_graph == null)
            {
                return;
            }

            Camera cam = Camera.main;
            Vector3 cachedPosition = default;
            float cachedOrtho = 0f;
            bool hasCachedCamera = false;
            if (preserveCameraState && cam != null)
            {
                cachedPosition = cam.transform.position;
                cachedOrtho = cam.orthographicSize;
                hasCachedCamera = true;
            }

            if (_runtimeRenderer == null)
            {
                _runtimeRenderer = GetComponent<MapRuntimeRenderer>();
            }

            if (_runtimeRenderer == null)
            {
                _runtimeRenderer = gameObject.AddComponent<MapRuntimeRenderer>();
            }

            _runtimeRenderer.Rebuild(
                _graph,
                _runState.currentNodeId,
                _availableNodeIds,
                _runState.visitedNodeIds,
                NodeColor,
                EdgeColor,
                StartColor,
                BossColor,
                CurrentNodeColor,
                AvailableNodeColor,
                SelectedPathEdgeColor,
                AvailableEdgeColor,
                LockedNodeTint);

            if (forceFitCamera)
            {
                FitCameraToMapView(true);
            }
            ConfigurePanZoom();

            if (preserveCameraState && hasCachedCamera && cam != null)
            {
                cam.transform.position = cachedPosition;
                if (cam.orthographic)
                {
                    cam.orthographicSize = cachedOrtho;
                }
            }
        }

        private void FitCameraToMapView(bool force)
        {
            if (_runtimeRenderer == null || _runtimeRenderer.ViewRoot == null)
            {
                return;
            }

            if (_cameraFitter == null)
            {
                _cameraFitter = GetComponent<MapCameraFitter>();
            }

            if (_cameraFitter == null)
            {
                _cameraFitter = gameObject.AddComponent<MapCameraFitter>();
            }

            _cameraFitter.TargetRoot = _runtimeRenderer.ViewRoot;
            if (_cameraFitter.TargetCamera == null)
            {
                _cameraFitter.TargetCamera = Camera.main;
            }

            if (force)
            {
                _cameraFitter.ForceFit();
            }
            else
            {
                _cameraFitter.Fit();
            }
        }

        private void ConfigurePanZoom()
        {
            if (_runtimeRenderer == null || _runtimeRenderer.ViewRoot == null)
            {
                return;
            }

            if (_cameraPanZoom == null)
            {
                _cameraPanZoom = GetComponent<MapCameraPanZoom>();
            }

            if (_cameraPanZoom == null)
            {
                _cameraPanZoom = gameObject.AddComponent<MapCameraPanZoom>();
            }

            _cameraPanZoom.TargetRoot = _runtimeRenderer.ViewRoot;
            if (_cameraPanZoom.TargetCamera == null)
            {
                _cameraPanZoom.TargetCamera = Camera.main;
            }

            _cameraPanZoom.RefreshBounds();
        }

        private void EnsureRoomController()
        {
            if (_roomController == null)
            {
                _roomController = GetComponent<RoomController>();
            }

            if (_roomController == null)
            {
                _roomController = gameObject.AddComponent<RoomController>();
            }
        }

        private void EnsureBattleController()
        {
            if (_battleController == null)
            {
                _battleController = GetComponent<BattleController>();
            }

            if (_battleController == null)
            {
                _battleController = gameObject.AddComponent<BattleController>();
            }
        }

        private static bool IsCombatRoom(MapRoomType roomType)
        {
            return roomType == MapRoomType.Monster
                   || roomType == MapRoomType.Elite
                   || roomType == MapRoomType.Boss;
        }

        private int NextRunSeed(string scope, int nodeId)
        {
            unchecked
            {
                int h = 17;
                h = h * 31 + _runState.seed;
                h = h * 31 + _runState.rewardRollCounter;
                h = h * 31 + nodeId;
                h = h * 31 + StableStringHash(scope);
                _runState.rewardRollCounter++;
                return h;
            }
        }

        private static int StableStringHash(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return 0;
            }

            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                {
                    hash = hash * 31 + value[i];
                }

                return hash;
            }
        }

        private void ResolveTreasurePotion(int nodeId, MapRoomType roomType)
        {
            EnsurePotionRewardController();
            string potionId = RollTreasurePotion(nodeId);
            if (!PotionLibrary.TryGet(potionId, out var potion))
            {
                CompleteRoom(nodeId, roomType, RoomCompletionResult.Cleared, null);
                return;
            }

            int emptyIndex = FindEmptyPotionSlot();
            if (emptyIndex >= 0)
            {
                TryGrantPotionIntoSlots(nodeId, potionId, false);
                CompleteRoom(nodeId, roomType, RoomCompletionResult.Cleared, null);
                return;
            }

            string[] slotNames = new string[3];
            for (int i = 0; i < 3; i++)
            {
                string id = _runState.potionSlots[i];
                slotNames[i] = PotionLibrary.TryGet(id, out var existing) ? existing.displayName : id;
            }

            _potionRewardController.ShowReplacePrompt(potion.displayName, slotNames, selectedSlot =>
            {
                if (selectedSlot.HasValue)
                {
                    int idx = Mathf.Clamp(selectedSlot.Value, 0, 2);
                    _runState.potionSlots[idx] = potionId;
                    Debug.Log($"[Map] Treasure potion replaced slot {idx} with {potionId}", this);
                }
                else
                {
                    Debug.Log($"[Map] Treasure potion skipped: {potionId}", this);
                }

                CompleteRoom(nodeId, roomType, RoomCompletionResult.Cleared, null);
            });
        }

        private int FindEmptyPotionSlot()
        {
            for (int i = 0; i < _runState.potionSlots.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(_runState.potionSlots[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        private string RollTreasurePotion(int nodeId)
        {
            int seed = NextRunSeed("potion_treasure", nodeId);
            var rng = new System.Random(seed);
            var pool = PotionLibrary.AllOrdered();
            if (pool.Count == 0)
            {
                return "healing_potion";
            }

            int idx = rng.Next(0, pool.Count);
            return pool[idx].id;
        }

        private void EnsurePotionRewardController()
        {
            if (_potionRewardController == null)
            {
                _potionRewardController = GetComponent<PotionRewardController>();
            }

            if (_potionRewardController == null)
            {
                _potionRewardController = gameObject.AddComponent<PotionRewardController>();
            }
        }

        private void EnsureEventController()
        {
            if (_eventController == null)
            {
                _eventController = GetComponent<EventController>();
            }

            if (_eventController == null)
            {
                _eventController = gameObject.AddComponent<EventController>();
            }
        }

        private void EnsureShopController()
        {
            if (_shopController == null)
            {
                _shopController = GetComponent<ShopController>();
            }

            if (_shopController == null)
            {
                _shopController = gameObject.AddComponent<ShopController>();
            }
        }

        private void EnsureRestController()
        {
            if (_restController == null)
            {
                _restController = GetComponent<RestController>();
            }

            if (_restController == null)
            {
                _restController = gameObject.AddComponent<RestController>();
            }
        }

        private void EnsureBossRewardController()
        {
            if (_bossRewardController == null)
            {
                _bossRewardController = GetComponent<BossRewardController>();
            }

            if (_bossRewardController == null)
            {
                _bossRewardController = gameObject.AddComponent<BossRewardController>();
            }
        }

        private void EnsureVictorySummaryController()
        {
            if (_victorySummaryController == null)
            {
                _victorySummaryController = GetComponent<VictorySummaryController>();
            }

            if (_victorySummaryController == null)
            {
                _victorySummaryController = gameObject.AddComponent<VictorySummaryController>();
            }
        }

        private EventDefinition RollEventDefinition(int nodeId)
        {
            var events = Resources.LoadAll<EventDefinition>("Events");
            if (events == null || events.Length == 0)
            {
                var fallback = ScriptableObject.CreateInstance<EventDefinition>();
                fallback.id = "fallback_event";
                fallback.title = "Quiet Hall";
                fallback.body = "Nothing happens.";
                fallback.options = new System.Collections.Generic.List<EventOption>
                {
                    new EventOption { buttonText = "Leave", resultText = "You move on.", endEvent = true }
                };
                return fallback;
            }

            int seed = NextRunSeed("event_pick", nodeId);
            var rng = new System.Random(seed);
            int idx = rng.Next(0, events.Length);
            return events[idx];
        }

        private string GrantRelicFromEffect(int nodeId, string desiredRelicId)
        {
            string relicId = desiredRelicId;
            if (string.IsNullOrWhiteSpace(relicId))
            {
                var pool = RelicLibrary.AllOrdered();
                if (pool.Count > 0)
                {
                    int seed = NextRunSeed("event_relic", nodeId);
                    var rng = new System.Random(seed);
                    relicId = pool[rng.Next(0, pool.Count)].id;
                }
            }

            if (string.IsNullOrWhiteSpace(relicId))
            {
                return string.Empty;
            }

            _runState.relicIds.Add(relicId);
            Debug.Log($"[Map] Event gained relic: {relicId}", this);
            return relicId;
        }

        private string GrantPotionFromEffect(int nodeId, string desiredPotionId)
        {
            string potionId = desiredPotionId;
            if (string.IsNullOrWhiteSpace(potionId))
            {
                var pool = PotionLibrary.AllOrdered();
                if (pool.Count > 0)
                {
                    int seed = NextRunSeed("event_potion", nodeId);
                    var rng = new System.Random(seed);
                    potionId = pool[rng.Next(0, pool.Count)].id;
                }
            }

            if (string.IsNullOrWhiteSpace(potionId))
            {
                return string.Empty;
            }

            bool ok = TryGrantPotionIntoSlots(nodeId, potionId, true);
            return ok ? potionId : string.Empty;
        }

        private void AddCardToDeckFromEffect(int nodeId, string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return;
            }

            _runState.deck.AddCard(cardId);
            Debug.Log($"[Map] Event added card to deck: {cardId}", this);
        }

        private bool TryGrantPotionIntoSlots(int nodeId, string potionId, bool allowReplace)
        {
            if (string.IsNullOrWhiteSpace(potionId))
            {
                return false;
            }

            int empty = FindEmptyPotionSlot();
            if (empty >= 0)
            {
                _runState.potionSlots[empty] = potionId;
                Debug.Log($"[Map] Potion gained: {potionId} -> slot {empty}", this);
                return true;
            }

            if (!allowReplace)
            {
                Debug.Log($"[Map] Potion not gained (slots full): {potionId}", this);
                return false;
            }

            int replace = NextRunSeed("potion_replace", nodeId) % 3;
            if (replace < 0) replace += 3;
            _runState.potionSlots[replace] = potionId;
            Debug.Log($"[Map] Potion replaced slot {replace}: {potionId}", this);
            return true;
        }

        private void GrantGoldForRoom(MapRoomType roomType)
        {
            int amount = roomType switch
            {
                MapRoomType.Monster => 15,
                MapRoomType.Elite => 30,
                MapRoomType.Boss => 100,
                MapRoomType.Treasure => 25,
                _ => 0
            };

            if (amount > 0)
            {
                _runState.AddGold(amount);
                Debug.Log($"[Map] Gold +{amount}, current={_runState.gold}", this);
            }
        }

        private void OnGUI()
        {
            if (!ShowGoldHud || _runState == null)
            {
                return;
            }

            GUI.Label(new Rect(18, 12, 280, 32), $"Gold: {_runState.gold}");
        }

        private void OpenBossRewardFlow(int bossNodeId)
        {
            EnsureBossRewardController();
            EnsureVictorySummaryController();
            var options = RollBossRelicOptions(bossNodeId);
            _bossRewardController.Open(options, selectedRelicId =>
            {
                if (!string.IsNullOrWhiteSpace(selectedRelicId))
                {
                    _runState.relicIds.Add(selectedRelicId);
                    Debug.Log($"[Boss] Selected relic: {selectedRelicId}", this);
                }

                ShowVictorySummary();
            });
        }

        private System.Collections.Generic.List<RelicDefinitionRuntime> RollBossRelicOptions(int nodeId)
        {
            var pool = RelicLibrary.GetBossRelicPool();
            var result = new System.Collections.Generic.List<RelicDefinitionRuntime>();
            if (pool.Count == 0)
            {
                return result;
            }

            var rng = new System.Random(NextRunSeed("boss_relic_options", nodeId));
            var copy = new System.Collections.Generic.List<RelicDefinitionRuntime>(pool);
            int take = Mathf.Min(3, copy.Count);
            for (int i = 0; i < take; i++)
            {
                int idx = rng.Next(0, copy.Count);
                result.Add(copy[idx]);
                copy.RemoveAt(idx);
            }

            return result;
        }

        private void ShowVictorySummary()
        {
            EnsureVictorySummaryController();
            int floorsCleared = Mathf.Max(0, _runState.visitedNodeIds.Count - 1);
            _victorySummaryController.Open(_runState, floorsCleared, RestartRun);
        }

        private void RestartRun()
        {
            EnsureRoomController();
            EnsureBattleController();
            EnsurePotionRewardController();
            EnsureEventController();
            EnsureShopController();
            EnsureRestController();
            EnsureBossRewardController();
            EnsureVictorySummaryController();
            _roomController.CloseRoom();
            _battleController.CloseBattle();
            _potionRewardController.Hide();
            _eventController.Hide();
            _shopController.Hide();
            _restController.Hide();
            _bossRewardController.Hide();
            _victorySummaryController.Hide();
            Generate();
        }
    }
}


