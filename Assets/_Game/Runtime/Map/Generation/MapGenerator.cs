using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
{
    /// <summary>
    /// Generates a Slay the Spire style layered DAG map with deterministic seed.
    /// </summary>
    public static class MapGenerator
    {
        private const float LayerSpacingY = 2.5f;
        private const float NodeSpacingX = 2.2f;

        public static MapGraph Generate(int runSeed)
        {
            var random = new System.Random(runSeed);
            var graph = new MapGraph { Seed = runSeed };

            int middleLayerCount = random.Next(10, 13);
            int totalLayers = middleLayerCount + 2;
            var layerSizes = BuildLayerSizes(random, totalLayers);
            BuildNodes(graph, layerSizes, random);
            BuildEdges(graph, layerSizes, random);
            AssignRoomTypes(graph, random);
            return graph;
        }

        private static int[] BuildLayerSizes(System.Random random, int totalLayers)
        {
            var sizes = new int[totalLayers];
            sizes[0] = 1;
            sizes[totalLayers - 1] = 1;
            for (int i = 1; i < totalLayers - 1; i++)
            {
                sizes[i] = random.Next(3, 7);
            }

            return sizes;
        }

        private static void BuildNodes(MapGraph graph, int[] layerSizes, System.Random random)
        {
            int nextId = 0;
            for (int layer = 0; layer < layerSizes.Length; layer++)
            {
                int count = layerSizes[layer];
                var ids = new List<int>(count);
                float rowWidth = (count - 1) * NodeSpacingX;
                for (int i = 0; i < count; i++)
                {
                    float baseX = -rowWidth * 0.5f + i * NodeSpacingX;
                    float jitter = (float)(random.NextDouble() * 0.8 - 0.4);
                    float x = baseX + jitter;
                    float y = -layer * LayerSpacingY;
                    var node = new MapNode
                    {
                        Id = nextId++,
                        Layer = layer,
                        IndexInLayer = i,
                        Position = new Vector2(x, y),
                        RoomType = MapRoomType.Monster
                    };
                    graph.Nodes.Add(node);
                    ids.Add(node.Id);
                }

                graph.NodeIdsByLayer.Add(ids);
            }
        }

        private static void BuildEdges(MapGraph graph, int[] layerSizes, System.Random random)
        {
            for (int layer = 0; layer < layerSizes.Length - 1; layer++)
            {
                var prevIds = graph.NodeIdsByLayer[layer];
                var nextIds = graph.NodeIdsByLayer[layer + 1];

                // Start layer is a special case: it fans out to all first middle-layer nodes
                // so that no first-layer node is isolated.
                if (layer == 0)
                {
                    var start = graph.GetNodeById(prevIds[0]);
                    foreach (int nextId in nextIds)
                    {
                        start.OutgoingNodeIds.Add(nextId);
                    }

                    continue;
                }

                int parentCount = prevIds.Count;
                int childCount = nextIds.Count;
                var parentOut = new int[parentCount];
                var links = new HashSet<long>();

                // Phase 1: ensure every child has at least one incoming edge using nearest parent index.
                for (int childIndex = 0; childIndex < childCount; childIndex++)
                {
                    float anchorParent = childCount == 1 ? 0f : (float)childIndex * (parentCount - 1) / (childCount - 1);
                    int parentIndex = PickNearestParentIndex(anchorParent, parentOut);
                    TryAddIndexedEdge(graph, prevIds, nextIds, parentOut, links, parentIndex, childIndex);
                }

                // Phase 2: ensure every parent has at least one outgoing edge.
                for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
                {
                    if (parentOut[parentIndex] > 0)
                    {
                        continue;
                    }

                    float anchorChild = parentCount == 1 ? 0f : (float)parentIndex * (childCount - 1) / (parentCount - 1);
                    int childIndex = PickNearestChildIndex(anchorChild, prevIds[parentIndex], nextIds, links);
                    TryAddIndexedEdge(graph, prevIds, nextIds, parentOut, links, parentIndex, childIndex);
                }

                // Phase 3: optional second branch to nearby child; chance decreases near boss for convergence.
                float progress = (float)layer / (layerSizes.Length - 2);
                float secondEdgeChance = Mathf.Lerp(0.35f, 0.12f, progress);
                for (int parentIndex = 0; parentIndex < parentCount; parentIndex++)
                {
                    if (parentOut[parentIndex] >= 2 || random.NextDouble() > secondEdgeChance)
                    {
                        continue;
                    }

                    int parentId = prevIds[parentIndex];
                    int childIndex = PickNearbyChildIndexForSecondEdge(parentId, nextIds, links, childCount);
                    if (childIndex >= 0)
                    {
                        TryAddIndexedEdge(graph, prevIds, nextIds, parentOut, links, parentIndex, childIndex);
                    }
                }
            }
        }

        private static void AssignRoomTypes(MapGraph graph, System.Random random)
        {
            int lastLayer = graph.NodeIdsByLayer.Count - 1;
            var eliteIds = new List<int>();

            foreach (int nodeId in graph.NodeIdsByLayer[0])
            {
                graph.GetNodeById(nodeId).RoomType = MapRoomType.Start;
            }

            foreach (int nodeId in graph.NodeIdsByLayer[lastLayer])
            {
                graph.GetNodeById(nodeId).RoomType = MapRoomType.Boss;
            }

            for (int layer = 1; layer < lastLayer; layer++)
            {
                bool forbidEliteAndShop = layer <= 2;
                foreach (int nodeId in graph.NodeIdsByLayer[layer])
                {
                    var node = graph.GetNodeById(nodeId);
                    node.RoomType = RollRoomType(random, layer, lastLayer, forbidEliteAndShop);
                    if (node.RoomType == MapRoomType.Elite)
                    {
                        eliteIds.Add(node.Id);
                    }
                }
            }

            foreach (int eliteId in eliteIds)
            {
                var eliteNode = graph.GetNodeById(eliteId);
                int previousLayer = eliteNode.Layer - 1;
                if (previousLayer <= 0)
                {
                    continue;
                }

                var parents = GetParentIds(graph, eliteId);
                if (parents.Count == 0)
                {
                    continue;
                }

                int selectedParentId = parents[random.Next(0, parents.Count)];
                var parent = graph.GetNodeById(selectedParentId);
                if (parent.RoomType != MapRoomType.Rest && random.NextDouble() < 0.55d)
                {
                    parent.RoomType = MapRoomType.Rest;
                }
            }
        }

        private static MapRoomType RollRoomType(System.Random random, int layer, int lastLayer, bool forbidEliteAndShop)
        {
            float t = Mathf.Clamp01((float)(layer - 1) / Mathf.Max(1, lastLayer - 2));
            int eliteWeight = Mathf.RoundToInt(Mathf.Lerp(5f, 20f, t));
            int shopWeight = Mathf.RoundToInt(Mathf.Lerp(3f, 7f, t));
            int eventWeight = Mathf.RoundToInt(Mathf.Lerp(24f, 16f, t));
            int restWeight = Mathf.RoundToInt(Mathf.Lerp(12f, 14f, t));
            int treasureWeight = Mathf.RoundToInt(Mathf.Lerp(9f, 6f, t));
            int monsterWeight = 100 - (eliteWeight + shopWeight + eventWeight + restWeight + treasureWeight);

            var entries = forbidEliteAndShop
                ? new[]
                {
                    (MapRoomType.Monster, monsterWeight + eliteWeight + shopWeight),
                    (MapRoomType.Event, eventWeight),
                    (MapRoomType.Rest, restWeight),
                    (MapRoomType.Treasure, treasureWeight)
                }
                : new[]
                {
                    (MapRoomType.Monster, monsterWeight),
                    (MapRoomType.Event, eventWeight),
                    (MapRoomType.Rest, restWeight),
                    (MapRoomType.Treasure, treasureWeight),
                    (MapRoomType.Elite, eliteWeight),
                    (MapRoomType.Shop, shopWeight)
                };

            int roll = random.Next(0, 100);
            int cumulative = 0;
            foreach ((MapRoomType type, int weight) in entries)
            {
                cumulative += weight;
                if (roll < cumulative)
                {
                    return type;
                }
            }

            return MapRoomType.Monster;
        }

        private static int PickNearestParentIndex(float anchorParent, int[] parentOut)
        {
            var candidates = Enumerable.Range(0, parentOut.Length)
                .Where(i => parentOut[i] < 2)
                .OrderBy(i => Mathf.Abs(i - anchorParent))
                .ThenBy(i => i)
                .ToList();

            if (candidates.Count > 0)
            {
                return candidates[0];
            }

            return Enumerable.Range(0, parentOut.Length)
                .OrderBy(i => parentOut[i])
                .ThenBy(i => Mathf.Abs(i - anchorParent))
                .First();
        }

        private static int PickNearestChildIndex(float anchorChild, int parentId, List<int> nextIds, HashSet<long> links)
        {
            var available = Enumerable.Range(0, nextIds.Count)
                .Where(ci => !links.Contains(MakeEdgeKey(parentId, nextIds[ci])))
                .OrderBy(ci => Mathf.Abs(ci - anchorChild))
                .ThenBy(ci => ci)
                .ToList();

            if (available.Count == 0)
            {
                return -1;
            }

            return available[0];
        }

        private static int PickNearbyChildIndexForSecondEdge(int parentId, List<int> nextIds, HashSet<long> links, int childCount)
        {
            var connected = new List<int>();
            for (int ci = 0; ci < childCount; ci++)
            {
                if (links.Contains(MakeEdgeKey(parentId, nextIds[ci])))
                {
                    connected.Add(ci);
                }
            }

            if (connected.Count == 0)
            {
                return -1;
            }

            int pivot = connected[0];
            int left = pivot - 1;
            int right = pivot + 1;
            if (left >= 0 && !links.Contains(MakeEdgeKey(parentId, nextIds[left])))
            {
                return left;
            }

            if (right < childCount && !links.Contains(MakeEdgeKey(parentId, nextIds[right])))
            {
                return right;
            }

            var fallback = Enumerable.Range(0, childCount)
                .Where(ci => !links.Contains(MakeEdgeKey(parentId, nextIds[ci])))
                .OrderBy(ci => Mathf.Abs(ci - pivot))
                .ThenBy(ci => ci)
                .ToList();

            return fallback.Count > 0 ? fallback[0] : -1;
        }

        private static void TryAddIndexedEdge(
            MapGraph graph,
            List<int> prevIds,
            List<int> nextIds,
            int[] parentOut,
            HashSet<long> links,
            int parentIndex,
            int childIndex)
        {
            if (parentIndex < 0 || parentIndex >= prevIds.Count || childIndex < 0 || childIndex >= nextIds.Count)
            {
                return;
            }

            if (parentOut[parentIndex] >= 2)
            {
                return;
            }

            int fromId = prevIds[parentIndex];
            int toId = nextIds[childIndex];
            long edgeKey = MakeEdgeKey(fromId, toId);
            if (links.Contains(edgeKey))
            {
                return;
            }

            links.Add(edgeKey);
            AddEdge(graph, fromId, toId);
            parentOut[parentIndex]++;
        }

        private static long MakeEdgeKey(int fromId, int toId)
        {
            return ((long)fromId << 32) ^ (uint)toId;
        }

        private static List<int> GetParentIds(MapGraph graph, int childId)
        {
            var result = new List<int>();
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                if (node.OutgoingNodeIds.Contains(childId))
                {
                    result.Add(node.Id);
                }
            }

            return result;
        }

        private static void AddEdge(MapGraph graph, int fromId, int toId)
        {
            var node = graph.GetNodeById(fromId);
            if (!node.OutgoingNodeIds.Contains(toId))
            {
                node.OutgoingNodeIds.Add(toId);
            }
        }
    }
}


