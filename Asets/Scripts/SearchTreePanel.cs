using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;


public class SearchTreePanel : MonoBehaviour
{
    public static SearchTreePanel Instance { get; private set; }

    // ── Colors
    static readonly Color BG = new Color(.04f, .07f, .16f, .97f);
    static readonly Color HDR = new Color(.05f, .10f, .22f, 1f);
    static readonly Color CYAN = new Color(.00f, .82f, .95f);
    static readonly Color WHITE = new Color(.90f, .94f, 1f);
    static readonly Color DIM = new Color(.38f, .52f, .72f);
    static readonly Color C_BFS = new Color(1f, .82f, .10f);
    static readonly Color C_DIJ = new Color(.00f, .90f, .55f);
    static readonly Color C_ASTAR = new Color(.40f, .85f, 1f);
    static readonly Color C_PATH = new Color(1f, .48f, .13f);
    static readonly Color C_START = new Color(.10f, .60f, .20f);
    static readonly Color C_GOAL = new Color(.85f, .15f, .15f);

    // ── Tree state
    bool visible = false;
    string algoName = "";
    Color algoColor = Color.white;
    int maxDepth = 0;
    int startCode = -1;
    int goalCode  = -1;   // known from algo start

    // depth → ordered list of node codes
    Dictionary<int, List<int>> byDepth = new Dictionary<int, List<int>>();
    // code → depth
    Dictionary<int, int> depthOf = new Dictionary<int, int>();
    // code → parent code (-1 = root)
    Dictionary<int, int> parentOf = new Dictionary<int, int>();
    // code → dot RectTransform
    Dictionary<int, RectTransform> dots = new Dictionary<int, RectTransform>();
    // code → edge GameObject (so we can reposition it)
    Dictionary<int, GameObject> edges = new Dictionary<int, GameObject>();
    // path set for colouring
    List<int> pathSet = new List<int>();

    // ── UI refs
    GameObject panelGO;
    RectTransform treeArea;
    float areaW = 0f;   // set once after layout
    TextMeshProUGUI lblAlgo, lblNodes, lblDepth, lblPathTxt;

    // Layout constants
    const float PANEL_W = 420f;
    const float PAD = 14f;     // padding so edge nodes stay inside
    const float DOT_D = 22f;      // dot diameter px
    const float LEVEL_H = 38f;     // vertical px per depth level
    const float MAX_STEP = 36f;     // max horizontal spacing

    // ── Static API
    public static void Toggle()
    {
        if (Instance != null) Instance.SetVisible(!Instance.visible);
    }
    public void ForceClose() { if (visible) SetVisible(false); }

    public static void OnAlgoStart(string algo)
    {
        if (Instance == null || !Instance.visible) return;
        Instance.ResetTree(algo);
    }

    // Called with start/goal so panel can colour them correctly from the start
    public static void OnAlgoStart(string algo, int start, int goal)
    {
        if (Instance == null || !Instance.visible) return;
        Instance.startCode = start;
        Instance.goalCode  = goal;
        Instance.ResetTree(algo);
    }

    public static void OnNodeVisited(int code, int parentCode, string algo)
    {
        if (Instance == null || !Instance.visible) return;
        Instance.AddNode(code, parentCode, algo);
    }

    public static void OnPathFound(List<int> path)
    {
        if (Instance == null || !Instance.visible) return;
        Instance.MarkPath(path);
    }

    // ── Lifecycle
    void Awake()
    {
        Instance = this;
        BuildUI();
        panelGO.SetActive(false);
    }

    void OnDestroy()
    {
        Instance = null;
        var cv = GameObject.Find("SearchTreeCanvas");
        if (cv != null) Destroy(cv);
    }

    // ── UI construction
    void BuildUI()
    {
        var cvGO = new GameObject("SearchTreeCanvas");
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 60;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // Panel — right side
        panelGO = new GameObject("TreePanel");
        panelGO.transform.SetParent(cvGO.transform, false);
        panelGO.AddComponent<Image>().color = BG;
        var pRT = panelGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(1, 0); pRT.anchorMax = new Vector2(1, 1);
        pRT.pivot = new Vector2(1, 0.5f);
        pRT.offsetMin = new Vector2(-PANEL_W, 0);
        pRT.offsetMax = new Vector2(0, 0);

        // Header
        var hdr = new GameObject("HDR"); hdr.transform.SetParent(pRT, false);
        hdr.AddComponent<Image>().color = HDR;
        var hRT = hdr.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
        hRT.offsetMin = new Vector2(0, -54); hRT.offsetMax = Vector2.zero;

        TL(hRT, "SEARCH TREE", 8, -8, 180, 18, 11, FontStyles.Bold, CYAN, TextAlignmentOptions.Left);
        lblAlgo = TL(hRT, "", 8, -26, 180, 14, 8, FontStyles.Normal, DIM, TextAlignmentOptions.Left);
        lblNodes = TL(hRT, "", 8, -40, 90, 12, 7, FontStyles.Normal, WHITE, TextAlignmentOptions.Left);
        lblDepth = TL(hRT, "", 110, -40, 120, 12, 7, FontStyles.Normal, WHITE, TextAlignmentOptions.Left);

        // Close button — anchored INSIDE panel (not outside right edge)
        var cl = new GameObject("CloseBtn"); cl.transform.SetParent(hRT, false);
        var clImg = cl.AddComponent<Image>(); clImg.color = new Color(.22f, .06f, .06f);
        var clBtn = cl.AddComponent<Button>(); clBtn.targetGraphic = clImg;
        var clCb = clBtn.colors;
        clCb.highlightedColor = new Color(.40f, .10f, .10f); clBtn.colors = clCb;
        clBtn.onClick.AddListener(() => SetVisible(false));
        var clRT = cl.GetComponent<RectTransform>();
        // Use left anchor + full width offset so button stays within panel bounds
        clRT.anchorMin = new Vector2(1, 0.5f); clRT.anchorMax = new Vector2(1, 0.5f);
        clRT.pivot = new Vector2(1f, 0.5f);
        clRT.anchoredPosition = new Vector2(-6, 0); clRT.sizeDelta = new Vector2(52, 20);
        TL(clRT, " Close ", 0, 0, 52, 20, 6.5f, FontStyles.Bold, new Color(1f, .65f, .65f), TextAlignmentOptions.Center);

        // Tree draw area (clipped + scrollable)
        var clip = new GameObject("Clip"); clip.transform.SetParent(pRT, false);
        clip.AddComponent<Image>().color = Color.clear;
        clip.AddComponent<RectMask2D>();
        var clipRT = clip.GetComponent<RectTransform>();
        clipRT.anchorMin = new Vector2(0, 0); clipRT.anchorMax = new Vector2(1, 1);
        clipRT.offsetMin = new Vector2(PAD, 106); clipRT.offsetMax = new Vector2(-PAD, -56);

        var content = new GameObject("Content"); content.transform.SetParent(clip.transform, false);
        content.AddComponent<Image>().color = Color.clear;
        treeArea = content.GetComponent<RectTransform>();
        treeArea.anchorMin = new Vector2(0, 1); treeArea.anchorMax = new Vector2(1, 1);
        treeArea.pivot = new Vector2(.5f, 1f);
        treeArea.offsetMin = Vector2.zero; treeArea.offsetMax = Vector2.zero;
        treeArea.sizeDelta = new Vector2(0, 600);
        areaW = PANEL_W - PAD * 2f;   // reliable fixed width

        // Legend
        var legBG = new GameObject("LegBG"); legBG.transform.SetParent(pRT, false);
        legBG.AddComponent<Image>().color = new Color(.04f, .08f, .18f, .80f);
        var legRT = legBG.GetComponent<RectTransform>();
        legRT.anchorMin = new Vector2(0, 0); legRT.anchorMax = new Vector2(1, 0);
        legRT.offsetMin = new Vector2(0, 42); legRT.offsetMax = new Vector2(0, 106);
        // Legend: 2 columns × 3 rows so everything fits
        var items = new[]{
            ("BFS",C_BFS),("Dijkstra",C_DIJ),("A*",C_ASTAR),
            ("Path",C_PATH),("Start",C_START),("Goal",C_GOAL)};
        for (int i = 0; i < items.Length; i++)
        {
            float col2x = (i % 2 == 0) ? 6f : PANEL_W / 2f;
            float ry = -8f - (i / 2) * 18f;          // row y, centred on dot
            var (nm, col) = items[i];
            // Dot — centred at ry
            var dgo = new GameObject("LD"); dgo.transform.SetParent(legRT, false);
            dgo.AddComponent<Image>().color = col;
            var drt = dgo.GetComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = new Vector2(0, 1);
            drt.pivot = new Vector2(0, .5f);
            drt.anchoredPosition = new Vector2(col2x, ry);
            drt.sizeDelta = new Vector2(9, 9);
            // Text — same ry, pivot centred vertically
            var tgo = new GameObject("T"); tgo.transform.SetParent(legRT, false);
            var tmp = tgo.AddComponent<TextMeshProUGUI>();
            tmp.text = nm; tmp.fontSize = 6.5f; tmp.color = col;
            tmp.alignment = TextAlignmentOptions.Left;
            var trt = tgo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0, 1);
            trt.pivot = new Vector2(0, .5f);
            trt.anchoredPosition = new Vector2(col2x + 12, ry);
            trt.sizeDelta = new Vector2(PANEL_W / 2f - 18, 12);
        }

        // Path text bar
        var pathBG = new GameObject("PathBG"); pathBG.transform.SetParent(pRT, false);
        pathBG.AddComponent<Image>().color = new Color(.05f, .09f, .20f);
        var pbRT = pathBG.GetComponent<RectTransform>();
        pbRT.anchorMin = new Vector2(0, 0); pbRT.anchorMax = new Vector2(1, 0);
        pbRT.offsetMin = new Vector2(0, 0); pbRT.offsetMax = new Vector2(0, 42);
        lblPathTxt = TL(pbRT, "Path will appear here", 4, -4, PANEL_W - 8, 36, 7,
            FontStyles.Normal, DIM, TextAlignmentOptions.TopLeft);
        lblPathTxt.enableWordWrapping = true;

        panelGO.SetActive(false);
    }

    void SetVisible(bool on)
    {
        visible = on;
        if (panelGO != null) panelGO.SetActive(on);
        if (!on) ResetTree("");
    }

    // ── Tree logic
    void ResetTree(string algo)
    {
        algoName = algo; startCode = -1; goalCode = -1;
        algoColor = algo == "BFS" ? C_BFS : algo == "Dijkstra" ? C_DIJ : C_ASTAR;
        maxDepth = 0;
        byDepth.Clear(); depthOf.Clear(); parentOf.Clear();
        dots.Clear(); edges.Clear(); pathSet.Clear();

        if (treeArea) foreach (Transform ch in treeArea) Destroy(ch.gameObject);
        if (treeArea) treeArea.sizeDelta = new Vector2(0, 600);

        if (lblAlgo) lblAlgo.text = algo.Length > 0 ? $"Algorithm: {algo}" : "";
        if (lblNodes) lblNodes.text = "Nodes: 0";
        if (lblDepth) lblDepth.text = "Depth: 0";
        if (lblPathTxt) lblPathTxt.text = "Path will appear here";
        if (lblAlgo) lblAlgo.color = algoColor;
    }

    void AddNode(int code, int parentCode, string algo)
    {
        if (dots.ContainsKey(code)) return;
        if (algoName != algo) ResetTree(algo);

        // ── Compute depth
        int depth = 0;
        if (parentCode >= 0 && depthOf.TryGetValue(parentCode, out int pd))
            depth = pd + 1;
        else if (parentCode >= 0)
            depth = 1;

        depthOf[code] = depth;
        parentOf[code] = parentCode;

        if (!byDepth.ContainsKey(depth)) byDepth[depth] = new List<int>();
        byDepth[depth].Add(code);
        if (depth > maxDepth) maxDepth = depth;

        // Record start node (root)
        if (parentCode < 0 && startCode < 0) startCode = code;

        // ── Spawn dot
        var dot = new GameObject($"D{code}");
        dot.transform.SetParent(treeArea, false);
        var dImg = dot.AddComponent<Image>();
        dImg.sprite = MakeCircle(16);

        // Color: start=green, goal=red, others=algo color
        if      (code == startCode) dImg.color = C_START;
        else if (code == goalCode)  dImg.color = C_GOAL;
        else                        dImg.color = algoColor;

        var dRT = dot.GetComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(.5f, 1f);
        dRT.pivot = new Vector2(.5f, .5f);
        dRT.sizeDelta = new Vector2(DOT_D, DOT_D);
        dots[code] = dRT;

        // Number label
        var lbl = new GameObject("L"); lbl.transform.SetParent(dot.transform, false);
        var txt = lbl.AddComponent<TextMeshProUGUI>();
        txt.text = code.ToString(); txt.fontSize = 7f; txt.fontStyle = FontStyles.Bold;
        txt.color = new Color(.05f, .08f, .18f); txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        var lRT = lbl.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;

        // ── Reposition entire row + redraw all edges in it ──
        RedistributeRow(depth);

        // ── Grow content if needed 
        float needed = Mathf.Abs(RowY(depth)) + 30f;
        if (treeArea.sizeDelta.y < needed)
            treeArea.sizeDelta = new Vector2(0, needed);

        // Stats
        lblNodes.text = $"Nodes: {dots.Count}";
        lblDepth.text = $"Depth: {maxDepth}";
    }

    // Reposition all dots in a row and redraw their edges to parents
    void RedistributeRow(int depth)
    {
        var row = byDepth[depth];
        int count = row.Count;
        float usable = areaW - DOT_D;
        float step = Mathf.Min(usable / Mathf.Max(count, 1), MAX_STEP);
        float xBase = -(count - 1) * step / 2f;
        float y = RowY(depth);

        for (int i = 0; i < count; i++)
        {
            int c = row[i];
            if (!dots.TryGetValue(c, out var dr)) continue;
            dr.anchoredPosition = new Vector2(xBase + i * step, y);
            RedrawEdge(c);
        }

        // FIX: after moving nodes in this row, redraw edges of their children
        // (children's edges point to parents — if parent moved, line is stale)
        if (byDepth.TryGetValue(depth + 1, out var childRow))
            foreach (int child in childRow)
                RedrawEdge(child);
    }

    // Remove old edge GO for this node and draw a fresh one
    void RedrawEdge(int code)
    {
        // Destroy old edge if exists
        if (edges.TryGetValue(code, out var old) && old != null)
            Destroy(old);
        edges.Remove(code);

        int pCode = parentOf.ContainsKey(code) ? parentOf[code] : -1;
        if (pCode < 0) return;   // root has no edge
        if (!dots.TryGetValue(pCode, out var pRT)) return;
        if (!dots.TryGetValue(code, out var cRT)) return;

        var go = new GameObject($"E{code}");
        go.transform.SetParent(treeArea, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(algoColor.r, algoColor.g, algoColor.b, .28f);

        Vector2 from = pRT.anchoredPosition;
        Vector2 to = cRT.anchoredPosition;
        Vector2 mid = (from + to) * .5f;
        float len = Vector2.Distance(from, to);
        float ang = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = mid;
        rt.sizeDelta = new Vector2(len, 1.2f);
        rt.localEulerAngles = new Vector3(0, 0, ang);
        rt.SetAsFirstSibling(); // edges behind dots

        edges[code] = go;
    }

    float RowY(int depth) => -(depth * LEVEL_H + DOT_D + PAD);

    void MarkPath(List<int> path)
    {
        pathSet = new List<int>(path);

        for (int i = 0; i < path.Count; i++)
        {
            int c = path[i];
            if (!dots.TryGetValue(c, out var rt)) continue;

            // Start = green, Goal (last) = red, intermediate = orange
            Color col = (i == 0) ? C_START :
                        (i == path.Count - 1) ? C_GOAL : C_PATH;
            rt.GetComponent<Image>().color = col;

            // Recolor edge to parent to orange for path visibility
            if (i > 0 && edges.TryGetValue(c, out var eGO) && eGO != null)
                eGO.GetComponent<Image>().color = new Color(C_PATH.r, C_PATH.g, C_PATH.b, .80f);
        }

        // Path text
        if (GraphMap.Instance != null)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < path.Count; i++)
            {
                if (GraphMap.Instance.Nodes.TryGetValue(path[i], out var nd))
                {
                    if (i > 0) sb.Append(" → ");
                    sb.Append(nd.Name);
                }
            }
            lblPathTxt.text = sb.ToString();
            lblPathTxt.color = C_PATH;
        }
    }

    // ── UI Helpers 

    TextMeshProUGUI TL(RectTransform p, string text, float x, float y, float w, float h,
        float size, FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(p, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = col; t.alignment = align;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return t;
    }

    static Sprite MakeCircle(int s)
    {
        var tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
        var px = new Color[s * s]; float r = s / 2f;
        for (int y = 0; y < s; y++) for (int x = 0; x < s; x++)
            {
                float d = Mathf.Sqrt((x - r + .5f) * (x - r + .5f) + (y - r + .5f) * (y - r + .5f));
                px[y * s + x] = new Color(1, 1, 1, d < r - 1 ? 1f : d < r ? r - d : 0f);
            }
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 1);
    }
}
