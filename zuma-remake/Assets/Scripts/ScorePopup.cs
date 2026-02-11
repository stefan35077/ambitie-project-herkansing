using TMPro;
using UnityEngine;

public class ScorePopup : MonoBehaviour
{
    [Header("Refs")]
    public Canvas canvas;                 // your UI canvas
    public RectTransform popupParent;     // usually a full-screen panel under the canvas
    public TMP_Text popupPrefab;          // ScorePopupText prefab

    [Header("Anim")]
    public float lifetime = 0.8f;
    public float floatUpPixels = 90f;
    public float startScale = 1f;
    public float endScale = 1.15f;

    static ScorePopup _instance;
    Camera _cam;

    void Awake()
    {
        _instance = this;
        _cam = Camera.main;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (!popupParent) popupParent = canvas.transform as RectTransform;
    }

    public static void Spawn(Vector3 worldPos, int points)
    {
        if (!_instance) return;
        _instance.SpawnInternal(worldPos, points);
    }

    void SpawnInternal(Vector3 worldPos, int points)
    {
        if (!_cam) _cam = Camera.main;

        Vector3 screen = _cam.WorldToScreenPoint(worldPos);
        if (screen.z < 0f) return;

        TMP_Text t = Instantiate(popupPrefab, popupParent);
        t.text = $"+{points}";
        t.alpha = 1f;

        RectTransform rt = t.rectTransform;

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // BEST for overlay: direct screen position (no giant offsets)
            rt.position = screen;
        }
        else
        {
            // ScreenSpaceCamera / WorldSpace: convert properly
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                popupParent,
                screen,
                canvas.worldCamera ? canvas.worldCamera : _cam,
                out Vector2 localPos
            );
            rt.anchoredPosition = localPos;
        }

        rt.localScale = Vector3.one * startScale;
        StartCoroutine(Animate(rt, t));
    }

    System.Collections.IEnumerator Animate(RectTransform rt, TMP_Text t)
    {
        Vector2 start = rt.anchoredPosition;
        Vector2 end = start + Vector2.up * floatUpPixels;

        float time = 0f;
        while (time < lifetime)
        {
            time += Time.deltaTime;
            float u = Mathf.Clamp01(time / lifetime);

            // smooth
            float s = u * u * (3f - 2f * u);

            rt.anchoredPosition = Vector2.LerpUnclamped(start, end, s);
            rt.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, s);
            t.alpha = 1f - s;

            yield return null;
        }

        Destroy(t.gameObject);
    }
}