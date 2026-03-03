using System.Collections.Generic;
using TMPro;
using UnityEngine;

using CardGame.Map;
using CardGame.Battle;
using CardGame.Content;
using CardGame.Run;
namespace CardGame.Map
{
    /// <summary>
    /// Builds runtime-visible map view using SpriteRenderer and LineRenderer.
    /// </summary>
    public class MapRuntimeRenderer : MonoBehaviour
    {
        [Header("Root")]
        public string RootName = "MapViewRoot";

        [Header("Node")]
        public float NodeScale = 0.6f;
        public float LabelOffsetX = 0.45f;
        public float LabelOffsetY = 0.2f;

        [Header("Line")]
        public float LineWidth = 0.07f;
        public int LineSortingOrder = -1;

        private Transform _root;
        private Sprite _whiteSprite;
        private Material _lineMaterial;

        /// <summary>
        /// Current map view root transform used for runtime rendering.
        /// </summary>
        public Transform ViewRoot => _root;

        /// <summary>
        /// Rebuilds map runtime view from graph and current selection state.
        /// </summary>
        public void Rebuild(
            MapGraph graph,
            int currentNodeId,
            HashSet<int> availableNodeIds,
            List<int> selectedPathNodeIds,
            Color nodeColor,
            Color edgeColor,
            Color startColor,
            Color bossColor,
            Color currentNodeColor,
            Color availableNodeColor,
            Color selectedPathEdgeColor,
            Color availableEdgeColor,
            Color lockedNodeTint)
        {
            EnsureResources();
            EnsureRoot();
            ClearRoot();

            if (graph == null || graph.Nodes == null || graph.Nodes.Count == 0)
            {
                return;
            }

            DrawEdges(graph, currentNodeId, availableNodeIds, selectedPathNodeIds, edgeColor, selectedPathEdgeColor, availableEdgeColor);
            DrawNodes(
                graph,
                currentNodeId,
                availableNodeIds,
                selectedPathNodeIds,
                nodeColor,
                startColor,
                bossColor,
                currentNodeColor,
                availableNodeColor,
                lockedNodeTint);
        }

        private void DrawEdges(
            MapGraph graph,
            int currentNodeId,
            HashSet<int> availableNodeIds,
            List<int> selectedPathNodeIds,
            Color edgeColor,
            Color selectedPathEdgeColor,
            Color availableEdgeColor)
        {
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                Vector3 from = ToWorld(node.Position);
                for (int j = 0; j < node.OutgoingNodeIds.Count; j++)
                {
                    int targetId = node.OutgoingNodeIds[j];
                    var target = graph.GetNodeById(targetId);
                    Vector3 to = ToWorld(target.Position);

                    var go = new GameObject($"Edge_{node.Id}_{targetId}");
                    go.transform.SetParent(_root, false);
                    var lr = go.AddComponent<LineRenderer>();
                    lr.positionCount = 2;
                    lr.SetPosition(0, from);
                    lr.SetPosition(1, to);
                    lr.widthMultiplier = LineWidth;
                    lr.material = _lineMaterial;
                    lr.useWorldSpace = true;
                    lr.sortingOrder = LineSortingOrder;
                    lr.startColor = ColorForEdge(node.Id, targetId, currentNodeId, availableNodeIds, selectedPathNodeIds, edgeColor, selectedPathEdgeColor, availableEdgeColor);
                    lr.endColor = lr.startColor;
                }
            }
        }

        private void DrawNodes(
            MapGraph graph,
            int currentNodeId,
            HashSet<int> availableNodeIds,
            List<int> selectedPathNodeIds,
            Color nodeColor,
            Color startColor,
            Color bossColor,
            Color currentNodeColor,
            Color availableNodeColor,
            Color lockedNodeTint)
        {
            int currentLayer = CurrentLayer(graph, currentNodeId);
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                var node = graph.Nodes[i];
                var nodeRoot = new GameObject($"NodeRoot_{node.Id}_{node.RoomType}");
                nodeRoot.transform.SetParent(_root, false);
                nodeRoot.transform.position = ToWorld(node.Position);
                nodeRoot.transform.localScale = Vector3.one;

                var visualGo = new GameObject("NodeVisual");
                visualGo.transform.SetParent(nodeRoot.transform, false);
                visualGo.transform.localPosition = Vector3.zero;
                visualGo.transform.localScale = new Vector3(NodeScale, NodeScale, 1f);

                var sr = visualGo.AddComponent<SpriteRenderer>();
                sr.sprite = _whiteSprite;
                sr.color = ColorForNode(
                    node,
                    currentNodeId,
                    currentLayer,
                    availableNodeIds,
                    selectedPathNodeIds,
                    nodeColor,
                    startColor,
                    bossColor,
                    currentNodeColor,
                    availableNodeColor,
                    lockedNodeTint);

                var labelGo = new GameObject("Label");
                labelGo.transform.SetParent(nodeRoot.transform, false);
                labelGo.transform.localPosition = new Vector3(LabelOffsetX, LabelOffsetY, 0f);
                labelGo.transform.localScale = Vector3.one;
                var text = labelGo.AddComponent<TextMeshPro>();
                text.text = Abbr(node.RoomType);
                text.fontSize = 4f;
                text.alignment = TextAlignmentOptions.Bottom;
                text.color = Color.white;
                text.sortingOrder = sr.sortingOrder + 1;
                text.rectTransform.pivot = new Vector2(0.5f, 0f);
            }
        }

        private Color ColorForNode(
            MapNode node,
            int currentNodeId,
            int currentLayer,
            HashSet<int> availableNodeIds,
            List<int> selectedPathNodeIds,
            Color nodeColor,
            Color startColor,
            Color bossColor,
            Color currentNodeColor,
            Color availableNodeColor,
            Color lockedNodeTint)
        {
            if (node.Id == currentNodeId)
            {
                return currentNodeColor;
            }

            if (availableNodeIds.Contains(node.Id))
            {
                return availableNodeColor;
            }

            bool isSelectedPath = selectedPathNodeIds.Contains(node.Id);
            if (!isSelectedPath && node.Layer <= currentLayer)
            {
                return lockedNodeTint;
            }

            return ColorForRoom(node.RoomType, nodeColor, startColor, bossColor);
        }

        private static Color ColorForRoom(MapRoomType type, Color nodeColor, Color startColor, Color bossColor)
        {
            if (type == MapRoomType.Start)
            {
                return startColor;
            }

            if (type == MapRoomType.Boss)
            {
                return bossColor;
            }

            return nodeColor;
        }

        private static Color ColorForEdge(
            int fromId,
            int toId,
            int currentNodeId,
            HashSet<int> availableNodeIds,
            List<int> selectedPathNodeIds,
            Color edgeColor,
            Color selectedPathEdgeColor,
            Color availableEdgeColor)
        {
            if (IsSelectedPathEdge(fromId, toId, selectedPathNodeIds))
            {
                return selectedPathEdgeColor;
            }

            if (fromId == currentNodeId && availableNodeIds.Contains(toId))
            {
                return availableEdgeColor;
            }

            return edgeColor;
        }

        private static bool IsSelectedPathEdge(int fromId, int toId, List<int> selectedPathNodeIds)
        {
            for (int i = 0; i < selectedPathNodeIds.Count - 1; i++)
            {
                if (selectedPathNodeIds[i] == fromId && selectedPathNodeIds[i + 1] == toId)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CurrentLayer(MapGraph graph, int currentNodeId)
        {
            if (graph == null || currentNodeId < 0)
            {
                return -1;
            }

            return graph.GetNodeById(currentNodeId).Layer;
        }

        private static Vector3 ToWorld(Vector2 p) => new Vector3(p.x, p.y, 0f);

        private static string Abbr(MapRoomType type)
        {
            return type switch
            {
                MapRoomType.Monster => "M",
                MapRoomType.Elite => "E",
                MapRoomType.Event => "EV",
                MapRoomType.Shop => "S",
                MapRoomType.Rest => "R",
                MapRoomType.Treasure => "T",
                MapRoomType.Boss => "B",
                MapRoomType.Start => "ST",
                _ => "?"
            };
        }

        private void EnsureResources()
        {
            if (_whiteSprite == null)
            {
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            }

            if (_lineMaterial == null)
            {
                _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            }
        }

        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            var t = transform.Find(RootName);
            if (t != null)
            {
                _root = t;
                return;
            }

            var rootGo = new GameObject(RootName);
            rootGo.transform.SetParent(transform, false);
            _root = rootGo.transform;
        }

        private void ClearRoot()
        {
            if (_root == null)
            {
                return;
            }

            for (int i = _root.childCount - 1; i >= 0; i--)
            {
                var child = _root.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }
    }
}


