using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 보드 타일 컴포넌트
public class BlockPuzzleTile : MonoBehaviour
{
    public int x, y;
    private GameManager gameManager;
    private BlockPuzzleUIController uiController;
    private Button button;
    private Image image;
    private TextMeshProUGUI text;

    private void Awake()
    {
        button = GetComponent<Button>();
        image = GetComponent<Image>();
        text = GetComponentInChildren<TextMeshProUGUI>();

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

        var tile = gameManager.GetTile(x, y);
        if (tile == null) return;

        if (tile.HasBlock)
        {
            // 현재 턴에 배치된 블록만 제거 가능
            bool removed = gameManager.RemoveBlock(x, y);
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
        if (gameManager == null) return;

        var tile = gameManager.GetTile(x, y);
        if (tile == null) return; // 타일이 아직 초기화되지 않았으면 리턴

        if (tile.HasBlock)
        {
            // 블록이 있을 때
            if (gameManager.useNumbersMode && tile.tileNumber > 0)
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
            // 빈 타일일 때
            if (gameManager.useNumbersMode && tile.tileNumber > 0)
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
