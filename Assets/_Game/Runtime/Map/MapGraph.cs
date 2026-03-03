using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Map
{
    /// <summary>
    /// Runtime container for the generated map graph.
    /// </summary>
    [Serializable]
    public class MapGraph
    {
        public int Seed;
        public List<MapNode> Nodes = new List<MapNode>();
        public List<List<int>> NodeIdsByLayer = new List<List<int>>();

        public MapNode GetNodeById(int nodeId)
        {
            return Nodes.First(n => n.Id == nodeId);
        }
    }
}
