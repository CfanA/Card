using System;
using System.Collections.Generic;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Content
{
    /// <summary>
    /// Data-driven event content definition.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Map/Event Definition", fileName = "EventDefinition")]
    public class EventDefinition : ScriptableObject
    {
        public string id;
        public string title;
        [TextArea(3, 10)] public string body;
        public List<EventOption> options = new List<EventOption>();
    }

    /// <summary>
    /// One selectable option in an event.
    /// </summary>
    [Serializable]
    public class EventOption
    {
        public string buttonText;
        [TextArea(2, 6)] public string resultText;
        public List<EventEffect> effects = new List<EventEffect>();
        public bool endEvent = true;
    }

    /// <summary>
    /// One effect entry executed when choosing an event option.
    /// </summary>
    [Serializable]
    public class EventEffect
    {
        public EventEffectType effectType;
        public int value;
        public string idParam;
    }

    /// <summary>
    /// Supported event effect types.
    /// </summary>
    public enum EventEffectType
    {
        Heal,
        LoseHP,
        GainRelic,
        GainPotion,
        AddCardToDeck
    }
}


