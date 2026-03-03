using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Static relic data definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Map/Relic Definition", fileName = "RelicDefinition")]
    public class RelicDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public RelicRarity rarity;
        public RelicEffectType effectType;
        public int effectValue;
    }

    /// <summary>
    /// Relic rarity bucket.
    /// </summary>
    public enum RelicRarity
    {
        Common,
        Uncommon,
        Rare
    }

    /// <summary>
    /// Supported relic effects for MVP.
    /// </summary>
    public enum RelicEffectType
    {
        AnchorBattleStartBlock,
        BagOfPreparationBattleStartDraw,
        BurningBloodVictoryHeal,
        BossStartTurnEnergy,
        BossStartTurnDraw
    }
}
