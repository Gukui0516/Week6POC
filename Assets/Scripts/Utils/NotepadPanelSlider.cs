using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NotepadPanelSlider : MonoBehaviour
{
    public enum SlideFrom { Right, Left, Top, Bottom }
    public enum TargetMode { CanvasCenter, CustomAnchored }

    [Header("Target")]
    [SerializeField] private RectTransform panel;           // 이 스크립트가 붙은 패널. 비우면 자동
    [SerializeField] private TargetMode targetMode = TargetMode.CanvasCenter;
    [SerializeField] private Vector2 customShownAnchoredPos = Vector2.zero; // Custom일 때 목표 좌표

    [Header("Direction")]
    [SerializeField] private SlideFrom slideFrom = SlideFrom.Right;
    [SerializeField] private float offscreenMargin = 40f;   // 화면 밖으로 더 밀어낼 여유

    [Header("Motion")]
    [SerializeField, Min(0.05f)] private float animDuration = 0.25f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0,0,1,1);
    [SerializeField] private bool useUnscaledTime = true;

    [Header("Input (옵션)")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Start")]
    [SerializeField] private bool startHidden = true;       // 시작 시 숨김

    private RectTransform _parent;
    private Vector2 _shownPos;
    private Vector2 _hiddenPos;
    private bool _visible;
    private Coroutine _motion;

    void Reset()
    {
        panel = GetComponent<RectTransform>();
    }

    void Awake()
    {
        if (panel == null) panel = GetComponent<RectTransform>();
        _parent = panel.parent as RectTransform;

        // 목표 위치 계산
        _shownPos = (targetMode == TargetMode.CanvasCenter) ? Vector2.zero : customShownAnchoredPos;

        // 숨김 위치 자동 계산
        _hiddenPos = ComputeHiddenPos(_parent, panel, _shownPos, slideFrom, offscreenMargin);

        // 시작 배치
        if (startHidden) {
            panel.anchoredPosition = _hiddenPos;
            _visible = false;
        } else {
            panel.anchoredPosition = _shownPos;
            _visible = true;
        }
    }

    void Update()
    {
        if (toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        SetVisible(!_visible);
    }

    public void Show()  => SetVisible(true);
    public void Hide()  => SetVisible(false);

    public void SetVisible(bool show)
    {
        if (_motion != null) StopCoroutine(_motion);
        _motion = StartCoroutine(CoSlide(show));
        _visible = show;
    }

    private IEnumerator CoSlide(bool show)
    {
        var from = panel.anchoredPosition;
        var to   = show ? _shownPos : _hiddenPos;

        float t = 0f;
        float dur = Mathf.Max(0.0001f, animDuration);

        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            panel.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
            yield return null;
        }
        panel.anchoredPosition = to;
    }

    public void RecomputePositions()
    {
        // 에디터/런타임에서 방향이나 목표를 바꿨을 때 호출
        _shownPos = (targetMode == TargetMode.CanvasCenter) ? Vector2.zero : customShownAnchoredPos;
        _hiddenPos = ComputeHiddenPos(_parent, panel, _shownPos, slideFrom, offscreenMargin);
        if (!_visible) panel.anchoredPosition = _hiddenPos;
        else panel.anchoredPosition = _shownPos;
    }

    private static Vector2 ComputeHiddenPos(RectTransform parent, RectTransform panel, Vector2 shown, SlideFrom from, float margin)
    {
        // 부모와 패널은 모두 피벗 0.5/0.5, 앵커 중앙 기준이라고 가정하면 가장 깔끔함
        var pRect = parent.rect;
        var rRect = panel.rect;

        float x = shown.x, y = shown.y;

        float halfParentW = pRect.width * 0.5f;
        float halfParentH = pRect.height * 0.5f;
        float halfPanelW  = rRect.width * 0.5f;
        float halfPanelH  = rRect.height * 0.5f;

        switch (from)
        {
            case SlideFrom.Right:
                x = shown.x + halfParentW + halfPanelW + margin;
                break;
            case SlideFrom.Left:
                x = shown.x - (halfParentW + halfPanelW + margin);
                break;
            case SlideFrom.Top:
                y = shown.y + halfParentH + halfPanelH + margin;
                break;
            case SlideFrom.Bottom:
                y = shown.y - (halfParentH + halfPanelH + margin);
                break;
        }
        return new Vector2(x, y);
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (panel == null) panel = GetComponent<RectTransform>();
        if (panel != null)
        {
            var parent = panel.parent as RectTransform;
            if (parent != null)
            {
                var show = (targetMode == TargetMode.CanvasCenter) ? Vector2.zero : customShownAnchoredPos;
                var hide = ComputeHiddenPos(parent, panel, show, slideFrom, offscreenMargin);
                // 에디터 미리보기
                if (!Application.isPlaying)
                    panel.anchoredPosition = startHidden ? hide : show;
            }
        }
    }
#endif
}
