using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Race Mode: runs BFS, Dijkstra and A* simultaneously on 3 mini-maps.
/// Each algorithm gets its own copy of the graph rendered in a panel.
/// </summary>
public class AlgoRace : MonoBehaviour
{
    public static AlgoRace Instance { get; private set; }

    // ── Colors ────────────────────────────────────────
    static readonly Color BG        = new Color(.03f,.05f,.13f,.98f);
    static readonly Color PANEL_BG  = new Color(.05f,.09f,.20f,1f);
    static readonly Color CYAN      = new Color(.00f,.82f,.95f);
    static readonly Color WHITE     = new Color(.92f,.96f,1f);
    static readonly Color DIM       = new Color(.40f,.58f,.80f);
    static readonly Color C_DEFAULT = new Color(.10f,.22f,.40f);
    static readonly Color C_START   = new Color(.00f,.90f,.55f);
    static readonly Color C_GOAL    = new Color(1f,.20f,.35f);
    static readonly Color C_VISITED = new Color(.24f,.50f,.95f);
    static readonly Color C_OPEN    = new Color(1f,.80f,.10f);
    static readonly Color C_PATH    = new Color(1f,.50f,.13f);

    static readonly Color[] ALGO_COLS = {
        new Color(1f,.82f,.10f),   // BFS — yellow
        new Color(.00f,.90f,.55f), // Dijkstra — green
        new Color(.40f,.85f,1f),   // A* — blue
    };
    static readonly string[] ALGO_NAMES = { "BFS", "Dijkstra", "A*" };

    // ── State ─────────────────────────────────────────
    Canvas cv;
    int    startCode = 13, goalCode = 33; // Tlemcen → Illizi default
    bool   racing = false;

    // Per-algo UI
    struct AlgoPanel
    {
        public RectTransform  rt;
        public Dictionary<int,Image> nodes;
        public List<GameObject>      lines;
        public TextMeshProUGUI lblVisited, lblDist, lblTime, lblStatus;
        public Image           progressBar;
        public bool            finished;
        public float           finishTime;
    }
    AlgoPanel[] panels = new AlgoPanel[3];

    Button btnStart, btnReset, btnClose;
    TextMeshProUGUI lblCountdown;
    RectTransform   selectionRT;
    TextMeshProUGUI lblStart, lblGoal;

    // ── Open ──────────────────────────────────────────
    public static void Open()
    {
        if (Instance != null) return;
        new GameObject("AlgoRace").AddComponent<AlgoRace>();
    }

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void OnDestroy() { Instance = null; }

    // ── UI ────────────────────────────────────────────
    void BuildUI()
    {
        var cvGO = new GameObject("RaceCanvas");
        cv = cvGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 75;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight  = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        // Background
        var bg = new GameObject("BG"); bg.transform.SetParent(cvGO.transform, false);
        bg.AddComponent<Image>().color = BG;
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        // Header
        T(bgRT,"ALGORITHM RACE",0,-16,1280,24,15,FontStyles.Bold,CYAN,TextAlignmentOptions.Center);
        T(bgRT,"Same start — same goal — who finds the shortest path first?",
            0,-38,1280,16,9,FontStyles.Normal,DIM,TextAlignmentOptions.Center);

        // Selection row
        selectionRT = MkBox(bgRT, 0, -62, 700, 24, new Color(.06f,.12f,.26f));
        lblStart = T(selectionRT,"Start: Tlemcen",-160,-5,300,18,9,FontStyles.Bold,C_START,TextAlignmentOptions.Center);
        T(selectionRT,"→",0,-5,40,18,11,FontStyles.Bold,DIM,TextAlignmentOptions.Center);
        lblGoal  = T(selectionRT,"Goal: Illizi",160,-5,300,18,9,FontStyles.Bold,C_GOAL,TextAlignmentOptions.Center);

        // Countdown label
        lblCountdown = T(bgRT,"",0,-62,1280,30,20,FontStyles.Bold,CYAN,TextAlignmentOptions.Center);
        lblCountdown.gameObject.SetActive(false);

        // 3 mini-map panels
        float panelW = 380f, panelH = 480f;
        float[] px = { -430f, 0f, 430f };
        for (int i = 0; i < 3; i++)
            panels[i] = BuildAlgoPanel(bgRT, i, px[i], -95f, panelW, panelH);

        // Bottom buttons
        btnStart = Btn(bgRT,"▶  Start Race", -120,-12,180,36,new Color(.04f,.36f,.16f),12);
        btnReset = Btn(bgRT,"Reset",           80,-12,100,28,new Color(.20f,.10f,.06f),10);
        btnClose = Btn(bgRT,"Close",           190,-12, 80,28,new Color(.30f,.08f,.08f),10);

        var bRT  = btnStart.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(.5f,0); bRT.anchorMax = new Vector2(.5f,0);
        bRT.anchoredPosition = new Vector2(-120,12);

        var rRT  = btnReset.GetComponent<RectTransform>();
        rRT.anchorMin = new Vector2(.5f,0); rRT.anchorMax = new Vector2(.5f,0);
        rRT.anchoredPosition = new Vector2(80,12);

        var cRT  = btnClose.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(.5f,0); cRT.anchorMax = new Vector2(.5f,0);
        cRT.anchoredPosition = new Vector2(190,12);

        btnStart.onClick.AddListener(()=>StartCoroutine(StartRace()));
        btnReset.onClick.AddListener(ResetRace);
        btnClose.onClick.AddListener(()=>{ Destroy(cv.gameObject); Destroy(gameObject); });

        // Pick random start/goal
        PickRandom();
        DrawAllMiniMaps();
    }

    AlgoPanel BuildAlgoPanel(RectTransform parent, int idx, float x, float y, float w, float h)
    {
        var ap = new AlgoPanel();
        ap.nodes = new Dictionary<int,Image>();
        ap.lines = new List<GameObject>();

        // Panel card
        var card = MkBox(parent, x, y, w, h, PANEL_BG);
        ap.rt = card;

        // Header bar
        var hdr = MkBox(card, 0, -0, w, 32, ALGO_COLS[idx]);
        hdr.GetComponent<RectTransform>().anchorMin = new Vector2(0,1);
        hdr.GetComponent<RectTransform>().anchorMax = new Vector2(1,1);
        hdr.GetComponent<RectTransform>().offsetMin = new Vector2(0,-32);
        hdr.GetComponent<RectTransform>().offsetMax = Vector2.zero;
        hdr.anchorMin = new Vector2(0,1); hdr.anchorMax = new Vector2(1,1);
        hdr.offsetMin = new Vector2(0,-32); hdr.offsetMax = Vector2.zero;
        T(hdr, ALGO_NAMES[idx], 0, -10, w, 22, 13, FontStyles.Bold, new Color(.05f,.08f,.18f), TextAlignmentOptions.Center);

        // Mini-map area
        var mapArea = new GameObject("MapArea"); mapArea.transform.SetParent(card, false);
        var maRT = mapArea.AddComponent<RectTransform>();
        maRT.anchorMin = new Vector2(0,1); maRT.anchorMax = new Vector2(1,1);
        maRT.offsetMin = new Vector2(4,-h+80); maRT.offsetMax = new Vector2(-4,-32);

        // Draw mini nodes
        DrawMiniMap(maRT, idx, ap);

        // Stats at bottom
        float sy = -h+18;
        ap.lblStatus  = T(card,"Waiting...", 0, sy, w-8, 16, 8, FontStyles.Normal, DIM, TextAlignmentOptions.Center); sy+=16;
        ap.lblVisited = StatLbl(card,"Visited","0", -w/4+10, sy);
        ap.lblDist    = StatLbl(card,"Distance","—", 0, sy);
        ap.lblTime    = StatLbl(card,"Time","—", w/4-10, sy);

        // Progress bar
        var pbBG = MkBox(card, 0, -h+52, w-8, 8, new Color(.08f,.14f,.28f));
        var pb   = MkBox(pbBG, 0, 0, 0, 8, ALGO_COLS[idx]);
        pb.anchorMin = new Vector2(0,.5f); pb.anchorMax = new Vector2(0,.5f);
        pb.pivot     = new Vector2(0,.5f);
        pb.anchoredPosition = Vector2.zero;
        ap.progressBar = pb.GetComponent<Image>();

        return ap;
    }

    void DrawMiniMap(RectTransform parent, int idx, AlgoPanel ap)
    {
        if (GraphMap.Instance == null) return;
        var nodes = GraphMap.Instance.Nodes;

        // Calculate bounds
        float minX=float.MaxValue, maxX=float.MinValue, minY=float.MaxValue, maxY=float.MinValue;
        foreach (var nd in nodes.Values)
        {
            var p = nd.GameObject.transform.position;
            if(p.x<minX)minX=p.x; if(p.x>maxX)maxX=p.x;
            if(p.y<minY)minY=p.y; if(p.y>maxY)maxY=p.y;
        }

        float rw = parent.rect.width  == 0 ? 360 : parent.rect.width;
        float rh = parent.rect.height == 0 ? 360 : parent.rect.height;
        float pad = 14f;

        Vector2 WorldToMini(Vector3 wp)
        {
            float nx = (wp.x-minX)/(maxX-minX);
            float ny = (wp.y-minY)/(maxY-minY);
            return new Vector2(
                Mathf.Lerp(-rw/2+pad, rw/2-pad, nx),
                Mathf.Lerp(-rh/2+pad, rh/2-pad, ny));
        }

        // Draw edges
        foreach (var nd in nodes.Values)
        {
            var pa = WorldToMini(nd.GameObject.transform.position);
            foreach (var (nb,dist) in nd.Neighbors)
            {
                if (nb <= nd.Code) continue;
                if (!nodes.ContainsKey(nb)) continue;
                var pb = WorldToMini(nodes[nb].GameObject.transform.position);
                var line = new GameObject("E"); line.transform.SetParent(parent, false);
                line.AddComponent<Image>().color = new Color(.12f,.22f,.40f,.60f);
                var lRT = line.GetComponent<RectTransform>();
                Vector2 mid = (pa+pb)*.5f;
                float len = Vector2.Distance(pa,pb);
                float ang = Mathf.Atan2(pb.y-pa.y,pb.x-pa.x)*Mathf.Rad2Deg;
                lRT.anchorMin = lRT.anchorMax = new Vector2(.5f,.5f);
                lRT.pivot = new Vector2(.5f,.5f);
                lRT.anchoredPosition = mid;
                lRT.sizeDelta = new Vector2(len,1f);
                lRT.localEulerAngles = new Vector3(0,0,ang);
                ap.lines.Add(line);
            }
        }

        // Draw nodes
        foreach (var nd in nodes.Values)
        {
            var pos = WorldToMini(nd.GameObject.transform.position);
            var dot = new GameObject("N"); dot.transform.SetParent(parent, false);
            var img = dot.AddComponent<Image>();
            img.color = (nd.Code==startCode)?C_START:(nd.Code==goalCode)?C_GOAL:C_DEFAULT;
            var nRT = dot.GetComponent<RectTransform>();
            nRT.anchorMin = nRT.anchorMax = new Vector2(.5f,.5f);
            nRT.pivot = new Vector2(.5f,.5f);
            nRT.anchoredPosition = pos;
            nRT.sizeDelta = new Vector2(5,5);
            ap.nodes[nd.Code] = img;
        }
    }

    void SetMiniNode(int panelIdx, int code, Color col)
    {
        if (panels[panelIdx].nodes.TryGetValue(code, out var img))
            if (img != null) img.color = col;
    }

    // ── Race logic ────────────────────────────────────
    IEnumerator StartRace()
    {
        if (racing) yield break;
        racing = true;
        ResetVisuals();
        btnStart.interactable = false;

        // Countdown
        lblCountdown.gameObject.SetActive(true);
        for (int i=3;i>=1;i--)
        {
            lblCountdown.text = i.ToString();
            yield return new WaitForSeconds(0.7f);
        }
        lblCountdown.text = "GO!";
        yield return new WaitForSeconds(0.4f);
        lblCountdown.gameObject.SetActive(false);

        // Launch all 3 simultaneously
        StartCoroutine(RunAlgo(0)); // BFS
        StartCoroutine(RunAlgo(1)); // Dijkstra
        StartCoroutine(RunAlgo(2)); // A*

        // Wait for all to finish
        yield return new WaitUntil(()=>panels[0].finished&&panels[1].finished&&panels[2].finished);

        racing = false;
        btnStart.interactable = true;
        ShowWinner();
    }

    IEnumerator RunAlgo(int idx)
    {
        panels[idx].finished   = false;
        panels[idx].finishTime = 0;
        panels[idx].lblStatus.text  = "Running...";
        panels[idx].lblStatus.color = ALGO_COLS[idx];

        var nodes = GraphMap.Instance.Nodes;
        float t0 = Time.realtimeSinceStartup;

        PathResult result = null;
        int visited = 0;

        if (idx==0)
            yield return RunBFS(idx, r=>{ result=r; }, v=>visited=v);
        else if (idx==1)
            yield return RunDijkstra(idx, r=>{ result=r; }, v=>visited=v);
        else
            yield return RunAStar(idx, r=>{ result=r; }, v=>visited=v);

        float elapsed = (Time.realtimeSinceStartup-t0)*1000f;
        panels[idx].finishTime = elapsed;

        if (result!=null && result.Found)
        {
            float dist = idx==0 ? Algorithms.PathDistance(nodes, result.Path) : result.Dist;

            // Animate path
            for (int i=0;i<result.Path.Count-1;i++)
            {
                SetMiniNode(idx, result.Path[i], C_PATH);
                yield return new WaitForSeconds(0.04f);
            }
            SetMiniNode(idx, startCode, C_START);
            SetMiniNode(idx, goalCode, C_GOAL);

            panels[idx].lblDist.text    = $"{dist:0} km";
            panels[idx].lblTime.text    = $"{elapsed:0.0}ms";
            panels[idx].lblVisited.text = visited.ToString();
            panels[idx].lblStatus.text  = "DONE";
        }
        else
        {
            panels[idx].lblStatus.text = "No path";
        }

        panels[idx].finished = true;
    }

    IEnumerator RunBFS(int idx, System.Action<PathResult> done, System.Action<int> onVisit)
    {
        var nodes   = GraphMap.Instance.Nodes;
        var queue   = new Queue<int>();
        var visited = new HashSet<int>();
        var parent  = new Dictionary<int,int>();
        queue.Enqueue(startCode); visited.Add(startCode);
        int vc = 0; float total = nodes.Count;

        while (queue.Count>0 && racing)
        {
            int cur = queue.Dequeue(); vc++;
            onVisit(vc);
            SetMiniNode(idx, cur, C_VISITED);
            UpdateProgress(idx, vc/total);
            if (cur==goalCode) { done(Reconstruct(parent, startCode, goalCode)); yield break; }
            foreach (var (nb,d) in nodes[cur].Neighbors)
                if (!visited.Contains(nb)) { visited.Add(nb); parent[nb]=cur; SetMiniNode(idx,nb,C_OPEN); queue.Enqueue(nb); }
            yield return new WaitForSeconds(0.03f);
        }
        done(null);
    }

    IEnumerator RunDijkstra(int idx, System.Action<PathResult> done, System.Action<int> onVisit)
    {
        var nodes  = GraphMap.Instance.Nodes;
        var dist   = new Dictionary<int,float>(); dist[startCode]=0;
        var parent = new Dictionary<int,int>();
        var open   = new HashSet<int>(); open.Add(startCode);
        var closed = new HashSet<int>();
        int vc=0; float total=nodes.Count;

        while (open.Count>0 && racing)
        {
            int cur=-1; float best=float.MaxValue;
            foreach (var n in open) if (dist.TryGetValue(n,out float d)&&d<best){best=d;cur=n;}
            if (cur<0) break;
            open.Remove(cur); closed.Add(cur); vc++;
            onVisit(vc);
            SetMiniNode(idx,cur,C_VISITED);
            UpdateProgress(idx,vc/total);
            if (cur==goalCode) { done(Reconstruct(parent,startCode,goalCode,dist)); yield break; }
            foreach (var (nb,d) in nodes[cur].Neighbors)
            { if (closed.Contains(nb)) continue; float nd=best+d; if (!dist.ContainsKey(nb)||nd<dist[nb]){dist[nb]=nd;parent[nb]=cur;open.Add(nb);SetMiniNode(idx,nb,C_OPEN);} }
            yield return new WaitForSeconds(0.03f);
        }
        done(null);
    }

    IEnumerator RunAStar(int idx, System.Action<PathResult> done, System.Action<int> onVisit)
    {
        var nodes  = GraphMap.Instance.Nodes;
        var g      = new Dictionary<int,float>(); g[startCode]=0;
        var parent = new Dictionary<int,int>();
        var open   = new HashSet<int>(); open.Add(startCode);
        var closed = new HashSet<int>();
        int vc=0; float total=nodes.Count;

        float H(int c)
        {
            var a = nodes[c].GameObject.transform.position;
            var b = nodes[goalCode].GameObject.transform.position;
            return Vector3.Distance(a,b);
        }

        while (open.Count>0 && racing)
        {
            int cur=-1; float best=float.MaxValue;
            foreach (var n in open) { float f=(g.TryGetValue(n,out float gv)?gv:float.MaxValue)+H(n); if(f<best){best=f;cur=n;} }
            if (cur<0) break;
            open.Remove(cur); closed.Add(cur); vc++;
            onVisit(vc);
            SetMiniNode(idx,cur,C_VISITED);
            UpdateProgress(idx,vc/total);
            if (cur==goalCode) { done(Reconstruct(parent,startCode,goalCode,g)); yield break; }
            foreach (var (nb,d) in nodes[cur].Neighbors)
            { if(closed.Contains(nb))continue; float ng=g[cur]+d; if(!g.ContainsKey(nb)||ng<g[nb]){g[nb]=ng;parent[nb]=cur;open.Add(nb);SetMiniNode(idx,nb,C_OPEN);} }
            yield return new WaitForSeconds(0.03f);
        }
        done(null);
    }

    void UpdateProgress(int idx, float t)
    {
        if (panels[idx].progressBar != null)
        {
            var rt = panels[idx].progressBar.GetComponent<RectTransform>();
            var parentW = rt.parent.GetComponent<RectTransform>().rect.width;
            if (parentW == 0) parentW = 370;
            rt.sizeDelta = new Vector2(Mathf.Clamp01(t)*parentW, 8);
        }
    }

    PathResult Reconstruct(Dictionary<int,int> parent, int start, int goal, Dictionary<int,float> dist=null)
    {
        var path = new List<int>(); int cur=goal;
        while (cur!=start&&parent.ContainsKey(cur)){path.Add(cur);cur=parent[cur];}
        path.Add(start); path.Reverse();
        float d = dist!=null&&dist.ContainsKey(goal)?dist[goal]:0;
        return new PathResult{ Path=path, Dist=d, Visited=path.Count };
    }

    void ShowWinner()
    {
        // Find fastest by time
        int winner = 0;
        for (int i=1;i<3;i++)
            if (panels[i].finishTime < panels[winner].finishTime) winner=i;

        for (int i=0;i<3;i++)
        {
            if (i==winner)
            {
                panels[i].lblStatus.text  = "WINNER!";
                panels[i].lblStatus.color = ALGO_COLS[i];
                panels[i].lblStatus.fontStyle = FontStyles.Bold;
            }
        }
    }

    void PickRandom()
    {
        if(GraphMap.Instance==null)return;
        var codes=new List<int>(GraphMap.Instance.Nodes.Keys);
        if (codes.Count < 2) return;
        startCode = codes[Random.Range(0,codes.Count)];
        do { goalCode = codes[Random.Range(0,codes.Count)]; }
        while (goalCode==startCode);

        if (GraphMap.Instance!=null)
        {
            lblStart.text = $"Start: {GraphMap.Instance.Nodes[startCode].Name}";
            lblGoal.text  = $"Goal:  {GraphMap.Instance.Nodes[goalCode].Name}";
        }
    }

    void DrawAllMiniMaps()
    {
        // Called once after UI built — nodes are drawn in BuildAlgoPanel
    }

    void ResetVisuals()
    {
        for (int i=0;i<3;i++)
        {
            foreach (var kvp in panels[i].nodes)
            {
                if (kvp.Value==null) continue;
                kvp.Value.color = kvp.Key==startCode?C_START:kvp.Key==goalCode?C_GOAL:C_DEFAULT;
            }
            panels[i].lblVisited.text = "0";
            panels[i].lblDist.text    = "—";
            panels[i].lblTime.text    = "—";
            panels[i].lblStatus.text  = "Ready";
            panels[i].lblStatus.color = DIM;
            panels[i].lblStatus.fontStyle = FontStyles.Normal;
            if (panels[i].progressBar!=null)
                panels[i].progressBar.GetComponent<RectTransform>().sizeDelta = new Vector2(0,8);
        }
    }

    void ResetRace()
    {
        racing = false;
        PickRandom();
        // Rebuild mini maps
        for (int i=0;i<3;i++)
        {
            foreach (Transform child in panels[i].rt) Destroy(child.gameObject);
            panels[i].nodes.Clear(); panels[i].lines.Clear();
        }
        // Rebuild
        float panelW=380f,panelH=480f;
        float[] px={-430f,0f,430f};
        for (int i=0;i<3;i++)
        {
            var hdr = MkBox(panels[i].rt,0,0,panelW,32,ALGO_COLS[i]);
            hdr.anchorMin=new Vector2(0,1);hdr.anchorMax=new Vector2(1,1);
            hdr.offsetMin=new Vector2(0,-32);hdr.offsetMax=Vector2.zero;
            T(hdr,ALGO_NAMES[i],0,-10,panelW,22,13,FontStyles.Bold,new Color(.05f,.08f,.18f),TextAlignmentOptions.Center);
            var ma=new GameObject("MapArea");ma.transform.SetParent(panels[i].rt,false);
            var maRT=ma.AddComponent<RectTransform>();
            maRT.anchorMin=new Vector2(0,1);maRT.anchorMax=new Vector2(1,1);
            maRT.offsetMin=new Vector2(4,-panelH+80);maRT.offsetMax=new Vector2(-4,-32);
            DrawMiniMap(maRT,i,panels[i]);
            float sy=-panelH+18;
            panels[i].lblStatus =T(panels[i].rt,"Ready",0,sy,panelW-8,16,8,FontStyles.Normal,DIM,TextAlignmentOptions.Center);sy+=16;
            panels[i].lblVisited=StatLbl(panels[i].rt,"Visited","0",-panelW/4+10,sy);
            panels[i].lblDist   =StatLbl(panels[i].rt,"Distance","—",0,sy);
            panels[i].lblTime   =StatLbl(panels[i].rt,"Time","—",panelW/4-10,sy);
            var pbBG=MkBox(panels[i].rt,0,-panelH+52,panelW-8,8,new Color(.08f,.14f,.28f));
            var pb=MkBox(pbBG,0,0,0,8,ALGO_COLS[i]);
            pb.anchorMin=new Vector2(0,.5f);pb.anchorMax=new Vector2(0,.5f);pb.pivot=new Vector2(0,.5f);pb.anchoredPosition=Vector2.zero;
            panels[i].progressBar=pb.GetComponent<Image>();
            panels[i].finished=false;
        }
        btnStart.interactable=true;
    }

    // ── Helpers ───────────────────────────────────────
    TextMeshProUGUI StatLbl(RectTransform p,string label,string val,float x,float y)
    {
        T(p,label,x,y+10,90,12,6,FontStyles.Normal,DIM,TextAlignmentOptions.Center);
        return T(p,val,x,y-2,90,16,11,FontStyles.Bold,WHITE,TextAlignmentOptions.Center);
    }

    RectTransform MkBox(RectTransform p,float x,float y,float w,float h,Color col)
    {
        var go=new GameObject("B");go.transform.SetParent(p,false);
        go.AddComponent<Image>().color=col;
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return rt;
    }

    Button Btn(RectTransform p,string label,float x,float y,float w,float h,Color col,float fs)
    {
        var box=MkBox(p,x,y,w,h,col);
        var btn=box.gameObject.AddComponent<Button>();btn.targetGraphic=box.GetComponent<Image>();
        var cb=btn.colors;cb.highlightedColor=Color.Lerp(col,Color.white,.2f);btn.colors=cb;
        T(box,label,0,-h/2+fs*.65f,w,fs*1.8f,fs,FontStyles.Bold,Color.white,TextAlignmentOptions.Center);
        return btn;
    }

    TextMeshProUGUI T(RectTransform p,string text,float x,float y,float w,float h,
        float size,FontStyles style,Color col,TextAlignmentOptions align)
    {
        var go=new GameObject("T");go.transform.SetParent(p,false);
        var t=go.AddComponent<TextMeshProUGUI>();
        t.text=text;t.fontSize=size;t.fontStyle=style;t.color=col;t.alignment=align;t.enableWordWrapping=true;
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return t;
    }
}
