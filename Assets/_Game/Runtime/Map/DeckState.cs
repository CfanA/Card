using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Persistent deck state for current run.
    /// </summary>
    [Serializable]
    public class DeckState
    {
        public List<string> cardIds = new List<string>();

        public void ResetStarterDeck()
        {
            cardIds.Clear();
            for (int i = 0; i < 5; i++)
            {
                cardIds.Add("strike");
            }

            for (int i = 0; i < 5; i++)
            {
                cardIds.Add("defend");
            }
        }

        public void AddCard(string cardId)
        {
            if (string.IsNullOrWhiteSpace(cardId))
            {
                return;
            }

            cardIds.Add(cardId);
        }

        public List<int> GetUpgradeableIndices()
        {
            var result = new List<int>();
            for (int i = 0; i < cardIds.Count; i++)
            {
                if (CardLibrary.CanUpgrade(cardIds[i]))
                {
                    result.Add(i);
                }
            }

            return result;
        }

        public bool TryUpgradeAt(int index, out string beforeId, out string afterId)
        {
            beforeId = string.Empty;
            afterId = string.Empty;
            if (index < 0 || index >= cardIds.Count)
            {
                return false;
            }

            beforeId = cardIds[index];
            if (!CardLibrary.CanUpgrade(beforeId))
            {
                return false;
            }

            afterId = CardLibrary.GetUpgradedId(beforeId);
            cardIds[index] = afterId;
            return true;
        }
    }
}
