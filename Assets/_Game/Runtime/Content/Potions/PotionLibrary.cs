using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Content
{
    /// <summary>
    /// Runtime potion registry loaded from resources with fallback defaults.
    /// </summary>
    public static class PotionLibrary
    {
        private static Dictionary<string, PotionDefinitionRuntime> _byId;

        public static bool TryGet(string potionId, out PotionDefinitionRuntime definition)
        {
            EnsureLoaded();
            return _byId.TryGetValue(potionId, out definition);
        }

        public static List<PotionDefinitionRuntime> AllOrdered()
        {
            EnsureLoaded();
            return _byId.Values.OrderBy(p => p.id).ToList();
        }

        private static void EnsureLoaded()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<string, PotionDefinitionRuntime>();
            var assets = Resources.LoadAll<PotionDefinition>("Potions");
            for (int i = 0; i < assets.Length; i++)
            {
                var a = assets[i];
                if (a == null || string.IsNullOrWhiteSpace(a.id))
                {
                    continue;
                }

                _byId[a.id] = PotionDefinitionRuntime.FromAsset(a);
            }

            AddFallback("healing_potion", "Healing Potion", "Heal 10 HP.", PotionTargeting.Self, PotionEffectType.Heal, 10);
            AddFallback("strength_potion", "Strength Potion", "Gain 2 Strength.", PotionTargeting.Self, PotionEffectType.GainStrength, 2);
            AddFallback("weak_potion", "Weak Potion", "Apply 2 Weak to enemy.", PotionTargeting.Enemy, PotionEffectType.ApplyWeak, 2);
        }

        private static void AddFallback(string id, string name, string description, PotionTargeting targeting, PotionEffectType effectType, int effectValue)
        {
            if (_byId.ContainsKey(id))
            {
                return;
            }

            _byId[id] = new PotionDefinitionRuntime
            {
                id = id,
                displayName = name,
                description = description,
                targeting = targeting,
                effectType = effectType,
                effectValue = effectValue
            };
        }
    }

    /// <summary>
    /// Runtime-safe potion data.
    /// </summary>
    public class PotionDefinitionRuntime
    {
        public string id;
        public string displayName;
        public string description;
        public PotionTargeting targeting;
        public PotionEffectType effectType;
        public int effectValue;

        public static PotionDefinitionRuntime FromAsset(PotionDefinition asset)
        {
            return new PotionDefinitionRuntime
            {
                id = asset.id,
                displayName = string.IsNullOrWhiteSpace(asset.displayName) ? asset.id : asset.displayName,
                description = asset.description,
                targeting = asset.targeting,
                effectType = asset.effectType,
                effectValue = asset.effectValue
            };
        }
    }
}


