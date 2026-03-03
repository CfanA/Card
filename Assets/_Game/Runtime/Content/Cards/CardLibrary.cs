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
    /// Runtime card definition registry loaded from resources with fallback built-ins.
    /// </summary>
    public static class CardLibrary
    {
        private static Dictionary<string, CardDefinitionRuntime> _runtimeById;

        public static IReadOnlyDictionary<string, CardDefinitionRuntime> GetAll()
        {
            EnsureLoaded();
            return _runtimeById;
        }

        public static bool TryGet(string cardId, out CardDefinitionRuntime definition)
        {
            EnsureLoaded();
            return _runtimeById.TryGetValue(cardId, out definition);
        }

        public static List<CardDefinitionRuntime> GetRewardPool()
        {
            EnsureLoaded();
            return _runtimeById.Values
                .Where(c => c.id != "strike" && c.id != "defend" && !IsUpgradedId(c.id))
                .OrderBy(c => c.id)
                .ToList();
        }

        public static bool IsUpgradedId(string cardId)
        {
            return !string.IsNullOrWhiteSpace(cardId) && cardId.EndsWith("_plus");
        }

        public static string GetBaseId(string cardId)
        {
            if (IsUpgradedId(cardId))
            {
                return cardId.Substring(0, cardId.Length - 5);
            }

            return cardId;
        }

        public static string GetUpgradedId(string cardId)
        {
            string baseId = GetBaseId(cardId);
            return $"{baseId}_plus";
        }

        public static bool CanUpgrade(string cardId)
        {
            EnsureLoaded();
            if (IsUpgradedId(cardId))
            {
                return false;
            }

            return _runtimeById.ContainsKey(GetUpgradedId(cardId));
        }

        private static void EnsureLoaded()
        {
            if (_runtimeById != null)
            {
                return;
            }

            _runtimeById = new Dictionary<string, CardDefinitionRuntime>();
            var definitions = Resources.LoadAll<CardDefinition>("Cards");
            for (int i = 0; i < definitions.Length; i++)
            {
                var def = definitions[i];
                if (def == null || string.IsNullOrWhiteSpace(def.id))
                {
                    continue;
                }

                _runtimeById[def.id] = CardDefinitionRuntime.FromAsset(def);
                TryCreateUpgradeEntry(_runtimeById[def.id]);
            }

            // Built-in fallback cards so MVP works even before assets are created.
            AddFallback("strike", "Strike", 1, CardType.Attack, "Deal 6 damage.", CardEffectType.Damage, 6, true, 1, "Deal 9 damage.", CardEffectType.Damage, 9);
            AddFallback("defend", "Defend", 1, CardType.Skill, "Gain 5 block.", CardEffectType.Block, 5, true, 1, "Gain 8 block.", CardEffectType.Block, 8);
            AddFallback("big_strike", "Big Strike", 1, CardType.Attack, "Deal 10 damage.", CardEffectType.Damage, 10);
            AddFallback("block_plus", "Block Plus", 1, CardType.Skill, "Gain 8 block.", CardEffectType.Block, 8);
            AddFallback("draw2", "Draw 2", 1, CardType.Skill, "Draw 2 cards.", CardEffectType.Draw, 2);
            AddFallback("gain_energy", "Gain Energy", 0, CardType.Skill, "Gain 1 energy.", CardEffectType.GainEnergy, 1);
            AddFallback("apply_weak", "Apply Weak", 1, CardType.Skill, "Apply 2 Weak to enemy.", CardEffectType.ApplyWeak, 2, true, 1, "Apply 3 Weak to enemy.", CardEffectType.ApplyWeak, 3);
            AddFallback("apply_vuln", "Apply Vulnerable", 1, CardType.Skill, "Apply 2 Vulnerable to enemy.", CardEffectType.ApplyVulnerable, 2);
            AddFallback("gain_strength", "Gain Strength", 1, CardType.Skill, "Gain 2 Strength.", CardEffectType.GainStrength, 2, true, 1, "Gain 3 Strength.", CardEffectType.GainStrength, 3);
        }

        private static void AddFallback(
            string id,
            string name,
            int cost,
            CardType cardType,
            string description,
            CardEffectType effectType,
            int effectValue,
            bool hasUpgrade = false,
            int upgradedCost = 0,
            string upgradedDescription = null,
            CardEffectType upgradedEffectType = CardEffectType.Damage,
            int upgradedEffectValue = 0)
        {
            if (_runtimeById.ContainsKey(id))
            {
                return;
            }

            _runtimeById[id] = new CardDefinitionRuntime
            {
                id = id,
                displayName = name,
                cost = cost,
                cardType = cardType,
                description = description,
                effectType = effectType,
                effectValue = effectValue,
                hasUpgrade = hasUpgrade,
                upgradedCost = upgradedCost,
                upgradedDescription = upgradedDescription,
                upgradedEffectType = upgradedEffectType,
                upgradedEffectValue = upgradedEffectValue
            };

            TryCreateUpgradeEntry(_runtimeById[id]);
        }

        private static void TryCreateUpgradeEntry(CardDefinitionRuntime baseDef)
        {
            if (baseDef == null || !baseDef.hasUpgrade)
            {
                return;
            }

            string upgradedId = GetUpgradedId(baseDef.id);
            if (_runtimeById.ContainsKey(upgradedId))
            {
                return;
            }

            _runtimeById[upgradedId] = new CardDefinitionRuntime
            {
                id = upgradedId,
                displayName = $"{baseDef.displayName}+",
                cost = baseDef.upgradedCost > 0 ? baseDef.upgradedCost : baseDef.cost,
                cardType = baseDef.cardType,
                description = string.IsNullOrWhiteSpace(baseDef.upgradedDescription) ? baseDef.description : baseDef.upgradedDescription,
                effectType = baseDef.upgradedEffectValue > 0 ? baseDef.upgradedEffectType : baseDef.effectType,
                effectValue = baseDef.upgradedEffectValue > 0 ? baseDef.upgradedEffectValue : baseDef.effectValue,
                hasUpgrade = false,
                upgradedCost = 0,
                upgradedDescription = string.Empty,
                upgradedEffectType = baseDef.effectType,
                upgradedEffectValue = 0
            };
        }
    }

    /// <summary>
    /// Serializable runtime-safe card data.
    /// </summary>
    public class CardDefinitionRuntime
    {
        public string id;
        public string displayName;
        public int cost;
        public CardType cardType;
        public string description;
        public CardEffectType effectType;
        public int effectValue;
        public bool hasUpgrade;
        public int upgradedCost;
        public string upgradedDescription;
        public CardEffectType upgradedEffectType;
        public int upgradedEffectValue;

        public static CardDefinitionRuntime FromAsset(CardDefinition asset)
        {
            return new CardDefinitionRuntime
            {
                id = asset.id,
                displayName = string.IsNullOrWhiteSpace(asset.displayName) ? asset.id : asset.displayName,
                cost = asset.cost,
                cardType = asset.cardType,
                description = asset.description,
                effectType = asset.effectType,
                effectValue = asset.effectValue,
                hasUpgrade = asset.hasUpgrade,
                upgradedCost = asset.upgradedCost,
                upgradedDescription = asset.upgradedDescription,
                upgradedEffectType = asset.upgradedEffectType,
                upgradedEffectValue = asset.upgradedEffectValue
            };
        }
    }
}


