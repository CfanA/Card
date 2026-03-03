#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Editor helper for creating sample EventDefinition assets.
    /// </summary>
    public static class EventDefinitionEditorTools
    {
        [MenuItem("Tools/Game/Map/Create Default Event Definitions")]
        public static void CreateDefaults()
        {
            const string folder = "Assets/_Game/Runtime/Map/Resources/Events";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            CreateOldFountain();
            CreateCrumblingShrine();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Event] Default EventDefinition assets created/updated.");
        }

        private static void CreateOldFountain()
        {
            var evt = LoadOrCreate("old_fountain");
            evt.id = "old_fountain";
            evt.title = "Old Fountain";
            evt.body = "A worn fountain still glimmers faintly. The water smells strange but warm.";
            evt.options = new List<EventOption>
            {
                new EventOption
                {
                    buttonText = "Drink (+10 HP)",
                    resultText = "You feel your wounds close.",
                    endEvent = true,
                    effects = new List<EventEffect>
                    {
                        new EventEffect { effectType = EventEffectType.Heal, value = 10 }
                    }
                },
                new EventOption
                {
                    buttonText = "Bottle Water (Gain Potion)",
                    resultText = "You carefully collect some water into a vial.",
                    endEvent = true,
                    effects = new List<EventEffect>
                    {
                        new EventEffect { effectType = EventEffectType.GainPotion, idParam = "healing_potion", value = 1 }
                    }
                }
            };
            EditorUtility.SetDirty(evt);
        }

        private static void CreateCrumblingShrine()
        {
            var evt = LoadOrCreate("crumbling_shrine");
            evt.id = "crumbling_shrine";
            evt.title = "Crumbling Shrine";
            evt.body = "A shrine of unknown origin hums with lingering power.";
            evt.options = new List<EventOption>
            {
                new EventOption
                {
                    buttonText = "Offer Blood (-6 HP, gain relic)",
                    resultText = "The shrine accepts your blood and bestows a relic.",
                    endEvent = true,
                    effects = new List<EventEffect>
                    {
                        new EventEffect { effectType = EventEffectType.LoseHP, value = 6 },
                        new EventEffect { effectType = EventEffectType.GainRelic, idParam = "anchor", value = 1 }
                    }
                },
                new EventOption
                {
                    buttonText = "Study Runes (add card)",
                    resultText = "You copy a potent pattern into your deck.",
                    endEvent = true,
                    effects = new List<EventEffect>
                    {
                        new EventEffect { effectType = EventEffectType.AddCardToDeck, idParam = "gain_strength", value = 1 }
                    }
                }
            };
            EditorUtility.SetDirty(evt);
        }

        private static EventDefinition LoadOrCreate(string id)
        {
            string path = $"Assets/_Game/Runtime/Map/Resources/Events/{id}.asset";
            var evt = AssetDatabase.LoadAssetAtPath<EventDefinition>(path);
            if (evt == null)
            {
                evt = ScriptableObject.CreateInstance<EventDefinition>();
                AssetDatabase.CreateAsset(evt, path);
            }

            return evt;
        }
    }
}
#endif
