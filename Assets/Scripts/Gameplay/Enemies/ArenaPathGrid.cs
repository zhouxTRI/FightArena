using UnityEngine;

namespace FightArena.Gameplay.Enemies
{
    public sealed class ArenaPathGrid : MonoBehaviour
    {
        [Header("Arena")]
        [SerializeField] private Vector2 arenaMin = new Vector2(-24f, -14f);
        [SerializeField] private Vector2 arenaMax = new Vector2(24f, 14f);

        [Header("Grid")]
        [SerializeField, Min(0.1f)] private float cellSize = 0.5f;
        [SerializeField, Min(0f)] private float agentRadius = 0.32f;
        [SerializeField] private LayerMask obstacleMask;

        private bool[] walkable;
        private int width;
        private int height;
        private bool built;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public bool IsBuilt => built;

        private void Awake()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            width = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    (arenaMax.x - arenaMin.x) / cellSize));
            height = Mathf.Max(
                1,
                Mathf.CeilToInt(
                    (arenaMax.y - arenaMin.y) / cellSize));

            walkable = new bool[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 world = CellToWorld(new Vector2Int(x, y));
                    walkable[y * width + x] =
                        Physics2D.OverlapCircle(
                            world,
                            agentRadius,
                            obstacleMask) == null;
                }
            }

            built = true;
        }

        public bool IsWalkable(Vector2 worldPosition)
        {
            return IsWalkable(WorldToCell(worldPosition));
        }

        public bool IsWalkable(Vector2Int cell)
        {
            if (!built ||
                !IsGridInBounds(cell) ||
                walkable == null)
            {
                return false;
            }

            return walkable[cell.y * width + cell.x];
        }

        public bool IsGridInBounds(Vector2Int cell)
        {
            return cell.x >= 0 &&
                   cell.x < width &&
                   cell.y >= 0 &&
                   cell.y < height;
        }

        public Vector2Int WorldToCell(Vector2 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(
                    (worldPosition.x - arenaMin.x) / cellSize),
                Mathf.FloorToInt(
                    (worldPosition.y - arenaMin.y) / cellSize));
        }

        public Vector2 CellToWorld(Vector2Int cell)
        {
            return new Vector2(
                arenaMin.x + (cell.x + 0.5f) * cellSize,
                arenaMin.y + (cell.y + 0.5f) * cellSize);
        }

        private void OnValidate()
        {
            cellSize = Mathf.Max(0.1f, cellSize);
            agentRadius = Mathf.Max(0f, agentRadius);

            if (arenaMax.x < arenaMin.x)
            {
                arenaMax.x = arenaMin.x;
            }

            if (arenaMax.y < arenaMin.y)
            {
                arenaMax.y = arenaMin.y;
            }
        }
    }
}
