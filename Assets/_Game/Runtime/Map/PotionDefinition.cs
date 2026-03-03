using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Static potion data definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Map/Potion Definition", fileName = "PotionDefinition")]
    public class PotionDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea] public string description;
        public PotionTargeting targeting;
        public PotionEffectType effectType;
        public int effectValue;
    }

    /// <summary>
    /// Targeting mode for potion usage.
    /// </summary>
    public enum PotionTargeting
    {
        None,
        Enemy,
        Self
    }

    /// <summary>
    /// Supported potion effects for MVP.
    /// </summary>
    public enum PotionEffectType
    {
        Heal,
        GainStrength,
        ApplyWeak
    }
}
