using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulatorUI : MonoBehaviour
{
    public static SimulatorUI Instance { get; private set; }

    public TextMeshProUGUI LabelStart { get; private set; }
    public TextMeshProUGUI LabelGoal { get; private set; }
    public TextMeshProUGUI LabelMessage { get; private set; }
    public TextMeshProUGUI LabelStep { get; private set; }
    public Button BtnRun { get; private set; }
    public Button BtnStop { get; private set; }
    public Button BtnReset { get; private set; }
    public Button BtnCompare { get; private set; }
    public Button BtnReplay { get; private set; }
    public Button BtnHistory { get; private set; }
    public Button BtnTheory { get; private set; }
    public Button BtnHistoryClose { get; private set; }
    public Button BtnClearHistory { get; private set; }
    public Button[] AlgoBtns { get; private set; } = new Button[3];
    public Slider SpeedSlider { get; private set; }

    TextMeshProUGUI rAlgo, rDist, rVisited, rAlgoTime, rTravel, rPath, rCompareProgress;
    GameObject statsPanel, historyPanel, compareProgressGO;
    //  Progress bar
    Image progressBarFill;
    TextMeshProUGUI lblProgressPct;

    // ── Audio
    AudioSource sfx;
    AudioClip clipClick, clipOpen, clipClose, clipRun, clipReset;

    void InitAudio()
    {
        sfx = gameObject.AddComponent<AudioSource>();
        sfx.playOnAwake = false;
        sfx.volume = 0.35f;

        // Generate sounds procedurally — no external files needed
        clipClick = MakeTone(0.06f, 880f, 0.04f);   // short hi click
        clipOpen = MakeTone(0.12f, 660f, 0.08f);   // open panel
        clipClose = MakeTone(0.10f, 440f, 0.06f);   // close / dismiss
        clipRun = MakeChord(0.18f, new[] { 440f, 554f, 659f }, 0.10f); // run — chord
        clipReset = MakeTone(0.14f, 330f, 0.10f);   // low reset
    }

    void Play(AudioClip c) { if (sfx && c) sfx.PlayOneShot(c); }

    // Sine-wave tone generator
    static AudioClip MakeTone(float dur, float freq, float decay)
    {
        int sr = 44100; int n = (int)(sr * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float env = Mathf.Exp(-t / decay);
            d[i] = Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.6f;
        }
        var clip = AudioClip.Create("tone", n, 1, sr, false);
        clip.SetData(d, 0); return clip;
    }

    // Chord generator (multiple freqs)
    static AudioClip MakeChord(float dur, float[] freqs, float decay)
    {
        int sr = 44100; int n = (int)(sr * dur);
        var d = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / sr;
            float env = Mathf.Exp(-t / decay);
            float s = 0;
            foreach (var f in freqs) s += Mathf.Sin(2 * Mathf.PI * f * t);
            d[i] = s / freqs.Length * env * 0.5f;
        }
        var clip = AudioClip.Create("chord", n, 1, sr, false);
        clip.SetData(d, 0); return clip;
    }
    RectTransform historyContent;
    GameObject leftPanelGO, rightPanelGO;
    bool panelsVisible = true;

    // ── Colors
    static readonly Color BG_MAIN = new Color(.06f, .10f, .20f, .97f);
    static readonly Color BG_CARD = new Color(.08f, .13f, .26f, 1f);
    static readonly Color LINE = new Color(.20f, .35f, .60f, .50f);
    static readonly Color DIM = new Color(.45f, .60f, .82f);
    static readonly Color TXT = new Color(.92f, .96f, 1f);
    static readonly Color CYAN = new Color(.00f, .80f, .95f);
    static readonly Color GREEN = new Color(.00f, .85f, .50f);
    static readonly Color RED = new Color(1f, .22f, .35f);
    static readonly Color YELLOW = new Color(1f, .82f, .15f);
    static readonly Color BLUE = new Color(.35f, .68f, 1f);
    static readonly Color ORANGE = new Color(1f, .55f, .15f);

    void Awake() { Instance = this; Build(); }

    void Build()
    {
        InitAudio();
        var go = new GameObject("Canvas");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        var sc = go.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        sc.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        BuildLeft(go.transform);
        BuildRight(go.transform);
        BuildTopBar(go.transform);

        AddQuitButton(go.transform);
    }

    void AddQuitButton(Transform root)
    {
        var go = new GameObject("BtnQuit");
        go.transform.SetParent(root, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(.72f, .14f, .14f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.highlightedColor = new Color(.90f, .20f, .20f);
        cb.pressedColor = new Color(.50f, .08f, .08f);
        cb.fadeDuration = 0.06f;
        btn.colors = cb;
        btn.onClick.AddListener(() => Application.Quit());
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(1, 1);
        rt.anchoredPosition = new Vector2(-4, -4);
        rt.sizeDelta = new Vector2(22, 16);
        var lGO = new GameObject("L"); lGO.transform.SetParent(go.transform, false);
        var lbl = lGO.AddComponent<TextMeshProUGUI>();
        lbl.text = "×"; lbl.fontSize = 10f; lbl.fontStyle = FontStyles.Bold;
        lbl.color = Color.white; lbl.alignment = TextAlignmentOptions.Center;
        lbl.raycastTarget = false;
        var lRT = lGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = lRT.offsetMax = Vector2.zero;
    }

    // ── TOP BAR (toggle + theme) 
    bool sideMenuOpen = false;
    bool mapVisible = false;  // map hidden by default
    GameObject sideMenuPanel;

    void BuildTopBar(Transform root)
    {
        // Single "☰ Menu" button top-left
        var mGO = new GameObject("MenuBtn"); mGO.transform.SetParent(root, false);
        var mImg = mGO.AddComponent<Image>(); mImg.color = new Color(.06f, .12f, .26f, .95f);
        var mBtn = mGO.AddComponent<Button>(); mBtn.targetGraphic = mImg;
        var mcb = mBtn.colors;
        mcb.highlightedColor = new Color(.10f, .22f, .42f); mBtn.colors = mcb;
        var mRT = mGO.GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0, 1); mRT.anchorMax = new Vector2(0, 1);
        mRT.pivot = new Vector2(0, 1);
        mRT.anchoredPosition = new Vector2(4, -4); mRT.sizeDelta = new Vector2(68, 24);
        MakeTxtGO(mGO.transform, "Menu", 9, FontStyles.Bold, CYAN);
        mBtn.onClick.AddListener(ToggleSideMenu);

        // Side menu dropdown panel
        sideMenuPanel = new GameObject("SideMenu"); sideMenuPanel.transform.SetParent(root, false);
        sideMenuPanel.AddComponent<Image>().color = new Color(.04f, .09f, .20f, .97f);
        var smRT = sideMenuPanel.GetComponent<RectTransform>();
        smRT.anchorMin = new Vector2(0, 1); smRT.anchorMax = new Vector2(0, 1);
        smRT.pivot = new Vector2(0, 1);
        smRT.anchoredPosition = new Vector2(4, -30); smRT.sizeDelta = new Vector2(200, 262);
        sideMenuPanel.AddComponent<Outline>().effectColor = new Color(.20f, .40f, .70f, .50f);

        float sy = -10;
        SideMenuBtn(smRT, "Simulation", sy, new Color(.00f, .82f, .95f), () => { CloseSideMenu(); }); sy -= 36;
        SideMenuBtn(smRT, "Road Network Puzzle", sy, new Color(1f, .60f, .15f), () => { CloseSideMenu(); SearchTreePanel.Instance?.ForceClose(); PathPuzzle.Open(); }); sy -= 36;
        SideMenuBtn(smRT, "Blocked Roads", sy, new Color(.90f, .18f, .22f), () => { CloseSideMenu(); SearchTreePanel.Instance?.ForceClose(); BlockedRoads.Open(); }); sy -= 36;
        SideMenuBtn(smRT, "Search Tree", sy, new Color(.40f, .72f, 1f), () => { CloseSideMenu(); PathPuzzle.Close(); BlockedRoads.Close(); SearchTreePanel.Toggle(); }); sy -= 36;
        SideMenuBtn(smRT, "Theory", sy, new Color(.55f, .68f, .92f), () => { CloseSideMenu(); SearchTreePanel.Instance?.ForceClose(); TheoryPanel.Instance?.Show(); }); sy -= 36;
        SideMenuBtn(smRT, "Hide Panels", sy, new Color(.40f, .50f, .70f), () => { TogglePanels(); CloseSideMenu(); }); sy -= 36;
        SideMenuBtn(smRT, "Main Menu", sy, new Color(.80f, .25f, .25f), () => { CloseSideMenu(); new GameObject("MenuScreen").AddComponent<MenuScreen>(); });

        sideMenuPanel.SetActive(false);
    }

    void SideMenuBtn(RectTransform parent, string label, float y, Color accent, System.Action onClick)
    {
        var go = new GameObject("SBtn"); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = new Color(.07f, .12f, .26f);
        var btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var cb = btn.colors; cb.highlightedColor = new Color(.14f, .22f, .42f); cb.selectedColor = new Color(.10f, .18f, .35f); btn.colors = cb;
        btn.onClick.AddListener(() => onClick());
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, 1f); rt.pivot = new Vector2(.5f, 1f);
        rt.anchoredPosition = new Vector2(0, y); rt.sizeDelta = new Vector2(188, 30);
        // Left accent strip
        var ac = new GameObject("Ac"); ac.transform.SetParent(go.transform, false);
        ac.AddComponent<Image>().color = accent;
        var acRT = ac.GetComponent<RectTransform>();
        acRT.anchorMin = Vector2.zero; acRT.anchorMax = new Vector2(0, 1);
        acRT.offsetMin = Vector2.zero; acRT.offsetMax = new Vector2(3, 0);
        // Label
        var lGO = new GameObject("L"); lGO.transform.SetParent(go.transform, false);
        var lTxt = lGO.AddComponent<TextMeshProUGUI>();
        lTxt.text = label; lTxt.fontSize = 9; lTxt.fontStyle = FontStyles.Bold;
        lTxt.color = Color.Lerp(accent, Color.white, .5f); lTxt.alignment = TextAlignmentOptions.Left;
        var lRT = lGO.GetComponent<RectTransform>();
        lRT.anchorMin = Vector2.zero; lRT.anchorMax = Vector2.one;
        lRT.offsetMin = new Vector2(10, 0); lRT.offsetMax = new Vector2(-4, 0);
    }

    void ToggleSideMenu()
    {
        sideMenuOpen = !sideMenuOpen;
        sideMenuPanel.SetActive(sideMenuOpen);
    }

    void CloseSideMenu()
    {
        sideMenuOpen = false;
        sideMenuPanel?.SetActive(false);
    }

    void TogglePanels()
    {
        panelsVisible = !panelsVisible;
        leftPanelGO?.SetActive(panelsVisible);
        rightPanelGO?.SetActive(panelsVisible);
    }



    // ── LEFT PANEL
    void BuildLeft(Transform root)
    {
        var p = Panel(root, "Left",
            new Vector2(0, 0), new Vector2(0, 1),
            Vector2.zero, new Vector2(200, 0), BG_MAIN);
        leftPanelGO = p.gameObject;

        float y = -16;

        Lbl(p, "ALGORITHM", 0, y, 200, 18, 9, DIM, TextAlignmentOptions.Center); y -= 22;
        AlgoCard(p, ref y, 0, "BFS", "Least hops", new Color(.10f, .18f, .36f));
        AlgoCard(p, ref y, 1, "Dijkstra", "Shortest path", new Color(.10f, .18f, .36f));
        AlgoCard(p, ref y, 2, "A*", "Smartest", new Color(.05f, .22f, .48f));

        HLine(p, y); y -= 14;

        Lbl(p, "Slow", 12, y, 50, 14, 7, DIM, TextAlignmentOptions.Left);
        Lbl(p, "SPEED", 0, y, 200, 14, 7, DIM, TextAlignmentOptions.Center);
        Lbl(p, "Fast", 140, y, 50, 14, 7, DIM, TextAlignmentOptions.Right);
        y -= 16;
        SpeedSlider = MakeSlider(p, 12, y, 176, 18); y -= 28;

        HLine(p, y); y -= 14;

        BtnRun = BigBtn(p, "Run", 12, y, 176, 36, new Color(.06f, .42f, .18f), 13); y -= 44;
        BtnCompare = BigBtn(p, "Compare", 12, y, 176, 28, new Color(.28f, .18f, .05f), 11); y -= 34;
        BtnReplay = BigBtn(p, "Replay", 12, y, 84, 26, new Color(.10f, .20f, .38f), 10);
        BtnReset = BigBtn(p, "Reset", 100, y, 88, 26, new Color(.22f, .10f, .08f), 10); y -= 34;

        // Map toggle button — default OFF
        var btnMap = BigBtn(p, "Map (off)", 12, y, 176, 26, new Color(.06f, .18f, .32f), 10);
        btnMap.onClick.AddListener(() => {
            mapVisible = !mapVisible;
            // FindObjectOfType works on inactive objects too
            var mapBG = Resources.FindObjectsOfTypeAll<SpriteRenderer>();
            foreach (var sr in mapBG)
                if (sr.gameObject.name == "AlgeriaMapBG")
                    sr.gameObject.SetActive(mapVisible);
            var lbl = btnMap.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null) lbl.text = mapVisible ? "Map" : "Map (off)";
        });
        y -= 34;

        // Screenshot button
        var btnShot = BigBtn(p, "Screenshot", 12, y, 176, 24, new Color(.08f, .16f, .28f), 9);
        btnShot.onClick.AddListener(() => StartCoroutine(TakeScreenshot()));
        y -= 30;

        // Stop — placed where STEP was (visible only while running)
        BtnStop = BigBtn(p, "Stop", 12, y, 176, 36, new Color(.46f, .08f, .12f), 13);
        BtnRun.interactable = false;
        BtnCompare.interactable = false;
        BtnReplay.interactable = false;
        BtnStop.gameObject.SetActive(false);
        y -= 44;

        HLine(p, y); y -= 12;

        // Status
        Lbl(p, "STATUS", 0, y, 200, 14, 7, DIM, TextAlignmentOptions.Center); y -= 16;
        var msgCard = Card(p, 12, y, 176, 48, new Color(.05f, .09f, .18f));
        LabelMessage = Lbl(msgCard, "Click a wilaya to set Start.", 8, -6, 160, 40, 9,
            new Color(.72f, .85f, .62f), TextAlignmentOptions.Left);
        LabelMessage.enableWordWrapping = true;
        y -= 56;

        HLine(p, y); y -= 12;

        // Legend
        Lbl(p, "COLOR LEGEND", 0, y, 200, 14, 7, DIM, TextAlignmentOptions.Center); y -= 18;
        string[] ln = { "Start", "Goal", "Visited", "Open", "Path" };
        Color[] lc = { GREEN, RED, new Color(.24f, .55f, 1), new Color(1, .80f, 0), ORANGE };
        for (int i = 0; i < 5; i++) { LegRow(p, ln[i], lc[i], y); y -= 18; }
    }

    void AlgoCard(RectTransform p, ref float y, int idx, string name, string sub, Color col)
    {
        var card = Card(p, 12, y, 176, 46, col);
        card.GetComponent<Image>().sprite = RoundedSprite(176, 46, 8, Color.white);
        card.GetComponent<Image>().type = Image.Type.Sliced;
        card.GetComponent<Image>().color = col;
        AlgoBtns[idx] = card.gameObject.AddComponent<Button>();
        AlgoBtns[idx].targetGraphic = card.GetComponent<Image>();
        var cb = AlgoBtns[idx].colors;
        cb.highlightedColor = Color.Lerp(col, Color.white, .18f);
        AlgoBtns[idx].colors = cb;
        if (idx == 2) { var ol = card.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(.20f, .55f, 1f, .80f); ol.effectDistance = new Vector2(1.5f, 1.5f); }
        Lbl(card, name, 10, -10, 120, 20, 13, TXT, TextAlignmentOptions.Left).fontStyle = FontStyles.Bold;
        Lbl(card, sub, 10, -28, 140, 14, 8, DIM, TextAlignmentOptions.Left);
        y -= 52;
    }

    // ── RIGHT PANEL 
    void BuildRight(Transform root)
    {
        var p = Panel(root, "Right",
            new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(-200, 0), Vector2.zero, BG_MAIN);
        rightPanelGO = p.gameObject;

        float y = -14;

        Lbl(p, "SELECTION", 0, y, 200, 16, 8, DIM, TextAlignmentOptions.Center); y -= 20;

        var sc = Card(p, 12, y, 176, 30, BG_CARD);
        LabelStart = Lbl(sc, "Start: —", 8, -5, 160, 20, 10, GREEN, TextAlignmentOptions.Left);
        LabelStart.fontStyle = FontStyles.Bold; y -= 36;

        var gc = Card(p, 12, y, 176, 30, BG_CARD);
        LabelGoal = Lbl(gc, "Goal:  —", 8, -5, 160, 20, 10, RED, TextAlignmentOptions.Left);
        LabelGoal.fontStyle = FontStyles.Bold; y -= 36;

        HLine(p, y); y -= 8;

        // Compare progress (visible during Compare)
        compareProgressGO = new GameObject("CompProg");
        compareProgressGO.transform.SetParent(p, false);
        var cpRT = compareProgressGO.AddComponent<RectTransform>();
        cpRT.anchorMin = new Vector2(0, 1); cpRT.anchorMax = new Vector2(1, 1);
        cpRT.pivot = new Vector2(0, 1);
        cpRT.anchoredPosition = new Vector2(0, y);
        cpRT.sizeDelta = new Vector2(0, 24);
        rCompareProgress = Lbl(cpRT, "", 0, -4, 200, 18, 9, YELLOW, TextAlignmentOptions.Center);
        compareProgressGO.SetActive(false);

        // Stats
        statsPanel = new GameObject("StatsPanel");
        statsPanel.transform.SetParent(p, false);
        statsPanel.AddComponent<RectTransform>();
        var spRT = statsPanel.GetComponent<RectTransform>();
        spRT.anchorMin = new Vector2(0, 0); spRT.anchorMax = new Vector2(1, 1);
        spRT.offsetMin = new Vector2(0, 0); spRT.offsetMax = new Vector2(0, y);
        statsPanel.SetActive(false);

        var sp = spRT; float ry = -8;

        Lbl(sp, "RESULTS", 0, ry, 200, 16, 8, DIM, TextAlignmentOptions.Center); ry -= 20;
        rAlgo = Lbl(sp, "—", 0, ry, 200, 26, 15, CYAN, TextAlignmentOptions.Center);
        rAlgo.fontStyle = FontStyles.Bold; ry -= 30;
        HLine(sp, ry); ry -= 8;

        ry = StatCard(sp, "Distance", "km", BLUE, ry, out rDist);
        ry = StatCard(sp, "Nodes visited", "", new Color(.35f, .65f, 1f), ry, out rVisited);
        ry = StatCard(sp, "Algorithm time", "ms (pure)", YELLOW, ry, out rAlgoTime);
        ry = StatCard(sp, "Travel @90km/h", "", GREEN, ry, out rTravel);

        HLine(sp, ry); ry -= 8;
        Lbl(sp, "PATH", 0, ry, 200, 14, 8, DIM, TextAlignmentOptions.Center); ry -= 18;
        var pathCard = Card(sp, 12, ry, 176, 96, BG_CARD);
        rPath = Lbl(pathCard, "", 8, -6, 160, 84, 8, TXT, TextAlignmentOptions.Left);
        rPath.enableWordWrapping = true;

        // History + Theory anchored at bottom
        BottomBtn(p, "History", 12, 8, 84, 26, new Color(.12f, .12f, .22f), 9, out var btnH); BtnHistory = btnH;
        BottomBtn(p, "Theory", 100, 8, 88, 26, new Color(.10f, .18f, .10f), 9, out var btnT); BtnTheory = btnT;

        BuildHistoryOverlay(p);
    }

    void BottomBtn(RectTransform parent, string label, float x, float y,
        float w, float h, Color col, float fs, out Button btn)
    {
        var go = new GameObject("Btn_" + label); go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>(); img.color = col;
        btn = go.AddComponent<Button>(); btn.targetGraphic = img;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(0, 0);
        rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        MakeTxtGO(go.transform, label, fs, FontStyles.Bold, TXT);
    }

    float StatCard(RectTransform p, string label, string unit, Color valCol,
        float y, out TextMeshProUGUI valTxt)
    {
        var card = Card(p, 12, y, 176, 46, BG_CARD);
        Lbl(card, label, 8, -5, 140, 13, 7, DIM, TextAlignmentOptions.Left);
        valTxt = Lbl(card, "—", 8, -20, 110, 22, 16, valCol, TextAlignmentOptions.Left);
        valTxt.fontStyle = FontStyles.Bold;

        return y - 52;
    }

    // History pagination
    int histPage = 0;
    const int HIST_PER_PAGE = 10;
    System.Collections.Generic.List<PathRecord> histAllRecords;
    TextMeshProUGUI lblHistPage;

    void BuildHistoryOverlay(RectTransform p)
    {
        historyPanel = new GameObject("HistoryPanel");
        historyPanel.transform.SetParent(p, false);
        var bg = historyPanel.AddComponent<Image>(); bg.color = new Color(.04f, .07f, .16f, .98f);
        var rt = historyPanel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        historyPanel.SetActive(false);

        // ── Header widget 
        var hdrGO = new GameObject("HistHdr"); hdrGO.transform.SetParent(historyPanel.transform, false);
        hdrGO.AddComponent<Image>().color = new Color(.05f, .10f, .24f);
        var hdrRT = hdrGO.GetComponent<RectTransform>();
        hdrRT.anchorMin = new Vector2(0, 1); hdrRT.anchorMax = new Vector2(1, 1);
        hdrRT.offsetMin = new Vector2(0, -34); hdrRT.offsetMax = Vector2.zero;

        // Cyan left stripe
        var stripe = new GameObject("Stripe"); stripe.transform.SetParent(hdrRT, false);
        stripe.AddComponent<Image>().color = CYAN;
        var strRT = stripe.GetComponent<RectTransform>();
        strRT.anchorMin = new Vector2(0, 0); strRT.anchorMax = new Vector2(0, 1);
        strRT.offsetMin = Vector2.zero; strRT.offsetMax = new Vector2(3, 0);

        Lbl(hdrRT, "HISTORY", 10, -8, 140, 14, 11, CYAN, TextAlignmentOptions.Left);

        // ── Clip area 
        var clip = new GameObject("HistClip"); clip.transform.SetParent(historyPanel.transform, false);
        clip.AddComponent<Image>().color = Color.clear;
        clip.AddComponent<RectMask2D>();
        var clipRT = clip.GetComponent<RectTransform>();
        clipRT.anchorMin = new Vector2(0, 0); clipRT.anchorMax = new Vector2(1, 1);
        clipRT.offsetMin = new Vector2(0, 58); clipRT.offsetMax = new Vector2(0, -34);

        var scroll = new GameObject("Scroll"); scroll.transform.SetParent(clip.transform, false);
        var sRT = scroll.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0, 1); sRT.anchorMax = new Vector2(1, 1);
        sRT.pivot = new Vector2(.5f, 1f);
        sRT.offsetMin = new Vector2(8, 0); sRT.offsetMax = new Vector2(-8, 0);
        historyContent = sRT;

        // ── Bottom buttons
        BottomBtn(rt, "Close", 12, 8, 60, 22, new Color(.3f, .1f, .1f), 8, out var btnClose); BtnHistoryClose = btnClose;
        BottomBtn(rt, "Clear All", 78, 8, 72, 22, new Color(.28f, .08f, .08f), 8, out var btnClear); BtnClearHistory = btnClear;

    }

    void RenderHistPage()
    {
        foreach (Transform child in historyContent) Destroy(child.gameObject);
        if (histAllRecords == null || histAllRecords.Count == 0)
        {
            Lbl(historyContent, "No history yet.", 0, -20, 184, 20, 10, DIM, TextAlignmentOptions.Center);
            historyContent.sizeDelta = new Vector2(0, 40);
            return;
        }
        int pages = Mathf.CeilToInt((float)histAllRecords.Count / HIST_PER_PAGE);
        histPage = Mathf.Clamp(histPage, 0, pages - 1);

        int start = histPage * HIST_PER_PAGE;
        int end = Mathf.Min(start + HIST_PER_PAGE, histAllRecords.Count);
        float ry = -4f;
        for (int i = start; i < end; i++)
        {
            var r = histAllRecords[i];
            // Clickable row — shows path on map without re-running
            var row = Card(historyContent, 4, ry, 176, 50, new Color(.08f, .12f, .24f));
            var rowBtn = row.gameObject.AddComponent<UnityEngine.UI.Button>();
            rowBtn.targetGraphic = row.GetComponent<Image>();
            var cb2 = rowBtn.colors;
            cb2.highlightedColor = new Color(.14f, .22f, .42f); rowBtn.colors = cb2;
            var rec = r;
            rowBtn.onClick.AddListener(() => {
                historyPanel.SetActive(false);
                SimulatorController.Instance?.ReplayFromHistory(rec);
            });
            Lbl(row, $"{r.algo}  {r.dist:0} km  •  {r.visited} nodes",
                8, -7, 160, 14, 8, CYAN, TextAlignmentOptions.Left);
            Lbl(row, $"{r.start} → {r.goal}",
                8, -21, 160, 13, 8, TXT, TextAlignmentOptions.Left);
            Lbl(row, $"Travel: {r.travel}  {r.date}  (tap to view)",
                8, -35, 160, 12, 7, DIM, TextAlignmentOptions.Left);
            ry -= 56;
        }

        // Dynamic pagination buttons — right after last record
        if (pages > 1)
        {
            float btnY = ry - 6f;
            // Prev
            var pGO = new GameObject("PBtnPrev"); pGO.transform.SetParent(historyContent, false);
            var pImg = pGO.AddComponent<Image>();
            pImg.color = histPage > 0 ? new Color(.08f, .14f, .30f) : new Color(.06f, .09f, .18f);
            var pBtn = pGO.AddComponent<UnityEngine.UI.Button>(); pBtn.targetGraphic = pImg;
            var pRT = pGO.GetComponent<RectTransform>();
            pRT.anchorMin = pRT.anchorMax = new Vector2(.5f, 1f); pRT.pivot = new Vector2(.5f, 1f);
            pRT.anchoredPosition = new Vector2(-30f, btnY); pRT.sizeDelta = new Vector2(56, 24);
            Lbl(pRT, "< Prev", 0, -6, 56, 20, 9, Color.white, TextAlignmentOptions.Center);
            pBtn.onClick.AddListener(() => { if (histPage > 0) { histPage--; RenderHistPage(); } });

            // Next
            var nGO = new GameObject("PBtnNext"); nGO.transform.SetParent(historyContent, false);
            var nImg = nGO.AddComponent<Image>();
            nImg.color = histPage < pages - 1 ? new Color(.08f, .14f, .30f) : new Color(.06f, .09f, .18f);
            var nBtn = nGO.AddComponent<UnityEngine.UI.Button>(); nBtn.targetGraphic = nImg;
            var nRT = nGO.GetComponent<RectTransform>();
            nRT.anchorMin = nRT.anchorMax = new Vector2(.5f, 1f); nRT.pivot = new Vector2(.5f, 1f);
            nRT.anchoredPosition = new Vector2(30f, btnY); nRT.sizeDelta = new Vector2(56, 24);
            Lbl(nRT, "Next >", 0, -6, 56, 20, 9, Color.white, TextAlignmentOptions.Center);
            nBtn.onClick.AddListener(() => { if (histPage < pages - 1) { histPage++; RenderHistPage(); } });

            // Page indicator centered below Prev/Next
            var pgGO = new GameObject("PageLbl"); pgGO.transform.SetParent(historyContent, false);
            var pgRT = pgGO.AddComponent<RectTransform>();
            pgRT.anchorMin = pgRT.anchorMax = new Vector2(.5f, 1f); pgRT.pivot = new Vector2(.5f, 1f);
            pgRT.anchoredPosition = new Vector2(0f, btnY - 28f); pgRT.sizeDelta = new Vector2(80, 14);
            lblHistPage = Lbl(pgRT, $"{histPage + 1}/{pages}", 0, 0, 80, 14, 8, DIM, TextAlignmentOptions.Center);

            ry = btnY - 46f;
        }
        historyContent.sizeDelta = new Vector2(0, Mathf.Abs(ry) + 8);
    }

    // ── PUBLIC API
    public void ShowStats(string algo, float dist, int visited, float algoMs,
        System.Collections.Generic.List<int> path,
        System.Collections.Generic.Dictionary<int, NodeData> graph)
    {
        CloseAllRightPanels();
        statsPanel.SetActive(true);
        rAlgo.text = algo;
        rDist.text = dist.ToString("0") + " km";
        rVisited.text = visited.ToString();
        rAlgoTime.text = algoMs.ToString("0.0") + " ms";
        float h = Mathf.Floor(dist / 90f), m = Mathf.Round((dist / 90f - h) * 60f);
        rTravel.text = h > 0 ? $"{h:0}h {m:0}min" : $"{m:0} min";
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < path.Count; i++)
        { sb.Append(graph[path[i]].Name); if (i < path.Count - 1) sb.Append(" → "); }
        rPath.text = sb.ToString();
        BtnReplay.interactable = true;
    }

    public void HideStats() => statsPanel.SetActive(false);
    public void SetMessage(string msg) { if (LabelMessage) LabelMessage.text = msg; }
    public void SetStep(int step, int total)
    {
        if (LabelStep) LabelStep.text = total > 0 ? $"Step {step} / {total}" : "—";
        // Update progress bar
        if (progressBarFill != null)
        {
            float pct = total > 0 ? Mathf.Clamp01((float)step / total) : 0f;
            var rt = progressBarFill.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(176f * pct, 0);
            // Color: green -> orange -> red as it fills
            progressBarFill.color = Color.Lerp(
                new Color(.10f, .72f, .38f),
                new Color(.95f, .45f, .10f), pct);
        }
        if (lblProgressPct != null)
            lblProgressPct.text = total > 0 ? $"{step} / {total}" : "0 / 48";
    }
    public void ShowCompareProgress(string msg)
    {
        CloseAllRightPanels();
        compareProgressGO.SetActive(true);
        rCompareProgress.text = msg;
    }
    public void HideCompareProgress() => compareProgressGO.SetActive(false);

    public void UpdateSelection(NodeData s, NodeData g)
    {
        LabelStart.text = s != null ? $"Start: {s.Name}" : "Start: —";
        LabelGoal.text = g != null ? $"Goal:  {g.Name}" : "Goal:  —";
    }

    public void HighlightAlgoButton(int idx)
    {
        Color[] bc = { new Color(.10f, .18f, .36f), new Color(.10f, .18f, .36f), new Color(.05f, .22f, .48f) };
        for (int i = 0; i < 3; i++)
        {
            AlgoBtns[i].GetComponent<Image>().color = (i == idx) ? new Color(.05f, .32f, .65f) : bc[i];
            var ol = AlgoBtns[i].GetComponent<Outline>();
            if (ol) ol.enabled = (i == idx);
        }
    }

    public void ShowHistory(System.Collections.Generic.List<PathRecord> records)
    {
        // Deduplicate
        var seen = new System.Collections.Generic.HashSet<string>();
        histAllRecords = new System.Collections.Generic.List<PathRecord>();
        foreach (var r in records)
        {
            string key = r.algo + r.start + r.goal;
            if (seen.Add(key)) histAllRecords.Add(r);
        }
        CloseAllRightPanels();
        histPage = 0;
        RenderHistPage();
        historyPanel.SetActive(true);
    }

    public void HideHistory() => historyPanel.SetActive(false);

    void CloseAllRightPanels()
    {
        statsPanel?.SetActive(false);
        historyPanel?.SetActive(false);
        compareProgressGO?.SetActive(false);
        TheoryPanel.Instance?.Hide();
        // SearchTreePanel is NOT closed here — it should persist after the algorithm
        // finishes. It is only closed when the user opens another panel from the menu.
    }

    public void ShowCompare(PathRecord[] results)
    {
        HideCompareProgress();
        if (comparePanel != null) Destroy(comparePanel);

        var cvGO = GameObject.Find("Canvas");
        comparePanel = new GameObject("ComparePanel");
        comparePanel.transform.SetParent(cvGO.transform, false);
        comparePanel.AddComponent<Image>().color = new Color(.03f, .05f, .12f, .97f);
        comparePanel.AddComponent<Canvas>().overrideSorting = true;
        comparePanel.GetComponent<Canvas>().sortingOrder = 999;
        comparePanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        var cpRT = comparePanel.GetComponent<RectTransform>();
        cpRT.anchorMin = Vector2.zero; cpRT.anchorMax = Vector2.one;
        cpRT.offsetMin = cpRT.offsetMax = Vector2.zero;

        Lbl(cpRT, "ALGORITHM COMPARISON", 0, -20, 1280, 20, 13,
            CYAN, TextAlignmentOptions.Center).fontStyle = FontStyles.Bold;
        var first = System.Array.Find(results, r => r != null);
        if (first != null)
            Lbl(cpRT, $"{first.start}  →  {first.goal}", 0, -40, 1280, 14, 9,
                DIM, TextAlignmentOptions.Center);

        // Table
        float tx = 30, ty = -66, rh = 40;
        float[] cws = { 110, 120, 100, 120, 110 };
        string[] cols = { "Algorithm", "Distance\n(km)", "Nodes\nVisited", "Algo Time\n(ms)", "Travel\n@90km/h" };
        float cx2 = tx;
        foreach (var (col, w) in System.Linq.Enumerable.Zip(cols, cws, (a, b) => (a, b)))
        {
            var hc = Card(cpRT, cx2, ty, w - 4, 32, new Color(.10f, .20f, .42f));
            Lbl(hc, col, 0, -4, w - 4, 32, 8, CYAN, TextAlignmentOptions.Center);
            cx2 += w;
        }
        ty -= 36;

        Color[] rowCols = { new Color(.10f, .18f, .32f), new Color(.07f, .15f, .27f), new Color(.05f, .13f, .24f) };
        Color[] algoCols = { YELLOW, GREEN, new Color(.4f, .9f, 1f) };

        float minDist = float.MaxValue, minV = float.MaxValue, minT = float.MaxValue;
        foreach (var r in results)
        {
            if (r == null) continue;
            minDist = Mathf.Min(minDist, r.dist);
            minV = Mathf.Min(minV, r.visited);
            minT = Mathf.Min(minT, float.Parse(r.date.Split('|')[0]));
        }

        for (int i = 0; i < results.Length; i++)
        {
            var r = results[i]; if (r == null) continue;
            float ms = float.Parse(r.date.Split('|')[0]);
            float rx2 = tx; int ii = i;
            void Cell(string txt, float w2, bool best)
            {
                var c = Card(cpRT, rx2, ty, w2 - 4, rh - 4, rowCols[ii]);
                var t = Lbl(c, txt, 4, -(rh - 4) / 2f + 10, w2 - 10, rh - 4, best ? 11f : 10f,
                    best ? algoCols[ii] : TXT, TextAlignmentOptions.Center);
                if (best) t.fontStyle = FontStyles.Bold;
                rx2 += w2;
            }
            Cell(r.algo, cws[0], false);
            Cell($"{r.dist:0}", cws[1], r.dist == minDist);
            Cell($"{r.visited}", cws[2], r.visited == (int)minV);
            Cell($"{ms:0.0}", cws[3], ms == minT);
            Cell(r.travel, cws[4], false);
            ty -= rh;
        }
        Lbl(cpRT, "* highlighted = best value", tx, ty - 12, 580, 12, 7,
            new Color(.6f, .75f, .5f), TextAlignmentOptions.Left);

        // Charts
        Color[] cc = { YELLOW, GREEN, new Color(.4f, .9f, 1f) };
        float[] msArr = new float[3];
        for (int i = 0; i < 3; i++) msArr[i] = results[i] != null ? float.Parse(results[i].date.Split('|')[0]) : 0;
        DrawBarChart(cpRT, "Distance (km)", new[] { results[0]?.dist ?? 0, results[1]?.dist ?? 0, results[2]?.dist ?? 0 }, cc, 640, -58, 290, 185);
        DrawBarChart(cpRT, "Nodes Visited", new[] { results[0]?.visited ?? 0f, results[1]?.visited ?? 0f, results[2]?.visited ?? 0f }, cc, 950, -58, 290, 185);
        DrawBarChart(cpRT, "Algorithm Time (ms)", msArr, cc, 640, -258, 600, 185);

        var close = BigBtn(cpRT, "Close", 0, -14, 100, 26, new Color(.3f, .1f, .1f), 10);
        var cRT2 = close.GetComponent<RectTransform>();
        cRT2.anchorMin = new Vector2(1, 1); cRT2.anchorMax = new Vector2(1, 1);
        cRT2.anchoredPosition = new Vector2(-110, -18);
        close.onClick.AddListener(() => {
            Destroy(comparePanel);
            // Restore map interaction
            var ray = Camera.main?.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
            if (ray) ray.enabled = true;
        });

        // Block map interaction while compare is open
        var raycaster = Camera.main?.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
        if (raycaster) raycaster.enabled = false;
    }

    void DrawBarChart(RectTransform parent, string title, float[] values, Color[] cols, float x, float y, float w, float h)
    {
        var card = Card(parent, x, y, w, h, new Color(.06f, .10f, .22f));
        var chartGO = new GameObject("Chart"); chartGO.transform.SetParent(card, false);
        var chartRT = chartGO.AddComponent<RectTransform>();
        chartRT.anchorMin = Vector2.zero; chartRT.anchorMax = Vector2.one;
        chartRT.offsetMin = new Vector2(4, 4); chartRT.offsetMax = new Vector2(-4, -4);
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(chartRT);
        var chart = chartGO.AddComponent<BarChart>();
        chart.ChartTitle = title;
        chart.GroupLabels = new[] { "BFS", "Dijkstra", "A*" };
        chart.SeriesLabels = new[] { "" };
        chart.SeriesColors = cols;
        chart.Values = new float[3, 1] { { values[0] }, { values[1] }, { values[2] } };
        StartCoroutine(DrawNextFrame(chart));
    }

    System.Collections.IEnumerator DrawNextFrame(BarChart chart) { yield return null; chart.Draw(); }
    GameObject comparePanel;

    // ── HELPERS
    RectTransform Panel(Transform parent, string name,
        Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color col)
    {
        var go = new GameObject(name); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = oMin; rt.offsetMax = oMax;
        return rt;
    }

    RectTransform Card(RectTransform parent, float x, float y, float w, float h, Color col)
    {
        var go = new GameObject("C"); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = col;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return rt;
    }

    Button BigBtn(RectTransform parent, string label, float x, float y, float w, float h, Color col, float fs)
    {
        var card = Card(parent, x, y, w, h, col);
        card.GetComponent<Image>().sprite = RoundedSprite((int)w, (int)h, 7, Color.white);
        card.GetComponent<Image>().type = Image.Type.Sliced;
        card.GetComponent<Image>().color = col;
        var btn = card.gameObject.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = Color.Lerp(col, Color.white, .20f);
        cb.pressedColor = Color.Lerp(col, Color.black, .20f);
        cb.disabledColor = new Color(col.r * .3f, col.g * .3f, col.b * .3f, .5f);
        btn.colors = cb; btn.targetGraphic = card.GetComponent<Image>();
        MakeTxtGO(card, label, fs, FontStyles.Bold, Color.white);

        // ── Sound on click — choose clip by label ──────
        btn.onClick.AddListener(() => {
            if (label == "Run") Play(clipRun);
            else if (label == "Reset") Play(clipReset);
            else if (label == "Stop") Play(clipReset);
            else if (label.Contains("Close") || label.Contains("close")) Play(clipClose);
            else if (label == "History" || label == "Theory" || label == "Compare") Play(clipOpen);
            else Play(clipClick);
        });

        return btn;
    }

    TextMeshProUGUI MakeTxtGO(Transform parent, string text, float size, FontStyles style, Color col)
    {
        var go = new GameObject("L"); go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = col; t.alignment = TextAlignmentOptions.Center;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        return t;
    }

    TextMeshProUGUI Lbl(RectTransform parent, string text, float x, float y,
        float w, float h, float size, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.alignment = align;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        return t;
    }

    void HLine(RectTransform parent, float y)
    {
        var go = new GameObject("Line"); go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = LINE;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = new Vector2(12, y - 1); rt.offsetMax = new Vector2(-12, y);
    }

    void LegRow(RectTransform parent, string label, Color col, float y)
    {
        var dot = new GameObject("D"); dot.transform.SetParent(parent, false);
        dot.AddComponent<Image>().color = col;
        var dRT = dot.GetComponent<RectTransform>();
        dRT.anchorMin = dRT.anchorMax = new Vector2(0, 1); dRT.pivot = new Vector2(0, 1);
        dRT.anchoredPosition = new Vector2(16, y + 2); dRT.sizeDelta = new Vector2(10, 10);
        Lbl(parent, label, 30, y, 160, 14, 9, TXT, TextAlignmentOptions.Left);
    }

    Slider MakeSlider(RectTransform parent, float x, float y, float w, float h)
    {
        var go = new GameObject("S"); go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(x, y); rt.sizeDelta = new Vector2(w, h);
        var s = go.AddComponent<Slider>();
        var bg = new GameObject("Bg"); bg.transform.SetParent(go.transform, false);
        bg.AddComponent<Image>().color = new Color(.10f, .16f, .30f);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, .4f); bgRT.anchorMax = new Vector2(1, .6f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var fa = new GameObject("FA"); fa.transform.SetParent(go.transform, false);
        var faRT = fa.AddComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, .4f); faRT.anchorMax = new Vector2(1, .6f);
        faRT.offsetMin = faRT.offsetMax = Vector2.zero;
        var fill = new GameObject("F"); fill.transform.SetParent(fa.transform, false);
        fill.AddComponent<Image>().color = CYAN;
        var fRT = fill.GetComponent<RectTransform>();
        fRT.anchorMin = Vector2.zero; fRT.anchorMax = new Vector2(.5f, 1);
        fRT.sizeDelta = new Vector2(0, 0); s.fillRect = fRT;
        var ha = new GameObject("HA"); ha.transform.SetParent(go.transform, false);
        var haRT = ha.AddComponent<RectTransform>();
        haRT.anchorMin = Vector2.zero; haRT.anchorMax = Vector2.one;
        haRT.sizeDelta = new Vector2(0, 0);
        var handle = new GameObject("H"); handle.transform.SetParent(ha.transform, false);
        var hImg = handle.AddComponent<Image>(); hImg.color = new Color(.00f, .85f, 1f);
        var hRT = handle.GetComponent<RectTransform>(); hRT.sizeDelta = new Vector2(12, 12);
        s.handleRect = hRT; s.targetGraphic = hImg;
        s.minValue = 1; s.maxValue = 10; s.value = 5; s.wholeNumbers = true;
        return s;
    }

    static Sprite RoundedSprite(int w, int h, int r, Color col)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var pixels = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int dx = 0, dy = 0;
                if (x < r && y < r) { dx = r - x; dy = r - y; }
                else if (x > w - r - 1 && y < r) { dx = x - (w - r - 1); dy = r - y; }
                else if (x < r && y > h - r - 1) { dx = r - x; dy = y - (h - r - 1); }
                else if (x > w - r - 1 && y > h - r - 1) { dx = x - (w - r - 1); dy = y - (h - r - 1); }
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float a = (dx == 0 && dy == 0) ? 1f : dist < r - 1 ? 1f : dist < r ? Mathf.Max(0f, r - dist) : 0f;
                pixels[y * w + x] = new Color(col.r, col.g, col.b, a * col.a);
            }
        tex.SetPixels(pixels); tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(.5f, .5f), 1);
    }

    // Screenshot — BMP format
    System.Collections.IEnumerator TakeScreenshot()
    {
        Debug.Log("[Screenshot] Starting...");
        yield return new WaitForEndOfFrame();
        int w = Screen.width, h = Screen.height;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();
        var raw = tex.GetRawTextureData();
        Debug.Log($"[Screenshot] {w}x{h} raw={raw.Length}");
        UnityEngine.Object.Destroy(tex);

        string filename = $"AlgeriaSim_{System.DateTime.Now:yyyyMMdd_HHmmss}.bmp";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);

        try
        {
            int rowBytes = w * 3;
            int padBytes = (4 - (rowBytes % 4)) % 4;   // BMP rows must be 4-byte aligned
            int stride = rowBytes + padBytes;
            int pixelData = 54 + stride * h;

            using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Create))
            using (var bw = new System.IO.BinaryWriter(fs))
            {
                // ── BMP File Header (14 bytes) ──
                bw.Write((byte)'B'); bw.Write((byte)'M');
                bw.Write(pixelData);   // file size
                bw.Write(0);           // reserved
                bw.Write(54);          // pixel data offset

                // ── DIB Header BITMAPINFOHEADER (40 bytes) ──
                bw.Write(40);          // header size
                bw.Write(w);
                bw.Write(h);           // positive = bottom-to-top (matches Unity raw)
                bw.Write((short)1);    // color planes
                bw.Write((short)24);   // bits per pixel
                bw.Write(0);           // compression = none
                bw.Write(stride * h);  // image size
                bw.Write(2835); bw.Write(2835); // pixels per metre (~72 dpi)
                bw.Write(0); bw.Write(0);        // colors in table

                // ── Pixel data — BMP is BGR, Unity raw is RGB ──
                var pad = new byte[padBytes];
                for (int row = 0; row < h; row++) // BMP bottom-to-top = Unity order
                {
                    int rowStart = row * rowBytes;
                    for (int x = 0; x < w; x++)
                    {
                        bw.Write(raw[rowStart + x * 3 + 2]);
                        bw.Write(raw[rowStart + x * 3 + 1]);
                        bw.Write(raw[rowStart + x * 3 + 0]);
                    }
                    if (padBytes > 0) bw.Write(pad);
                }
            }
            Debug.Log($"[Screenshot] Saved: {path}");
            SetMessage($"Screenshot saved:\n{filename}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Screenshot] FAILED: {e.Message}");
            SetMessage("Screenshot failed!");
        }
    }
}