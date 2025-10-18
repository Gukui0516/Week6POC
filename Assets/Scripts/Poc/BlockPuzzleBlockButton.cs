using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// 인벤토리 블록 버튼 컴포넌트
public class BlockPuzzleBlockButton : MonoBehaviour
{
    public GameManager.BlockType blockType;
    private GameManager gameManager;
    private BlockPuzzleUIController uiController;
    private Button button;
    private Image buttonImage;
    private TextMeshProUGUI text;
    private Color originalColor;

    private void Awake()
    {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        originalColor = buttonImage.color;

        button.onClick.AddListener(OnClick);
    }

    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
        uiController = FindFirstObjectByType<BlockPuzzleUIController>();
    }

    private void OnClick()
    {
        if (gameManager == null || uiController == null) return;

        // 현재 턴에 해당 블록 타입이 있는지 확인
        var turn = gameManager.GetCurrentTurn();
        if (turn == null || turn.availableBlocks == null) return;

        var availableBlock = turn.availableBlocks.FirstOrDefault(b => b.type == blockType);
        if (availableBlock == null)
        {
            Debug.Log($"블록 타입 {blockType}이(가) 인벤토리에 없습니다.");
            return;
        }

        // 블록 선택
        uiController.SelectBlock(blockType, this);
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
}
