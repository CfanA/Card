using System;
using System.Collections.Generic;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
{
    /// <summary>
    /// A single node in the layered DAG map.
    /// </summary>
    [Serializable]
    public class MapNode
    {
        public int Id;
        public int Layer;
        public int IndexInLayer;
        public Vector2 Position;
        public MapRoomType RoomType;
        public List<int> OutgoingNodeIds = new List<int>();
    }
}


