using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 툴팁 UI를 제어하는 싱글톤 컨트롤러
public class TooltipController : MonoBehaviour
{
    #region Singleton
    private static TooltipController instance;
    public static TooltipController Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<TooltipController>();

                if (instance == null)
                {
                    GameObject tooltipObj = new GameObject("TooltipController");
                    instance = tooltipObj.AddComponent<TooltipController>();
                }
            }
            return instance;
        }
    }
    #endregion

    [Header("Tooltip UI References")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI tooltipText;
    [SerializeField] private RectTransform tooltipRect;

    [Header("Settings")]
    [SerializeField] private Vector2 offsetTop = new Vector2(0f, 10f);
    [SerializeField] private Vector2 offsetBottom = new Vector2(0f, -10f);
    [SerializeField] private Vector2 offsetLeft = new Vector2(-10f, 0f);
    [SerializeField] private Vector2 offsetRight = new Vector2(10f, 0f);
    [SerializeField] private float fadeSpeed = 10f;

    private CanvasGroup canvasGroup;
    [SerializeField] private Canvas canvas;
    private bool isShowing = false;
    private RectTransform targetRect;
    private BlockTypeTooltip.TooltipDirection currentDirection;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }

        InitializeTooltip();
    }

    private void InitializeTooltip()
    {
        // 툴팁 패널이 없으면 자동 생성
        if (tooltipPanel == null)
        {
            CreateTooltipUI();
        }

        if (tooltipPanel != null)
        {
            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
            }

            // 툴팁이 마우스 이벤트를 차단하지 않도록 설정
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            tooltipRect = tooltipPanel.GetComponent<RectTransform>();
            tooltipText = tooltipPanel.GetComponentInChildren<TextMeshProUGUI>();

            HideTooltip();
        }
    }

    private void CreateTooltipUI()
    {
        // Canvas 찾기 또는 생성
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[TooltipController] Canvas를 찾을 수 없습니다!");
            return;
        }

        // 툴팁 패널 생성
        tooltipPanel = new GameObject("TooltipPanel");
        tooltipPanel.transform.SetParent(canvas.transform, false);

        tooltipRect = tooltipPanel.AddComponent<RectTransform>();
        tooltipRect.sizeDelta = new Vector2(300f, 150f);

        var image = tooltipPanel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        image.raycastTarget = false;

        canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        // 텍스트 오브젝트 생성
        GameObject textObj = new GameObject("TooltipText");
        textObj.transform.SetParent(tooltipPanel.transform, false);

        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10f, 10f);
        textRect.offsetMax = new Vector2(-10f, -10f);

        tooltipText = textObj.AddComponent<TextMeshProUGUI>();
        tooltipText.fontSize = 14;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.enableWordWrapping = true;
        tooltipText.raycastTarget = false;

        Debug.Log("[TooltipController] 툴팁 UI 자동 생성 완료");
    }

    private void Update()
    {
        if (isShowing && tooltipPanel != null)
        {
            // 타겟에 따라 툴팁 위치 업데이트
            UpdateTooltipPosition();

            // 페이드 인 효과
            if (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, fadeSpeed * Time.deltaTime);
            }
        }
    }

    private void UpdateTooltipPosition()
    {
        if (tooltipRect == null || canvas == null) return;

        Vector2 targetPosition;
        Vector2 offset;

        // 타겟이 있으면 타겟 기준, 없으면 마우스 기준
        if (targetRect != null)
        {
            targetPosition = targetRect.position;
        }
        else
        {
            targetPosition = Input.mousePosition;
        }

        // 방향에 따른 오프셋 결정
        offset = GetOffsetByDirection(currentDirection, targetPosition);

        Vector2 finalPos = targetPosition + offset;
        Vector2 tooltipSize = tooltipRect.sizeDelta;

        // 화면 경계 체크 및 자동 조정
        finalPos = ClampToScreen(finalPos, tooltipSize);

        tooltipRect.position = finalPos;
    }

    private Vector2 GetOffsetByDirection(BlockTypeTooltip.TooltipDirection direction, Vector2 targetPos)
    {
        if (direction == BlockTypeTooltip.TooltipDirection.Auto)
        {
            // 화면 중앙 기준으로 자동 결정
            float screenCenterX = Screen.width * 0.5f;
            float screenCenterY = Screen.height * 0.5f;

            bool isRight = targetPos.x < screenCenterX;
            bool isTop = targetPos.y < screenCenterY;

            if (isRight && isTop)
                return offsetRight + offsetTop;
            else if (isRight && !isTop)
                return offsetRight + offsetBottom;
            else if (!isRight && isTop)
                return offsetLeft + offsetTop;
            else
                return offsetLeft + offsetBottom;
        }

        switch (direction)
        {
            case BlockTypeTooltip.TooltipDirection.Top:
                return offsetTop;
            case BlockTypeTooltip.TooltipDirection.Bottom:
                return offsetBottom;
            case BlockTypeTooltip.TooltipDirection.Left:
                return offsetLeft;
            case BlockTypeTooltip.TooltipDirection.Right:
                return offsetRight;
            case BlockTypeTooltip.TooltipDirection.TopLeft:
                return offsetLeft + offsetTop;
            case BlockTypeTooltip.TooltipDirection.TopRight:
                return offsetRight + offsetTop;
            case BlockTypeTooltip.TooltipDirection.BottomLeft:
                return offsetLeft + offsetBottom;
            case BlockTypeTooltip.TooltipDirection.BottomRight:
                return offsetRight + offsetBottom;
            default:
                return Vector2.zero;
        }
    }

    private Vector2 ClampToScreen(Vector2 position, Vector2 size)
    {
        float padding = 10f;
        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        // 오른쪽 경계
        if (position.x + halfWidth > Screen.width - padding)
        {
            position.x = Screen.width - halfWidth - padding;
        }

        // 왼쪽 경계
        if (position.x - halfWidth < padding)
        {
            position.x = halfWidth + padding;
        }

        // 위쪽 경계
        if (position.y + halfHeight > Screen.height - padding)
        {
            position.y = Screen.height - halfHeight - padding;
        }

        // 아래쪽 경계
        if (position.y - halfHeight < padding)
        {
            position.y = halfHeight + padding;
        }

        return position;
    }

    public void ShowTooltip(string text, BlockTypeTooltip.TooltipDirection direction = BlockTypeTooltip.TooltipDirection.Auto, RectTransform target = null)
    {
        if (tooltipPanel == null || tooltipText == null) return;

        tooltipText.text = text;
        currentDirection = direction;
        targetRect = target;

        tooltipPanel.SetActive(true);
        canvasGroup.alpha = 0f;
        isShowing = true;

        // 텍스트 크기에 맞게 패널 크기 조정
        Canvas.ForceUpdateCanvases();
        Vector2 textSize = tooltipText.GetPreferredValues();
        tooltipRect.sizeDelta = new Vector2(
            Mathf.Min(textSize.x + 20f, 400f),
            Mathf.Min(textSize.y + 20f, 300f)
        );
    }

    public void HideTooltip()
    {
        if (tooltipPanel != null)
        {
            tooltipPanel.SetActive(false);
            isShowing = false;
            canvasGroup.alpha = 0f;
            targetRect = null;
        }
    }
}