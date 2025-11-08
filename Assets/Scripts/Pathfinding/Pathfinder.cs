using System.Collections.Generic;
using System.Linq;
using ThiefSimulator.Managers;
using ThiefSimulator.Objects;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ThiefSimulator.Pathfinding
{
    public static class Pathfinder
    {
        private class Node
        {
            public Vector2Int position;
            public Node parent;
            public int gCost;
            public int hCost;
            public int fCost => gCost + hCost;
            public Node(Vector2Int position) { this.position = position; }
        }

        public static List<Vector2Int> FindPath(Vector2Int startPosition, Vector2Int targetPosition, Tilemap obstacleTilemap, Vector2Int mapOrigin, HashSet<Vector2Int> dynamicObstacles, HashSet<Vector2Int> temporarilyWalkableDoors = null, bool allowClosedDoors = false, System.Func<Vector2Int, bool> additionalWalkableCondition = null)
        {
            Node startNode = new Node(startPosition);
            Node targetNode = new Node(targetPosition);

            List<Node> openSet = new List<Node>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            openSet.Add(startNode);

            int iterationCount = 0;
            const int maxIterations = 20000;

            while (openSet.Count > 0)
            {
                iterationCount++;
                if (iterationCount > maxIterations)
                {
                    Debug.LogError("[Pathfinder] Pathfinding timed out.");
                    return new List<Vector2Int>();
                }

                Node currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < currentNode.fCost || (openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost))
                        currentNode = openSet[i];
                }

                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);

                if (currentNode.position == targetNode.position)
                {
                    return RetracePath(startNode, currentNode);
                }

                foreach (Vector2Int neighborPos in GetNeighborPositions(currentNode, obstacleTilemap, mapOrigin, dynamicObstacles, temporarilyWalkableDoors, allowClosedDoors, additionalWalkableCondition))
                {
                    if (closedSet.Contains(neighborPos))
                    {
                        continue;
                    }

                    int newMovementCostToNeighbor = currentNode.gCost + GetDistance(currentNode.position, neighborPos);

                    Node neighborNode = openSet.FirstOrDefault(node => node.position == neighborPos);

                    if (neighborNode == null) // If not in open set
                    {
                        neighborNode = new Node(neighborPos)
                        {
                            gCost = newMovementCostToNeighbor,
                            hCost = GetDistance(neighborPos, targetPosition),
                            parent = currentNode
                        };
                        openSet.Add(neighborNode);
                    }
                    else // If already in open set
                    {
                        if (newMovementCostToNeighbor < neighborNode.gCost)
                        {
                            // Found a better path to this node
                            neighborNode.gCost = newMovementCostToNeighbor;
                            neighborNode.parent = currentNode;
                        }
                    }
                }
            }

            return new List<Vector2Int>(); // No path found
        }

        private static List<Vector2Int> RetracePath(Node startNode, Node endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            Node currentNode = endNode;
            while (currentNode != startNode)
            {
                path.Add(currentNode.position);
                currentNode = currentNode.parent;
            }
            path.Reverse();
            return path;
        }

        public static bool IsWalkable(Vector2Int relativePos, Tilemap obstacleTilemap, Vector2Int mapOrigin, HashSet<Vector2Int> dynamicObstacles, HashSet<Vector2Int> temporarilyWalkableDoors = null, bool allowClosedDoors = false, System.Func<Vector2Int, bool> additionalWalkableCondition = null)
        {
            // 1. Check for dynamic obstacles (other NPCs)
            if (dynamicObstacles != null && dynamicObstacles.Contains(relativePos))
            {
                return false; // Another NPC is here
            }

            if (additionalWalkableCondition != null && !additionalWalkableCondition(relativePos))
            {
                return false;
            }

            // 2. Check for static obstacles (walls)
            Vector2Int absolutePos = relativePos + mapOrigin;
            if (obstacleTilemap.HasTile((Vector3Int)absolutePos))
            {
                return false; // It's a wall
            }

            // 3. Check for dynamic obstacles (doors)
            if (DoorManager.Instance != null && DoorManager.Instance.IsDoorAt(relativePos, out Door door))
            {
                // If this door is temporarily walkable, treat it as such.
                if (temporarilyWalkableDoors != null && temporarilyWalkableDoors.Contains(relativePos))
                {
                    return true;
                }
                // Otherwise, a door exists here. It's walkable only if it's open, unless override is enabled.
                return allowClosedDoors || door.IsOpen;
            }

            // 4. Not a dynamic obstacle, wall, or closed door, so it's walkable
            return true;
        }

        private static IEnumerable<Vector2Int> GetNeighborPositions(Node node, Tilemap obstacleTilemap, Vector2Int mapOrigin, HashSet<Vector2Int> dynamicObstacles, HashSet<Vector2Int> temporarilyWalkableDoors, bool allowClosedDoors, System.Func<Vector2Int, bool> additionalWalkableCondition)
        {
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var dir in directions)
            {
                Vector2Int relativeNeighborPos = node.position + dir;
                
                if (IsWalkable(relativeNeighborPos, obstacleTilemap, mapOrigin, dynamicObstacles, temporarilyWalkableDoors, allowClosedDoors, additionalWalkableCondition))
                {
                    yield return relativeNeighborPos;
                }
            }
        }

        private static int GetDistance(Vector2Int posA, Vector2Int posB)
        {
            int dstX = Mathf.Abs(posA.x - posB.x);
            int dstY = Mathf.Abs(posA.y - posB.y);
            return dstX + dstY;
        }
    }
}
