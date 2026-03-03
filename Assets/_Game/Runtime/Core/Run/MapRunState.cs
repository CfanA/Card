using System;
using System.Collections.Generic;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Run
{
    /// <summary>
    /// Runtime state for one map run progression.
    /// </summary>
    [Serializable]
    public class MapRunState
    {
        public int seed;
        public int gold = 99;
        public int maxPlayerHp = 80;
        public int currentPlayerHp = 80;
        public int currentNodeId = -1;
        public int pendingNodeId = -1;
        public bool isInRoom;
        public bool routeLocked;
        public int rewardRollCounter;
        public List<int> visitedNodeIds = new List<int>();
        public List<int> availableNodeIds = new List<int>();
        public DeckState deck = new DeckState();
        public List<string> relicIds = new List<string>();
        public List<string> potionSlots = new List<string>();

        public void ResetForNewRun(int runSeed, int startNodeId, int playerMaxHp = 80, int startGold = 99)
        {
            seed = runSeed;
            gold = Mathf.Max(0, startGold);
            maxPlayerHp = playerMaxHp;
            currentPlayerHp = playerMaxHp;
            currentNodeId = startNodeId;
            pendingNodeId = -1;
            isInRoom = false;
            routeLocked = false;
            rewardRollCounter = 0;
            visitedNodeIds.Clear();
            availableNodeIds.Clear();
            visitedNodeIds.Add(startNodeId);
            deck.ResetStarterDeck();
            relicIds.Clear();
            potionSlots.Clear();
            for (int i = 0; i < 3; i++)
            {
                potionSlots.Add(string.Empty);
            }
        }

        public void AddGold(int amount)
        {
            gold = Mathf.Max(0, gold + Mathf.Max(0, amount));
        }

        public bool SpendGold(int amount)
        {
            int need = Mathf.Max(0, amount);
            if (gold < need)
            {
                return false;
            }

            gold -= need;
            return true;
        }
    }
}


