using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Algorithms
{
    static readonly Color COLOR_VISITED = new Color(0.24f, 0.55f, 1.00f);
    static readonly Color COLOR_OPEN = new Color(1.00f, 0.80f, 0.00f);

    // FIX 5: algoName is now a local variable in each method instead of a shared
    // static field, eliminating the race condition if a previous coroutine is still
    // winding down when a new one starts.

    public static IEnumerator BFS(
        Dictionary<int, NodeData> graph,
        int startCode, int goalCode,
        float stepDelay, Func<bool> shouldStop,
        Action<PathResult> onDone, Action<int, int> onStep = null)
    {
        string algoName = "BFS";
        SearchTreePanel.OnAlgoStart(algoName, startCode, goalCode);

        var queue = new Queue<int>();
        var visited = new HashSet<int>();
        var parent = new Dictionary<int, int>();
        int vc = 0;

        queue.Enqueue(startCode);
        visited.Add(startCode);
        parent[startCode] = -1;

        // FIX 2 & 3: Add the start node to the tree as the root.
        SearchTreePanel.OnNodeVisited(startCode, -1, algoName);

        while (queue.Count > 0)
        {
            if (shouldStop()) { onDone(new PathResult()); yield break; }

            int cur = queue.Dequeue();
            vc++;

            if (cur == goalCode)
            {
                // FIX 2: Add the goal node to the tree before reporting done.
                SearchTreePanel.OnNodeVisited(cur, parent.ContainsKey(cur) ? parent[cur] : -1, algoName);
                var result = new PathResult { Path = Reconstruct(parent, startCode, goalCode), Visited = vc };
                SearchTreePanel.OnPathFound(result.Path);
                onDone(result);
                yield break;
            }

            if (cur != startCode)
            {
                GraphMap.Instance.SetColor(cur, COLOR_VISITED);
                onStep?.Invoke(vc, -1);
                // FIX 1: Removed duplicate OnNodeVisited call.
                SearchTreePanel.OnNodeVisited(cur, parent.ContainsKey(cur) ? parent[cur] : -1, algoName);
                yield return new WaitForSeconds(stepDelay);
            }

            foreach (var (nb, _) in graph[cur].Neighbors)
            {
                if (visited.Contains(nb)) continue;
                visited.Add(nb);
                parent[nb] = cur;
                if (nb != goalCode)
                    GraphMap.Instance.SetColor(nb, COLOR_OPEN);
                queue.Enqueue(nb);
            }
        }

        onDone(new PathResult { Visited = vc });
    }

    public static IEnumerator Dijkstra(
        Dictionary<int, NodeData> graph,
        int startCode, int goalCode,
        float stepDelay, Func<bool> shouldStop,
        Action<PathResult> onDone, Action<int, int> onStep = null)
    {
        string algoName = "Dijkstra";
        SearchTreePanel.OnAlgoStart(algoName, startCode, goalCode);

        var dist = new Dictionary<int, float>();
        var parent = new Dictionary<int, int>();
        var closed = new HashSet<int>();
        int vc = 0;

        foreach (var nd in graph.Values) dist[nd.Code] = float.MaxValue;
        dist[startCode] = 0f;
        parent[startCode] = -1;

        // FIX 2 & 3: Add the start node to the tree as the root.
        SearchTreePanel.OnNodeVisited(startCode, -1, algoName);

        while (true)
        {
            if (shouldStop()) { onDone(new PathResult()); yield break; }

            // NOTE: This scan is O(n) per iteration (O(n²) overall). Acceptable for
            // a teaching tool; consider a SortedSet or priority queue for larger graphs.
            int cur = -1;
            float best = float.MaxValue;
            foreach (var kv in dist)
                if (!closed.Contains(kv.Key) && kv.Value < best) { best = kv.Value; cur = kv.Key; }

            if (cur == -1) break;

            closed.Add(cur);
            vc++;

            if (cur == goalCode)
            {
                // FIX 2: Add the goal node to the tree before reporting done.
                SearchTreePanel.OnNodeVisited(cur, parent.ContainsKey(cur) ? parent[cur] : -1, algoName);
                var result = new PathResult { Path = Reconstruct(parent, startCode, goalCode), Visited = vc, Dist = dist[goalCode] };
                SearchTreePanel.OnPathFound(result.Path);
                onDone(result);
                yield break;
            }

            if (cur != startCode)
            {
                GraphMap.Instance.SetColor(cur, COLOR_VISITED);
                onStep?.Invoke(vc, -1);
                // FIX 1: Removed duplicate OnNodeVisited call.
                SearchTreePanel.OnNodeVisited(cur, parent.ContainsKey(cur) ? parent[cur] : -1, algoName);
                yield return new WaitForSeconds(stepDelay);
            }

            foreach (var (nb, d) in graph[cur].Neighbors)
            {
                if (closed.Contains(nb)) continue;
                float newDist = dist[cur] + d;
                if (newDist < dist[nb])
                {
                    dist[nb] = newDist;
                    parent[nb] = cur;
                    if (nb != goalCode)
                        GraphMap.Instance.SetColor(nb, COLOR_OPEN);
                }
            }
        }

        onDone(new PathResult { Visited = vc });
    }

    public static IEnumerator AStar(
        Dictionary<int, NodeData> graph,
        int startCode, int goalCode,
        float stepDelay, Func<bool> shouldStop,
        Action<PathResult> onDone, Action<int, int> onStep = null)
    {
        string algoName = "A*";
        SearchTreePanel.OnAlgoStart(algoName, startCode, goalCode);

        var gCost = new Dictionary<int, float>();
        var parent = new Dictionary<int, int>();
        var closed = new HashSet<int>();
        var open = new HashSet<int> { startCode };
        int vc = 0;

        foreach (var nd in graph.Values) gCost[nd.Code] = float.MaxValue;
        gCost[startCode] = 0f;
        parent[startCode] = -1;

        var goal = graph[goalCode];
        float H(int c)
        {
            var n = graph[c];
            float dLat = (n.Lat - goal.Lat) * 111f;
            float dLon = (n.Lon - goal.Lon) * 111f * Mathf.Cos(n.Lat * Mathf.Deg2Rad);
            return Mathf.Sqrt(dLat * dLat + dLon * dLon);
        }

        // FIX 2 & 3: Add the start node to the tree as the root.
        SearchTreePanel.OnNodeVisited(startCode, -1, algoName);

        while (open.Count > 0)
        {
            if (shouldStop()) { onDone(new PathResult()); yield break; }

            int cur = -1;
            float best = float.MaxValue;
            foreach (int c in open)
            {
                float f = gCost[c] + H(c);
                if (f < best) { best = f; cur = c; }
            }

            open.Remove(cur);
            closed.Add(cur);
            vc++;

            if (cur == goalCode)
            {
                // FIX 2: Add the goal node to the tree before reporting done.
                SearchTreePanel.OnNodeVisited(cur, parent.ContainsKey(cur) ? parent[cur] : -1, algoName);
                var result = new PathResult { Path = Reconstruct(parent, startCode, goalCode), Visited = vc, Dist = gCost[goalCode] };
                SearchTreePanel.OnPathFound(result.Path);
                onDone(result);
                yield break;
            }

            if (cur != startCode)
            {
                GraphMap.Instance.SetColor(cur, COLOR_VISITED);
                onStep?.Invoke(vc, -1);
                // FIX 1: Removed duplicate OnNodeVisited call.
                SearchTreePanel.OnNodeVisited(cur, parent.ContainsKey(cur) ? parent[cur] : -1, algoName);
                yield return new WaitForSeconds(stepDelay);
            }

            foreach (var (nb, d) in graph[cur].Neighbors)
            {
                if (closed.Contains(nb)) continue;
                float newG = gCost[cur] + d;
                if (newG < gCost[nb])
                {
                    gCost[nb] = newG;
                    parent[nb] = cur;
                    open.Add(nb);
                    if (nb != goalCode)
                        GraphMap.Instance.SetColor(nb, COLOR_OPEN);
                }
            }
        }

        onDone(new PathResult { Visited = vc });
    }

    public static float PathDistance(Dictionary<int, NodeData> graph, List<int> path)
    {
        float total = 0f;
        for (int i = 0; i < path.Count - 1; i++)
            foreach (var (nb, d) in graph[path[i]].Neighbors)
                if (nb == path[i + 1]) { total += d; break; }
        return total;
    }

    static List<int> Reconstruct(Dictionary<int, int> parent, int start, int goal)
    {
        var path = new List<int>();
        int cur = goal;
        while (cur != -1)
        {
            path.Insert(0, cur);
            cur = parent.ContainsKey(cur) ? parent[cur] : -1;
        }
        return path.Count > 0 && path[0] == start ? path : new List<int>();
    }
}
