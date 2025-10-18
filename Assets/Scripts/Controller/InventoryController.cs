using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using GameCore.Data;

// 인벤토리 전용 컨트롤러 - 블록 선택 및 인벤토리 UI 관리
public class InventoryController : MonoBehaviour
{
    private GameManager gameManager;
    private InventoryButton[] blockButtons;

    // 선택된 블록 상태
    private BlockType? selectedBlockType = null;
    private InventoryButton selectedButton = null;

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
        if (selectedButton != null)
        {
            selectedButton.SetSelected(false);
        }
        selectedBlockType = null;
        selectedButton = null;
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

    // Getter
    public BlockType? GetSelectedBlockType() => selectedBlockType;
}