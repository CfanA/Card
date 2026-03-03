using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Runtime relic registry loaded from resources with fallback defaults.
    /// </summary>
    public static class RelicLibrary
    {
        private static Dictionary<string, RelicDefinitionRuntime> _byId;

        public static IReadOnlyDictionary<string, RelicDefinitionRuntime> GetAll()
        {
            EnsureLoaded();
            return _byId;
        }

        public static bool TryGet(string relicId, out RelicDefinitionRuntime definition)
        {
            EnsureLoaded();
            return _byId.TryGetValue(relicId, out definition);
        }

        public static List<RelicDefinitionRuntime> AllOrdered()
        {
            EnsureLoaded();
            return _byId.Values.OrderBy(r => r.id).ToList();
        }

        public static List<RelicDefinitionRuntime> GetBossRelicPool()
        {
            EnsureLoaded();
            return _byId.Values
                .Where(r => r.effectType == RelicEffectType.BossStartTurnEnergy
                            || r.effectType == RelicEffectType.BossStartTurnDraw
                            || r.effectType == RelicEffectType.BurningBloodVictoryHeal
                            || r.effectType == RelicEffectType.AnchorBattleStartBlock
                            || r.effectType == RelicEffectType.BagOfPreparationBattleStartDraw)
                .OrderBy(r => r.id)
                .ToList();
        }

        private static void EnsureLoaded()
        {
            if (_byId != null)
            {
                return;
            }

            _byId = new Dictionary<string, RelicDefinitionRuntime>();
            var assets = Resources.LoadAll<RelicDefinition>("Relics");
            for (int i = 0; i < assets.Length; i++)
            {
                var a = assets[i];
                if (a == null || string.IsNullOrWhiteSpace(a.id))
                {
                    continue;
                }

                _byId[a.id] = RelicDefinitionRuntime.FromAsset(a);
            }

            AddFallback("anchor", "Anchor", "At battle start, gain 10 Block.", RelicRarity.Common, RelicEffectType.AnchorBattleStartBlock, 10);
            AddFallback("bag_of_preparation", "Bag of Preparation", "At battle start, draw 2 extra cards.", RelicRarity.Common, RelicEffectType.BagOfPreparationBattleStartDraw, 2);
            AddFallback("burning_blood", "Burning Blood", "On victory, heal 6 HP.", RelicRarity.Uncommon, RelicEffectType.BurningBloodVictoryHeal, 6);
            AddFallback("clockwork_heart", "Clockwork Heart", "At the start of each turn, gain 1 Energy.", RelicRarity.Rare, RelicEffectType.BossStartTurnEnergy, 1);
            AddFallback("ancient_tome", "Ancient Tome", "At the start of each turn, draw 1 card.", RelicRarity.Rare, RelicEffectType.BossStartTurnDraw, 1);
        }

        private static void AddFallback(string id, string name, string description, RelicRarity rarity, RelicEffectType effectType, int effectValue)
        {
            if (_byId.ContainsKey(id))
            {
                return;
            }

            _byId[id] = new RelicDefinitionRuntime
            {
                id = id,
                displayName = name,
                description = description,
                rarity = rarity,
                effectType = effectType,
                effectValue = effectValue
            };
        }
    }

    /// <summary>
    /// Runtime-safe relic data.
    /// </summary>
    public class RelicDefinitionRuntime
    {
        public string id;
        public string displayName;
        public string description;
        public RelicRarity rarity;
        public RelicEffectType effectType;
        public int effectValue;

        public static RelicDefinitionRuntime FromAsset(RelicDefinition asset)
        {
            return new RelicDefinitionRuntime
            {
                id = asset.id,
                displayName = string.IsNullOrWhiteSpace(asset.displayName) ? asset.id : asset.displayName,
                description = asset.description,
                rarity = asset.rarity,
                effectType = asset.effectType,
                effectValue = asset.effectValue
            };
        }
    }
}
