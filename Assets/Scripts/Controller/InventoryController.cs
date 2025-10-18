using GameCore.Data;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 인벤토리 전용 컨트롤러 - 블록 선택 및 인벤토리 UI 관리
public class InventoryController : MonoBehaviour
{
    private GameManager gameManager;
    private InventoryButton[] blockButtons;

    // 선택된 블록 상태
    private BlockType? selectedBlockType = null;
    private InventoryButton selectedButton = null;


    #region Drag

    private GameObject dragPreview;
    private Canvas mainCanvas;

    #endregion

    public void Initialize(GameManager gm = null)
    {
        // 싱글톤 우선, 파라미터로 전달된 경우 그것 사용
        gameManager = gm ?? GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[InventoryController] GameManager를 찾을 수 없습니다!");
            return;
        }

        // 모든 인벤토리 버튼 찾기
        blockButtons = FindObjectsByType<InventoryButton>(FindObjectsSortMode.None);

        Debug.Log($"[InventoryController] 발견된 블록 버튼: {blockButtons?.Length ?? 0}개");

        // 각 버튼에 참조 설정
        foreach (var btn in blockButtons)
        {
            btn.SetInventoryController(this);
        }

        mainCanvas = FindFirstObjectByType<Canvas>();

    }

    // 인벤토리 UI 업데이트
    public void UpdateInventory()
    {
        if (blockButtons == null || gameManager?.GetCurrentTurn() == null) return;

        var turn = gameManager.GetCurrentTurn();
        var groups = turn.availableBlocks.GroupBy(b => b.type).ToDictionary(g => g.Key, g => g.Count());

        foreach (var btn in blockButtons)
        {
            int count = groups.ContainsKey(btn.blockType) ? groups[btn.blockType] : 0;
            btn.UpdateCount(count);
        }
    }

    // 블록 선택
    public void SelectBlock(BlockType blockType, InventoryButton button)
    {
        // 이전 선택 해제
        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }

        selectedBlockType = blockType;
        selectedButton = button;
        selectedButton.SetSelected(true);

        Debug.Log($"블록 {blockType} 선택됨. 배치할 빈 타일을 클릭하세요.");
    }

    // 선택 해제
    public void DeselectBlock()
    {
        Debug.Log("DeselectBlock");
        /*if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }
        selectedBlockType = null;
        selectedButton = null;*/
    }

    // 모든 버튼 활성화
    public void EnableAllButtons()
    {
        if (blockButtons == null || gameManager == null) return;

        foreach (var btn in blockButtons)
        {
            var button = btn.GetComponent<Button>();
            if (button != null)
            {
                var turn = gameManager.GetCurrentTurn();
                if (turn != null)
                {
                    int count = turn.availableBlocks.Count(b => b.type == btn.blockType);
                    button.interactable = count > 0;
                }
            }
        }
    }

    // 모든 버튼 비활성화
    public void DisableAllButtons()
    {
        if (blockButtons == null) return;

        foreach (var btn in blockButtons)
        {
            var button = btn.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
        }
    }

    #region Drag

    public void OnBeginDrag(BlockType blockType, InventoryButton button)
    {
        // 블록 선택
        SelectBlock(blockType, button);

        // 드래그 프리뷰 생성
        CreateDragPreview(button);
    }


    public void OnDragging(Vector2 screenPosition)
    {
        // 드래그 프리뷰 위치 업데이트
        if (dragPreview != null)
        {
            dragPreview.transform.position = screenPosition;
        }
    }

    public void OnEndDrag()
    {
        // 드래그 프리뷰 제거
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }


    private void CreateDragPreview(InventoryButton button)
    {
        if (mainCanvas == null) return;

        // 기존 프리뷰 제거
        if (dragPreview != null)
        {
            Destroy(dragPreview);
        }

        // 새 프리뷰 생성
        dragPreview = new GameObject("DragPreview");
        dragPreview.transform.SetParent(mainCanvas.transform, false);

        // 최상단에 표시되도록
        dragPreview.transform.SetAsLastSibling();

        // 이미지 복사
        var image = dragPreview.AddComponent<Image>();
        var originalImage = button.GetComponent<Image>();
        if (originalImage != null)
        {
            image.sprite = originalImage.sprite;
            image.color = new Color(1, 1, 1, 0.7f); // 반투명
        }

        // 레이캐스트 차단 방지
        image.raycastTarget = false;

        // 크기 설정
        var rectTransform = dragPreview.GetComponent<RectTransform>();
        rectTransform.sizeDelta = button.GetComponent<RectTransform>().sizeDelta;

        // 텍스트도 복사 (선택 사항)
        var originalText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (originalText != null)
        {
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(dragPreview.transform, false);

            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = originalText.text;
            text.fontSize = originalText.fontSize;
            text.color = new Color(originalText.color.r, originalText.color.g, originalText.color.b, 0.7f);
            text.alignment = originalText.alignment;
            text.raycastTarget = false;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }

        Debug.Log($"[InventoryController] 드래그 프리뷰 생성: {selectedBlockType}");
    }

    #endregion







    // Getter
    public BlockType? GetSelectedBlockType() => selectedBlockType;
}