using UnityEngine;
using UnityEngine.EventSystems;
using GameCore.Data;
using System.Collections;

// 블록 타입 툴팁을 표시하는 컴포넌트 (인벤토리 모드 + 타일 모드)
public class BlockTypeTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TooltipMode
    {
        Inventory,  // 블록 타입 정보
        Tile       // 점수 계산식
    }

    [Header("Tooltip Mode")]
    public TooltipMode tooltipMode = TooltipMode.Inventory;

    [Header("Block Type (For Inventory Mode)")]
    public BlockType blockType;

    [Header("Tile Position (For Tile Mode)")]
    public int tileX = -1;
    public int tileY = -1;

    [Header("Tooltip Settings")]
    public TooltipDirection preferredDirection = TooltipDirection.Auto;

    [Tooltip("마우스를 올린 후 툴팁이 표시되기까지의 시간 (초)")]
    [Range(0f, 2f)]
    public float showDelay = 0.3f;

    [Tooltip("마우스가 벗어난 후에도 툴팁을 유지할 시간 (0이면 즉시 사라짐)")]
    [Range(0f, 3f)]
    public float hideDelay = 0f;

    private Coroutine showCoroutine;
    private Coroutine hideCoroutine;
    private bool isHovering = false;

    public enum TooltipDirection
    {
        Auto,       // 자동으로 최적 위치 선택
        Top,        // 위쪽
        Bottom,     // 아래쪽
        Left,       // 왼쪽
        Right,      // 오른쪽
        TopLeft,    // 좌상단
        TopRight,   // 우상단
        BottomLeft, // 좌하단
        BottomRight // 우하단
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;

        // 이전 숨김 코루틴 정리
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // 지연 시간 후 표시
        if (showCoroutine != null)
            StopCoroutine(showCoroutine);

        showCoroutine = StartCoroutine(ShowTooltipAfterDelay());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;

        // 지연된 표시 취소
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }

        // 지속 시간 적용
        if (hideDelay > 0f)
        {
            if (hideCoroutine != null)
                StopCoroutine(hideCoroutine);
            hideCoroutine = StartCoroutine(HideTooltipAfterDelay());
        }
        else
        {
            Hide();
        }
    }

    private IEnumerator ShowTooltipAfterDelay()
    {
        yield return new WaitForSeconds(showDelay);

        if (isHovering) // 여전히 호버 중인지 확인
        {
            Show();
        }
    }

    private IEnumerator HideTooltipAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        if (!isHovering) // 여전히 벗어난 상태인지 확인
        {
            Hide();
        }
    }

    public void Show()
    {
        if (TooltipController.Instance == null) return;

        string tooltipText = "";

        switch (tooltipMode)
        {
            case TooltipMode.Inventory:
                tooltipText = BlockTypeInfo.GetTooltipText(blockType);
                break;

            case TooltipMode.Tile:
                tooltipText = GetTileScoreTooltip();
                break;
        }

        TooltipController.Instance.ShowTooltip(tooltipText, preferredDirection, transform as RectTransform);
    }

    private string GetTileScoreTooltip()
    {
        // GameManager 확인
        if (GameManager.Instance?.GetBoard() == null)
        {
            return "게임 정보를 로드 중...";
        }

        // 타일 확인
        var tile = GameManager.Instance.GetTile(tileX, tileY);
        if (tile == null || !tile.HasBlock)
        {
            return "이 타일에는 블록이 없습니다.";
        }

        // GameManager의 공용 메서드를 통해 ScoreBreakdown 가져오기
        var breakdown = GameManager.Instance.GetScoreBreakdown(tileX, tileY);
        if (breakdown != null)
        {
            return ScoreTooltipFormatter.GetScoreBreakdownText(breakdown);
        }

        return "점수 계산 정보를 가져올 수 없습니다.";
    }

    public void Hide()
    {
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.HideTooltip();
        }
    }

    // 외부에서 타일 위치 설정 (BlockPuzzleTile에서 사용)
    public void SetTilePosition(int x, int y)
    {
        tileX = x;
        tileY = y;
    }

    // 외부에서 툴팁 모드 설정
    public void SetTooltipMode(TooltipMode mode)
    {
        tooltipMode = mode;
    }

    private void OnDisable()
    {
        // 컴포넌트 비활성화 시 툴팁 숨김
        if (isHovering)
        {
            Hide();
        }

        isHovering = false;

        // 실행 중인 코루틴 정리
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }
    }
}