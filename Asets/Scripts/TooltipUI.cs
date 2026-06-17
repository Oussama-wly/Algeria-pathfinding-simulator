using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipUI : MonoBehaviour
{
    public static TooltipUI Instance { get; private set; }

    GameObject      panel;
    TextMeshProUGUI titleTxt;
    TextMeshProUGUI bodyTxt;
    RectTransform   panelRT;

    static readonly Color BG_COL = new Color(0.04f, 0.07f, 0.14f, 0.93f);

    void Awake()
    {
        Instance = this;
        Build();
    }

    void Build()
    {
        var canvasGO = new GameObject("TooltipCanvas");
        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode   = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960, 540);
        scaler.matchWidthOrHeight  = 0.5f;

        panel = new GameObject("Tooltip");
        panel.transform.SetParent(canvasGO.transform, false);

        var img    = panel.AddComponent<Image>();
        img.color  = BG_COL;
        var ol     = panel.AddComponent<Outline>();
        ol.effectColor    = new Color(0.30f, 0.55f, 1.00f, 0.55f);
        ol.effectDistance = new Vector2(1.5f, 1.5f);

        panelRT       = panel.GetComponent<RectTransform>();
        panelRT.pivot = new Vector2(0f, 1f);

        // Title
        var tGO = new GameObject("T"); tGO.transform.SetParent(panel.transform, false);
        titleTxt           = tGO.AddComponent<TextMeshProUGUI>();
        titleTxt.fontSize  = 14;
        titleTxt.fontStyle = FontStyles.Bold;
        titleTxt.color     = new Color(0.00f, 0.85f, 1.00f);
        titleTxt.alignment = TextAlignmentOptions.Left;
        var tRT = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = new Vector2(0,1); tRT.anchorMax = new Vector2(1,1);
        tRT.pivot = new Vector2(0,1);
        tRT.anchoredPosition = new Vector2(10, -10);
        tRT.sizeDelta = new Vector2(-20, 22);

        // Divider
        var dGO  = new GameObject("D"); dGO.transform.SetParent(panel.transform, false);
        dGO.AddComponent<Image>().color = new Color(0.25f, 0.45f, 0.80f, 0.45f);
        var dRT  = dGO.GetComponent<RectTransform>();
        dRT.anchorMin = new Vector2(0,1); dRT.anchorMax = new Vector2(1,1);
        dRT.pivot = new Vector2(0,1);
        dRT.anchoredPosition = new Vector2(8, -34);
        dRT.sizeDelta = new Vector2(-16, 1);

        // Body
        var bGO = new GameObject("B"); bGO.transform.SetParent(panel.transform, false);
        bodyTxt                    = bGO.AddComponent<TextMeshProUGUI>();
        bodyTxt.fontSize           = 10;
        bodyTxt.color              = new Color(0.80f, 0.90f, 1.00f);
        bodyTxt.alignment          = TextAlignmentOptions.Left;
        bodyTxt.enableWordWrapping = true;
        var bRT = bGO.GetComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0,1); bRT.anchorMax = new Vector2(1,0);
        bRT.pivot     = new Vector2(0,1);
        bRT.anchoredPosition = new Vector2(10, -38);
        bRT.sizeDelta        = new Vector2(-20, -48);

        panel.SetActive(false);
    }

    public void Show(NodeData nd, Vector2 screenPos)
    {
        titleTxt.text = $"W{nd.Code:D2} — {nd.Name}";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Wilaya #{nd.Code}");
        sb.AppendLine($"Neighbors ({nd.Neighbors.Count}):");
        foreach (var (nb, dist) in nd.Neighbors)
        {
            if (GraphMap.Instance.Nodes.TryGetValue(nb, out var nbNd))
                sb.AppendLine($"  • {nbNd.Name}  ({dist:0} km)");
        }

        bodyTxt.text = sb.ToString().TrimEnd();

        int   lines = nd.Neighbors.Count + 2;
        float w     = 200f;
        float h     = 42 + lines * 14;
        panelRT.sizeDelta = new Vector2(w, h);

        // Convert screen pos to canvas space (canvas pivot is bottom-left)
        float cx = screenPos.x + 16;
        float cy = screenPos.y - 16;

        // Keep inside screen
        if (cx + w > Screen.width)  cx = screenPos.x - w - 4;
        if (cy - h < 0)             cy = screenPos.y + h + 4;

        // Convert to anchored position (canvas anchor = bottom-left)
        panelRT.position = new Vector3(cx, cy, 0);
        panel.SetActive(true);
    }

    void Update()
    {
        if (panel.activeSelf)
        {
            float mx = Input.mousePosition.x;
            float my = Input.mousePosition.y;
            float w  = panelRT.sizeDelta.x;
            float h  = panelRT.sizeDelta.y;

            float cx = mx + 16;
            float cy = my - 16;

            // Keep fully inside screen on all 4 sides
            if (cx + w > Screen.width)  cx = mx - w - 4;
            if (cx < 0)                 cx = 4;
            if (cy - h < 0)             cy = my + h + 4;
            if (cy > Screen.height)     cy = Screen.height - 4;

            panelRT.position = new Vector3(cx, cy, 0);
        }
    }

    public void Hide() => panel.SetActive(false);
}
