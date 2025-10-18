using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameCore.Data;

// UI 컨트롤러 - 런타임에서 UI와 GameManager 연결 (인벤토리 제외)
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
    private InventoryController inventoryController;

    // UI 참조들
    private TextMeshProUGUI turnText;
    private TextMeshProUGUI targetText;
    private TextMeshProUGUI scoreText;
    private Slider progressBar;

    private BlockPuzzleTile[] tiles;

    public void SetGameManager(GameManager gm)
    {
        gameManager = gm;
    }

    private void Start()
    {

        // 싱글톤을 통해 GameManager 참조
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("[UIController] GameManager 싱글톤을 찾을 수 없습니다!");
            return;
        }

        // GameManager의 Start가 실행될 때까지 대기
        StartCoroutine(InitializeAfterGameManager());
    }

    private System.Collections.IEnumerator InitializeAfterGameManager()
    {

        int waitCount = 0;
        // GameManager의 보드가 초기화될 때까지 대기
        while (gameManager.GetBoard() == null)
        {
            waitCount++;
            if (waitCount > 100) // 무한 루프 방지
            {
                Debug.LogError("[UIController] GameManager 초기화 대기 시간 초과!");
                yield break;
            }
            yield return null;
        }



        CacheUIReferences();
        InitializeInventoryController();
        SubscribeToEvents();
        UpdateUI();


    }

    private void CacheUIReferences()
    {
        Debug.Log("[UIController] UI 참조 캐싱 시작");

        turnText = GameObject.Find("TurnText")?.GetComponent<TextMeshProUGUI>();
        targetText = GameObject.Find("TargetText")?.GetComponent<TextMeshProUGUI>();
        scoreText = GameObject.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        progressBar = GameObject.Find("ProgressBar")?.GetComponent<Slider>();

        tiles = FindObjectsByType<BlockPuzzleTile>(FindObjectsSortMode.None);

        Debug.Log($"[UIController] 발견된 UI 요소들:");
        Debug.Log($"- 타일: {tiles?.Length ?? 0}개");

        // 각 타일에 참조 설정
        foreach (var tile in tiles)
        {
            tile.SetUIController(this);
        }

        var gameButtons = FindObjectsByType<GameUIButton>(FindObjectsSortMode.None);
        Debug.Log($"- 게임 버튼: {gameButtons?.Length ?? 0}개");

        var modeToggle = FindFirstObjectByType<BlockPuzzleModeToggle>();
        if (modeToggle != null)
        {
            Debug.Log("- 모드 토글: 발견");
        }
        else
        {
            Debug.Log("- 모드 토글: 없음");
        }

        Debug.Log("[UIController] UI 참조 캐싱 완료");
    }

    private void InitializeInventoryController()
    {
        Debug.Log("[UIController] InventoryController 초기화 시작");

        // InventoryController 찾기 또는 생성
        inventoryController = FindFirstObjectByType<InventoryController>();

        if (inventoryController == null)
        {
            // 없으면 새로 생성
            GameObject inventoryObj = new GameObject("InventoryController");
            inventoryController = inventoryObj.AddComponent<InventoryController>();
            Debug.Log("[UIController] InventoryController 새로 생성됨");
        }

        inventoryController.Initialize(gameManager);
        Debug.Log("[UIController] InventoryController 초기화 완료");
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

    private void OnGameStateChanged(GameState state)
    {
        if (state == GameState.GameOver)
        {
            ShowGameOverUI();
        }
        else if (state == GameState.Victory)
        {
            ShowVictoryUI();
        }
        else if (state == GameState.Playing)
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
        // 인벤토리 버튼 비활성화 (위임)
        inventoryController?.DisableAllButtons();

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
        // 인벤토리 버튼 활성화 (위임)
        inventoryController?.EnableAllButtons();

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

            // 인벤토리 업데이트 (위임)
            inventoryController?.UpdateInventory();
            /*
            // 다음 마일스톤 찾기 - GameManager 헬퍼 메소드 사용
            Milestone nextMilestone = gameManager.GetNextMilestone(turn.turnNumber);

            if (turnText)
            {
                int maxTurns = gameManager.GetCurrentStageMaxTurns();
                turnText.text = $"턴: {turn.turnNumber}/{maxTurns}";
            }

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
            }*/

        }

        UpdateBoard();
    }

    private void UpdateBoard()
    {
        if (tiles == null || gameManager == null) return;
        if (gameManager.GetBoard() == null) return;

        foreach (var tile in tiles)
        {
            tile.UpdateVisual();
        }
    }

    // 타일에서 호출: 선택된 블록 배치 시도
    public bool TryPlaceSelectedBlock(int x, int y)
    {
        if (inventoryController == null)
        {
            Debug.Log("InventoryController가 초기화되지 않았습니다.");
            return false;
        }

        var selectedBlockType = inventoryController.GetSelectedBlockType();

        if (selectedBlockType == null)
        {
            Debug.Log("먼저 배치할 블록을 선택하세요.");
            return false;
        }

        bool success = gameManager.PlaceBlock(x, y, selectedBlockType.Value);
        if (success)
        {
            inventoryController.DeselectBlock();
        }
        return success;
    }

    // Getter
    public CardType? GetSelectedBlockType() => inventoryController?.GetSelectedBlockType();
}