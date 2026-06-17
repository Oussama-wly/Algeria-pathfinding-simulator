using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Road Network Puzzle: user clicks wilayas to build a path manually,
/// then compares with Dijkstra's optimal result.
/// </summary>
public class PathPuzzle : MonoBehaviour
{
    public static PathPuzzle Instance { get; private set; }

    // ── Colors 
    static readonly Color C_USER = new Color(1f, .70f, .10f);   // gold
    static readonly Color C_OPTIMAL = new Color(.00f, .90f, .55f); // green
    static readonly Color C_START = new Color(.00f, .90f, .55f);
    static readonly Color C_GOAL = new Color(1f, .20f, .35f);
    static readonly Color C_DEFAULT = new Color(.10f, .22f, .40f);
    static readonly Color CYAN = new Color(.00f, .82f, .95f);
    static readonly Color WHITE = new Color(.92f, .96f, 1f);
    static readonly Color DIM = new Color(.40f, .58f, .80f);
    static readonly Color YELLOW = new Color(1f, .85f, .15f);
    static readonly Color BG_PANEL = new Color(.05f, .09f, .20f, .97f);

    // ── State 
    enum PuzzleState { Idle, SelectingStart, Drawing, Done }
    PuzzleState state = PuzzleState.Idle;

    int startCode = -1, goalCode = -1;
    List<int> userPath = new List<int>();
    List<int> optimalPath = new List<int>();
    float userDist, optimalDist;

    // UI
    Canvas puzzleCanvas;
    GameObject panel;
    TextMeshProUGUI lblInstruction, lblUserDist, lblOptDist, lblDiff, lblStatus;
    Button btnStartPuzzle, btnReset, btnReveal, btnClose;
    GameObject resultCard;

    // ── Open / Close 
    public static void Open()
    {
        if (Instance != null) return;
        new GameObject("PathPuzzle").AddComponent<PathPuzzle>();
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
        BlockedRoads.Close();
        SearchTreePanel.Instance?.ForceClose();
        BuildUI();
    }

    void OnDestroy()
    {
        Instance = null;
        ResetMapColors();
        // Re-enable normal controller clicks
        SimulatorController.Instance?.SetPuzzleMode(false);
    }

    // ── UI
    void BuildUI()
    {
        var cvGO = new GameObject("PuzzleCanvas");
        puzzleCanvas = cvGO.AddComponent<Canvas>();
        puzzleCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        puzzleCanvas.sortingOrder = 70;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // Side panel — right side
        panel = new GameObject("PuzzlePanel");
        panel.transform.SetParent(cvGO.transform, false);
        panel.AddComponent<Image>().color = BG_PANEL;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1, 0); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(-220, 0); rt.offsetMax = Vector2.zero;

        float y = -16;

        T(rt, "ROAD PUZZLE", 0, y, 220, 20, 13, FontStyles.Bold, CYAN, TextAlignmentOptions.Center); y -= 24;
        T(rt, "Can you beat Dijkstra?", 0, y, 220, 16, 8, FontStyles.Normal, DIM, TextAlignmentOptions.Center); y -= 22;

        HLine(rt, y); y -= 14;

        // Instruction
        lblInstruction = T(rt, "Press Start\nto begin", 0, y, 200, 44, 9,
            FontStyles.Normal, WHITE, TextAlignmentOptions.Center);
        lblInstruction.enableWordWrapping = true; y -= 52;

        HLine(rt, y); y -= 14;

        // Score area
        T(rt, "YOUR PATH", 0, y, 220, 14, 8, FontStyles.Bold, YELLOW, TextAlignmentOptions.Center); y -= 18;
        lblUserDist = T(rt, "— km", 0, y, 220, 22, 14, FontStyles.Bold, YELLOW, TextAlignmentOptions.Center); y -= 26;

        T(rt, "DIJKSTRA", 0, y, 220, 14, 8, FontStyles.Bold, C_OPTIMAL, TextAlignmentOptions.Center); y -= 18;
        lblOptDist = T(rt, "— km", 0, y, 220, 22, 14, FontStyles.Bold, C_OPTIMAL, TextAlignmentOptions.Center); y -= 26;

        HLine(rt, y); y -= 12;
        lblDiff = T(rt, "", 0, y, 200, 40, 10, FontStyles.Bold, WHITE, TextAlignmentOptions.Center);
        lblDiff.enableWordWrapping = true; y -= 48;

        // Status
        lblStatus = T(rt, "", 0, y, 200, 36, 8, FontStyles.Normal, DIM, TextAlignmentOptions.Center);
        lblStatus.enableWordWrapping = true; y -= 44;

        // Buttons
        btnStartPuzzle = Btn(rt, "Start Game", 0, y, 190, 36, new Color(.05f, .35f, .18f), 12); y -= 44;
        btnReveal = Btn(rt, "Show Optimal", 0, y, 190, 28, new Color(.08f, .20f, .42f), 10); y -= 34;
        btnReset = Btn(rt, "Reset", 0, y, 190, 26, new Color(.22f, .12f, .04f), 10); y -= 32;
        btnClose = Btn(rt, "Close", 0, y, 190, 24, new Color(.32f, .08f, .08f), 9); y -= 32;
        HLine(rt, y); y -= 14;

        btnReveal.interactable = false;
        btnReset.interactable = false;

        btnStartPuzzle.onClick.AddListener(StartPuzzle);
        btnReveal.onClick.AddListener(RevealOptimal);
        btnReset.onClick.AddListener(ResetPuzzle);
        btnClose.onClick.AddListener(() => {
            Destroy(puzzleCanvas.gameObject);
            Destroy(gameObject);
        });

        // Legend
        float ly = y - 16;
        HLine(rt, ly); ly -= 12;
        // Header widget
        var lhGO = new GameObject("LegHdr"); lhGO.transform.SetParent(rt, false);
        lhGO.AddComponent<Image>().color = new Color(.05f, .10f, .24f);
        var lhRT = lhGO.GetComponent<RectTransform>();
        lhRT.anchorMin = new Vector2(0f, 1f); lhRT.anchorMax = new Vector2(1f, 1f);
        lhRT.pivot = new Vector2(.5f, 1f);
        lhRT.anchoredPosition = new Vector2(0, ly); lhRT.sizeDelta = new Vector2(0, 20);
        T(lhRT, "COLOR LEGEND", 0, -4, 220, 16, 8, FontStyles.Bold, DIM, TextAlignmentOptions.Center);
        ly -= 24f;
        LegRow(rt, "Your path", C_USER, ly); ly -= 20;
        LegRow(rt, "Optimal path", C_OPTIMAL, ly); ly -= 20;
        LegRow(rt, "Start", C_START, ly); ly -= 20;
        LegRow(rt, "Goal", C_GOAL, ly);
    }

    // ── Puzzle logic
    void StartPuzzle()
    {
        ResetPuzzle();

        // Pick random start and goal (far apart)
        var codes = new List<int>(GraphMap.Instance.Nodes.Keys);
        startCode = codes[Random.Range(0, codes.Count)];
        do { goalCode = codes[Random.Range(0, codes.Count)]; }
        while (goalCode == startCode ||
               Vector3.Distance(
                   GraphMap.Instance.Nodes[startCode].GameObject.transform.position,
                   GraphMap.Instance.Nodes[goalCode].GameObject.transform.position) < 4f);

        // Color start/goal
        GraphMap.Instance.SetColor(startCode, C_START);
        GraphMap.Instance.SetColor(goalCode, C_GOAL);
        GraphMap.Instance.ShowName(startCode, true);
        GraphMap.Instance.ShowName(goalCode, true);

        // Add start to user path
        userPath.Clear();
        userPath.Add(startCode);
        userDist = 0;

        state = PuzzleState.Drawing;
        SimulatorController.Instance?.SetPuzzleMode(true);

        string sName = GraphMap.Instance.Nodes[startCode].Name;
        string gName = GraphMap.Instance.Nodes[goalCode].Name;
        lblInstruction.text = $"From: {sName}\nTo: {gName}\n\nClick adjacent\nwilayas to draw\nyour path!";
        lblStatus.text = $"Nodes in path: 1\nCurrent: {sName}";
        lblUserDist.text = "0 km";

        btnStartPuzzle.interactable = false;
        btnReset.interactable = true;
        btnReveal.interactable = true;
    }

    public void OnWilayaClicked(int code)
    {
        if (state != PuzzleState.Drawing) return;

        int last = userPath[userPath.Count - 1];

        // Check if neighbor
        var nd = GraphMap.Instance.Nodes[last];
        float edgeDist = -1;
        foreach (var (nb, dist) in nd.Neighbors)
            if (nb == code) { edgeDist = dist; break; }

        if (edgeDist < 0)
        {
            lblStatus.text = $"Not adjacent!\nMust click a\nneighboring wilaya.";
            return;
        }

        // Check if already in path (no loops)
        if (userPath.Contains(code) && code != goalCode)
        {
            lblStatus.text = "Already visited!\nNo loops allowed.";
            return;
        }

        // Add to path
        userPath.Add(code);
        userDist += edgeDist;

        // Draw line
        GraphMap.Instance.AddColoredLine(last, code, C_USER, 0.18f);
        if (code != startCode && code != goalCode)
            GraphMap.Instance.SetColor(code, C_USER);

        lblUserDist.text = $"{userDist:0} km";
        lblStatus.text = $"Nodes: {userPath.Count}\nLast: {GraphMap.Instance.Nodes[code].Name}";

        // Reached goal?
        if (code == goalCode)
        {
            state = PuzzleState.Done;
            SimulatorController.Instance?.SetPuzzleMode(false);
            StartCoroutine(EvaluatePath());
        }
    }

    IEnumerator EvaluatePath()
    {
        lblInstruction.text = "Calculating\noptimal path...";

        // Run Dijkstra silently
        PathResult result = null;
        yield return Algorithms.Dijkstra(GraphMap.Instance.Nodes,
            startCode, goalCode, 0f, () => false, r => result = r, null);

        if (result != null && result.Found)
        {
            optimalPath = result.Path;
            optimalDist = result.Dist;
            lblOptDist.text = $"{optimalDist:0} km";

            float diff = userDist - optimalDist;
            float pct = (diff / optimalDist) * 100f;

            if (diff <= 0)
            {
                lblDiff.text = "PERFECT!\nYou matched\nDijkstra!";
                lblDiff.color = C_OPTIMAL;
            }
            else if (pct < 10)
            {
                lblDiff.text = $"+{diff:0} km\n({pct:0.0}% over)\nAlmost perfect!";
                lblDiff.color = YELLOW;
            }
            else if (pct < 30)
            {
                lblDiff.text = $"+{diff:0} km\n({pct:0.0}% over)\nNot bad!";
                lblDiff.color = YELLOW;
            }
            else
            {
                lblDiff.text = $"+{diff:0} km\n({pct:0.0}% over)\nDijkstra wins!";
                lblDiff.color = new Color(1f, .4f, .4f);
            }

            lblInstruction.text = "Done! Press\n'Show Optimal'\nto see best path.";
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

    void ResetPuzzle()
    {
        state = PuzzleState.Idle;
        SimulatorController.Instance?.SetPuzzleMode(false);
        ResetMapColors();

        userPath.Clear(); optimalPath.Clear();
        userDist = 0; optimalDist = 0;
        startCode = -1; goalCode = -1;

        lblInstruction.text = "Press Start\nto begin";
        lblUserDist.text = "— km"; lblOptDist.text = "— km";
        lblDiff.text = ""; lblStatus.text = "";

        btnStartPuzzle.interactable = true;
        btnReveal.interactable = false;
        btnReset.interactable = false;
    }

    void ResetMapColors()
    {
        if (GraphMap.Instance == null) return;
        GraphMap.Instance.ClearPathLines();
        GraphMap.Instance.ResetColors();
    }

    // ── Helpers
    TextMeshProUGUI T(RectTransform p, string text, float x, float y,
        float w, float h, float size, FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(p, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style; t.color = col; t.alignment = align;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h); return t;
    }

    Button Btn(RectTransform p, string label, float x, float y, float w, float h, Color col, float fs)
    {
        var go = new GameObject("B"); go.transform.SetParent(p, false);
        go.AddComponent<Image>().color = col;
        var btn = go.AddComponent<Button>(); btn.targetGraphic = go.GetComponent<Image>();
        var cb = btn.colors; cb.highlightedColor = Color.Lerp(col, Color.white, .2f);
        cb.disabledColor = new Color(col.r * .3f, col.g * .3f, col.b * .3f, .5f);
        btn.colors = cb;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        T(rt, label, 0, -h / 2f + fs * .65f, w, fs * 1.8f, fs, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        return btn;
    }

    void HLine(RectTransform p, float y)
    {
        var go = new GameObject("L"); go.transform.SetParent(p, false);
        go.AddComponent<Image>().color = new Color(.20f, .35f, .60f, .40f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(8, y - 1); rt.offsetMax = new Vector2(-8, y);
    }

    void LegRow(RectTransform p, string label, Color col, float y)
    {
        var dot = new GameObject("D"); dot.transform.SetParent(p, false);
        dot.AddComponent<Image>().color = col;
        var dRT = dot.GetComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(0f, 1f);
        dRT.pivot = new Vector2(0f, 1f);
        dRT.anchoredPosition = new Vector2(16, y + 2);
        dRT.sizeDelta = new Vector2(12, 12);
        T(p, label, 46, y, 150, 14, 9, FontStyles.Normal, WHITE, TextAlignmentOptions.Left);
    }
}
