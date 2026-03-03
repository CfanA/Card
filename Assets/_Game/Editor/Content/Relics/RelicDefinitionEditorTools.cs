#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardGame.Content
{
    /// <summary>
    /// Editor helper to create default RelicDefinition assets.
    /// </summary>
    public static class RelicDefinitionEditorTools
    {
        [MenuItem("Tools/Game/Map/Create Default Relic Definitions")]
        public static void CreateDefaultRelics()
        {
            const string folder = "Assets/_Game/Runtime/Map/Resources/Relics";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            CreateOrUpdate("anchor", "Anchor", "At battle start, gain 10 Block.", RelicRarity.Common, RelicEffectType.AnchorBattleStartBlock, 10);
            CreateOrUpdate("bag_of_preparation", "Bag of Preparation", "At battle start, draw 2 extra cards.", RelicRarity.Common, RelicEffectType.BagOfPreparationBattleStartDraw, 2);
            CreateOrUpdate("burning_blood", "Burning Blood", "On victory, heal 6 HP.", RelicRarity.Uncommon, RelicEffectType.BurningBloodVictoryHeal, 6);
            CreateOrUpdate("clockwork_heart", "Clockwork Heart", "At the start of each turn, gain 1 Energy.", RelicRarity.Rare, RelicEffectType.BossStartTurnEnergy, 1);
            CreateOrUpdate("ancient_tome", "Ancient Tome", "At the start of each turn, draw 1 card.", RelicRarity.Rare, RelicEffectType.BossStartTurnDraw, 1);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Relic] Default RelicDefinition assets created/updated.");
        }

        private static void CreateOrUpdate(
            string id,
            string displayName,
            string description,
            RelicRarity rarity,
            RelicEffectType effectType,
            int effectValue)
        {
            string path = $"Assets/_Game/Runtime/Map/Resources/Relics/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<RelicDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<RelicDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.id = id;
            asset.displayName = displayName;
            asset.description = description;
            asset.rarity = rarity;
            asset.effectType = effectType;
            asset.effectValue = effectValue;
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif

