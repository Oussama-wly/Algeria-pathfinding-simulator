using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MenuScreen : MonoBehaviour
{
    static readonly Color BG = new Color(.03f, .06f, .13f);
    static readonly Color CYAN = new Color(.00f, .78f, .92f);
    static readonly Color WHITE = new Color(.88f, .93f, 1f);
    static readonly Color MUTED = new Color(.35f, .50f, .70f);
    static readonly Color G_PRI = new Color(.04f, .32f, .16f);
    static readonly Color G_SEC = new Color(.07f, .12f, .25f);

    Canvas cv; GameObject menuGO, aboutCard;

    void Awake()
    {
        var cvGO = new GameObject("MenuCanvas");
        cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 110;
        var sc = cvGO.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280, 720);
        sc.matchWidthOrHeight = 0.5f;
        cvGO.AddComponent<GraphicRaycaster>();
        Build(cvGO.transform);
    }

    void Build(Transform root)
    {
        menuGO = new GameObject("M");
        menuGO.transform.SetParent(root, false);
        menuGO.AddComponent<Image>().color = BG;
        var rt = menuGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // ── Background
        Circle(rt, 520, 200, 340, new Color(CYAN.r, CYAN.g, CYAN.b, .04f));
        Circle(rt, -480, -220, 200, new Color(CYAN.r, CYAN.g, CYAN.b, .03f));
        Rect(rt, 0, 0, 1280, 1, new Color(CYAN.r, CYAN.g, CYAN.b, .06f));

        // ── LEFT — Branding 
        Rect(rt, -308, 0, 3, 420, new Color(CYAN.r, CYAN.g, CYAN.b, .70f));

        T(rt, "ALGERIA", -220, 148, 500, 70, 52, FontStyles.Bold, WHITE, TextAlignmentOptions.Left);
        T(rt, "PATHFINDING", -220, 78, 500, 36, 22, FontStyles.Bold, CYAN, TextAlignmentOptions.Left);
        T(rt, "SIMULATOR", -220, 44, 500, 36, 22, FontStyles.Bold, CYAN, TextAlignmentOptions.Left);

        Rect(rt, -220, -2, 380, 1, new Color(CYAN.r, CYAN.g, CYAN.b, .30f));

        T(rt, "Visualize BFS, Dijkstra, and A*\n" +
              "on Algeria’s real 58-wilaya road network",
              -275, -38, 390, 42, 9, FontStyles.Italic, MUTED, TextAlignmentOptions.Left);

        // Feature tags
        string[] tags = { "Real Data", "Step-by-step", "Open Source" };
        float tx = -220;
        foreach (var tag in tags)
        {
            float tw = tag.Length * 6.5f + 18;
            Tag(rt, tag, tx + tw / 2, -102, tw, 24);
            tx += tw + 8;
        }

        T(rt, "   C# · Unity 2022 LTS · ",
            -220, -165, 390, 13, 7, FontStyles.Italic,
            new Color(.20f, .30f, .48f), TextAlignmentOptions.Left);

        // ── RIGHT — Buttons
        float bx = 225f, by = 148f, bw = 295f;

        var bs = Btn(rt, "START SIMULATION", bx, by, bw, 52, G_PRI, 14);
        AddCyanBorder(bs.GetComponent<RectTransform>(), .50f);
        bs.onClick.AddListener(() => StartCoroutine(Out(null)));
        by -= 72;

        Btn(rt, "Road Network (Puzzle)", bx, by, bw, 38, G_SEC, 11)
            .onClick.AddListener(() => StartCoroutine(Out(() => PathPuzzle.Open())));
        by -= 50;

        Btn(rt, "Blocked Roads (Puzzle)", bx, by, bw, 38, G_SEC, 11)
            .onClick.AddListener(() => StartCoroutine(Out(() => BlockedRoads.Open())));
        by -= 50;

        Rect(rt, bx, by + 10, bw, 1, new Color(CYAN.r, CYAN.g, CYAN.b, .15f)); by -= 20;

        Btn(rt, "Search Tree", bx, by, bw, 34, G_SEC, 10)
            .onClick.AddListener(() => StartCoroutine(Out(() => SearchTreePanel.Toggle())));
        by -= 44;

        Btn(rt, "Theory & Algorithms", bx, by, bw, 34, G_SEC, 10)
            .onClick.AddListener(() => {
                var tp = FindObjectOfType<TheoryPanel>();
                if (tp != null) tp.Show(); else TheoryPanel.Instance?.Show();
                var tCv = GameObject.Find("TheoryCanvas")?.GetComponent<Canvas>();
                if (tCv != null) tCv.sortingOrder = 120;
            });
        by -= 44;

        Btn(rt, "About", bx, by, bw, 30, G_SEC, 10)
            .onClick.AddListener(() => aboutCard?.SetActive(!(aboutCard?.activeSelf ?? false)));

        // ── About card
        BuildAbout(rt);

        StartCoroutine(In(menuGO));
    }

    void BuildAbout(RectTransform rt)
    {
        aboutCard = new GameObject("About");
        aboutCard.transform.SetParent(rt, false);

        var aImg = aboutCard.AddComponent<Image>();
        aImg.color = new Color(.04f, .08f, .18f, .97f);

        var aRT = aboutCard.GetComponent<RectTransform>();
        aRT.anchorMin = aRT.anchorMax = new Vector2(.5f, .5f);
        aRT.pivot = new Vector2(.5f, .5f);
        aRT.anchoredPosition = Vector2.zero;
        aRT.sizeDelta = new Vector2(500, 290);

        AddCyanBorder(aRT, .30f);

        // Header bar
        var aHdr = new GameObject("AHdr");
        aHdr.transform.SetParent(aRT, false);
        aHdr.AddComponent<Image>().color = new Color(.05f, .12f, .28f);
        var aHdrRT = aHdr.GetComponent<RectTransform>();
        aHdrRT.anchorMin = new Vector2(0, 1); aHdrRT.anchorMax = new Vector2(1, 1);
        aHdrRT.offsetMin = new Vector2(0, -48); aHdrRT.offsetMax = Vector2.zero;
        T(aHdrRT, "ABOUT", 0, -12, 500, 30, 14, FontStyles.Bold, CYAN, TextAlignmentOptions.Center);

        // Divider
        Rect(aRT, 0, -48, 500, 1, new Color(CYAN.r, CYAN.g, CYAN.b, .25f));

        // Content
        T(aRT, "Algeria Pathfinding Simulator",
            0, 72, 480, 26, 13, FontStyles.Bold, WHITE, TextAlignmentOptions.Center);
        T(aRT, "Unity 2022.3 LTS  ·  C#  ·  19 scripts",
            0, 44, 480, 20, 9, FontStyles.Normal, new Color(.60f, .75f, .95f), TextAlignmentOptions.Center);
        Rect(aRT, 0, 28, 180, 1, new Color(CYAN.r, CYAN.g, CYAN.b, .20f));
        T(aRT, "BFS  —  Dijkstra  —  A*",
            0, 8, 480, 20, 11, FontStyles.Bold, CYAN, TextAlignmentOptions.Center);
        T(aRT, "58 wilayas  ·  Real road network data",
            0, -14, 480, 18, 9, FontStyles.Normal, new Color(.55f, .70f, .90f), TextAlignmentOptions.Center);
        Rect(aRT, 0, -28, 180, 1, new Color(CYAN.r, CYAN.g, CYAN.b, .20f));
        T(aRT, "Licence en Informatique  —  2025 / 2026",
            0, -50, 480, 18, 9, FontStyles.Normal, new Color(.45f, .58f, .80f), TextAlignmentOptions.Center);

        Btn(aRT, "Close", 0, -112, 110, 30, G_SEC, 10)
            .onClick.AddListener(() => aboutCard.SetActive(false));

        aboutCard.SetActive(false);
    }

    // ── Helpers

    // Plain rectangle button — NO sprite, guaranteed rectangular shape
    Button Btn(RectTransform p, string lbl, float x, float y,
               float w, float h, Color col, float fs)
    {
        var go = new GameObject("Btn_" + lbl);
        go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>();
        img.color = col;
        // NO sprite assigned → solid rectangle

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = Color.Lerp(col, new Color(.20f, .40f, .80f), .22f);
        cb.pressedColor = Color.Lerp(col, Color.black, .18f);
        cb.fadeDuration = 0.08f;
        btn.colors = cb;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);

        T(rt, lbl, 0, 0, w, h, fs, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
        return btn;
    }

    // Subtle cyan outline (1 px child image)
    void AddCyanBorder(RectTransform rt, float alpha)
    {
        var go = new GameObject("Border");
        go.transform.SetParent(rt, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(CYAN.r, CYAN.g, CYAN.b, alpha);
        img.raycastTarget = false;
        var bRT = go.GetComponent<RectTransform>();
        bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
        bRT.offsetMin = new Vector2(-1, -1); bRT.offsetMax = new Vector2(1, 1);
        go.transform.SetAsFirstSibling();
    }

    void Tag(RectTransform p, string text, float x, float y, float w, float h)
    {
        var go = new GameObject("Tag");
        go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(CYAN.r * .15f, CYAN.g * .15f, CYAN.b * .15f, .85f);
        // No sprite → rectangle
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        AddCyanBorder(rt, .28f);
        T(rt, text, 0, 0, w, h, 7.5f, FontStyles.Bold,
            new Color(CYAN.r * .5f + .5f, CYAN.g * .5f + .5f, CYAN.b * .5f + .5f),
            TextAlignmentOptions.Center);
    }

    void Label(RectTransform p, string text, float x, float y, Color col)
        => T(p, text, x, y, 200, 13, 7, FontStyles.Bold, col, TextAlignmentOptions.Left);

    void Rect(RectTransform p, float x, float y, float w, float h, Color col)
    {
        var go = new GameObject("R");
        go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>();
        img.color = col; img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
    }

    void Circle(RectTransform p, float x, float y, float s, Color col)
    {
        var go = new GameObject("C");
        go.transform.SetParent(p, false);
        var img = go.AddComponent<Image>();
        img.color = col; img.raycastTarget = false;
        img.sprite = CircSpr(64);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(s, s);
    }

    TextMeshProUGUI T(RectTransform p, string text, float x, float y,
        float w, float h, float size, FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T");
        go.transform.SetParent(p, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = col; t.alignment = align; t.enableWordWrapping = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(w, h);
        return t;
    }

    // ── Animations
    IEnumerator Out(System.Action done)
    {
        var cg = menuGO.GetComponent<CanvasGroup>() ?? menuGO.AddComponent<CanvasGroup>();
        for (float t = 0; t < .5f; t += Time.deltaTime) { cg.alpha = 1 - t / .5f; yield return null; }
        Destroy(cv.gameObject);
        var ray = Camera.main?.GetComponent<UnityEngine.EventSystems.PhysicsRaycaster>();
        if (ray) ray.enabled = true;
        done?.Invoke();
    }

    IEnumerator In(GameObject go)
    {
        var cg = go.AddComponent<CanvasGroup>(); cg.alpha = 0;
        for (float t = 0; t < .8f; t += Time.deltaTime) { cg.alpha = Mathf.Clamp01(t / .8f); yield return null; }
        cg.alpha = 1;
    }

    // ── Circle sprite
    static Sprite CircSpr(int s)
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
