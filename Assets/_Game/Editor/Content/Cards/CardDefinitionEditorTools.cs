#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardGame.Content
{
    /// <summary>
    /// Editor helper to create default CardDefinition assets.
    /// </summary>
    public static class CardDefinitionEditorTools
    {
        [MenuItem("Tools/Game/Map/Create Default Card Definitions")]
        public static void CreateDefaultCardDefinitions()
        {
            const string folder = "Assets/_Game/Runtime/Map/Resources/Cards";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            CreateOrUpdate("strike", "Strike", 1, CardType.Attack, "Deal 6 damage.", CardEffectType.Damage, 6, true, 1, "Deal 9 damage.", CardEffectType.Damage, 9);
            CreateOrUpdate("defend", "Defend", 1, CardType.Skill, "Gain 5 block.", CardEffectType.Block, 5, true, 1, "Gain 8 block.", CardEffectType.Block, 8);
            CreateOrUpdate("big_strike", "Big Strike", 1, CardType.Attack, "Deal 10 damage.", CardEffectType.Damage, 10);
            CreateOrUpdate("block_plus", "Block Plus", 1, CardType.Skill, "Gain 8 block.", CardEffectType.Block, 8);
            CreateOrUpdate("draw2", "Draw 2", 1, CardType.Skill, "Draw 2 cards.", CardEffectType.Draw, 2);
            CreateOrUpdate("gain_energy", "Gain Energy", 0, CardType.Skill, "Gain 1 energy.", CardEffectType.GainEnergy, 1);
            CreateOrUpdate("apply_weak", "Apply Weak", 1, CardType.Skill, "Apply 2 Weak to enemy.", CardEffectType.ApplyWeak, 2, true, 1, "Apply 3 Weak to enemy.", CardEffectType.ApplyWeak, 3);
            CreateOrUpdate("apply_vuln", "Apply Vulnerable", 1, CardType.Skill, "Apply 2 Vulnerable to enemy.", CardEffectType.ApplyVulnerable, 2);
            CreateOrUpdate("gain_strength", "Gain Strength", 1, CardType.Skill, "Gain 2 Strength.", CardEffectType.GainStrength, 2, true, 1, "Gain 3 Strength.", CardEffectType.GainStrength, 3);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Card] Default CardDefinition assets created/updated.");
        }

        private static void CreateOrUpdate(
            string id,
            string displayName,
            int cost,
            CardType type,
            string description,
            CardEffectType effectType,
            int effectValue,
            bool hasUpgrade = false,
            int upgradedCost = 0,
            string upgradedDescription = "",
            CardEffectType upgradedEffectType = CardEffectType.Damage,
            int upgradedEffectValue = 0)
        {
            string path = $"Assets/_Game/Runtime/Map/Resources/Cards/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<CardDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.id = id;
            asset.displayName = displayName;
            asset.cost = cost;
            asset.cardType = type;
            asset.description = description;
            asset.effectType = effectType;
            asset.effectValue = effectValue;
            asset.hasUpgrade = hasUpgrade;
            asset.upgradedCost = upgradedCost;
            asset.upgradedDescription = upgradedDescription;
            asset.upgradedEffectType = upgradedEffectType;
            asset.upgradedEffectValue = upgradedEffectValue;
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif

