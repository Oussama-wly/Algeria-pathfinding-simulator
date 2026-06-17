using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Draws a simple bar chart using Unity UI RectTransforms.
/// Call Draw() once to populate.
/// </summary>
public class BarChart : MonoBehaviour
{
    // ── public setup ─────────────────────────────────────────
    public string   ChartTitle  = "Chart";
    public string   YAxisLabel  = "";
    public string[] GroupLabels;          // one per bar-group (algo names)
    public string[] SeriesLabels;         // one per metric
    public float[,] Values;              // [group, series]
    public Color[]  SeriesColors;

    // ── layout constants ─────────────────────────────────────
    const float PAD_L  = 52f;
    const float PAD_R  = 16f;
    const float PAD_T  = 36f;
    const float PAD_B  = 48f;
    const float BAR_GAP = 6f;
    const float GROUP_GAP = 18f;

    // ── internal ─────────────────────────────────────────────
    RectTransform rt;

    void Awake() { rt = GetComponent<RectTransform>(); }

    public void Draw()
    {
        // Clear previous children
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        if (Values == null) return;

        int groups  = Values.GetLength(0);
        int series  = Values.GetLength(1);

        float W = rt.rect.width;
        float H = rt.rect.height;
        float chartW = W - PAD_L - PAD_R;
        float chartH = H - PAD_T - PAD_B;

        // Max value (per series, normalized separately)
        float[] maxVal = new float[series];
        for (int s = 0; s < series; s++)
        {
            maxVal[s] = 0.001f;
            for (int g = 0; g < groups; g++)
                if (Values[g, s] > maxVal[s]) maxVal[s] = Values[g, s];
        }

        // Bar width
        float totalBars = groups * series;
        float barW = (chartW - GROUP_GAP * (groups - 1) - BAR_GAP * (groups * (series - 1)))
                     / totalBars;
        barW = Mathf.Max(barW, 8f);

        // Title
        MakeTxt(ChartTitle, W / 2f, H - PAD_T / 2f, W, 18f, 11f, FontStyles.Bold,
            new Color(.85f,.95f,1f), TextAlignmentOptions.Center);

        // Y-axis label
        if (!string.IsNullOrEmpty(YAxisLabel))
        {
            var yLbl = MakeTxt(YAxisLabel, 8f, H / 2f, 20f, chartH, 8f,
                FontStyles.Normal, new Color(.55f,.70f,.90f), TextAlignmentOptions.Center);
            yLbl.rectTransform.localEulerAngles = new Vector3(0, 0, 90f);
        }

        // Y grid lines + labels (5 steps)
        for (int step = 0; step <= 4; step++)
        {
            float fy  = step / 4f;
            float py  = PAD_B + fy * chartH;

            // grid line
            MakeLine(PAD_L, py, W - PAD_R, py,
                new Color(.25f,.35f,.55f, step == 0 ? .9f : .35f), step == 0 ? 1.5f : 0.8f);
        }

        // Draw bars
        for (int g = 0; g < groups; g++)
        {
            float groupX = PAD_L + g * (barW * series + BAR_GAP * (series - 1) + GROUP_GAP);

            for (int s = 0; s < series; s++)
            {
                float val    = Values[g, s];
                float norm   = val / maxVal[s];
                float bh     = norm * chartH;
                float bx     = groupX + s * (barW + BAR_GAP);
                float by     = PAD_B;
                Color col    = s < SeriesColors.Length ? SeriesColors[s] : Color.white;

                // Bar body
                MakeBar(bx, by, barW, bh, col);

                // Value label on top of bar
                string valStr = val >= 1000 ? $"{val/1000f:0.0}k"
                              : val >= 10   ? $"{val:0}"
                              :               $"{val:0.0}";
                MakeTxt(valStr, bx + barW/2f, by + bh + 6f, barW + 10f, 14f, 7.5f,
                    FontStyles.Bold, col, TextAlignmentOptions.Center);
            }

            // Group label (algo name)
            float labelX = groupX + (barW * series + BAR_GAP * (series-1)) / 2f;
            MakeTxt(GroupLabels[g], labelX, PAD_B - 14f, barW * series + 20f, 16f, 9f,
                FontStyles.Bold, new Color(.80f,.90f,1f), TextAlignmentOptions.Center);
        }

        // Series legend (bottom right)
        float lx = W - PAD_R - series * 90f;
        for (int s = 0; s < series; s++)
        {
            Color c = s < SeriesColors.Length ? SeriesColors[s] : Color.white;
            MakeDot(lx + s * 90f + 4f, 14f, 10f, c);
            MakeTxt(SeriesLabels[s], lx + s * 90f + 18f, 14f, 80f, 14f, 8f,
                FontStyles.Normal, new Color(.75f,.85f,.95f), TextAlignmentOptions.Left);
        }
    }

    // ── helpers ──────────────────────────────────────────────

    TextMeshProUGUI MakeTxt(string text, float x, float y, float w, float h,
        float size, FontStyles style, Color col, TextAlignmentOptions align)
    {
        var go = new GameObject("T"); go.transform.SetParent(transform, false);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.fontStyle = style;
        t.color = col; t.alignment = align;
        var r  = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = new Vector2(.5f,.5f);
        r.anchoredPosition = new Vector2(x, y); r.sizeDelta = new Vector2(w, h);
        return t;
    }

    void MakeBar(float x, float y, float w, float h, Color col)
    {
        // shadow
        var sh = new GameObject("Sh"); sh.transform.SetParent(transform, false);
        sh.AddComponent<Image>().color = new Color(0,0,0,.25f);
        var sr = sh.GetComponent<RectTransform>();
        sr.anchorMin = sr.anchorMax = Vector2.zero; sr.pivot = new Vector2(.5f,0f);
        sr.anchoredPosition = new Vector2(x + w/2f + 2f, y - 2f);
        sr.sizeDelta = new Vector2(w, h);

        // bar
        var go = new GameObject("B"); go.transform.SetParent(transform, false);
        var img = go.AddComponent<Image>(); img.color = col;
        var r   = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = new Vector2(.5f,0f);
        r.anchoredPosition = new Vector2(x + w/2f, y); r.sizeDelta = new Vector2(w, h);

        // highlight (top edge)
        var hl = new GameObject("HL"); hl.transform.SetParent(transform, false);
        hl.AddComponent<Image>().color = new Color(1,1,1,.18f);
        var hr = hl.GetComponent<RectTransform>();
        hr.anchorMin = hr.anchorMax = Vector2.zero; hr.pivot = new Vector2(.5f,0f);
        hr.anchoredPosition = new Vector2(x + w/2f, y + h - 2f); hr.sizeDelta = new Vector2(w, 2f);
    }

    void MakeLine(float x1, float y1, float x2, float y2, Color col, float thickness = 1f)
    {
        var go = new GameObject("L"); go.transform.SetParent(transform, false);
        var img = go.AddComponent<Image>(); img.color = col;
        var r   = go.GetComponent<RectTransform>();
        float cx = (x1+x2)/2f, cy = (y1+y2)/2f;
        float len = Mathf.Sqrt((x2-x1)*(x2-x1)+(y2-y1)*(y2-y1));
        float angle = Mathf.Atan2(y2-y1, x2-x1)*Mathf.Rad2Deg;
        r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = new Vector2(.5f,.5f);
        r.anchoredPosition = new Vector2(cx,cy); r.sizeDelta = new Vector2(len, thickness);
        r.localEulerAngles = new Vector3(0,0,angle);
    }

    void MakeDot(float x, float y, float size, Color col)
    {
        var go = new GameObject("D"); go.transform.SetParent(transform, false);
        go.AddComponent<Image>().color = col;
        var r  = go.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = Vector2.zero; r.pivot = new Vector2(0,.5f);
        r.anchoredPosition = new Vector2(x,y); r.sizeDelta = new Vector2(size,size);
    }
}
