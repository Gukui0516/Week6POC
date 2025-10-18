using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Linq;
using GameCore.Data;

// 인벤토리 블록 버튼 컴포넌트 (툴팁 기능 추가)
public class InventoryButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public BlockType blockType;
    private InventoryController inventoryController;
    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI text;
    private Color originalColor;

    //--- drag
    private bool isDragging = false;
    private Vector3 originalScale;


    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        originalColor = buttonImage.color;
        originalScale = transform.localScale;

        button.onClick.AddListener(OnClick);
    }

    public void SetInventoryController(InventoryController controller)
    {
        inventoryController = controller;
    }

    private void OnClick()
    {
        if (isDragging) return; // 드래그 중이면 클릭 무시

        // 싱글톤으로 GameManager 접근
        if (GameManager.Instance == null || inventoryController == null) return;


        // 현재 턴에 해당 블록 타입이 있는지 확인
        var turn = GameManager.Instance.GetCurrentTurn();
        if (turn == null || turn.availableBlocks == null) return;

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null)
        {
            Debug.Log($"블록 타입 {blockType}이(가) 인벤토리에 없습니다.");
            return;
        }

        inventoryController.SelectBlock(blockType, this);
    }
    // 드래그 시작

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (GameManager.Instance == null || inventoryController == null) return;

        var turn = GameManager.Instance.GetCurrentTurn();
        if (turn == null) return;

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null) return;

        isDragging = true;

        inventoryController.OnBeginDrag(blockType, this);


        //inventoryController.SelectBlock(blockType, this);

        // 선택 사항: 드래그 중 시각적 피드백
        transform.localScale = Vector3.one * 1.2f;
    }





    public void SetSelected(bool selected)
    {
        if (selected)
        {
            // 선택됨 - 테두리 효과
            buttonImage.color = Color.yellow;
            transform.localScale = Vector3.one * 1.1f;
        }
        else
        {
            // 선택 해제 - 원래대로
            buttonImage.color = originalColor;
            transform.localScale = Vector3.one;
        }
    }

    public void UpdateCount(int count)
    {
        if (text != null)
        {
            text.text = $"{blockType}\n×{count}";

            // 블록이 없으면 버튼 비활성화 표시
            if (button != null)
            {
                button.interactable = count > 0;
            }

            // 개수에 따른 텍스트 투명도 조절
            if (count == 0)
                text.color = new Color(1f, 1f, 1f, 0.3f);
            else
                text.color = Color.black;
        }
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 툴팁이 표시 중이면 숨김
        if (TooltipController.Instance != null)
        {
            TooltipController.Instance.HideTooltip();
        }
    }


    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        inventoryController.OnDragging(eventData.position);
    }

    // 드래그 종료
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        transform.localScale = originalScale;

        // InventoryController에 드래그 종료 알림
        inventoryController.OnEndDrag();
    }


}
