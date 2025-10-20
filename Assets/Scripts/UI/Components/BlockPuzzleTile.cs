using GameCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보드 타일 컴포넌트
public class BlockPuzzleTile : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    #region Fields
    public int x, y;

    // UI 컴포넌트
    private UIController uiController;
    private Button button;
    private Image image;
    private GameObject blockIcon; // 실제 블록 아이콘 (Image의 자식일 수 있음)
    private TextMeshProUGUI powerText; // 점수 표시 전용 (기존 text에서 이름 변경)
    private TextMeshProUGUI tileInfoText; // 땅 정보 표시 전용 (새로 추가)
    private BlockTypeTooltip tooltip; // 툴팁 컴포넌트 참조
    private Vector3 initialIconScale; // 아이콘의 초기 스케일 저장

    // 미리보기 관련
    private Image overlayImage;
    private TextMeshProUGUI previewText; // 미리보기 점수 변화 텍스트
    private static BoardPreview currentBoardPreview; // 전체 타일이 공유하는 미리보기 데이터
    private static BlockPuzzleTile previewOriginTile; // 미리보기를 시작한 타일

    // 드래그 관련
    private bool isHoveringDuringDrag = false;
    private Color originalColor;
    #endregion


    #region Unity Lifecycle

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();

        // 여러 가능성의 아이콘 찾기
        // 1. "BlockIcon" 또는 "Icon" 이름의 자식
        Transform iconTransform = transform.Find("BlockIcon");
        if (iconTransform == null)
            iconTransform = transform.Find("Icon");
        if (iconTransform == null)
            iconTransform = transform.Find("Sprite");

        // 2. Image 컴포넌트를 가진 첫 번째 자식 (자기 자신 제외)
        if (iconTransform == null)
        {
            Image[] childImages = GetComponentsInChildren<Image>();
            foreach (var img in childImages)
            {
                // 자기 자신이 아니고, 텍스트가 아닌 Image를 찾음
                if (img != image && img.GetComponent<TextMeshProUGUI>() == null)
                {
                    iconTransform = img.transform;
                    Debug.Log($"[BlockPuzzleTile] 아이콘 찾음: {img.gameObject.name}");
                    break;
                }
            }
        }

        if (iconTransform != null)
        {
            blockIcon = iconTransform.gameObject;
            initialIconScale = blockIcon.transform.localScale; // 초기 스케일 저장
            Debug.Log($"[BlockPuzzleTile] BlockIcon 설정됨: {blockIcon.name}, 초기 스케일: {initialIconScale}");
        }
        else
        {
            initialIconScale = image.transform.localScale; // Image의 초기 스케일 저장
            Debug.LogWarning($"[BlockPuzzleTile] BlockIcon을 찾지 못함. Image를 직접 스케일합니다. 초기 스케일: {initialIconScale}");
        }

        // PowerText와 TileInfoText를 자식에서 찾기
        var textComponents = GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length >= 1)
        {
            powerText = textComponents[0]; // 첫 번째는 PowerText
        }
        if (textComponents.Length >= 2)
        {
            tileInfoText = textComponents[1]; // 두 번째는 TileInfoText (있다면)
        }

        // 없으면 기존 방식대로 폴백
        if (powerText == null)
        {
            powerText = GetComponentInChildren<TextMeshProUGUI>();
        }

        tooltip = GetComponent<BlockTypeTooltip>(); // 툴팁 컴포넌트 가져오기
        originalColor = image.color;

        button.onClick.AddListener(OnClick);
        CreatePreviewOverlay();
    }

    #endregion

    #region Initialization

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

    #endregion

    #region Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        var selectedCardType = uiController?.GetSelectedCardType();
        if (selectedCardType != null && CanPlaceBlock())
        {
            ShowBoardPreview(selectedCardType.Value);
        }


        // Drage
        if (eventData.pointerDrag != null &&
     eventData.pointerDrag.GetComponent<InventoryButton>() != null)
        {
            Debug.Log("OnPointerEnter - Dragging Block");

            var tile = GameManager.Instance?.GetTile(x, y);
            if (tile != null && tile.IsEmpty)
            {
                isHoveringDuringDrag = true;
                ShowDropHint();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (previewOriginTile == this)
        {
            HideBoardPreview();
        }

        // Drag
        if (isHoveringDuringDrag)
        {
            isHoveringDuringDrag = false;
            UpdateVisual();
        }
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

    // 드롭 처리
    public void OnDrop(PointerEventData eventData)
    {
        // 미리보기 제거
        if (previewOriginTile != null)
        {
            previewOriginTile.HideBoardPreview();
        }

        if (uiController == null) return;

        // 드래그된 오브젝트가 InventoryButton인지 확인
        var draggedButton = eventData.pointerDrag?.GetComponent<InventoryButton>();
        if (draggedButton == null) return;

        // 블록 배치 시도
        bool success = uiController.TryPlaceSelectedBlock(x, y);

        if (success)
        {
            Debug.Log($"블록 배치 성공: ({x}, {y})");
        }

        // 호버 피드백 제거
        isHoveringDuringDrag = false;
        UpdateVisual();
    }

    #endregion

    #region Preview System

    private bool CanPlaceBlock()
    {
        var tile = GameManager.Instance?.GetTile(x, y);
        return tile != null && tile.IsEmpty;
    }

    private void ShowBoardPreview(CardType blockType)
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
            previewText.color = new Color(0f, 0f, 0f);
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

    #endregion

    #region Public API

    public void SetGameManager(GameManager gm)
    {
        // 싱글톤 사용으로 더 이상 필요 없지만 호환성을 위해 유지
    }

    public void SetUIController(UIController controller)
    {
        uiController = controller;
    }

    public void UpdateVisual()
    {
        if (GameManager.Instance == null) return;

        var tile = GameManager.Instance.GetTile(x, y);
        if (tile == null) return; // 타일이 아직 초기화되지 않았으면 리턴

        bool isNumberMode = GameManager.Instance.GetTileMode() == TileMode.WithNumbers;

        if (tile.HasBlock)
        {
            UpdateBlockVisual(tile, isNumberMode);
        }
        else
        {
            UpdateEmptyTileVisual(tile, isNumberMode);
        }
    }

    #endregion

    #region Visual Update

    private void UpdateBlockVisual(GameCore.Data.Tile tile, bool isNumberMode)
    {
        // 블록이 있을 때 - 툴팁을 타일 모드로 설정
        if (tooltip != null)
        {
            tooltip.SetTooltipMode(BlockTypeTooltip.TooltipMode.Tile);
            tooltip.SetTilePosition(x, y);
            tooltip.cardType = tile.block.type; // 백업용
            tooltip.enabled = true;
        }

        // PowerText: 점수만 표시
        if (powerText != null)
        {
            powerText.text = $"{tile.calculatedScore:+#;-#;0}";
        }

        // TileInfoText: 땅 정보 표시 (턴 정보 포함)
        if (tileInfoText != null)
        {
            if (isNumberMode && tile.tileNumber > 0)
            {
                string turnText = tile.tileNumber == 1 ? "1턴 유지" : $"{tile.tileNumber}턴 유지";
                tileInfoText.text = $"[{turnText}]";
            }
            else
            {
                tileInfoText.text = "";
            }
        }

        // 아이콘 표시 및 스프라이트 설정
        SetBlockIcon(tile.block.type);

        // 아이콘 크기 조정 (점수 기반)
        float targetScale = CalculateScaleFromScore(tile.calculatedScore);
        Transform targetTransform = blockIcon != null ? blockIcon.transform : image.transform;

        if (targetTransform != null)
        {
            Vector3 oldScale = targetTransform.localScale;
            targetTransform.localScale = Vector3.one * targetScale;

            Debug.Log($"[BlockPuzzleTile] 타일({x},{y}) 블록:{tile.block.type} 점수:{tile.calculatedScore} " +
                     $"스케일: {oldScale} → {targetScale:F2} " +
                     $"대상: {targetTransform.gameObject.name}");
        }
        else
        {
            Debug.LogError($"[BlockPuzzleTile] 타일({x},{y}) 스케일 조절 실패: targetTransform이 null");
        }

        // 점수에 따른 색상 차등 표시
        if (tile.calculatedScore > 0)
            image.color = new Color(0.6f, 1f, 0.6f); // 밝은 녹색
        else if (tile.calculatedScore == 0)
            image.color = new Color(0.8f, 0.8f, 0.8f); // 회색
        else
            image.color = new Color(1f, 0.6f, 0.6f); // 밝은 빨강
    }

    private void UpdateEmptyTileVisual(GameCore.Data.Tile tile, bool isNumberMode)
    {
        // 빈 타일일 때 - 툴팁 비활성화
        if (tooltip != null)
        {
            tooltip.enabled = false;
            tooltip.Hide();
        }

        // PowerText: 빈 칸으로 유지
        if (powerText != null)
        {
            powerText.text = "";
        }

        // TileInfoText: 턴 정보 표시
        if (tileInfoText != null)
        {
            if (isNumberMode && tile.tileNumber > 0)
            {
                string turnText = tile.tileNumber == 1 ? "1턴 유지" : $"{tile.tileNumber}턴 유지";
                tileInfoText.text = $"[{turnText}]";
            }
            else
            {
                tileInfoText.text = "";
            }
        }
        else if (powerText != null)
        {
            // 하위 호환성: tileInfoText가 없으면 powerText에 턴 표시
            if (isNumberMode && tile.tileNumber > 0)
            {
                string turnText = tile.tileNumber == 1 ? "1턴 유지" : $"{tile.tileNumber}턴 유지";
                powerText.text = $"[{turnText}]";
            }
        }

        // 빈 타일은 아이콘 숨기기
        HideBlockIcon();

        // 빈 타일은 초기 크기로 복원
        Transform targetTransform = blockIcon != null ? blockIcon.transform : image.transform;
        if (targetTransform != null)
        {
            targetTransform.localScale = initialIconScale;
        }

        image.color = Color.white;
    }

    // 점수를 기반으로 스케일 계산 (1점=0.5, 5점=1.0)
    private float CalculateScaleFromScore(int score)
    {
        // 마이너스 점수는 최소 크기
        if (score <= 0)
        {
            return 0.5f;
        }

        // 점수를 1~5 범위로 클램프
        int clampedScore = Mathf.Clamp(score, 1, 5);

        // 선형 보간: 1점=0.5, 5점=1.0
        float scale = 0.5f + (clampedScore - 1) * (0.5f / 4f);

        return scale;
    }

    /// <summary>
    /// 블록 아이콘 설정 (SO에서 로드)
    /// </summary>
    private void SetBlockIcon(CardType cardType)
    {
        if (blockIcon == null) return;

        var cardData = CardDataLoader.GetData(cardType);
        if (cardData != null && cardData.iconSprite != null)
        {
            Image iconImage = blockIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = cardData.iconSprite;
                iconImage.enabled = true; // 아이콘 표시
                Debug.Log($"[BlockPuzzleTile] 타일({x},{y}) 아이콘 설정: {cardType}");
            }
        }
        else
        {
            Debug.LogWarning($"[BlockPuzzleTile] 타일({x},{y}) {cardType}의 아이콘을 찾을 수 없습니다.");
        }
    }

    /// <summary>
    /// 블록 아이콘 숨기기
    /// </summary>
    private void HideBlockIcon()
    {
        if (blockIcon != null)
        {
            Image iconImage = blockIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.enabled = false; // 아이콘 숨김
            }
        }
    }

    private void ShowDropHint()
    {
        // 드롭 가능한 타일 강조
        image.color = new Color(0.7f, 1f, 0.7f); // 연한 녹색
    }

    private void UpdateHoverVisual()
    {
        if (GameManager.Instance == null) return;
        var tile = GameManager.Instance.GetTile(x, y);
        if (tile == null) return;

        if (isHoveringDuringDrag && tile.IsEmpty)
        {
            // 드롭 가능한 타일 강조
            image.color = new Color(0.8f, 1f, 0.8f, 0.5f);
        }
        else
        {
            // 일반 상태로 복귀
            UpdateVisual();
        }
    }

    #endregion

}