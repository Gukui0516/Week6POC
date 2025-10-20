using UnityEngine;
using TMPro;
using GameCore.Data;

/// <summary>
/// 게임 상태 관련 UI 전담 매니저
/// 게임 오버, 승리, 재시작 등의 상태 메시지를 표시
/// </summary>
public class GameStateUIManager : MonoBehaviour
{
    [Header("UI 요소 참조")]
    [SerializeField]
    private TextMeshProUGUI stateMessageText; // 게임 상태 메시지 (게임오버/승리)

    [Header("상태별 색상 설정")]
    public Color gameOverColor = Color.red;        // 게임 오버
    public Color victoryColor = Color.cyan;        // 승리
    public Color normalColor = Color.white;        // 일반 상태


    [Header("상점 UI")]
    [SerializeField] private GameObject shopPanel; // 상점 패널

    private GameManager gameManager;
    private InventoryController inventoryController;
    private TileGridManager tileGridManager;

    /// <summary>
    /// GameStateUIManager 초기화
    /// </summary>
    /// <param name="gm">GameManager 인스턴스</param>
    /// <param name="invCtrl">InventoryController 인스턴스</param>
    /// <param name="tileGrid">TileGridManager 인스턴스</param>
    public void Initialize(GameManager gm, InventoryController invCtrl, TileGridManager tileGrid)
    {
        gameManager = gm;
        inventoryController = invCtrl;
        tileGridManager = tileGrid;

        if (gameManager == null)
        {
            Debug.LogError("[GameStateUIManager] GameManager를 찾을 수 없습니다!");
            return;
        }

        // stateMessageText가 Inspector에서 할당되지 않았으면 찾기 시도
        if (stateMessageText == null)
        {
            FindStateMessageText();
        }

        SubscribeToGameStateEvents();
    }

    /// <summary>
    /// 씬에서 상태 메시지 텍스트 찾기
    /// </summary>
    private void FindStateMessageText()
    {
        var textObjects = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);

        foreach (var txt in textObjects)
        {
            if (txt.name.Contains("Score") || txt.name.Contains("State") || txt.name.Contains("Message"))
            {
                stateMessageText = txt;
                break;
            }
        }

        if (stateMessageText == null)
        {
            Debug.LogWarning("[GameStateUIManager] 상태 메시지 텍스트를 찾을 수 없습니다. Inspector에서 할당해주세요.");
        }
    }

    /// <summary>
    /// 게임 상태 변경 이벤트 구독
    /// </summary>
    private void SubscribeToGameStateEvents()
    {
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += OnGameStateChanged;
        }
    }

    /// <summary>
    /// 게임 상태 변경 처리
    /// </summary>
    private void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.GameOver:
                ShowGameOverUI();
                break;

            case GameState.Victory:
                ShowVictoryUI();
                break;

            case GameState.Playing:
                OnGameRestart();
                break;

            case GameState.Shop:  // 상점 상태 추가
                ShowShopUI();
                break;

            default:
                break;
        }
    }



    /// <summary>
    /// 게임 오버 UI 표시
    /// </summary>
    public void ShowGameOverUI()
    {
        if (stateMessageText == null)
        {
            Debug.LogWarning("[GameStateUIManager] 상태 메시지 텍스트가 없습니다");
            return;
        }

        var turn = gameManager?.GetCurrentTurn();
        if (turn != null)
        {
            int cumulativeScore = gameManager.GetCumulativeScore();
            stateMessageText.text = $"게임 오버!\n목표: {turn.targetScore} / 달성: {cumulativeScore}";
            stateMessageText.color = gameOverColor;
        }
        else
        {
            stateMessageText.text = "게임 오버!";
            stateMessageText.color = gameOverColor;
        }

        // 모든 버튼 비활성화
        DisableAllInteractions();
    }

    /// <summary>
    /// 승리 UI 표시
    /// </summary>
    public void ShowVictoryUI()
    {
        if (stateMessageText == null)
        {
            Debug.LogWarning("[GameStateUIManager] 상태 메시지 텍스트가 없습니다");
            return;
        }

        int finalScore = gameManager.GetCumulativeScore();
        stateMessageText.text = $"게임 클리어!\n최종 점수: {finalScore}";
        stateMessageText.color = victoryColor;

        // 모든 버튼 비활성화
        DisableAllInteractions();
    }

    /// <summary>
    /// 게임 재시작 처리
    /// </summary>
    private void OnGameRestart()
    {
        if (stateMessageText != null)
        {
            stateMessageText.color = normalColor;
        }

        // 모든 버튼 활성화
        EnableAllInteractions();
    }




    /// <summary>
    /// 상점 UI 표시
    /// </summary>
    private void ShowShopUI()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            Debug.Log("[GameStateUIManager] 상점 UI 표시");
        }
        else
        {
            Debug.LogWarning("[GameStateUIManager] shopPanel이 할당되지 않았습니다!");
        }

        // 게임 버튼 비활성화
        DisableAllInteractions();
    }

    /// <summary>
    /// 상점 UI 숨기기
    /// </summary>
    public void HideShopUI()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }







    /// <summary>
    /// 일반 상태 메시지 표시
    /// </summary>
    /// <param name="message">표시할 메시지</param>
    /// <param name="color">메시지 색상 (옵션)</param>
    public void ShowMessage(string message, Color? color = null)
    {
        if (stateMessageText == null) return;

        stateMessageText.text = message;
        stateMessageText.color = color ?? normalColor;
    }

    /// <summary>
    /// 메시지 지우기
    /// </summary>
    public void ClearMessage()
    {
        if (stateMessageText != null)
        {
            stateMessageText.text = "";
        }
    }

    /// <summary>
    /// 모든 상호작용 비활성화
    /// </summary>
    private void DisableAllInteractions()
    {
        inventoryController?.DisableAllButtons();
        tileGridManager?.DisableAllTiles();
    }

    /// <summary>
    /// 모든 상호작용 활성화
    /// </summary>
    private void EnableAllInteractions()
    {
        inventoryController?.EnableAllButtons();
        tileGridManager?.EnableAllTiles();
    }

    /// <summary>
    /// StateMessageText 외부 설정 (Inspector 또는 코드에서)
    /// </summary>
    public void SetStateMessageText(TextMeshProUGUI textComponent)
    {
        stateMessageText = textComponent;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= OnGameStateChanged;
        }
    }
}
