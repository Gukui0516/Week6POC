using GameCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보드 타일 컴포넌트 - 블록 교체 기능 추가
public class BlockPuzzleTile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    #region Fields
    public int x, y;

    // UI 컴포넌트
    private UIController uiController;
    private Button button;
    private Image image;
    private GameObject blockIcon;
    private TextMeshProUGUI powerText;
    private TextMeshProUGUI tileInfoText;
    private Vector3 initialIconScale;

    // 미리보기 관련
    private Image overlayImage;
    private TextMeshProUGUI previewText;
    private static BoardPreview currentBoardPreview;
    private static BlockPuzzleTile previewOriginTile;

    // 드래그 관련
    private bool isHoveringDuringDrag = false;
    private Color originalColor;
    private bool isReplaceMode = false; // ⭐ 교체 모드 플래그

    // ToolBox 관련
    private bool isHoveringForToolBox = false;
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();

        // 아이콘 찾기
        Transform iconTransform = transform.Find("BlockIcon");
        if (iconTransform == null)
            iconTransform = transform.Find("Icon");
        if (iconTransform == null)
            iconTransform = transform.Find("Sprite");

        if (iconTransform == null)
        {
            Image[] childImages = GetComponentsInChildren<Image>();
            foreach (var img in childImages)
            {
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
            initialIconScale = blockIcon.transform.localScale;
        }
        else
        {
            initialIconScale = image.transform.localScale;
            Debug.LogWarning($"[BlockPuzzleTile] BlockIcon을 찾지 못함. Image를 직접 스케일합니다. 초기 스케일: {initialIconScale}");
        }

        // PowerText와 TileInfoText를 자식에서 찾기
        var textComponents = GetComponentsInChildren<TextMeshProUGUI>();
        if (textComponents.Length >= 1)
        {
            powerText = textComponents[0];
        }
        if (textComponents.Length >= 2)
        {
            tileInfoText = textComponents[1];
        }

        if (powerText == null)
        {
            powerText = GetComponentInChildren<TextMeshProUGUI>();
        }

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

        previewTextObj.transform.SetAsLastSibling();
    }

    #endregion

    #region Event Handlers

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ⭐ Drag 중일 때 처리
        if (eventData.pointerDrag != null &&
            eventData.pointerDrag.GetComponent<InventoryButton>() != null)
        {
            Debug.Log("OnPointerEnter - Dragging Block");

            var tile = GameManager.Instance?.GetTile(x, y);
            if (tile != null)
            {
                isHoveringDuringDrag = true;

                // 드래그 중인 카드 타입 가져오기
                var draggedButton = eventData.pointerDrag.GetComponent<InventoryButton>();
                if (draggedButton != null)
                {
                    CardType draggedCardType = draggedButton.CardType;

                    // ⭐ 타일이 비어있으면 배치 모드, 차있으면 교체 모드
                    if (tile.IsEmpty)
                    {
                        isReplaceMode = false;
                        ShowDropHint();
                        // 빈 타일: 점수 미리보기 표시
                        ShowBoardPreview(draggedCardType);
                    }
                    else
                    {
                        isReplaceMode = true;
                        ShowReplaceHint();

                        // ⭐ 차있는 타일: 교체 가능하면 점수 미리보기 표시
                        var currentTurn = GameManager.Instance?.GetCurrentTurn();
                        if (currentTurn != null && tile.IsRemovable(currentTurn.turnNumber))
                        {
                            ShowBoardPreviewForReplace(draggedCardType);
                        }
                    }
                }
            }
        }
        else
        {
            // ⭐ 일반 마우스 호버 (드래그 아님)
            var selectedCardType = uiController?.GetSelectedCardType();
            if (selectedCardType != null && CanPlaceBlock())
            {
                ShowBoardPreview(selectedCardType.Value);
            }

            // ToolBox 표시 (드래그 중이 아닐 때만)
            isHoveringForToolBox = true;
            ShowToolBoxInfo();
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
            isReplaceMode = false;
            UpdateVisual();
        }

        // ToolBox 숨기기
        if (isHoveringForToolBox)
        {
            isHoveringForToolBox = false;
            HideToolBoxInfo();
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
            bool removed = GameManager.Instance.RemoveBlock(x, y);
            if (!removed)
            {
                Debug.Log("이전 턴에 배치된 블록은 제거할 수 없습니다!");
            }
        }
        else
        {
            uiController.TryPlaceSelectedBlock(x, y);
        }
    }

    /*public void OnDrop(PointerEventData eventData)
    {
        if (previewOriginTile != null)
        {
            previewOriginTile.HideBoardPreview();
        }

        if (uiController == null) return;

        var draggedButton = eventData.pointerDrag?.GetComponent<InventoryButton>();
        if (draggedButton == null) return;

        // ⭐ 드래그된 블록 타입 가져오기
        CardType draggedCardType = draggedButton.CardType;

        var tile = GameManager.Instance?.GetTile(x, y);
        if (tile == null) return;

        bool success = false;

        // ⭐ 타일이 차있으면 교체 시도
        if (tile.HasBlock)
        {
            // ⭐ 같은 타입이면 아무것도 하지 않기
            if (tile.block.type == draggedCardType)
            {
                Debug.Log($"같은 타입의 블록입니다. 교체하지 않습니다: {draggedCardType}");
                isHoveringDuringDrag = false;
                isReplaceMode = false;
                UpdateVisual();
                return;
            }

            var currentTurn = GameManager.Instance.GetCurrentTurn();
            if (currentTurn != null && tile.IsRemovable(currentTurn.turnNumber))
            {
                // 기존 블록 타입 저장
                CardType oldBlockType = tile.block.type;

                // 기존 블록 제거 (인벤토리에 반환됨)
                bool removed = GameManager.Instance.RemoveBlock(x, y);
                if (removed)
                {
                    // ⭐ 드래그된 블록을 직접 배치
                    success = GameManager.Instance.PlaceBlock(x, y, draggedCardType);

                    if (success)
                    {
                        Debug.Log($"블록 교체 성공: {oldBlockType} → {draggedCardType} at ({x}, {y})");

                        // 인벤토리 컨트롤러에 드래그 완료 알림
                        if (uiController != null)
                        {
                            // 선택 해제 (드래그 완료)
                            var inventoryController = FindFirstObjectByType<InventoryController>();
                            inventoryController?.DeselectBlock();
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"블록 교체 실패: 새 블록 배치 불가 ({x}, {y})");

                        // ⭐ 실패 시 원래 블록 복구
                        GameManager.Instance.PlaceBlock(x, y, oldBlockType);
                    }
                }
                else
                {
                    Debug.Log("블록 제거 실패!");
                }
            }
            else
            {
                Debug.Log("이전 턴에 배치된 블록은 교체할 수 없습니다!");
            }
        }
        else
        {
            // 빈 타일이면 일반 배치
            success = uiController.TryPlaceSelectedBlock(x, y);

            if (success)
            {
                Debug.Log($"블록 배치 성공: ({x}, {y})");
            }
        }

        isHoveringDuringDrag = false;
        isReplaceMode = false;
        UpdateVisual();
    }*/

    #endregion

    #region ToolBox System

    private void ShowToolBoxInfo()
    {
        if (ToolBoxController.Instance == null) return;
        if (GameManager.Instance?.GetBoard() == null) return;

        var tile = GameManager.Instance.GetTile(x, y);
        if (tile == null) return;

        string toolboxText = "";

        if (tile.HasBlock)
        {
            // 블록이 있을 때: 점수 계산 정보 표시
            var breakdown = GameManager.Instance.GetScoreBreakdown(x, y);
            if (breakdown != null)
            {
                toolboxText = ScoreTooltipFormatter.GetScoreBreakdownText(breakdown);
            }
            else
            {
                toolboxText = "점수 계산 정보를 가져올 수 없습니다.";
            }
        }
        else
        {
            // 빈 타일일 때: 타일 정보 표시
            bool isNumberMode = GameManager.Instance.GetTileMode() == TileMode.WithNumbers;
            if (isNumberMode && tile.tileNumber > 0)
            {
                string turnText = tile.tileNumber == 1 ? "1턴 고용" : $"{tile.tileNumber}턴 고용";
                toolboxText = $"빈 타일\n[{turnText}]";
            }
            else
            {
                toolboxText = "빈 타일";
            }
        }

        ToolBoxController.Instance.ShowToolBox(toolboxText);
    }

    private void HideToolBoxInfo()
    {
        if (ToolBoxController.Instance != null)
        {
            ToolBoxController.Instance.HideToolBox();
        }
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

        if (previewOriginTile != null)
        {
            previewOriginTile.HideBoardPreview();
        }

        currentBoardPreview = GameManager.Instance.GetBoardPreview(x, y, blockType);
        previewOriginTile = this;

        if (currentBoardPreview != null)
        {
            NotifyAllTilesPreviewUpdate();
        }
    }

    // ⭐ 교체 시 점수 미리보기
    private void ShowBoardPreviewForReplace(CardType newBlockType)
    {
        if (GameManager.Instance == null) return;

        if (previewOriginTile != null)
        {
            previewOriginTile.HideBoardPreview();
        }

        var tile = GameManager.Instance.GetTile(x, y);
        if (tile == null || !tile.HasBlock) return;

        // ⭐ 교체 전용 미리보기 메서드 사용
        currentBoardPreview = GameManager.Instance.GetBoardPreviewForReplace(x, y, newBlockType);

        previewOriginTile = this;

        if (currentBoardPreview != null)
        {
            NotifyAllTilesPreviewUpdate();
        }
    }

    private void HideBoardPreview()
    {
        if (previewOriginTile != this) return;

        currentBoardPreview = null;
        previewOriginTile = null;

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
            overlayImage.color = new Color(1f, 1f, 0f, 1f);

            int newScore = currentBoardPreview.previewScores[x, y];
            previewText.text = $"{newScore:+#;-#;0}";
            previewText.color = newScore > 0 ? new Color(0f, 0.8f, 0f) :
                               newScore < 0 ? new Color(1f, 0f, 0f) :
                               new Color(0.5f, 0.5f, 0.5f);
        }
        else if (scoreChange > 0)
        {
            overlayImage.color = new Color(0f, 1f, 0f, 1f);
            previewText.text = $"+{scoreChange}";
            previewText.color = new Color(0f, 0.8f, 0f);
        }
        else if (scoreChange < 0)
        {
            overlayImage.color = new Color(1f, 0f, 0f, 1f);
            previewText.text = $"{scoreChange}";
            previewText.color = new Color(0f, 0f, 0f);
        }
        else
        {
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
        if (tile == null) return;

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
            targetTransform.localScale = Vector3.one * targetScale;
        }

        // 점수에 따른 색상 차등 표시
        if (tile.calculatedScore > 0)
            image.color = new Color(0.6f, 1f, 0.6f);
        else if (tile.calculatedScore == 0)
            image.color = new Color(0.8f, 0.8f, 0.8f);
        else
            image.color = new Color(1f, 0.6f, 0.6f);
    }

    private void UpdateEmptyTileVisual(GameCore.Data.Tile tile, bool isNumberMode)
    {
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

    private float CalculateScaleFromScore(int score)
    {
        if (score <= 0)
        {
            return 0.5f;
        }

        int clampedScore = Mathf.Clamp(score, 1, 5);
        float scale = 0.5f + (clampedScore - 1) * (0.5f / 4f);

        return scale;
    }

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
                iconImage.enabled = true;
            }
        }
    }

    private void HideBlockIcon()
    {
        if (blockIcon != null)
        {
            Image iconImage = blockIcon.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.enabled = false;
            }
        }
    }

    // ⭐ 드롭 가능 힌트 (빈 타일)
    private void ShowDropHint()
    {
        image.color = new Color(0.7f, 1f, 0.7f); // 연한 녹색
    }

    // ⭐ 교체 가능 힌트 (차있는 타일)
    private void ShowReplaceHint()
    {
        var tile = GameManager.Instance?.GetTile(x, y);
        var currentTurn = GameManager.Instance?.GetCurrentTurn();

        if (tile != null && currentTurn != null && tile.IsRemovable(currentTurn.turnNumber))
        {
            // 교체 가능 - 주황색
            image.color = new Color(1f, 0.8f, 0.3f);
        }
        else
        {
            // 교체 불가능 - 빨간색
            image.color = new Color(1f, 0.4f, 0.4f);
        }
    }

    #endregion
}