using System.Collections.Generic;
using UnityEngine;

public sealed class AStarPathfinder : MonoBehaviour
{
    [SerializeField] private Vector2 worldSize = new Vector2(40f, 30f);
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private LineRenderer lineRenderer;

    private readonly List<Vector3> lastPath = new();
    private bool[,] blocked;
    private int gridX;
    private int gridZ;
    private Vector3 origin;

    public float ReferenceDistance { get; private set; }
    public IReadOnlyList<Vector3> LastPath => lastPath;

    public float CalculatePath(Vector3 start, Vector3 goal)
    {
        BuildGrid();
        lastPath.Clear();
        ReferenceDistance = 0f;

        Vector2Int startNode = WorldToGrid(start);
        Vector2Int goalNode = WorldToGrid(goal);
        blocked[startNode.x, startNode.y] = false;
        blocked[goalNode.x, goalNode.y] = false;

        List<Vector2Int> open = new() { startNode };
        HashSet<Vector2Int> closed = new();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new();
        Dictionary<Vector2Int, float> gScore = new() { [startNode] = 0f };
        Dictionary<Vector2Int, float> fScore = new() { [startNode] = Heuristic(startNode, goalNode) };

        while (open.Count > 0)
        {
            int bestIndex = 0;
            float bestF = fScore.GetValueOrDefault(open[0], float.MaxValue);
            for (int i = 1; i < open.Count; i++)
            {
                float f = fScore.GetValueOrDefault(open[i], float.MaxValue);
                if (f < bestF)
                {
                    bestF = f;
                    bestIndex = i;
                }
            }

            Vector2Int current = open[bestIndex];
            if (current == goalNode)
            {
                ReconstructPath(cameFrom, current, start, goal);
                UpdateLine();
                return ReferenceDistance;
            }

            open.RemoveAt(bestIndex);
            closed.Add(current);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0)
                        continue;

                    int nx = current.x + dx;
                    int nz = current.y + dz;
                    if (nx < 0 || nx >= gridX || nz < 0 || nz >= gridZ)
                        continue;

                    if (blocked[nx, nz])
                        continue;

                    if (dx != 0 && dz != 0 && (blocked[current.x + dx, current.y] || blocked[current.x, current.y + dz]))
                        continue;

                    Vector2Int neighbor = new Vector2Int(nx, nz);
                    if (closed.Contains(neighbor))
                        continue;

                    float stepCost = (dx != 0 && dz != 0) ? 1.4142f : 1f;
                    float tentativeG = gScore[current] + stepCost;

                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                    else if (tentativeG >= gScore.GetValueOrDefault(neighbor, float.MaxValue))
                        continue;

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goalNode);
                }
            }
        }

        UpdateLine();
        return 0f;
    }

    private void BuildGrid()
    {
        gridX = Mathf.Max(2, Mathf.RoundToInt(worldSize.x / cellSize));
        gridZ = Mathf.Max(2, Mathf.RoundToInt(worldSize.y / cellSize));
        blocked = new bool[gridX, gridZ];
        origin = transform.position - new Vector3(worldSize.x * 0.5f, 0f, worldSize.y * 0.5f);
        Vector3 halfExtents = new Vector3(cellSize * 0.48f, 1f, cellSize * 0.48f);

        for (int x = 0; x < gridX; x++)
        {
            for (int z = 0; z < gridZ; z++)
            {
                Vector3 pos = GridToWorld(new Vector2Int(x, z)) + Vector3.up * 0.5f;
                blocked[x, z] = Physics.CheckBox(pos, halfExtents, Quaternion.identity, obstacleMask, QueryTriggerInteraction.Ignore);
            }
        }
    }

    private void ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Vector3 start, Vector3 goal)
    {
        List<Vector3> points = new() { goal };
        while (cameFrom.TryGetValue(current, out Vector2Int prev))
        {
            current = prev;
            if (cameFrom.ContainsKey(current))
                points.Add(GridToWorld(current));
        }
        points.Add(start);
        points.Reverse();

        lastPath.AddRange(points);
        float total = 0f;
        for (int i = 1; i < lastPath.Count; i++)
            total += Vector3.Distance(lastPath[i - 1], lastPath[i]);

        ReferenceDistance = total;
    }

    private void UpdateLine()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = lastPath.Count;
        for (int i = 0; i < lastPath.Count; i++)
            lineRenderer.SetPosition(i, lastPath[i] + Vector3.up * 0.25f);
    }

    private Vector2Int WorldToGrid(Vector3 pos)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((pos.x - origin.x) / cellSize), 0, gridX - 1);
        int z = Mathf.Clamp(Mathf.FloorToInt((pos.z - origin.z) / cellSize), 0, gridZ - 1);
        return new Vector2Int(x, z);
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        return origin + new Vector3((cell.x + 0.5f) * cellSize, 0f, (cell.y + 0.5f) * cellSize);
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Vector2Int.Distance(a, b);
    }

    private void OnDrawGizmos()
    {
        if (lastPath == null || lastPath.Count < 2)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 1; i < lastPath.Count; i++)
            Gizmos.DrawLine(lastPath[i - 1] + Vector3.up * 0.25f, lastPath[i] + Vector3.up * 0.25f);
    }
}
