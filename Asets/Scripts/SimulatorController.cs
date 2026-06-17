using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimulatorController : MonoBehaviour
{
    public static SimulatorController Instance { get; private set; }

    static readonly Color COLOR_START = new Color(0f, 1f, 0.6f);
    static readonly Color COLOR_GOAL  = new Color(1f, 0.18f, 0.33f);
    static readonly Color COLOR_PATH  = new Color(1f, 0.44f, 0.13f);

    NodeData    startNode, goalNode;
    int         selectedAlgo = 2;
    float       stepDelay    = 0.12f;
    bool        isRunning    = false;
    bool        stopFlag     = false;
    PathRecord  lastRecord   = null;

    // Replay
    PathResult  lastResult   = null;
    string      lastAlgoName = "";

    SimulatorUI ui;
    GraphMap    map;

    AudioSource sfxController;
    AudioClip   clipVictory;

    void Awake()
    {
        Instance = this;
        // Victory sound
        sfxController = gameObject.AddComponent<AudioSource>();
        sfxController.playOnAwake = false;
        sfxController.volume = 0.55f;
        clipVictory = MakeVictory();

        var camGO = new GameObject("MainCamera"); camGO.tag = "MainCamera";
        var cam   = camGO.AddComponent<Camera>();
        cam.orthographic     = true; cam.orthographicSize = 6f;
        cam.backgroundColor  = new Color(.04f,.08f,.14f);
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f,.5f,-10f);
        camGO.AddComponent<CameraRig>();

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        var raycaster = cam.gameObject.AddComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
        raycaster.enabled = false; // disabled until menu is dismissed

        var mapGO = new GameObject("GraphMap");
        map = mapGO.AddComponent<GraphMap>();

        new GameObject("TooltipUI").AddComponent<TooltipUI>();
        new GameObject("TheoryPanel").AddComponent<TheoryPanel>();
        new GameObject("SearchTreePanel").AddComponent<SearchTreePanel>();

        var uiGO = new GameObject("SimulatorUI");
        ui = uiGO.AddComponent<SimulatorUI>();

        // Menu shown last — appears above everything
        new GameObject("MenuScreen").AddComponent<MenuScreen>();
    }

    void Start()
    {
        ui.AlgoBtns[0].onClick.AddListener(() => SelectAlgo(0));
        ui.AlgoBtns[1].onClick.AddListener(() => SelectAlgo(1));
        ui.AlgoBtns[2].onClick.AddListener(() => SelectAlgo(2));
        ui.BtnRun.onClick.AddListener(RunAlgorithm);
        ui.BtnStop.onClick.AddListener(() => stopFlag = true);
        ui.BtnReset.onClick.AddListener(ResetAll);
        ui.BtnCompare.onClick.AddListener(RunCompare);
        ui.BtnReplay.onClick.AddListener(RunReplay);
        ui.BtnHistory.onClick.AddListener(() => ui.ShowHistory(HistoryManager.Load()));
        ui.BtnHistoryClose.onClick.AddListener(ui.HideHistory);
        ui.BtnClearHistory.onClick.AddListener(() => { HistoryManager.Clear(); ui.ShowHistory(HistoryManager.Load()); });
        ui.BtnTheory.onClick.AddListener(() => TheoryPanel.Instance.Toggle());
        ui.SpeedSlider.onValueChanged.AddListener(v =>
            stepDelay = Mathf.Lerp(0.50f, 0.01f, (v-1f)/9f));
    }

    bool puzzleMode      = false;
    bool blockedRoadsMode = false;
    public void SetPuzzleMode(bool on)       { puzzleMode      = on; }
    public void SetBlockedRoadsMode(bool on) { blockedRoadsMode = on; }

    public void OnNodeClicked(int code)
    {
        // Route to puzzle if active
        if (puzzleMode && PathPuzzle.Instance != null)
        { PathPuzzle.Instance.OnWilayaClicked(code); return; }

        // Route to blocked roads if active
        if (blockedRoadsMode && BlockedRoads.Instance != null)
        { BlockedRoads.Instance.OnWilayaClicked(code); return; }

        if (isRunning) return;
        var nd = map.Nodes[code];

        if (startNode == null)
        {
            startNode = nd; map.SetColor(code, COLOR_START); map.ShowName(code, true);
            ui.UpdateSelection(startNode, goalNode);
            ui.SetMessage($"Start: {nd.Name}\nNow click the goal wilaya.");
            return;
        }
        if (goalNode == null)
        {
            if (nd == startNode)
            {
                map.SetColor(startNode.Code, new Color(.10f,.22f,.40f));
                startNode = null; ui.UpdateSelection(null, null);
                ui.SetMessage("Cancelled. Click a wilaya to set Start.");
                return;
            }
            goalNode = nd; map.SetColor(code, COLOR_GOAL); map.ShowName(code, true);
            ui.UpdateSelection(startNode, goalNode);
            ui.SetMessage($"Goal: {nd.Name}\nPress RUN to start.");
            ui.BtnRun.interactable     = true;
            ui.BtnCompare.interactable = true;
            return;
        }
        ResetAll();
        startNode = nd; map.SetColor(code, COLOR_START); map.ShowName(code, true);
        ui.UpdateSelection(startNode, goalNode);
        ui.SetMessage($"Start: {nd.Name}\nNow click the goal wilaya.");
    }

    void SelectAlgo(int idx) { selectedAlgo = idx; ui.HighlightAlgoButton(idx); }
    void RunAlgorithm() { if (!isRunning && startNode != null && goalNode != null) StartCoroutine(Execute()); }
    void RunReplay()    { if (!isRunning && lastResult != null) StartCoroutine(ReplayPath()); }

    IEnumerator Execute()
    {
        isRunning = true; stopFlag = false;
        map.ClearPathLines();
        map.ResetColors(startNode.Code, goalNode.Code);
        map.SetColor(startNode.Code, COLOR_START);
        map.SetColor(goalNode.Code,  COLOR_GOAL);

        ui.BtnRun.interactable  = false;
        ui.BtnStop.gameObject.SetActive(true);
        ui.HideStats();
        ui.SetStep(0, 0);

        string[] names = { "BFS","Dijkstra","A*" };
        ui.SetMessage($"Running {names[selectedAlgo]}...");

        PathResult result = null;

        // Measure PURE algorithm time (no animation)
        float t0 = Time.realtimeSinceStartup;
        if (selectedAlgo == 0)
            yield return Algorithms.BFS(map.Nodes, startNode.Code, goalNode.Code,
                stepDelay, () => stopFlag, r => result = r, (s,tot) => ui.SetStep(s,tot));
        else if (selectedAlgo == 1)
            yield return Algorithms.Dijkstra(map.Nodes, startNode.Code, goalNode.Code,
                stepDelay, () => stopFlag, r => result = r, (s,tot) => ui.SetStep(s,tot));
        else
            yield return Algorithms.AStar(map.Nodes, startNode.Code, goalNode.Code,
                stepDelay, () => stopFlag, r => result = r, (s,tot) => ui.SetStep(s,tot));
        float algoMs = (Time.realtimeSinceStartup - t0) * 1000f;

        if (stopFlag) { ui.SetMessage("Stopped."); FinishRun(); yield break; }
        if (result == null || !result.Found)
        { ui.SetMessage("No path found."); FinishRun(); yield break; }

        if (selectedAlgo == 0) result.Dist = Algorithms.PathDistance(map.Nodes, result.Path);

        // Notify search tree of solution path
        SearchTreePanel.OnPathFound(result.Path);
        //  Play victory sound
        if (sfxController && clipVictory) sfxController.PlayOneShot(clipVictory);

        foreach (int code in result.Path)
        { map.ShowName(code, true); if (code != startNode.Code && code != goalNode.Code) map.SetColor(code, COLOR_PATH); }

        for (int i = 0; i < result.Path.Count-1; i++)
        { map.AddPathLine(result.Path[i], result.Path[i+1]); yield return new WaitForSeconds(Mathf.Max(.03f, stepDelay*.7f)); }

        map.SetColor(startNode.Code, COLOR_START);
        map.SetColor(goalNode.Code,  COLOR_GOAL);

        lastResult   = result;
        lastAlgoName = names[selectedAlgo];

        ui.ShowStats(names[selectedAlgo], result.Dist, result.Visited, algoMs, result.Path, map.Nodes);

        float h = Mathf.Floor(result.Dist/90f), m = Mathf.Round((result.Dist/90f-h)*60f);
        string travel = h>0 ? $"{h:0}h {m:0}min" : $"{m:0} min";
        ui.SetMessage($"{names[selectedAlgo]}: {result.Dist:0} km — {result.Path.Count} wilayas\nTravel: {travel}");

        // Auto-save to history
        lastRecord = new PathRecord { algo=names[selectedAlgo], start=startNode.Name,
            goal=goalNode.Name, dist=result.Dist, visited=result.Visited,
            travel=travel, date=System.DateTime.Now.ToString("dd/MM HH:mm") };
        HistoryManager.Save(lastRecord);

        FinishRun();
    }

    IEnumerator ReplayPath()
    {
        if (lastResult == null || startNode == null || goalNode == null) yield break;
        isRunning = true;
        map.ClearPathLines();
        map.ResetColors(startNode.Code, goalNode.Code);
        map.SetColor(startNode.Code, COLOR_START);
        map.SetColor(goalNode.Code,  COLOR_GOAL);
        ui.BtnStop.gameObject.SetActive(true);
        ui.BtnRun.interactable = false;

        foreach (int code in lastResult.Path)
        { map.ShowName(code, true); if (code!=startNode.Code && code!=goalNode.Code) map.SetColor(code, COLOR_PATH); }

        for (int i = 0; i < lastResult.Path.Count-1; i++)
        { map.AddPathLine(lastResult.Path[i], lastResult.Path[i+1]); yield return new WaitForSeconds(Mathf.Max(.03f, stepDelay*.7f)); }

        map.SetColor(startNode.Code, COLOR_START);
        map.SetColor(goalNode.Code,  COLOR_GOAL);
        FinishRun();
    }

    void FinishRun()
    {
        isRunning = false;
        ui.BtnStop.gameObject.SetActive(false);
        ui.BtnRun.interactable     = startNode != null && goalNode != null;
        ui.BtnCompare.interactable = startNode != null && goalNode != null;
    }

    void ResetAll()
    {
        if (isRunning) { stopFlag = true; return; }
        map.ClearPathLines(); map.ResetColors();
        startNode = null; goalNode = null; lastResult = null;
        ui.UpdateSelection(null, null);
        ui.HideStats(); ui.SetStep(0,0);
        ui.BtnRun.interactable     = false;
        ui.BtnCompare.interactable = false;
        ui.BtnReplay.interactable  = false;
        ui.SetMessage("Click a wilaya to set Start.");
    }

    void RunCompare() { if (!isRunning && startNode!=null && goalNode!=null) StartCoroutine(CompareAll()); }

    IEnumerator CompareAll()
    {
        isRunning = true; stopFlag = false;
        string[] names = { "BFS","Dijkstra","A*" };
        PathRecord[] records = new PathRecord[3];
        ui.BtnRun.interactable = false; ui.BtnCompare.interactable = false;
        ui.BtnStop.gameObject.SetActive(true);

        for (int a = 0; a < 3; a++)
        {
            ui.ShowCompareProgress($"Running {names[a]}...  ({a+1} / 3)");
            map.ClearPathLines(); map.ResetColors(startNode.Code, goalNode.Code);
            map.SetColor(startNode.Code, COLOR_START); map.SetColor(goalNode.Code, COLOR_GOAL);

            PathResult result = null;
            float t0 = Time.realtimeSinceStartup;

            if (a==0) yield return Algorithms.BFS(map.Nodes, startNode.Code, goalNode.Code, .02f, ()=>stopFlag, r=>result=r, null);
            else if (a==1) yield return Algorithms.Dijkstra(map.Nodes, startNode.Code, goalNode.Code, .02f, ()=>stopFlag, r=>result=r, null);
            else yield return Algorithms.AStar(map.Nodes, startNode.Code, goalNode.Code, .02f, ()=>stopFlag, r=>result=r, null);

            if (stopFlag) break;
            float ms = (Time.realtimeSinceStartup - t0) * 1000f;
            if (a==0 && result!=null) result.Dist = Algorithms.PathDistance(map.Nodes, result.Path);

            if (result!=null && result.Found)
            {
                float h=Mathf.Floor(result.Dist/90f), m2=Mathf.Round((result.Dist/90f-h)*60f);
                records[a] = new PathRecord { algo=names[a], start=startNode.Name, goal=goalNode.Name,
                    dist=result.Dist, visited=result.Visited,
                    travel=h>0?$"{h:0}h {m2:0}min":$"{m2:0} min",
                    date=ms.ToString("0.0")+"|" };
            }
        }

        isRunning = false;
        ui.BtnStop.gameObject.SetActive(false);
        ui.BtnRun.interactable = true; ui.BtnCompare.interactable = true;

        if (!stopFlag) { ui.SetMessage("Comparison done!"); ui.ShowCompare(records); }
        else { ui.HideCompareProgress(); ui.SetMessage("Stopped."); }
    }

    // History replay — shows path on map without re-running animation
    public void ReplayFromHistory(PathRecord r)
    {
        if (GraphMap.Instance == null) return;
        GraphMap.Instance.ResetColors(-1, -1);
        GraphMap.Instance.ClearPathLines();

        int startCode = -1, goalCode = -1;
        foreach (var kv in GraphMap.Instance.Nodes)
        {
            if (kv.Value.Name == r.start) startCode = kv.Key;
            if (kv.Value.Name == r.goal)  goalCode  = kv.Key;
        }
        if (startCode < 0 || goalCode < 0) return;

        GraphMap.Instance.SetColor(startCode, new Color(.10f, .80f, .30f));
        GraphMap.Instance.SetColor(goalCode,  new Color(.90f, .15f, .15f));
        StartCoroutine(ReplayHistCo(startCode, goalCode));
    }

    System.Collections.IEnumerator ReplayHistCo(int startCode, int goalCode)
    {
        PathResult result = null;
        yield return StartCoroutine(Algorithms.Dijkstra(
            GraphMap.Instance.Nodes, startCode, goalCode,
            0f, () => false, r => result = r, null));

        if (result == null || !result.Found) yield break;
        for (int i = 0; i < result.Path.Count - 1; i++)
            GraphMap.Instance.AddPathLine(result.Path[i], result.Path[i + 1]);

        var n0 = GraphMap.Instance.Nodes[startCode].Name;
        var n1 = GraphMap.Instance.Nodes[goalCode].Name;
        SimulatorUI.Instance?.SetMessage($"History: {n0} -> {n1}");
    }

    // Procedural victory arpeggio
    static AudioClip MakeVictory()
    {
        int sr = 44100;
        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f }; // C5 E5 G5 C6
        float noteDur = 0.12f;
        float totalDur = notes.Length * noteDur + 0.3f;
        int n = (int)(sr * totalDur);
        var d = new float[n];
        for (int ni = 0; ni < notes.Length; ni++)
        {
            float freq = notes[ni];
            int start = (int)(ni * noteDur * sr);
            int end   = Mathf.Min(start + (int)(noteDur * sr), n);
            for (int i = start; i < end; i++)
            {
                float t   = (float)(i - start) / sr;
                float env = Mathf.Exp(-t / 0.09f);
                d[i] += Mathf.Sin(2 * Mathf.PI * freq * t) * env * 0.45f;
            }
        }
        var clip = AudioClip.Create("victory", n, 1, sr, false);
        clip.SetData(d, 0);
        return clip;
    }
}