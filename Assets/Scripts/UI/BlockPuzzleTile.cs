using GameCore.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 보드 타일 컴포넌트
public class BlockPuzzleTile : MonoBehaviour
{
    public int x, y;
    private BlockPuzzleUIController uiController;
    private Button button;
    private Image image;
    private TextMeshProUGUI text;
    private BlockTypeTooltip tooltip; // 툴팁 컴포넌트 참조


    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>(); 
        tooltip = GetComponent<BlockTypeTooltip>(); // 툴팁 컴포넌트 가져오기


        button.onClick.AddListener(OnClick);
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
            // 블록이 있을 때 - 툴팁 타입 업데이트
            if (tooltip != null)
            {
                tooltip.blockType = tile.block.type;
                tooltip.enabled = true; // 툴팁 활성화
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