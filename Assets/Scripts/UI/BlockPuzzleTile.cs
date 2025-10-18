using GameCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보드 타일 컴포넌트
public class BlockPuzzleTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int x, y;
    private BlockPuzzleUIController uiController;
    private Button button;
    private Image image;
    private TextMeshProUGUI text;
    private BlockTypeTooltip tooltip; // 툴팁 컴포넌트 참조

    // 미리보기 관련 추가
    private Image overlayImage;
    private TextMeshProUGUI previewText; // 미리보기 점수 변화 텍스트
    private static BoardPreview currentBoardPreview; // 전체 타일이 공유하는 미리보기 데이터
    private static BlockPuzzleTile previewOriginTile; // 미리보기를 시작한 타일

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        tooltip = GetComponent<BlockTypeTooltip>(); // 툴팁 컴포넌트 가져오기

        button.onClick.AddListener(OnClick);
        CreatePreviewOverlay();
    }

    private void CreatePreviewOverlay()
    {
        // 오버레이 이미지 생성
        GameObject overlay = new GameObject("PreviewOverlay");
        overlay.transform.SetParent(transform, false);

        overlayImage = overlay.AddComponent<Image>();
        overlayImage.color = Color.clear;
        overlayImage.raycastTarget = false;

        var rect = overlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        // 미리보기 텍스트 생성
        GameObject previewTextObj = new GameObject("PreviewText");
        previewTextObj.transform.SetParent(transform, false);

        previewText = previewTextObj.AddComponent<TextMeshProUGUI>();
        previewText.fontSize = 24;
        previewText.fontStyle = FontStyles.Bold;
        previewText.alignment = TextAlignmentOptions.Center;
        previewText.raycastTarget = false;
        previewText.text = "";

        var textRect = previewTextObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;

        // 텍스트가 오버레이 위에 렌더링되도록 설정
        previewTextObj.transform.SetAsLastSibling();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectedBlockType = uiController?.GetSelectedBlockType();
        if (selectedBlockType != null && CanPlaceBlock())
        {
            ShowBoardPreview(selectedBlockType.Value);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (previewOriginTile == this)
        {
            HideBoardPreview();
        }
    }

    private bool CanPlaceBlock()
    {
        var tile = GameManager.Instance?.GetTile(x, y);
        return tile != null && tile.IsEmpty;
    }

    private void ShowBoardPreview(BlockType blockType)
    {
        if (GameManager.Instance == null) return;

        // 이전 미리보기가 있다면 제거
        if (previewOriginTile != null)
        {
            previewOriginTile.HideBoardPreview();
        }

        // 새로운 미리보기 계산
        currentBoardPreview = GameManager.Instance.GetBoardPreview(x, y, blockType);
        previewOriginTile = this;

        if (currentBoardPreview != null)
        {
            // 모든 타일에게 미리보기 업데이트 지시
            NotifyAllTilesPreviewUpdate();
        }
    }

    private void HideBoardPreview()
    {
        if (previewOriginTile != this) return;

        currentBoardPreview = null;
        previewOriginTile = null;

        // 모든 타일의 미리보기 제거
        NotifyAllTilesPreviewClear();
    }

    private void NotifyAllTilesPreviewUpdate()
    {
        var allTiles = FindObjectsByType<BlockPuzzleTile>(FindObjectsSortMode.None);
        foreach (var tile in allTiles)
        {
            tile.UpdatePreviewVisual();
        }
    }

    private void NotifyAllTilesPreviewClear()
    {
        var allTiles = FindObjectsByType<BlockPuzzleTile>(FindObjectsSortMode.None);
        foreach (var tile in allTiles)
        {
            tile.ClearPreviewVisual();
        }
    }

    private void UpdatePreviewVisual()
    {
        if (currentBoardPreview == null || overlayImage == null || previewText == null) return;

        int scoreChange = currentBoardPreview.GetScoreChange(x, y);

        if (x == currentBoardPreview.previewX && y == currentBoardPreview.previewY)
        {
            // 배치할 타일: 노란색 반투명 오버레이 + 배치 후 점수 표시
            overlayImage.color = new Color(1f, 1f, 0f, 1f);

            int newScore = currentBoardPreview.previewScores[x, y];
            previewText.text = $"{newScore:+#;-#;0}";
            previewText.color = newScore > 0 ? new Color(0f, 0.8f, 0f) :
                               newScore < 0 ? new Color(1f, 0f, 0f) :
                               new Color(0.5f, 0.5f, 0.5f);
        }
        else if (scoreChange > 0)
        {
            // 점수 증가 타일: 녹색 반투명 오버레이 + 증가량 표시
            overlayImage.color = new Color(0f, 1f, 0f, 1f);
            previewText.text = $"+{scoreChange}";
            previewText.color = new Color(0f, 0.8f, 0f);
        }
        else if (scoreChange < 0)
        {
            // 점수 감소 타일: 빨간색 반투명 오버레이 + 감소량 표시
            overlayImage.color = new Color(1f, 0f, 0f, 1f);
            previewText.text = $"{scoreChange}";
            previewText.color = new Color(1f, 0f, 0f);
        }
        else
        {
            // 변화 없음: 오버레이 제거
            overlayImage.color = Color.clear;
            previewText.text = "";
        }
    }

    private void ClearPreviewVisual()
    {
        if (overlayImage != null)
        {
            overlayImage.color = Color.clear;
        }

        if (previewText != null)
        {
            previewText.text = "";
        }
    }

    public void SetGameManager(GameManager gm)
    {
        // 싱글톤 사용으로 더 이상 필요 없지만 호환성을 위해 유지
    }

    public void SetUIController(BlockPuzzleUIController controller)
    {
        uiController = controller;
    }

    private void OnClick()
    {
        // 미리보기 제거 후 클릭 처리
        if (previewOriginTile != null)
        {
            previewOriginTile.HideBoardPreview();
        }

        if (GameManager.Instance == null || uiController == null) return;

        var tile = GameManager.Instance.GetTile(x, y);
        if (tile == null) return;

        if (tile.HasBlock)
        {
            // 현재 턴에 배치된 블록만 제거 가능
            bool removed = GameManager.Instance.RemoveBlock(x, y);
            if (!removed)
            {
                Debug.Log("이전 턴에 배치된 블록은 제거할 수 없습니다!");
            }
        }
        else
        {
            // 빈 칸이면 선택된 블록 배치 시도
            uiController.TryPlaceSelectedBlock(x, y);
        }
    }

    public void UpdateVisual()
    {
        if (GameManager.Instance == null) return;

        var tile = GameManager.Instance.GetTile(x, y);
        if (tile == null) return; // 타일이 아직 초기화되지 않았으면 리턴

        bool isNumberMode = GameManager.Instance.GetTileMode() == TileMode.WithNumbers;

        if (tile.HasBlock)
        {
            // 블록이 있을 때 - 툴팁을 타일 모드로 설정
            if (tooltip != null)
            {
                tooltip.SetTooltipMode(BlockTypeTooltip.TooltipMode.Tile);
                tooltip.SetTilePosition(x, y);
                tooltip.blockType = tile.block.type; // 백업용
                tooltip.enabled = true;
            }

            // 블록이 있을 때
            if (isNumberMode && tile.tileNumber > 0)
            {
                string turnText = tile.tileNumber == 1 ? "1턴 동안 유지" : $"{tile.tileNumber}턴 동안 유지";
                text.text = $"{tile.block.type} [{turnText}]\n{tile.calculatedScore:+#;-#;0}";
            }
            else
            {
                text.text = $"{tile.block.type}\n{tile.calculatedScore:+#;-#;0}";
            }

            // 점수에 따른 색상 차등 표시
            if (tile.calculatedScore > 0)
                image.color = new Color(0.6f, 1f, 0.6f); // 밝은 녹색
            else if (tile.calculatedScore == 0)
                image.color = new Color(0.8f, 0.8f, 0.8f); // 회색
            else
                image.color = new Color(1f, 0.6f, 0.6f); // 밝은 빨강
        }
        else
        {
            // 빈 타일일 때 - 툴팁 비활성화
            if (tooltip != null)
            {
                tooltip.enabled = false;
                tooltip.Hide();
            }

            // 빈 타일일 때
            if (isNumberMode && tile.tileNumber > 0)
            {
                string turnText = tile.tileNumber == 1 ? "1턴 동안 유지" : $"{tile.tileNumber}턴 동안 유지";
                text.text = $"[{turnText}]";
            }
            else
            {
                // 숫자 모드가 아니거나 타일 숫자가 0이면 아무것도 표시하지 않음
                text.text = "";
            }
            image.color = Color.white;
        }
    }
}