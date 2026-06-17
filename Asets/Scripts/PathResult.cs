using System.Collections.Generic;

public class PathResult
{
    public List<int> Path    = new List<int>();
    public int       Visited = 0;
    public float     Dist    = 0f;
    public bool      Found   => Path != null && Path.Count > 0;
}
