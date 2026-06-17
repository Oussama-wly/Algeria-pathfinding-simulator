using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Blocked Roads Puzzle:
/// Several edges on the map are randomly blocked (shown in red).
/// The player must find a valid path from Start to Goal manually,
/// avoiding all blocked edges. Then Dijkstra also runs on the
/// restricted graph and compares the result.
///
/// Difficulty levels control how many edges are blocked and
/// whether the blocked edges cut the most obvious route.
/// </summary>
public class BlockedRoads : MonoBehaviour
{
    public static BlockedRoads Instance { get; private set; }

    // ── Colors 
    static readonly Color C_USER = new Color(1f, .70f, .10f);   // gold
    static readonly Color C_OPTIMAL = new Color(.00f, .90f, .55f); // green
    static readonly Color C_BLOCKED = new Color(1f, .18f, .25f);   // red
    static readonly Color C_START = new Color(.00f, .90f, .55f);
    static readonly Color C_GOAL = new Color(1f, .20f, .35f);
    static readonly Color C_DEFAULT = new Color(.10f, .22f, .40f);
    static readonly Color CYAN = new Color(.00f, .82f, .95f);
    static readonly Color WHITE = new Color(.92f, .96f, 1f);
    static readonly Color DIM = new Color(.40f, .58f, .80f);
    static readonly Color YELLOW = new Color(1f, .85f, .15f);
    static readonly Color RED = new Color(1f, .30f, .30f);
    static readonly Color BG_PANEL = new Color(.05f, .09f, .20f, .97f);
    static readonly Color BG_HDR = new Color(.04f, .07f, .17f, 1f);
    readonly Color[] DIFF_COLORS = {
    new Color(.05f,.30f,.15f),
    new Color(.25f,.18f,.04f),
    new Color(.35f,.06f,.06f)
};

    // ── Difficulty 
    enum Difficulty { Easy, Medium, Hard }
    Difficulty difficulty = Difficulty.Medium;

    // Easy=3 blocked, Medium=5, Hard=8
    static readonly int[] BLOCK_COUNT = { 3, 5, 8 };

    // ── State
    enum PuzzleState { Idle, Drawing, Done }
    PuzzleState state = PuzzleState.Idle;

    int startCode = -1, goalCode = -1;
    List<int> userPath = new List<int>();
    List<int> optimalPath = new List<int>();
    float userDist, optimalDist;

    // Blocked edges stored as sorted pairs
    HashSet<(int, int)> blockedEdges = new HashSet<(int, int)>();
    // Blocked edge LineRenderers for cleanup
    List<LineRenderer> blockedLines = new List<LineRenderer>();

    // ── UI refs 
    Canvas puzzleCanvas;
    GameObject panel;
    TextMeshProUGUI lblTitle, lblInstruction, lblUserDist,
                    lblOptDist, lblDiff, lblStatus, lblBlockCount;
    Button btnStart, btnReset, btnReveal, btnClose;
    Button btnEasy, btnMed, btnHard;
    GameObject resultCard;

    // ── Open / Close 
    public static void Open()
    {
        if (Instance != null) return;
        new GameObject("BlockedRoads").AddComponent<BlockedRoads>();
    }

    public static void Close()
    {
        if (Instance == null) return;
        Destroy(Instance.puzzleCanvas.gameObject);
        Destroy(Instance.gameObject);
    }

    void Awake()
    {
        Instance = this;
        // Close other panels that conflict
        PathPuzzle.Close();
        SearchTreePanel.Instance?.ForceClose();
        BuildUI();
    }

    void OnDestroy()
    {
        Instance = null;
        CleanUp();
        SimulatorController.Instance?.SetBlockedRoadsMode(false);
    }

    // ── UI Construction 
    void BuildUI()
    {
        var cvGO = new GameObject("BlockedCanvas");
        puzzleCanvas = cvGO.AddComponent<Canvas>();
        puzzleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        puzzleCanvas.sortingOrder = 70;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // Side panel — right side
        panel = new GameObject("BlockPanel");
        panel.transform.SetParent(cvGO.transform, false);
        panel.AddComponent<Image>().color = BG_PANEL;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-230, 0); rt.offsetMax = Vector2.zero;

        float y = -14f;

        // ── Header ──
        var hdr = new GameObject("Hdr"); hdr.transform.SetParent(rt, false);
        hdr.AddComponent<Image>().color = BG_HDR;
        var hRT = hdr.GetComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
        hRT.offsetMin = new Vector2(0, -56); hRT.offsetMax = Vector2.zero;

        T(hRT, "BLOCKED ROADS", 0, -14, 220, 22, 13, FontStyles.Bold, CYAN, TextAlignmentOptions.Center);
        T(hRT, "Avoid the red roads!", 0, -34, 220, 16, 8, FontStyles.Normal, DIM, TextAlignmentOptions.Center);
        lblBlockCount = T(hRT, "", 0, -48, 220, 12, 7, FontStyles.Normal, RED, TextAlignmentOptions.Center);

        y = -70f;

        // ── Difficulty buttons ──
        T(rt, "DIFFICULTY", 0, y, 220, 14, 7, FontStyles.Bold, DIM, TextAlignmentOptions.Center); y -= 18f;

        var diffRow = new GameObject("DiffRow"); diffRow.transform.SetParent(rt, false);
        var drRT = diffRow.GetComponent<RectTransform>();
        if (drRT == null) drRT = diffRow.AddComponent<RectTransform>();
        drRT.anchorMin = drRT.anchorMax = new Vector2(.5f, 1f); drRT.pivot = new Vector2(.5f, 1f);
        drRT.anchoredPosition = new Vector2(0, y); drRT.sizeDelta = new Vector2(220, 28);

        btnEasy = DiffBtn(drRT, "Easy", -72, new Color(.05f, .30f, .15f));
        btnMed = DiffBtn(drRT, "Medium", 0, new Color(.25f, .18f, .04f));
        btnHard = DiffBtn(drRT, "Hard", 72, new Color(.35f, .06f, .06f));

        btnEasy.onClick.AddListener(() => SetDifficulty(Difficulty.Easy));
        btnMed.onClick.AddListener(() => SetDifficulty(Difficulty.Medium));
        btnHard.onClick.AddListener(() => SetDifficulty(Difficulty.Hard));
        HighlightDiffBtn();
        y -= 36f;

        HLine(rt, y); y -= 14f;

        // ── Instruction ──
        lblInstruction = T(rt, "Choose difficulty\nthen press Start!", 0, y, 210, 50, 9,
            FontStyles.Normal, WHITE, TextAlignmentOptions.Center);
        lblInstruction.enableWordWrapping = true; y -= 56f;

        HLine(rt, y); y -= 14f;

        // ── Score ──
        T(rt, "YOUR PATH", 0, y, 220, 13, 8, FontStyles.Bold, YELLOW, TextAlignmentOptions.Center); y -= 16f;
        lblUserDist = T(rt, "— km", 0, y, 220, 24, 15, FontStyles.Bold, YELLOW, TextAlignmentOptions.Center); y -= 28f;

        T(rt, "BEST POSSIBLE", 0, y, 220, 13, 8, FontStyles.Bold, C_OPTIMAL, TextAlignmentOptions.Center); y -= 16f;
        lblOptDist = T(rt, "— km", 0, y, 220, 24, 15, FontStyles.Bold, C_OPTIMAL, TextAlignmentOptions.Center); y -= 28f;

        HLine(rt, y); y -= 12f;
        lblDiff = T(rt, "", 0, y, 205, 44, 10, FontStyles.Bold, WHITE, TextAlignmentOptions.Center);
        lblDiff.enableWordWrapping = true; y -= 50f;

        lblStatus = T(rt, "", 0, y, 205, 36, 8, FontStyles.Normal, DIM, TextAlignmentOptions.Center);
        lblStatus.enableWordWrapping = true; y -= 42f;

        // ── Buttons
        btnStart = Btn(rt, "Start Game", 0, y, 195, 38, new Color(.05f, .32f, .18f), 12); y -= 46f;
        btnReveal = Btn(rt, "Show Best Path", 0, y, 195, 28, new Color(.08f, .20f, .42f), 10); y -= 34f;
        btnReset = Btn(rt, "Reset", 0, y, 195, 26, new Color(.22f, .12f, .04f), 10); y -= 32f;
        btnClose = Btn(rt, "Close", 0, y, 195, 24, new Color(.32f, .08f, .08f), 9); y -= 32f;
        HLine(rt, y); y -= 14f;

        btnReveal.interactable = false;
        btnReset.interactable = false;

        btnStart.onClick.AddListener(StartGame);
        btnReveal.onClick.AddListener(RevealOptimal);
        btnReset.onClick.AddListener(ResetGame);
        btnClose.onClick.AddListener(() => {
            Destroy(puzzleCanvas.gameObject);
            Destroy(gameObject);
        });

        // ── Legend
        float ly = y - 18f;
        HLine(rt, ly); ly -= 14f;
        // Header widget
        var lhGO = new GameObject("LegHdr"); lhGO.transform.SetParent(rt, false);
        lhGO.AddComponent<Image>().color = new Color(.05f, .10f, .24f);
        var lhRT = lhGO.GetComponent<RectTransform>();
        lhRT.anchorMin = new Vector2(0f, 1f); lhRT.anchorMax = new Vector2(1f, 1f);
        lhRT.pivot = new Vector2(.5f, 1f);
        lhRT.anchoredPosition = new Vector2(0, ly); lhRT.sizeDelta = new Vector2(0, 20);
        T(lhRT, "COLOR LEGEND", 0, -4, 220, 16, 8, FontStyles.Bold, DIM, TextAlignmentOptions.Center);
        ly -= 24f;
        LegRow(rt, "Your path", C_USER, ly); ly -= 20f;
        LegRow(rt, "Best path", C_OPTIMAL, ly); ly -= 20f;
        LegRow(rt, "Blocked road", C_BLOCKED, ly); ly -= 20f;
        LegRow(rt, "Start node", C_START, ly); ly -= 20f;
        LegRow(rt, "Goal node", C_GOAL, ly);
    }

    // ── Difficulty 
    void SetDifficulty(Difficulty d)
    {
        if (state != PuzzleState.Idle) return;
        difficulty = d;
        HighlightDiffBtn();
    }

    void HighlightDiffBtn()
    {
        SetDiffAlpha(btnEasy, difficulty == Difficulty.Easy);
        SetDiffAlpha(btnMed, difficulty == Difficulty.Medium);
        SetDiffAlpha(btnHard, difficulty == Difficulty.Hard);
    }

    void SetDiffAlpha(Button b, bool selected)
    {
        var img = b.GetComponent<Image>();
        int idx = b == btnEasy ? 0 : b == btnMed ? 1 : 2;
        Color c = DIFF_COLORS[idx];
        img.color = selected
            ? new Color(c.r * 1.8f, c.g * 1.8f, c.b * 1.8f, 1f)
            : new Color(c.r * .5f, c.g * .5f, c.b * .5f, .75f);
    }

    // ── Game Logic 
    void StartGame()
    {
        ResetGame();

        var gm = GraphMap.Instance;
        var codes = new List<int>(gm.Nodes.Keys);

        // Pick start and goal far apart
        startCode = codes[Random.Range(0, codes.Count)];
        int attempts = 0;
        do
        {
            goalCode = codes[Random.Range(0, codes.Count)];
            attempts++;
        }
        while (attempts < 200 && (goalCode == startCode ||
               Vector3.Distance(
                   gm.Nodes[startCode].GameObject.transform.position,
                   gm.Nodes[goalCode].GameObject.transform.position) < 5f));

        // ── Block edges 
        blockedEdges.Clear();
        int blockCount = BLOCK_COUNT[(int)difficulty];

        // Strategy: block edges on the Dijkstra shortest path first (makes it hard),
        // then add random blocks — but always verify the graph stays connected (path exists).
        // Step 1: find shortest path to know which edges to target
        var shortPath = QuickDijkstra(gm.Nodes, startCode, goalCode);

        // Candidate edges to block — prefer edges on shortest path
        var candidates = new List<(int, int)>();
        if (shortPath != null)
            for (int i = 0; i < shortPath.Count - 1; i++)
                candidates.Add(Pair(shortPath[i], shortPath[i + 1]));

        // Shuffle and add random edges too
        var allEdges = AllEdges(gm.Nodes);
        Shuffle(allEdges);
        foreach (var e in allEdges)
            if (!candidates.Contains(e)) candidates.Add(e);

        // Try to block up to blockCount edges — always keep a valid path
        foreach (var edge in candidates)
        {
            if (blockedEdges.Count >= blockCount) break;
            blockedEdges.Add(edge);
            // Verify path still exists
            if (QuickDijkstra(BuildRestrictedNodes(gm.Nodes, blockedEdges), startCode, goalCode) == null)
                blockedEdges.Remove(edge); // would disconnect — skip
        }

        // ── Draw blocked edges in red ──
        foreach (var (a, b) in blockedEdges)
        {
            var lr = DrawBlockedLine(a, b);
            blockedLines.Add(lr);
        }

        // Color start / goal
        gm.SetColor(startCode, C_START);
        gm.SetColor(goalCode, C_GOAL);
        gm.ShowName(startCode, true);
        gm.ShowName(goalCode, true);

        userPath.Clear();
        userPath.Add(startCode);
        userDist = 0f;

        state = PuzzleState.Drawing;
        SimulatorController.Instance?.SetBlockedRoadsMode(true);

        string sName = gm.Nodes[startCode].Name;
        string gName = gm.Nodes[goalCode].Name;
        lblInstruction.text = $"From: {sName}\nTo: {gName}\n\nAvoid red roads!\nClick adjacent\nwilayas.";
        lblStatus.text = $"In path: 1\nAt: {sName}";
        lblUserDist.text = "0 km";
        lblBlockCount.text = $"{blockedEdges.Count} roads blocked";

        btnStart.interactable = false;
        btnReset.interactable = true;
        btnReveal.interactable = true;

        btnEasy.interactable = false;
        btnMed.interactable = false;
        btnHard.interactable = false;
    }

    public void OnWilayaClicked(int code)
    {
        if (state != PuzzleState.Drawing) return;

        int last = userPath[userPath.Count - 1];
        var nd = GraphMap.Instance.Nodes[last];

        // Find edge distance
        float edgeDist = -1f;
        foreach (var (nb, dist) in nd.Neighbors)
            if (nb == code) { edgeDist = dist; break; }

        if (edgeDist < 0f)
        {
            lblStatus.text = "Not adjacent!\nClick a neighboring\nwilaya.";
            return;
        }

        // Check if edge is blocked
        var ep = Pair(last, code);
        if (blockedEdges.Contains(ep))
        {
            lblStatus.text = "Road blocked!\nChoose another\nroute.";
            StartCoroutine(FlashBlocked(last, code));
            return;
        }

        // No loops
        if (userPath.Contains(code) && code != goalCode)
        {
            lblStatus.text = "Already visited!\nNo loops allowed.";
            return;
        }

        // Accept move
        userPath.Add(code);
        userDist += edgeDist;

        GraphMap.Instance.AddColoredLine(last, code, C_USER, 0.18f);
        if (code != startCode && code != goalCode)
            GraphMap.Instance.SetColor(code, C_USER);

        lblUserDist.text = $"{userDist:0} km";
        lblStatus.text = $"Nodes: {userPath.Count}\nLast: {GraphMap.Instance.Nodes[code].Name}";

        if (code == goalCode)
        {
            state = PuzzleState.Done;
            SimulatorController.Instance?.SetBlockedRoadsMode(false);
            StartCoroutine(Evaluate());
        }
    }

    IEnumerator FlashBlocked(int a, int b)
    {
        // Flash the blocked line bright white briefly
        var pos0 = GraphMap.Instance.Nodes[a].GameObject.transform.position;
        var pos1 = GraphMap.Instance.Nodes[b].GameObject.transform.position;
        var lr = SpawnTempLine(pos0, pos1, Color.white, 0.25f);
        yield return new WaitForSeconds(0.18f);
        if (lr) Destroy(lr.gameObject);
    }

    IEnumerator Evaluate()
    {
        lblInstruction.text = "Calculating\nbest path...";

        // Run Dijkstra on restricted graph
        var restricted = BuildRestrictedNodes(GraphMap.Instance.Nodes, blockedEdges);
        PathResult result = null;
        yield return Algorithms.Dijkstra(restricted, startCode, goalCode,
            0f, () => false, r => result = r, null);

        // Restore map colors after Dijkstra painted them
        GraphMap.Instance.ResetColors();
        GraphMap.Instance.SetColor(startCode, C_START);
        GraphMap.Instance.SetColor(goalCode, C_GOAL);
        // Re-color user path nodes
        foreach (var c in userPath)
            if (c != startCode && c != goalCode)
                GraphMap.Instance.SetColor(c, C_USER);

        if (result != null && result.Found)
        {
            optimalPath = result.Path;
            optimalDist = result.Dist;
            lblOptDist.text = $"{optimalDist:0} km";

            float diff = userDist - optimalDist;
            float pct = optimalDist > 0 ? (diff / optimalDist) * 100f : 0f;

            if (diff <= 1f)
            {
                lblDiff.text = " PERFECT!\nYou found the\nbest route!";
                lblDiff.color = C_OPTIMAL;
            }
            else if (pct < 12f)
            {
                lblDiff.text = $"+{diff:0} km ({pct:0.0}%)\nAlmost optimal!";
                lblDiff.color = YELLOW;
            }
            else if (pct < 35f)
            {
                lblDiff.text = $"+{diff:0} km ({pct:0.0}%)\nNot bad!";
                lblDiff.color = YELLOW;
            }
            else
            {
                lblDiff.text = $"+{diff:0} km ({pct:0.0}%)\nThere was a\nbetter route!";
                lblDiff.color = RED;
            }

            lblInstruction.text = "Done!\nPress 'Show Best'\nto see optimal path.";
        }
        else
        {
            lblDiff.text = "Could not find\nbest path.";
            lblDiff.color = DIM;
        }
    }

    void RevealOptimal()
    {
        if (optimalPath == null || optimalPath.Count == 0) return;
        StartCoroutine(AnimateOptimal());
    }

    IEnumerator AnimateOptimal()
    {
        btnReveal.interactable = false;
        for (int i = 0; i < optimalPath.Count - 1; i++)
        {
            int a = optimalPath[i], b = optimalPath[i + 1];
            GraphMap.Instance.AddColoredLine(a, b, C_OPTIMAL, 0.22f);
            if (b != startCode && b != goalCode)
                GraphMap.Instance.SetColor(b, C_OPTIMAL);
            yield return new WaitForSeconds(0.15f);
        }
        GraphMap.Instance.SetColor(startCode, C_START);
        GraphMap.Instance.SetColor(goalCode, C_GOAL);
    }

    void ResetGame()
    {
        state = PuzzleState.Idle;
        SimulatorController.Instance?.SetBlockedRoadsMode(false);
        CleanUp();

        userPath.Clear(); optimalPath.Clear();
        userDist = 0f; optimalDist = 0f;
        startCode = -1; goalCode = -1;

        lblInstruction.text = "Choose difficulty\nthen press Start!";
        lblUserDist.text = "— km";
        lblOptDist.text = "— km";
        lblDiff.text = "";
        lblStatus.text = "";
        lblBlockCount.text = "";

        btnStart.interactable = true;
        btnReveal.interactable = false;
        btnReset.interactable = false;

        btnEasy.interactable = true;
        btnMed.interactable = true;
        btnHard.interactable = true;

        HighlightDiffBtn();
    }

    void CleanUp()
    {
        // Remove blocked edge lines
        foreach (var lr in blockedLines)
            if (lr) Destroy(lr.gameObject);
        blockedLines.Clear();
        blockedEdges.Clear();

        if (GraphMap.Instance == null) return;
        GraphMap.Instance.ClearPathLines();
        GraphMap.Instance.ResetColors();
    }

    // ── Graph helpers

    /// Build a copy of nodes with blocked edges removed (for Dijkstra)
    static Dictionary<int, NodeData> BuildRestrictedNodes(
        Dictionary<int, NodeData> original,
        HashSet<(int, int)> blocked)
    {
        var restricted = new Dictionary<int, NodeData>();
        foreach (var kv in original)
        {
            var orig = kv.Value;
            var copy = new NodeData
            {
                Code = orig.Code,
                Name = orig.Name,
                Lat = orig.Lat,
                Lon = orig.Lon,
                GameObject = orig.GameObject,
                Renderer = orig.Renderer,
                CodeLabel = orig.CodeLabel,
                NameLabel = orig.NameLabel,
            };
            copy.Neighbors = new List<(int, float)>();
            foreach (var (nb, dist) in orig.Neighbors)
            {
                var ep = Pair(orig.Code, nb);
                if (!blocked.Contains(ep))
                    copy.Neighbors.Add((nb, dist));
            }
            restricted[copy.Code] = copy;
        }
        return restricted;
    }

    /// Quick Dijkstra without coroutines (for setup validation)
    static List<int> QuickDijkstra(Dictionary<int, NodeData> graph, int start, int goal)
    {
        var dist = new Dictionary<int, float>();
        var parent = new Dictionary<int, int>();
        var closed = new HashSet<int>();

        foreach (var k in graph.Keys) dist[k] = float.MaxValue;
        dist[start] = 0f; parent[start] = -1;

        while (true)
        {
            int cur = -1;
            float best = float.MaxValue;
            foreach (var kv in dist)
                if (!closed.Contains(kv.Key) && kv.Value < best) { best = kv.Value; cur = kv.Key; }

            if (cur == -1) return null;
            if (cur == goal)
            {
                var path = new List<int>();
                int c = goal;
                while (c != -1) { path.Add(c); parent.TryGetValue(c, out c); }
                path.Reverse();
                return path;
            }
            closed.Add(cur);
            if (!graph.ContainsKey(cur)) continue;
            foreach (var (nb, d) in graph[cur].Neighbors)
            {
                if (closed.Contains(nb)) continue;
                float nd2 = dist[cur] + d;
                if (nd2 < dist[nb]) { dist[nb] = nd2; parent[nb] = cur; }
            }
        }
    }

    static List<(int, int)> AllEdges(Dictionary<int, NodeData> nodes)
    {
        var seen = new HashSet<string>();
        var edges = new List<(int, int)>();
        foreach (var nd in nodes.Values)
            foreach (var (nb, _) in nd.Neighbors)
            {
                var key = nd.Code < nb ? $"{nd.Code}-{nb}" : $"{nb}-{nd.Code}";
                if (seen.Add(key)) edges.Add(Pair(nd.Code, nb));
            }
        return edges;
    }

    static (int, int) Pair(int a, int b) => a < b ? (a, b) : (b, a);

    static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    // ── Line drawing
    LineRenderer DrawBlockedLine(int a, int b)
    {
        var gm = GraphMap.Instance;
        var lr = SpawnLine(C_BLOCKED, 0.14f, 5);
        Vector3 p0 = gm.Nodes[a].GameObject.transform.position;
        Vector3 p1 = gm.Nodes[b].GameObject.transform.position;
        Vector3 dir = (p1 - p0).normalized;
        float nodeR = 0.40f * 0.5f;
        lr.SetPosition(0, p0 + dir * nodeR);
        lr.SetPosition(1, p1 - dir * nodeR);

        // Also add an X marker at midpoint
        Vector3 mid = (p0 + p1) * 0.5f;
        var xLr = SpawnLine(new Color(1f, 1f, 1f, .70f), 0.09f, 6);
        xLr.positionCount = 4;
        float hs = 0.18f;
        xLr.SetPosition(0, mid + new Vector3(-hs, hs, 0));
        xLr.SetPosition(1, mid + new Vector3(hs, -hs, 0));
        xLr.SetPosition(2, mid + new Vector3(-hs, -hs, 0)); // trick: same line obj, Z shape
        xLr.SetPosition(3, mid + new Vector3(hs, hs, 0));
        blockedLines.Add(xLr);

        return lr;
    }

    LineRenderer SpawnTempLine(Vector3 a, Vector3 b, Color col, float w)
    {
        var lr = SpawnLine(col, w, 6);
        lr.SetPosition(0, a); lr.SetPosition(1, b);
        return lr;
    }

    LineRenderer SpawnLine(Color col, float width, int order)
    {
        var go = new GameObject("BL");
        go.transform.SetParent(GraphMap.Instance.transform);
        var lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = lr.endWidth = width;
        lr.startColor = lr.endColor = col;
        lr.sortingOrder = order;
        return lr;
    }

    // ── UI Helpers 
    TextMeshProUGUI T(RectTransform p, string text, float x, float y,
        float w, float h, float size, FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(p, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = col; t.alignment = align; t.enableWordWrapping = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return t;
    }

    Button Btn(RectTransform p, string label, float x, float y,
        float w, float h, Color col, float fs)
    {
        var go = new GameObject("B"); go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>(); img.color = col;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = Color.Lerp(col, Color.white, .22f);
        cb.disabledColor = new Color(col.r * .3f, col.g * .3f, col.b * .3f, .5f);
        btn.colors = cb;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        T(rt, label, 0, 0, w, h, fs, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        return btn;
    }

    Button DiffBtn(RectTransform p, string label, float x, Color col)
    {
        var go = new GameObject("D"); go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>(); img.color = col;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = Color.Lerp(col, Color.white, .25f); btn.colors = cb;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f); rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, 0); rt.sizeDelta = new Vector2(64, 24);
        T(rt, label, 0, 0, 64, 24, 7.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        return btn;
    }

    void HLine(RectTransform p, float y)
    {
        var go = new GameObject("L"); go.transform.SetParent(p, false);
        go.AddComponent<Image>().color = new Color(.20f, .35f, .60f, .35f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(8, y - 1); rt.offsetMax = new Vector2(-8, y);
    }

    void LegRow(RectTransform p, string label, Color col, float y)
    {
        var dot = new GameObject("LD"); dot.transform.SetParent(p, false);
        dot.AddComponent<Image>().color = col;
        var dRT = dot.GetComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(0f, 1f);
        dRT.pivot = new Vector2(0f, 1f);
        dRT.anchoredPosition = new Vector2(16, y + 2);
        dRT.sizeDelta = new Vector2(12, 12);
        
        T(p, label, 46, y, 150, 14, 9, FontStyles.Normal, WHITE, TextAlignmentOptions.Left);
    }
}
