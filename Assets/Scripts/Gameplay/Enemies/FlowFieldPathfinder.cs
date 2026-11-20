using System.Collections.Generic;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class FlowFieldPathfinder : MonoBehaviour
    {
        [SerializeField] private ArenaPathGrid grid;
        [SerializeField] private Transform target;
        [SerializeField, Min(0.05f)] private float rebuildInterval = 0.2f;

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.right,
            Vector2Int.left,
            Vector2Int.up,
            Vector2Int.down
        };

        private readonly Queue<Vector2Int> openCells =
            new Queue<Vector2Int>();

        private int[] distance;
        private Vector2[] flowDirections;
        private bool[] visited;
        private float rebuildTimer;
        private bool ready;

        public bool IsReady => ready;

        private void Awake()
        {
            if (grid != null && target != null)
            {
                Rebuild();
            }
        }

        private void Update()
        {
            if (grid == null || target == null)
            {
                return;
            }

            rebuildTimer -= Time.deltaTime;

            if (rebuildTimer <= 0f)
            {
                rebuildTimer = rebuildInterval;
                Rebuild();
            }
        }

        public void Rebuild()
        {
            ready = false;

            if (grid == null ||
                target == null ||
                !grid.IsBuilt ||
                grid.Width <= 0 ||
                grid.Height <= 0)
            {
                return;
            }

            int cellCount = grid.Width * grid.Height;

            if (distance == null || distance.Length != cellCount)
            {
                distance = new int[cellCount];
                flowDirections = new Vector2[cellCount];
                visited = new bool[cellCount];
            }

            for (int i = 0; i < cellCount; i++)
            {
                distance[i] = int.MaxValue;
                flowDirections[i] = Vector2.zero;
                visited[i] = false;
            }

            Vector2Int targetCell =
                grid.WorldToCell(target.position);

            if (!grid.IsWalkable(targetCell))
            {
                return;
            }

            openCells.Clear();
            openCells.Enqueue(targetCell);
            int targetIndex = GetIndex(targetCell);
            distance[targetIndex] = 0;
            visited[targetIndex] = true;

            while (openCells.Count > 0)
            {
                Vector2Int current = openCells.Dequeue();
                int currentDistance = distance[GetIndex(current)];

                for (int i = 0; i < Directions.Length; i++)
                {
                    Vector2Int neighbor = current + Directions[i];

                    if (!grid.IsWalkable(neighbor))
                    {
                        continue;
                    }

                    int neighborIndex = GetIndex(neighbor);

                    if (visited[neighborIndex])
                    {
                        continue;
                    }

                    visited[neighborIndex] = true;
                    distance[neighborIndex] = currentDistance + 1;

                    Vector2 neighborWorld = grid.CellToWorld(neighbor);
                    Vector2 currentWorld = grid.CellToWorld(current);
                    flowDirections[neighborIndex] =
                        (currentWorld - neighborWorld).normalized;

                    openCells.Enqueue(neighbor);
                }
            }

            ready = true;
        }

        public bool TryGetDirection(
            Vector2 worldPosition,
            out Vector2 direction)
        {
            direction = Vector2.zero;

            if (!ready || grid == null)
            {
                return false;
            }

            Vector2Int cell = grid.WorldToCell(worldPosition);

            if (!grid.IsWalkable(cell))
            {
                return false;
            }

            direction = flowDirections[GetIndex(cell)];
            return direction.sqrMagnitude > 0.0001f;
        }

        private int GetIndex(Vector2Int cell)
        {
            return cell.y * grid.Width + cell.x;
        }
    }
}
