using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TheoryPanel : MonoBehaviour
{
    public static TheoryPanel Instance { get; private set; }

    static readonly Color BG       = new Color(.03f,.05f,.13f,.98f);
    static readonly Color BG_CARD  = new Color(.07f,.11f,.24f,1f);
    static readonly Color BG_TAB   = new Color(.10f,.16f,.32f,1f);
    static readonly Color BG_SEL   = new Color(.05f,.28f,.55f,1f);
    static readonly Color CYAN     = new Color(.00f,.85f,1f);
    static readonly Color WHITE    = new Color(.92f,.96f,1f);
    static readonly Color DIM      = new Color(.50f,.65f,.85f);
    static readonly Color YELLOW   = new Color(1f,.85f,.20f);
    static readonly Color GREEN    = new Color(.00f,.92f,.55f);
    static readonly Color BLUE     = new Color(.40f,.72f,1f);

    static readonly string[][] CONTENT = {
        // BFS
        new[]{
            "BFS — Breadth-First Search",
            "Definition",
            "BFS explores all neighbours of a node before moving to nodes at the next depth level. " +
            "It uses a Queue (FIFO) to process nodes in the order they are discovered.",
            "Complexity",
            "Time:   O(V + E)   where V = vertices, E = edges\n" +
            "Space:  O(V)       for the visited set and queue",
            "Guarantees",
            "~  Finds the path with the fewest hops (minimum number of wilayas)\n" +
            "x  Does NOT guarantee the shortest distance in km\n" +
            "x  Ignores edge weights entirely",
            "When to use",
            "When all edge costs are equal, or when you want the minimum number of\n" +
            "intermediate stops regardless of distance.",
            "In this simulator",
            "BFS visits wilayas level by level from the start. The path shown has\n" +
            "the fewest wilaya transitions, but may not be the shortest road route."
        },
        // Dijkstra
        new[]{
            "Dijkstra's Algorithm",
            "Definition",
            "Dijkstra finds the shortest path in a weighted graph by always expanding\n" +
            "the unvisited node with the smallest known cumulative distance. It uses\n" +
            "a priority queue (min-heap concept).",
            "Complexity",
            "Time:   O((V + E) log V)   with a binary heap\n" +
            "Space:  O(V)               for distance and parent tables",
            "Guarantees",
            "~  Finds the optimal (shortest distance) path\n" +
            "~  Works correctly with any non-negative edge weights\n" +
            "x  Explores in all directions — no sense of goal direction",
            "When to use",
            "When edge weights vary (different road distances) and you need the\n" +
            "provably shortest route.",
            "In this simulator",
            "Dijkstra uses real road distances (km). It typically visits more nodes\n" +
            "than A* but guarantees the minimum-distance path between two wilayas."
        },
        // A*
        new[]{
            "A* Search",
            "Definition",
            "A* extends Dijkstra by adding a heuristic h(n): an estimate of the\n" +
            "remaining cost to the goal. It prioritises nodes by f(n) = g(n) + h(n)\n" +
            "where g(n) is the true cost from start and h(n) is the heuristic.",
            "Complexity",
            "Time:   O(E)   in best case with perfect heuristic\n" +
            "Space:  O(V)   for open/closed sets\n" +
            "Depends heavily on heuristic quality",
            "Guarantees",
            "~  Optimal if heuristic is admissible (never overestimates)\n" +
            "~  Much faster than Dijkstra in practice\n" +
            "~  Focuses search toward the goal",
            "Heuristic used",
            "h(n) = Haversine distance(n, goal) × 111 km/degree\n" +
            "This is admissible because road distance ≥ straight-line distance.",
            "In this simulator",
            "A* visits significantly fewer nodes than BFS or Dijkstra by using\n" +
            "geographic distance as a guide. It is the preferred algorithm for\n" +
            "real navigation systems (GPS, Google Maps)."
        }
    };

    static readonly Color[] ALGO_COLORS = {
        new Color(1f,.85f,.20f),
        new Color(.00f,.92f,.55f),
        new Color(.40f,.72f,1f)
    };

    GameObject panel;
    int        selectedTab = 0;
    Button[]   tabs        = new Button[3];
    RectTransform contentRT;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Build();
    }

    void Build()
    {
        // Dedicated canvas with sortingOrder=50 so it renders above map
        var cvGO = new GameObject("TheoryCanvas");
        var cv   = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 120;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // Full-screen overlay
        panel = new GameObject("TheoryPanel"); panel.transform.SetParent(cvGO.transform, false);
        panel.AddComponent<Image>().color = BG;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        panel.SetActive(false);

        var pRT = rt;

        // Title
        T(pRT, "THEORETICAL BACKGROUND", 0, -22, 1280, 22, 14,
            FontStyles.Bold, CYAN, TextAlignmentOptions.Center);
        T(pRT, "How BFS, Dijkstra and A* work — applied to the Algeria map",
            0, -42, 1280, 16, 9, FontStyles.Normal, DIM, TextAlignmentOptions.Center);

        // Tab bar
        string[] tnames = { "BFS", "Dijkstra", "A*" };
        for (int i = 0; i < 3; i++)
        {
            int ii = i;
            var tab = MakeCard(pRT, -540 + i * 180, -72, 170, 32,
                i == 0 ? BG_SEL : BG_TAB);
            T(tab, tnames[i], 0, -8, 170, 22, 11, FontStyles.Bold,
                ALGO_COLORS[i], TextAlignmentOptions.Center);
            tabs[i] = tab.gameObject.AddComponent<Button>();
            tabs[i].targetGraphic = tab.GetComponent<Image>();
            var cb = tabs[i].colors;
            cb.highlightedColor = Color.Lerp(BG_TAB, Color.white, .15f);
            tabs[i].colors = cb;
            tabs[i].onClick.AddListener(() => SelectTab(ii));
        }

        // Content area
        // Content area — stretches to fill screen below tabs
        var contentCard = new GameObject("ContentCard");
        contentCard.transform.SetParent(pRT, false);
        contentCard.AddComponent<Image>().color = BG_CARD;
        var ccRT = contentCard.GetComponent<RectTransform>();
        ccRT.anchorMin = new Vector2(0,0); ccRT.anchorMax = new Vector2(1,1);
        ccRT.offsetMin = new Vector2(8, 8); ccRT.offsetMax = new Vector2(-8, -108);
        contentRT = ccRT;

        // Close button
        var closeCard = MakeCard(pRT, 0, -18, 100, 26, new Color(.3f,.1f,.1f));
        closeCard.anchorMin = new Vector2(1,1); closeCard.anchorMax = new Vector2(1,1);
        closeCard.anchoredPosition = new Vector2(-62,-18);
        T(closeCard, "Close", 0, -4, 100, 22, 9, FontStyles.Bold, WHITE, TextAlignmentOptions.Center);
        var closeBtn = closeCard.gameObject.AddComponent<Button>();
        closeBtn.targetGraphic = closeCard.GetComponent<Image>();
        closeBtn.onClick.AddListener(Hide);

        PopulateContent(0);
    }

    void SelectTab(int idx)
    {
        selectedTab = idx;
        for (int i = 0; i < 3; i++)
            tabs[i].GetComponent<Image>().color = i == idx ? BG_SEL : BG_TAB;
        PopulateContent(idx);
    }

    void PopulateContent(int idx)
    {
        foreach (Transform child in contentRT) Destroy(child.gameObject);

        var data  = CONTENT[idx];
        Color col = ALGO_COLORS[idx];
        float y   = -16;
        float cw  = 1140f;

        // Title
        TL(contentRT, data[0], 0, y, cw, 26, 15, FontStyles.Bold, col, TextAlignmentOptions.Center);
        y -= 34;

        // Separator line
        var div = new GameObject("Line"); div.transform.SetParent(contentRT, false);
        div.AddComponent<Image>().color = new Color(.20f,.35f,.60f,.50f);
        var dRT = div.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0,1); dRT.anchorMax = new Vector2(1,1);
        dRT.offsetMin = new Vector2(10, y-1); dRT.offsetMax = new Vector2(-10, y);
        y -= 14;

        // Sections
        for (int s = 1; s < data.Length; s += 2)
        {
            string heading = data[s];
            string body    = s+1 < data.Length ? data[s+1] : "";

            // Heading pill
            var pill = new GameObject("Pill"); pill.transform.SetParent(contentRT, false);
            pill.AddComponent<Image>().color = new Color(col.r*.3f, col.g*.3f, col.b*.3f, .9f);
            var pRT2 = pill.GetComponent<RectTransform>();
            pRT2.anchorMin = pRT2.anchorMax = new Vector2(0,1);
            pRT2.pivot = new Vector2(0,1);
            pRT2.anchoredPosition = new Vector2(12, y); pRT2.sizeDelta = new Vector2(150, 18);
            TL(pRT2, heading.ToUpper(), 0, -1, 150, 16, 7.5f, FontStyles.Bold, col, TextAlignmentOptions.Center);
            y -= 24;

            // Body
            int lines  = body.Split('\n').Length;
            float bh   = lines * 16 + 8;
            var bt = TL(contentRT, body, 16, y, cw-32, bh, 10, FontStyles.Normal, WHITE, TextAlignmentOptions.TopLeft);
            bt.enableWordWrapping = true;
            y -= bh + 12;
        }

        // Summary box for A*
        if (idx == 2)
        {
            y -= 8;
            var box = new GameObject("Box"); box.transform.SetParent(contentRT, false);
            box.AddComponent<Image>().color = new Color(.05f,.12f,.25f);
            var bRT = box.GetComponent<RectTransform>();
            bRT.anchorMin = bRT.anchorMax = new Vector2(0,1);
            bRT.pivot = new Vector2(0,1);
            bRT.anchoredPosition = new Vector2(12, y); bRT.sizeDelta = new Vector2(cw-24, 50);
            TL(bRT, "Summary: A* ≥ Dijkstra ≥ BFS for path quality.\nA* and Dijkstra are both optimal. A* is faster. BFS ignores weights.",
                8, -6, cw-40, 42, 9, FontStyles.Normal, new Color(.75f,.90f,.70f), TextAlignmentOptions.Center);
        }
    }

    // Top-left anchored text
    TextMeshProUGUI TL(RectTransform parent, string text,
        float x, float y, float w, float h, float size,
        FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(parent, false);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = col; t.alignment = align;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0,1);
        rt.pivot = new Vector2(0,1);
        rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(w,h);
        return t;
    }

    // ── public 
    public void Show() { panel.SetActive(true); }
    public void Hide() { panel.SetActive(false); }
    public void Toggle() { panel.SetActive(!panel.activeSelf); }

    // ── helpers
    RectTransform MakeCard(RectTransform parent, float x, float y, float w, float h, Color col)
    {
        var go = new GameObject("C"); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f,1f);
        rt.pivot = new Vector2(.5f,1f);
        rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(w,h);
        return rt;
    }

    TextMeshProUGUI T(RectTransform parent, string text,
        float x, float y, float w, float h, float size,
        FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(parent, false);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = col; t.alignment = align; t.enableWordWrapping = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f,1f);
        rt.pivot = new Vector2(.5f,1f);
        rt.anchoredPosition = new Vector2(x,y); rt.sizeDelta = new Vector2(w,h);
        return t;
    }

    void MakeLine(RectTransform parent, float x1, float y1, float x2, float y2, Color col)
    {
        var go = new GameObject("L"); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f,1f);
        rt.pivot = new Vector2(.5f,.5f);
        rt.anchoredPosition = new Vector2((x1+x2)/2f, y1);
        rt.sizeDelta = new Vector2(x2-x1, 1f);
    }
}
