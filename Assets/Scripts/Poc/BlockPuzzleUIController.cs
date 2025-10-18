using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

// UI 컨트롤러 - 런타임에서 UI와 GameManager 연결
public class BlockPuzzleUIController : MonoBehaviour
{
    [Header("점수 텍스트 색상 설정")]
    public Color scoreHighColor = Color.green;     // 목표 달성
    public Color scoreMidColor = Color.yellow;     // 70% 이상
    public Color scoreLowColor = Color.white;      // 70% 미만

    [Header("진행바 색상 설정")]
    public Color progressCompleteColor = Color.green;   // 100% 이상
    public Color progressHighColor = Color.yellow;      // 70% 이상
    public Color progressLowColor = new Color(1f, 0.5f, 0f); // 70% 미만 (주황색)

    private GameManager gameManager;

    // UI 참조들
    private TextMeshProUGUI turnText;
    private TextMeshProUGUI targetText;
    private TextMeshProUGUI scoreText;
    private Slider progressBar;

    private BlockPuzzleTile[] tiles;
    private BlockPuzzleBlockButton[] blockButtons;

    // 선택된 블록 타입
    private GameManager.BlockType? selectedBlockType = null;
    private BlockPuzzleBlockButton selectedButton = null;

    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    private void Start()
    {
        // GameManager가 설정되지 않았다면 찾기
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError("GameManager를 찾을 수 없습니다! Tools > Generate Block Puzzle Canvas를 다시 실행하세요.");
            return;
        }

        // GameManager의 Start가 실행될 때까지 대기
        StartCoroutine(InitializeAfterGameManager());
    }

    private System.Collections.IEnumerator InitializeAfterGameManager()
    {
        // GameManager의 보드가 초기화될 때까지 대기
        while (gameManager.GetBoard() == null)
        {
            yield return null;
        }

        CacheUIReferences();
        SubscribeToEvents();
        UpdateUI();
    }

    private void CacheUIReferences()
    {
        turnText = GameObject.Find("TurnText")?.GetComponent<TextMeshProUGUI>();
        targetText = GameObject.Find("TargetText")?.GetComponent<TextMeshProUGUI>();
        scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        progressBar = GameObject.Find("ProgressBar")?.GetComponent<Slider>();

        tiles = FindObjectsByType<BlockPuzzleTile>(FindObjectsSortMode.None);
        blockButtons = FindObjectsByType<BlockPuzzleBlockButton>(FindObjectsSortMode.None);

        // 각 컴포넌트에 참조 설정
        foreach (var tile in tiles)
            tile.SetGameManager(gameManager);

        foreach (var btn in blockButtons)
            btn.SetGameManager(gameManager);

        var gameButtons = FindObjectsByType<BlockPuzzleGameButton>(FindObjectsSortMode.None);
        foreach (var btn in gameButtons)
            btn.SetGameManager(gameManager);

        var modeToggle = FindFirstObjectByType<BlockPuzzleModeToggle>();
        if (modeToggle != null)
            modeToggle.SetGameManager(gameManager);
    }

    private void SubscribeToEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnTurnStart += (turn) => UpdateUI();
            gameManager.OnScoreUpdated += (score) => UpdateUI();
            gameManager.OnBoardUpdated += UpdateBoard;
            gameManager.OnGameStateChanged += OnGameStateChanged;
        }
    }

    private void OnGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            ShowGameOverUI();
        }
        else if (state == GameManager.GameState.Victory)
        {
            ShowVictoryUI();
        }
        else if (state == GameManager.GameState.Playing)
        {
            // 게임 재시작 시 모든 버튼 활성화
            EnableAllButtons();
        }
        UpdateUI();
    }

    private void ShowGameOverUI()
    {
        // 게임 오버 메시지 표시
        if (scoreText != null)
        {
            var turn = gameManager?.GetCurrentTurn();
            if (turn != null)
            {
                int cumulativeScore = gameManager.GetCumulativeScore();
                scoreText.text = $"게임 오버!\n목표: {turn.targetScore} / 달성: {cumulativeScore}";
                scoreText.color = Color.red;
            }
        }

        DisableAllButtons();
        Debug.Log("게임 오버! 누적 목표 점수를 달성하지 못했습니다. '새 게임' 버튼을 눌러 다시 시작하세요.");
    }

    private void ShowVictoryUI()
    {
        // 승리 메시지 표시
        if (scoreText != null)
        {
            int finalScore = gameManager.GetCumulativeScore();
            scoreText.text = $"게임 클리어!\n최종 점수: {finalScore}";
            scoreText.color = Color.cyan;
        }

        DisableAllButtons();
        Debug.Log("축하합니다! 모든 턴을 완료했습니다!");
    }

    private void DisableAllButtons()
    {
        // 모든 블록 버튼 비활성화
        if (blockButtons != null)
        {
            foreach (var btn in blockButtons)
            {
                var button = btn.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }

        // 모든 타일 버튼 비활성화
        if (tiles != null)
        {
            foreach (var tile in tiles)
            {
                var button = tile.GetComponent<Button>();
                if (button != null)
                    button.interactable = false;
            }
        }
    }

    private void EnableAllButtons()
    {
        // 모든 블록 버튼 활성화 (개수가 0인 것 제외)
        if (blockButtons != null)
        {
            foreach (var btn in blockButtons)
            {
                var button = btn.GetComponent<Button>();
                if (button != null)
                {
                    var turn = gameManager?.GetCurrentTurn();
                    if (turn != null)
                    {
                        int count = turn.availableBlocks.Count(b => b.type == btn.blockType);
                        button.interactable = count > 0;
                    }
                }
            }
        }

        // 모든 타일 버튼 활성화
        if (tiles != null)
        {
            foreach (var tile in tiles)
            {
                var button = tile.GetComponent<Button>();
                if (button != null)
                    button.interactable = true;
            }
        }
    }

    private void UpdateUI()
    {
        var turn = gameManager?.GetCurrentTurn();
        if (turn != null)
        {
            int currentTurnScore = gameManager.GetTotalScore(); // 현재 보드 점수
            int cumulativeScore = gameManager.GetCumulativeScore(); // 게임 전체 누적 점수

            // 다음 마일스톤 찾기
            GameManager.Milestone nextMilestone = null;
            foreach (var milestone in gameManager.milestones)
            {
                if (milestone.checkTurn >= turn.turnNumber)
                {
                    nextMilestone = milestone;
                    break;
                }
            }

            if (turnText) turnText.text = $"턴: {turn.turnNumber}/{gameManager.maxTurns}";

            if (targetText && nextMilestone != null)
            {
                targetText.text = $"다음 목표: {nextMilestone.checkTurn}턴까지 {nextMilestone.targetScore}점";
            }

            if (scoreText)
            {
                scoreText.text = $"누적: {cumulativeScore} | 보드: {currentTurnScore}";

                // 다음 마일스톤 기준으로 색상 변경
                if (nextMilestone != null)
                {
                    if (cumulativeScore >= nextMilestone.targetScore)
                        scoreText.color = scoreHighColor;
                    else if (cumulativeScore >= nextMilestone.targetScore * 0.7f)
                        scoreText.color = scoreMidColor;
                    else
                        scoreText.color = scoreLowColor;
                }
            }

            if (progressBar && nextMilestone != null)
            {
                float progress = (float)cumulativeScore / nextMilestone.targetScore;
                progressBar.value = Mathf.Clamp01(progress);

                // 진행도에 따른 색상 변경
                var fillImage = progressBar.fillRect?.GetComponent<Image>();
                if (fillImage != null)
                {
                    if (progress >= 1f)
                        fillImage.color = progressCompleteColor;
                    else if (progress >= 0.7f)
                        fillImage.color = progressHighColor;
                    else
                        fillImage.color = progressLowColor;
                }
            }

            UpdateInventory();
        }

        UpdateBoard();
    }

    private void UpdateBoard()
    {
        if (tiles == null || gameManager == null) return;
        if (gameManager.GetBoard() == null) return; // 보드가 초기화되지 않았으면 리턴

        foreach (var tile in tiles)
        {
            tile.UpdateVisual();
        }
    }

    private void UpdateInventory()
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
    public void SelectBlock(GameManager.BlockType blockType, BlockPuzzleBlockButton button)
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

    // 타일에 블록 배치
    public bool TryPlaceSelectedBlock(int x, int y)
    {
        if (selectedBlockType == null)
        {
            Debug.Log("먼저 배치할 블록을 선택하세요.");
            return false;
        }

        bool success = gameManager.PlaceBlock(x, y, selectedBlockType.Value);
        if (success)
        {
            DeselectBlock();
        }
        return success;
    }

    // Getter
    public GameManager.BlockType? GetSelectedBlockType() => selectedBlockType;
}
