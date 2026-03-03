using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Content
{
    /// <summary>
    /// Static card data definition used to build runtime battle cards.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Map/Card Definition", fileName = "CardDefinition")]
    public class CardDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        public int cost;
        public CardType cardType;
        [TextArea] public string description;
        public CardEffectType effectType;
        public int effectValue;
        public bool hasUpgrade;
        public int upgradedCost;
        [TextArea] public string upgradedDescription;
        public CardEffectType upgradedEffectType;
        public int upgradedEffectValue;
    }

    /// <summary>
    /// High-level card category.
    /// </summary>
    public enum CardType
    {
        Attack,
        Skill
    }

    /// <summary>
    /// Supported effect types for MVP combat cards.
    /// </summary>
    public enum CardEffectType
    {
        Damage,
        Block,
        Draw,
        GainEnergy,
        ApplyWeak,
        ApplyVulnerable,
        GainStrength
    }
}


