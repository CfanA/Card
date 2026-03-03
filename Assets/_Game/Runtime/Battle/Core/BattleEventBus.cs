using System;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Battle
{
    /// <summary>
    /// Battle event hub for relic triggers.
    /// </summary>
    public class BattleEventBus
    {
        public event Action OnBattleStart;
        public event Action OnTurnStart;
        public event Action<BattleCardInstance> OnPlayCard;
        public event Action OnVictory;

        public void RaiseBattleStart() => OnBattleStart?.Invoke();
        public void RaiseTurnStart() => OnTurnStart?.Invoke();
        public void RaisePlayCard(BattleCardInstance card) => OnPlayCard?.Invoke(card);
        public void RaiseVictory() => OnVictory?.Invoke();
    }
}


