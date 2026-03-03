using System.Collections.Generic;

namespace Game.Map
{
    /// <summary>
    /// Executes relic effects by subscribing to battle events.
    /// </summary>
    public class RelicSystem
    {
        private readonly IRelicBattleApi _api;
        private readonly List<RelicDefinitionRuntime> _activeRelics = new List<RelicDefinitionRuntime>();
        private BattleEventBus _bus;

        public IReadOnlyList<RelicDefinitionRuntime> ActiveRelics => _activeRelics;

        public RelicSystem(IRelicBattleApi api)
        {
            _api = api;
        }

        public void Initialize(List<string> relicIds, BattleEventBus bus)
        {
            Dispose();
            _bus = bus;
            _activeRelics.Clear();

            if (relicIds != null)
            {
                for (int i = 0; i < relicIds.Count; i++)
                {
                    if (RelicLibrary.TryGet(relicIds[i], out var relic))
                    {
                        _activeRelics.Add(relic);
                    }
                }
            }

            if (_bus == null)
            {
                return;
            }

            _bus.OnBattleStart += HandleBattleStart;
            _bus.OnTurnStart += HandleTurnStart;
            _bus.OnPlayCard += HandlePlayCard;
            _bus.OnVictory += HandleVictory;
        }

        public void Dispose()
        {
            if (_bus != null)
            {
                _bus.OnBattleStart -= HandleBattleStart;
                _bus.OnTurnStart -= HandleTurnStart;
                _bus.OnPlayCard -= HandlePlayCard;
                _bus.OnVictory -= HandleVictory;
            }
        }

        private void HandleBattleStart()
        {
            for (int i = 0; i < _activeRelics.Count; i++)
            {
                var relic = _activeRelics[i];
                switch (relic.effectType)
                {
                    case RelicEffectType.AnchorBattleStartBlock:
                        _api.GainPlayerBlock(relic.effectValue);
                        _api.NotifyRelicTriggered($"Relic Triggered: {relic.displayName} +{relic.effectValue} Block");
                        break;
                    case RelicEffectType.BagOfPreparationBattleStartDraw:
                        _api.DrawPlayerCards(relic.effectValue);
                        _api.NotifyRelicTriggered($"Relic Triggered: {relic.displayName} Draw +{relic.effectValue}");
                        break;
                }
            }
        }

        private void HandleTurnStart()
        {
            for (int i = 0; i < _activeRelics.Count; i++)
            {
                var relic = _activeRelics[i];
                switch (relic.effectType)
                {
                    case RelicEffectType.BossStartTurnEnergy:
                        _api.GainPlayerEnergy(relic.effectValue);
                        _api.NotifyRelicTriggered($"Relic Triggered: {relic.displayName} +{relic.effectValue} Energy");
                        break;
                    case RelicEffectType.BossStartTurnDraw:
                        _api.DrawPlayerCards(relic.effectValue);
                        _api.NotifyRelicTriggered($"Relic Triggered: {relic.displayName} Draw +{relic.effectValue}");
                        break;
                }
            }
        }

        private void HandlePlayCard(BattleCardInstance card)
        {
        }

        private void HandleVictory()
        {
            for (int i = 0; i < _activeRelics.Count; i++)
            {
                var relic = _activeRelics[i];
                if (relic.effectType == RelicEffectType.BurningBloodVictoryHeal)
                {
                    int healed = _api.HealPlayer(relic.effectValue);
                    _api.NotifyRelicTriggered($"Relic Triggered: {relic.displayName} Heal +{healed}");
                }
            }
        }
    }

    /// <summary>
    /// Battle operations exposed to relic handlers.
    /// </summary>
    public interface IRelicBattleApi
    {
        void GainPlayerBlock(int amount);
        void GainPlayerEnergy(int amount);
        void DrawPlayerCards(int amount);
        int HealPlayer(int amount);
        void NotifyRelicTriggered(string message);
    }
}
