#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CardGame.Content
{
    /// <summary>
    /// Editor helper to create default PotionDefinition assets.
    /// </summary>
    public static class PotionDefinitionEditorTools
    {
        [MenuItem("Tools/Game/Map/Create Default Potion Definitions")]
        public static void CreateDefaultPotions()
        {
            const string folder = "Assets/_Game/Runtime/Map/Resources/Potions";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            CreateOrUpdate("healing_potion", "Healing Potion", "Heal 10 HP.", PotionTargeting.Self, PotionEffectType.Heal, 10);
            CreateOrUpdate("strength_potion", "Strength Potion", "Gain 2 Strength.", PotionTargeting.Self, PotionEffectType.GainStrength, 2);
            CreateOrUpdate("weak_potion", "Weak Potion", "Apply 2 Weak to enemy.", PotionTargeting.Enemy, PotionEffectType.ApplyWeak, 2);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Potion] Default PotionDefinition assets created/updated.");
        }

        private static void CreateOrUpdate(
            string id,
            string displayName,
            string description,
            PotionTargeting targeting,
            PotionEffectType effectType,
            int effectValue)
        {
            string path = $"Assets/_Game/Runtime/Map/Resources/Potions/{id}.asset";
            var asset = AssetDatabase.LoadAssetAtPath<PotionDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<PotionDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.id = id;
            asset.displayName = displayName;
            asset.description = description;
            asset.targeting = targeting;
            asset.effectType = effectType;
            asset.effectValue = effectValue;
            EditorUtility.SetDirty(asset);
        }
    }
}
#endif

