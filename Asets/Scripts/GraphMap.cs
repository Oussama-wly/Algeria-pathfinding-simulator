using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphMap : MonoBehaviour
{
    public static GraphMap Instance { get; private set; }

    public Dictionary<int, NodeData> Nodes { get; private set; }

    const float LAT_MIN = 18.0f, LAT_MAX = 38.2f;
    const float LON_MIN = -9.5f, LON_MAX = 10.5f;
    const float WORLD_W = 18f, WORLD_H = 11f;

    static readonly Color COLOR_DEFAULT = new Color(0.10f, 0.22f, 0.40f);
    static readonly Color COLOR_EDGE = new Color(0.12f, 0.25f, 0.50f, 0.55f);

    // Wilayas whose names are hidden by default (too crowded in north)
    static readonly HashSet<int> HIDE_NAME = new HashSet<int>
    { 2,6,9,10,14,15,16,17,18,19,20,21,22,25,26,27,28,29,31,34,35,38,42,43,44,46,48,51,53,55 };

    readonly List<LineRenderer> edgeLines = new List<LineRenderer>();
    readonly List<LineRenderer> pathLines = new List<LineRenderer>();

    void Awake()
    {
        Instance = this;
        Nodes = new Dictionary<int, NodeData>();
        Build();
    }

    void Build()
    {
        var adj = WilayaData.BuildAdjacency();

        SpawnMapBackground();

        foreach (var w in WilayaData.Wilayas)
        {
            var nd = new NodeData
            {
                Code = w.Code,
                Name = w.Name,
                Lat = w.Lat,
                Lon = w.Lon,
                Neighbors = adj[w.Code],
            };
            nd.GameObject = SpawnNode(nd);
            Nodes[w.Code] = nd;
        }

        DrawEdges();
        StartCoroutine(FitCamera());
    }

    void SpawnMapBackground()
    {
        var sp = Resources.Load<Sprite>("AlgeriaMap");
        if (sp == null)
        {
            Debug.LogWarning("AlgeriaMap sprite not found in Resources folder.");
            return;
        }

        var go = new GameObject("AlgeriaMapBG");
        go.transform.SetParent(transform);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = new Color(1f, 1f, 1f, 0.22f);
        sr.sortingOrder = -1;
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sp.texture.filterMode = FilterMode.Bilinear;

        // Scale to match exact world bounds of the graph
        Vector3 worldBL = GeoToWorld(LAT_MIN, LON_MIN);
        Vector3 worldTR = GeoToWorld(LAT_MAX, LON_MAX);
        float worldW = worldTR.x - worldBL.x;
        float worldH = worldTR.y - worldBL.y;

        float sprW = sp.rect.width / sp.pixelsPerUnit;
        float sprH = sp.rect.height / sp.pixelsPerUnit;

        go.transform.localScale = new Vector3(worldW / sprW, worldH / sprH, 1f);

        // Center on world bounds midpoint, behind nodes
        float cx = (worldBL.x + worldTR.x) * 0.5f;
        float cy = (worldBL.y + worldTR.y) * 0.5f;
        float offsetX = 1.2f;
        float offsetY = -0.4f;
        go.transform.position = new Vector3(cx + offsetX, cy + offsetY, 1f);

        go.SetActive(false);   // hidden by default — toggled via SimulatorUI Map button
    }

    GameObject SpawnNode(NodeData nd)
    {
        var go = new GameObject($"W{nd.Code:D2}_{nd.Name}");
        go.transform.SetParent(transform);
        go.transform.position = GeoToWorld(nd.Lat, nd.Lon);
        go.transform.localScale = Vector3.one * 0.40f;

        nd.Renderer = go.AddComponent<SpriteRenderer>();
        nd.Renderer.sprite = MakeCircleSprite(64);
        nd.Renderer.color = COLOR_DEFAULT;
        nd.Renderer.sortingOrder = 2;

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        // Code number inside node
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform);
        lblGO.transform.localPosition = new Vector3(0f, 0f, -0.1f);
        var tm = lblGO.AddComponent<TextMesh>();
        tm.text = nd.Code.ToString();
        tm.fontSize = 30;
        tm.fontStyle = FontStyle.Bold;
        tm.color = Color.white;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.characterSize = 0.075f;
        lblGO.GetComponent<MeshRenderer>().sortingOrder = 3;
        nd.CodeLabel = tm;

        nd.NameLabel = null; // names shown in tooltip only

        var clicker = go.AddComponent<NodeClicker>();
        clicker.Code = nd.Code;

        return go;
    }

    void DrawEdges()
    {
        var drawn = new HashSet<string>();
        foreach (var nd in Nodes.Values)
        {
            foreach (var (nb, _) in nd.Neighbors)
            {
                string key = nd.Code < nb ? $"{nd.Code}-{nb}" : $"{nb}-{nd.Code}";
                if (drawn.Contains(key)) continue;
                drawn.Add(key);

                var lr = SpawnLine(COLOR_EDGE, 0.07f, -1);
                lr.SetPosition(0, nd.GameObject.transform.position);
                lr.SetPosition(1, Nodes[nb].GameObject.transform.position);
                edgeLines.Add(lr);
            }
        }
    }

    IEnumerator FitCamera()
    {
        yield return null;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var nd in Nodes.Values)
        {
            var p = nd.GameObject.transform.position;
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        var cam = Camera.main;
        float cx = (minX + maxX) / 2f;
        float cy = (minY + maxY) / 2f;
        float asp = Screen.width > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
        float size = Mathf.Max((maxY - minY) / 2f + 1f, (maxX - minX) / asp / 2f + 1f);

        cam.transform.position = new Vector3(cx, cy, -10f);
        cam.orthographicSize = size;
    }

    public void SetColor(int code, Color col)
    {
        if (Nodes.TryGetValue(code, out var nd))
            nd.Renderer.color = col;
    }

    // Show name when node is selected (start/goal/path)
    public void ShowName(int code, bool show)
    {
        if (!Nodes.TryGetValue(code, out var nd)) return;
        if (nd.NameLabel == null) return;
        nd.NameLabel.color = show
            ? new Color(0.88f, 0.94f, 1f)
            : (HIDE_NAME.Contains(code) ? new Color(0, 0, 0, 0) : new Color(0.88f, 0.94f, 1f));
    }

    public void ResetColors(int exceptStart = -1, int exceptGoal = -1)
    {
        foreach (var nd in Nodes.Values)
        {
            if (nd.Code != exceptStart && nd.Code != exceptGoal)
            {
                nd.Renderer.color = COLOR_DEFAULT;
                ShowName(nd.Code, false);
            }
        }
    }

    public void ClearPathLines()
    {
        foreach (var lr in pathLines)
            if (lr) Destroy(lr.gameObject);
        pathLines.Clear();
    }

    public LineRenderer AddPathLine(int fromCode, int toCode)
    {
        // Feature 6: Glow — draw a wider dimmer line behind for bloom effect
        AddColoredLine(fromCode, toCode, new Color(1f, 0.55f, 0.05f, 0.22f), 0.38f);
        // Feature 2: Main path line — thicker and brighter orange
        return AddColoredLine(fromCode, toCode, new Color(1f, 0.55f, 0.10f), 0.18f);
    }

    public LineRenderer AddColoredLine(int fromCode, int toCode, Color col, float width)
    {
        var lr = SpawnLine(col, width, 4);
        Vector3 a = Nodes[fromCode].GameObject.transform.position;
        Vector3 b = Nodes[toCode].GameObject.transform.position;
        float nodeR = 0.40f * 0.5f;
        Vector3 dir = (b - a).normalized;
        lr.SetPosition(0, a + dir * nodeR);
        lr.SetPosition(1, b - dir * nodeR);
        pathLines.Add(lr);
        return lr;
    }

    LineRenderer SpawnLine(Color col, float width, int order)
    {
        var go = new GameObject("Line");
        go.transform.SetParent(transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = col;
        lr.sortingOrder = order;
        return lr;
    }

    public static Vector3 GeoToWorld(float lat, float lon)
    {
        // Linear mapping: lat/lon : world space proportionally (no distortion)
        float x = (lon - LON_MIN) / (LON_MAX - LON_MIN) * WORLD_W - WORLD_W / 2f;
        float y = (lat - LAT_MIN) / (LAT_MAX - LAT_MIN) * WORLD_H - WORLD_H / 2f;
        return new Vector3(x, y, 0f);
    }

    static Sprite MakeCircleSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float r = size * 0.5f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Mathf.Sqrt((x - r) * (x - r) + (y - r) * (y - r));
                float a = d < r - 1.5f ? 1f : d < r ? Mathf.Max(0f, r - d) : 0f;
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
    }
}
