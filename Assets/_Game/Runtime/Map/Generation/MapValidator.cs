using System;
using System.Collections.Generic;
using System.Linq;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
{
    /// <summary>
    /// Validates generated map connectivity and provides basic map stats.
    /// </summary>
    public static class MapValidator
    {
        public static MapValidationReport Validate(MapGraph graph)
        {
            var report = new MapValidationReport();
            if (graph == null || graph.Nodes.Count == 0)
            {
                report.IsValid = false;
                report.Errors.Add("Graph is null or empty.");
                return report;
            }

            int startId = graph.NodeIdsByLayer[0][0];
            var reachable = ComputeReachable(graph, startId);
            report.ReachableNodeCount = reachable.Count;
            report.TotalNodeCount = graph.Nodes.Count;

            foreach (var node in graph.Nodes)
            {
                if (!reachable.Contains(node.Id))
                {
                    report.IslandNodeIds.Add(node.Id);
                }
            }

            int bossId = graph.NodeIdsByLayer[graph.NodeIdsByLayer.Count - 1][0];
            report.IsBossReachable = reachable.Contains(bossId);
            report.AverageOutDegree = graph.Nodes.Average(n => (double)n.OutgoingNodeIds.Count);

            foreach (MapRoomType type in Enum.GetValues(typeof(MapRoomType)))
            {
                report.RoomTypeCounts[type] = graph.Nodes.Count(n => n.RoomType == type);
            }

            if (!report.IsBossReachable)
            {
                report.Errors.Add("Boss is unreachable from Start.");
            }

            if (report.IslandNodeIds.Count > 0)
            {
                report.Errors.Add($"Found {report.IslandNodeIds.Count} unreachable island node(s).");
            }

            report.IsValid = report.Errors.Count == 0;
            return report;
        }

        private static HashSet<int> ComputeReachable(MapGraph graph, int startId)
        {
            var visited = new HashSet<int> { startId };
            var queue = new Queue<int>();
            queue.Enqueue(startId);

            while (queue.Count > 0)
            {
                int nodeId = queue.Dequeue();
                var node = graph.GetNodeById(nodeId);
                for (int i = 0; i < node.OutgoingNodeIds.Count; i++)
                {
                    int nextId = node.OutgoingNodeIds[i];
                    if (visited.Add(nextId))
                    {
                        queue.Enqueue(nextId);
                    }
                }
            }

            return visited;
        }
    }

    /// <summary>
    /// Validation result and summary stats of a generated map.
    /// </summary>
    [Serializable]
    public class MapValidationReport
    {
        public bool IsValid;
        public bool IsBossReachable;
        public int ReachableNodeCount;
        public int TotalNodeCount;
        public double AverageOutDegree;
        public Dictionary<MapRoomType, int> RoomTypeCounts = new Dictionary<MapRoomType, int>();
        public List<int> IslandNodeIds = new List<int>();
        public List<string> Errors = new List<string>();
    }
}


