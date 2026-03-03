using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Game.Map
{
    /// <summary>
    /// Exports compact map summary data for regression testing.
    /// </summary>
    public static class MapSummaryExporter
    {
        [Serializable]
        public class MapSummary
        {
            public int seed;
            public List<int> nodesPerLayer = new List<int>();
            public List<RoomTypeCount> roomTypeCounts = new List<RoomTypeCount>();
            public int edgeCount;
        }

        [Serializable]
        public class RoomTypeCount
        {
            public string roomType;
            public int count;
        }

        public static MapSummary BuildSummary(MapGraph graph)
        {
            var summary = new MapSummary
            {
                seed = graph.Seed,
                edgeCount = graph.Nodes.Sum(n => n.OutgoingNodeIds.Count)
            };

            for (int i = 0; i < graph.NodeIdsByLayer.Count; i++)
            {
                summary.nodesPerLayer.Add(graph.NodeIdsByLayer[i].Count);
            }

            foreach (MapRoomType type in Enum.GetValues(typeof(MapRoomType)))
            {
                summary.roomTypeCounts.Add(new RoomTypeCount
                {
                    roomType = type.ToString(),
                    count = graph.Nodes.Count(n => n.RoomType == type)
                });
            }

            return summary;
        }

        public static string SaveSummaryToJson(MapGraph graph, string outputDirectory, string fileName = null)
        {
            if (graph == null)
            {
                throw new ArgumentNullException(nameof(graph));
            }

            var summary = BuildSummary(graph);
            string json = JsonUtility.ToJson(summary, true);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string safeName = string.IsNullOrWhiteSpace(fileName)
                ? $"map_summary_seed_{summary.seed}.json"
                : fileName;
            string path = Path.Combine(outputDirectory, safeName);
            File.WriteAllText(path, json);
            return path;
        }
    }
}
