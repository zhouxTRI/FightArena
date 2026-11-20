using System.Collections.Generic;
using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class EnemySpatialHash : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float cellSize = 4.6f;

        private readonly Dictionary<Vector2Int, List<EnemyAgent>> cells =
            new Dictionary<Vector2Int, List<EnemyAgent>>();

        private readonly List<EnemyAgent> queryBuffer =
            new List<EnemyAgent>();

        public void Rebuild(IReadOnlyList<EnemyAgent> enemies)
        {
            foreach (List<EnemyAgent> list in cells.Values)
            {
                list.Clear();
            }

            if (enemies == null)
            {
                return;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyAgent enemy = enemies[i];

                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                Vector2Int cell = WorldToCell(enemy.transform.position);

                if (!cells.TryGetValue(
                        cell,
                        out List<EnemyAgent> list))
                {
                    list = new List<EnemyAgent>();
                    cells.Add(cell, list);
                }

                list.Add(enemy);
            }
        }

        public void QueryNeighbors(
            Vector2 worldPosition,
            List<EnemyAgent> results)
        {
            if (results == null)
            {
                return;
            }

            results.Clear();
            Vector2Int center = WorldToCell(worldPosition);

            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    if (cells.TryGetValue(
                            center + new Vector2Int(x, y),
                            out List<EnemyAgent> list))
                    {
                        results.AddRange(list);
                    }
                }
            }
        }

        public Vector2 CalculateSeparation(
            EnemyAgent self,
            float radius)
        {
            if (self == null || radius <= 0f)
            {
                return Vector2.zero;
            }

            QueryNeighbors(
                self.transform.position,
                queryBuffer);

            Vector2 separation = Vector2.zero;
            Vector2 selfPosition = self.transform.position;

            for (int i = 0; i < queryBuffer.Count; i++)
            {
                EnemyAgent other = queryBuffer[i];

                if (other == null || other == self || other.IsDead)
                {
                    continue;
                }

                Vector2 delta =
                    selfPosition - (Vector2)other.transform.position;
                float distance = delta.magnitude;

                if (distance > 0.0001f && distance < radius)
                {
                    separation += delta.normalized *
                        (1f - distance / radius);
                }
            }

            return Vector2.ClampMagnitude(separation, 1f);
        }

        public void Clear()
        {
            foreach (List<EnemyAgent> list in cells.Values)
            {
                list.Clear();
            }
        }

        private Vector2Int WorldToCell(Vector2 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / cellSize),
                Mathf.FloorToInt(position.y / cellSize));
        }
    }
}
