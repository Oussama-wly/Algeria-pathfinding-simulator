using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Heuristic Demo: shows A* with 3 different heuristics side by side.
/// Haversine (admissible), Manhattan (overestimates), Zero (= Dijkstra).
/// </summary>
public class HeuristicDemo : MonoBehaviour
{
    public static HeuristicDemo Instance { get; private set; }

    static readonly Color BG       = new Color(.03f,.05f,.13f,.98f);
    static readonly Color PANEL_BG = new Color(.05f,.09f,.20f,1f);
    static readonly Color CYAN     = new Color(.00f,.82f,.95f);
    static readonly Color WHITE    = new Color(.92f,.96f,1f);
    static readonly Color DIM      = new Color(.40f,.58f,.80f);
    static readonly Color C_DEFAULT= new Color(.10f,.22f,.40f);
    static readonly Color C_START  = new Color(.00f,.90f,.55f);
    static readonly Color C_GOAL   = new Color(1f,.20f,.35f);
    static readonly Color C_VISITED= new Color(.24f,.50f,.95f);
    static readonly Color C_OPEN   = new Color(1f,.80f,.10f);
    static readonly Color C_PATH   = new Color(1f,.50f,.13f);

    static readonly string[] H_NAMES = { "Haversine\n(Admissible)", "Manhattan\n(Overestimates)", "Zero\n(= Dijkstra)" };
    static readonly Color[]  H_COLS  = { new Color(.00f,.85f,1f), new Color(1f,.60f,.10f), new Color(.60f,.40f,1f) };
    static readonly string[] H_DESC  = {
        "Uses straight-line distance.\nNever overestimates → optimal.",
        "Uses grid distance.\nMay overestimate → faster but\nnot always optimal.",
        "h(n)=0: A* becomes\nDijkstra. Explores all\ndirections equally."
    };

    Canvas cv;
    int startCode=-1, goalCode=-1;
    bool running=false;

    struct HPanel
    {
        public RectTransform rt;
        public Dictionary<int,Image> nodes;
        public TextMeshProUGUI lblVisited,lblDist,lblOptimal,lblStatus;
        public bool finished;
    }
    HPanel[] panels = new HPanel[3];
    Button btnRun, btnReset, btnClose;
    TextMeshProUGUI lblStart, lblGoal;

    public static void Open()
    {
        if (Instance!=null) return;
        new GameObject("HeuristicDemo").AddComponent<HeuristicDemo>();
    }

    void Awake() { Instance=this; BuildUI(); }
    void OnDestroy() { Instance=null; }

    void BuildUI()
    {
        var cvGO=new GameObject("HDCanvas");
        cv=cvGO.AddComponent<Canvas>();
        cv.renderMode=RenderMode.ScreenSpaceOverlay; cv.sortingOrder=75;
        var sc=cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution=new Vector2(1280,720); sc.matchWidthOrHeight=0.5f;
        cvGO.AddComponent<GraphicRaycaster>();

        var bg=new GameObject("BG"); bg.transform.SetParent(cvGO.transform,false);
        bg.AddComponent<Image>().color=BG;
        var bgRT=bg.GetComponent<RectTransform>();
        bgRT.anchorMin=Vector2.zero; bgRT.anchorMax=Vector2.one;
        bgRT.offsetMin=bgRT.offsetMax=Vector2.zero;

        T(bgRT,"HEURISTIC IMPACT",0,-16,1280,24,15,FontStyles.Bold,CYAN,TextAlignmentOptions.Center);
        T(bgRT,"How different heuristics affect A* — same start, same goal, different strategies",
            0,-38,1280,16,9,FontStyles.Normal,DIM,TextAlignmentOptions.Center);

        // Selection
        var sel=MkBox(bgRT,0,-60,700,22,new Color(.06f,.12f,.26f));
        lblStart=T(sel,"Start: —",-160,-4,280,16,9,FontStyles.Bold,C_START,TextAlignmentOptions.Center);
        T(sel,"→",0,-4,40,16,10,FontStyles.Bold,DIM,TextAlignmentOptions.Center);
        lblGoal =T(sel,"Goal: —",160,-4,280,16,9,FontStyles.Bold,C_GOAL, TextAlignmentOptions.Center);

        // 3 panels
        float pw=370f,ph=460f;
        float[] px={-425f,0f,425f};
        for (int i=0;i<3;i++) panels[i]=BuildHPanel(bgRT,i,px[i],-90f,pw,ph);

        // Buttons
        btnRun   =Btn(bgRT,"Run All",  -120,12,150,34,new Color(.04f,.36f,.16f),12);
        btnReset =Btn(bgRT,"New Route", 50, 12,110,26,new Color(.20f,.10f,.06f),10);
        btnClose =Btn(bgRT,"Close",    175, 12, 80,26,new Color(.30f,.08f,.08f),10);
        foreach (var b in new[]{btnRun,btnReset,btnClose})
        { var rt=b.GetComponent<RectTransform>(); rt.anchorMin=new Vector2(.5f,0); rt.anchorMax=new Vector2(.5f,0); }
        btnRun.GetComponent<RectTransform>().anchoredPosition=new Vector2(-120,12);
        btnReset.GetComponent<RectTransform>().anchoredPosition=new Vector2(55,12);
        btnClose.GetComponent<RectTransform>().anchoredPosition=new Vector2(175,12);

        btnRun.onClick.AddListener(()=>StartCoroutine(RunAll()));
        btnReset.onClick.AddListener(PickAndReset);
        btnClose.onClick.AddListener(()=>{Destroy(cv.gameObject);Destroy(gameObject);});

        PickAndReset();
    }

    HPanel BuildHPanel(RectTransform parent,int idx,float x,float y,float w,float h)
    {
        var ap=new HPanel(); ap.nodes=new Dictionary<int,Image>();
        var card=MkBox(parent,x,y,w,h,PANEL_BG); ap.rt=card;

        // Header
        var hdr=new GameObject("H"); hdr.transform.SetParent(card,false);
        hdr.AddComponent<Image>().color=H_COLS[idx];
        var hRT=hdr.GetComponent<RectTransform>();
        hRT.anchorMin=new Vector2(0,1);hRT.anchorMax=new Vector2(1,1);
        hRT.offsetMin=new Vector2(0,-38);hRT.offsetMax=Vector2.zero;
        T(hRT,H_NAMES[idx],0,-12,w,30,10,FontStyles.Bold,new Color(.05f,.08f,.18f),TextAlignmentOptions.Center);

        // Description
        T(card,H_DESC[idx],0,-52,w-8,40,7,FontStyles.Normal,DIM,TextAlignmentOptions.Center);

        // Mini map
        var ma=new GameObject("MA"); ma.transform.SetParent(card,false);
        var maRT=ma.AddComponent<RectTransform>();
        maRT.anchorMin=new Vector2(0,1);maRT.anchorMax=new Vector2(1,1);
        maRT.offsetMin=new Vector2(4,-h+70);maRT.offsetMax=new Vector2(-4,-95);
        DrawMiniNodes(maRT,idx,ap);

        // Stats
        float sy=-h+14;
        ap.lblStatus =T(card,"Waiting",0,sy,w-8,14,7,FontStyles.Normal,DIM,TextAlignmentOptions.Center);sy+=14;
        ap.lblVisited=StatLbl(card,"Visited","—",-w/3+10,sy);
        ap.lblDist   =StatLbl(card,"Distance","—",0,sy);
        ap.lblOptimal=StatLbl(card,"Optimal?","—",w/3-10,sy);

        return ap;
    }

    void DrawMiniNodes(RectTransform parent,int idx,HPanel ap)
    {
        if (GraphMap.Instance==null) return;
        var nodes=GraphMap.Instance.Nodes;
        float minX=float.MaxValue,maxX=float.MinValue,minY=float.MaxValue,maxY=float.MinValue;
        foreach (var nd in nodes.Values){var p=nd.GameObject.transform.position;if(p.x<minX)minX=p.x;if(p.x>maxX)maxX=p.x;if(p.y<minY)minY=p.y;if(p.y>maxY)maxY=p.y;}
        float rw=350f,rh=280f,pad=10f;
        Vector2 W2M(Vector3 wp){return new Vector2(Mathf.Lerp(-rw/2+pad,rw/2-pad,(wp.x-minX)/(maxX-minX)),Mathf.Lerp(-rh/2+pad,rh/2-pad,(wp.y-minY)/(maxY-minY)));}
        foreach (var nd in nodes.Values)
        {
            var pa=W2M(nd.GameObject.transform.position);
            foreach (var (nb,d) in nd.Neighbors)
            {
                if (nb<=nd.Code||!nodes.ContainsKey(nb)) continue;
                var pb=W2M(nodes[nb].GameObject.transform.position);
                var line=new GameObject("E");line.transform.SetParent(parent,false);
                line.AddComponent<Image>().color=new Color(.12f,.22f,.40f,.50f);
                var lRT=line.GetComponent<RectTransform>();
                Vector2 mid=(pa+pb)*.5f;float len=Vector2.Distance(pa,pb);float ang=Mathf.Atan2(pb.y-pa.y,pb.x-pa.x)*Mathf.Rad2Deg;
                lRT.anchorMin=lRT.anchorMax=new Vector2(.5f,.5f);lRT.pivot=new Vector2(.5f,.5f);
                lRT.anchoredPosition=mid;lRT.sizeDelta=new Vector2(len,1f);lRT.localEulerAngles=new Vector3(0,0,ang);
            }
            var dot=new GameObject("N");dot.transform.SetParent(parent,false);
            var img=dot.AddComponent<Image>();
            img.color=(nd.Code==startCode)?C_START:(nd.Code==goalCode)?C_GOAL:C_DEFAULT;
            var nRT=dot.GetComponent<RectTransform>();
            nRT.anchorMin=nRT.anchorMax=new Vector2(.5f,.5f);nRT.pivot=new Vector2(.5f,.5f);
            nRT.anchoredPosition=W2M(nd.GameObject.transform.position);nRT.sizeDelta=new Vector2(5,5);
            ap.nodes[nd.Code]=img;
        }
    }

    void SetNode(int pidx,int code,Color col){if(panels[pidx].nodes.TryGetValue(code,out var i)&&i!=null)i.color=col;}

    IEnumerator RunAll()
    {
        if (running) yield break;
        running=true; btnRun.interactable=false;
        ResetVisuals();

        StartCoroutine(RunHeuristic(0)); // Haversine
        StartCoroutine(RunHeuristic(1)); // Manhattan
        StartCoroutine(RunHeuristic(2)); // Zero

        yield return new WaitUntil(()=>panels[0].finished&&panels[1].finished&&panels[2].finished);
        running=false; btnRun.interactable=true;
        CompareResults();
    }

    IEnumerator RunHeuristic(int idx)
    {
        panels[idx].finished=false;
        panels[idx].lblStatus.text="Running...";
        panels[idx].lblStatus.color=H_COLS[idx];

        var nodes=GraphMap.Instance.Nodes;
        var g=new Dictionary<int,float>();g[startCode]=0;
        var parent=new Dictionary<int,int>();
        var open=new HashSet<int>();open.Add(startCode);
        var closed=new HashSet<int>();
        int vc=0;

        float H(int c)
        {
            if (!nodes.ContainsKey(c)||!nodes.ContainsKey(goalCode)) return 0;
            var a=nodes[c]; var b=nodes[goalCode];
            if (idx==0) // Haversine
            {
                float lat1=a.Lat*Mathf.Deg2Rad,lat2=b.Lat*Mathf.Deg2Rad;
                float dLat=(b.Lat-a.Lat)*Mathf.Deg2Rad,dLon=(b.Lon-a.Lon)*Mathf.Deg2Rad;
                float s=Mathf.Sin(dLat/2);float s2=Mathf.Sin(dLon/2);
                float x2=s*s+Mathf.Cos(lat1)*Mathf.Cos(lat2)*s2*s2;
                return 6371f*2f*Mathf.Atan2(Mathf.Sqrt(x2),Mathf.Sqrt(1-x2));
            }
            else if (idx==1) // Manhattan (using lat/lon degrees × 111)
                return (Mathf.Abs(a.Lat-b.Lat)+Mathf.Abs(a.Lon-b.Lon))*111f;
            else return 0; // Zero = Dijkstra
        }

        while (open.Count>0&&running)
        {
            int cur=-1;float best=float.MaxValue;
            foreach (var n in open){float f=(g.TryGetValue(n,out float gv)?gv:float.MaxValue)+H(n);if(f<best){best=f;cur=n;}}
            if (cur<0) break;
            open.Remove(cur);closed.Add(cur);vc++;
            SetNode(idx,cur,C_VISITED);
            if (cur==goalCode)
            {
                var path=new List<int>();int c2=goalCode;
                while(c2!=startCode&&parent.ContainsKey(c2)){path.Add(c2);c2=parent[c2];}
                path.Add(startCode);path.Reverse();
                foreach (int pc in path) SetNode(idx,pc,C_PATH);
                SetNode(idx,startCode,C_START);SetNode(idx,goalCode,C_GOAL);
                float dist=g.ContainsKey(goalCode)?g[goalCode]:0;
                panels[idx].lblDist.text=$"{dist:0} km";
                panels[idx].lblVisited.text=vc.ToString();
                panels[idx].lblStatus.text="Done";
                break;
            }
            foreach (var (nb,d) in nodes[cur].Neighbors)
            {if(closed.Contains(nb))continue;float ng=g[cur]+d;if(!g.ContainsKey(nb)||ng<g[nb]){g[nb]=ng;parent[nb]=cur;open.Add(nb);SetNode(idx,nb,C_OPEN);}}
            yield return new WaitForSeconds(0.025f);
        }
        panels[idx].finished=true;
    }

    void CompareResults()
    {
        // Find minimum distance (Haversine should be optimal)
        float minDist=float.MaxValue;
        for (int i=0;i<3;i++)
        {
            if (float.TryParse(panels[i].lblDist.text.Replace(" km",""),out float d))
                minDist=Mathf.Min(minDist,d);
        }
        for (int i=0;i<3;i++)
        {
            if (float.TryParse(panels[i].lblDist.text.Replace(" km",""),out float d))
                panels[i].lblOptimal.text = Mathf.Abs(d-minDist)<1 ? "Yes" : $"+{d-minDist:0}km";
        }
    }

    void PickAndReset()
    {
        running=false;
        if(GraphMap.Instance==null)return;
        var codes=new List<int>(GraphMap.Instance.Nodes.Keys);
        if (codes.Count<2) return;
        startCode=codes[Random.Range(0,codes.Count)];
        do{goalCode=codes[Random.Range(0,codes.Count)];}while(goalCode==startCode);
        lblStart.text=$"Start: {GraphMap.Instance.Nodes[startCode].Name}";
        lblGoal.text =$"Goal:  {GraphMap.Instance.Nodes[goalCode].Name}";
        ResetVisuals();
        for (int i=0;i<3;i++){foreach(Transform ch in panels[i].rt)Destroy(ch.gameObject);panels[i].nodes.Clear();}
        float pw=370f,ph=460f;float[] px={-425f,0f,425f};
        for (int i=0;i<3;i++) panels[i]=BuildHPanel(panels[i].rt.parent.GetComponent<RectTransform>().parent.GetComponent<RectTransform>(),i,px[i],-90f,pw,ph);
        btnRun.interactable=true;
    }

    void ResetVisuals()
    {
        for(int i=0;i<3;i++){foreach(var kv in panels[i].nodes){if(kv.Value==null)continue;kv.Value.color=kv.Key==startCode?C_START:kv.Key==goalCode?C_GOAL:C_DEFAULT;}panels[i].lblVisited.text="—";panels[i].lblDist.text="—";panels[i].lblOptimal.text="—";panels[i].lblStatus.text="Waiting";panels[i].lblStatus.color=DIM;panels[i].finished=false;}
    }

    TextMeshProUGUI StatLbl(RectTransform p,string label,string val,float x,float y)
    {
        T(p,label,x,y+10,90,12,6,FontStyles.Normal,DIM,TextAlignmentOptions.Center);
        return T(p,val,x,y-2,90,14,10,FontStyles.Bold,WHITE,TextAlignmentOptions.Center);
    }

    RectTransform MkBox(RectTransform p,float x,float y,float w,float h,Color col)
    {
        var go=new GameObject("B");go.transform.SetParent(p,false);go.AddComponent<Image>().color=col;
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return rt;
    }

    Button Btn(RectTransform p,string label,float x,float y,float w,float h,Color col,float fs)
    {
        var box=MkBox(p,x,y,w,h,col);var btn=box.gameObject.AddComponent<Button>();btn.targetGraphic=box.GetComponent<Image>();
        var cb=btn.colors;cb.highlightedColor=Color.Lerp(col,Color.white,.2f);btn.colors=cb;
        T(box,label,0,-h/2+fs*.65f,w,fs*1.8f,fs,FontStyles.Bold,Color.white,TextAlignmentOptions.Center);return btn;
    }

    TextMeshProUGUI T(RectTransform p,string text,float x,float y,float w,float h,float size,FontStyles style,Color col,TextAlignmentOptions align)
    {
        var go=new GameObject("T");go.transform.SetParent(p,false);
        var t=go.AddComponent<TextMeshProUGUI>();t.text=text;t.fontSize=size;t.fontStyle=style;t.color=col;t.alignment=align;t.enableWordWrapping=true;
        var rt=go.GetComponent<RectTransform>();
        rt.anchorMin=rt.anchorMax=new Vector2(.5f,.5f);rt.pivot=new Vector2(.5f,.5f);
        rt.anchoredPosition=new Vector2(x,y);rt.sizeDelta=new Vector2(w,h);return t;
    }
}
