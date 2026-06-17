using UnityEngine;

public class NodeClicker : MonoBehaviour
{
    public int Code;

    // ── Desktop (mouse) ──────────────────────────────
    void OnMouseDown()
    {
        SimulatorController.Instance?.OnNodeClicked(Code);
    }

    void OnMouseEnter()
    {
        if (GraphMap.Instance.Nodes.TryGetValue(Code, out var nd))
            TooltipUI.Instance?.Show(nd, Input.mousePosition);
    }

    void OnMouseExit()  => TooltipUI.Instance?.Hide();

    void OnMouseOver()
    {
        if (GraphMap.Instance.Nodes.TryGetValue(Code, out var nd))
            TooltipUI.Instance?.Show(nd, Input.mousePosition);
    }


}
