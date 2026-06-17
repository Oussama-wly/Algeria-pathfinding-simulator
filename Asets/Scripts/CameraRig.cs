using UnityEngine;

public class CameraRig : MonoBehaviour
{
    Camera  cam;
    bool    panning;
    Vector3 panOrigin, camOrigin;

    // pinch state
    float   pinchStartDist;
    float   pinchStartSize;

    void Awake() { cam = GetComponent<Camera>(); }

    void Update()
    {
        if (Input.touchCount == 2)
        {
            HandlePinch();
            HandleTwoPanTouch();
        }
        else if (Input.touchCount == 0)
        {
            HandleScrollZoom();
            HandleMousePan();
        }
        // single touch pan (one finger drag on empty space)
        else if (Input.touchCount == 1)
            HandleOneTouchPan();
    }

    // ── Desktop
    void HandleScrollZoom()
    {
        float scroll = 0f;
        try { scroll = Input.GetAxis("Mouse ScrollWheel"); } catch { }
        if (Mathf.Abs(scroll) < 0.001f) return;
        cam.orthographicSize = Mathf.Clamp(
            cam.orthographicSize - scroll * cam.orthographicSize, 1.5f, 15f);
    }

    void HandleMousePan()
    {
        try
        {
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                panning   = true;
                panOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
                camOrigin = cam.transform.position;
            }
            if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                panning = false;
            if (panning)
            {
                var delta = panOrigin - cam.ScreenToWorldPoint(Input.mousePosition);
                cam.transform.position = camOrigin + new Vector3(delta.x, delta.y, 0f);
            }
        }
        catch { }
    }

    // ── Mobile: pinch to zoom 
    void HandlePinch()
    {
        var t0 = Input.GetTouch(0);
        var t1 = Input.GetTouch(1);

        if (t0.phase == TouchPhase.Began || t1.phase == TouchPhase.Began)
        {
            pinchStartDist = Vector2.Distance(t0.position, t1.position);
            pinchStartSize = cam.orthographicSize;
        }
        else
        {
            float dist = Vector2.Distance(t0.position, t1.position);
            if (pinchStartDist > 0.1f)
            {
                float newSize = pinchStartSize * (pinchStartDist / dist);
                cam.orthographicSize = Mathf.Clamp(newSize, 1.5f, 15f);
            }
        }
    }

    // ── Mobile: two-finger pan
    void HandleTwoPanTouch()
    {
        var t0 = Input.GetTouch(0);
        var t1 = Input.GetTouch(1);
        if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
        {
            Vector2 midCurr  = (t0.position + t1.position) * 0.5f;
            Vector2 midPrev  = ((t0.position - t0.deltaPosition) +
                                (t1.position - t1.deltaPosition)) * 0.5f;
            Vector3 wCurr    = cam.ScreenToWorldPoint(midCurr);
            Vector3 wPrev    = cam.ScreenToWorldPoint(midPrev);
            Vector3 delta    = wPrev - wCurr;
            cam.transform.position += new Vector3(delta.x, delta.y, 0f);
        }
    }

    // ── Mobile: one-finger pan (only if not on a node) ──
    Vector2 oneTouchOriginScreen;
    Vector3 oneTouchCamOrigin;
    bool    oneTouchPanning;

    void HandleOneTouchPan()
    {
        var t = Input.GetTouch(0);
        if (t.phase == TouchPhase.Began)
        {
            oneTouchPanning      = true;
            oneTouchOriginScreen = t.position;
            oneTouchCamOrigin    = cam.transform.position;
        }

        if (oneTouchPanning && t.phase == TouchPhase.Moved)
        {
            Vector3 wCurr  = cam.ScreenToWorldPoint(t.position);
            Vector3 wStart = cam.ScreenToWorldPoint(oneTouchOriginScreen);
            Vector3 delta  = wStart - wCurr;
            cam.transform.position = oneTouchCamOrigin + new Vector3(delta.x, delta.y, 0f);
        }

        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            oneTouchPanning = false;
    }
}
