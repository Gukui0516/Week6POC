using UnityEngine;
using GameCore.Data;

/// <summary>
/// 게임 흐름 제어 컨트롤러
/// 게임 로직(GameManager)과 UI 컨트롤러들 사이의 중개자 역할
/// 블록 배치, 턴 진행 등 게임 플레이 흐름을 관리
/// </summary>
public class GameFlowController : MonoBehaviour
{
    private GameManager gameManager;
    private InventoryController inventoryController;
    private TileGridManager tileGridManager;
    private GameStateUIManager gameStateUIManager;
    private UIController uiController; // 보드 UI 업데이트용

    /// <summary>
    /// GameFlowController 초기화
    /// </summary>
    public void Initialize(
        GameManager gm,
        InventoryController invCtrl,
        TileGridManager tileGrid,
        GameStateUIManager stateUI,
        UIController ui)
    {
        gameManager = gm;
        inventoryController = invCtrl;
        tileGridManager = tileGrid;
        gameStateUIManager = stateUI;
        uiController = ui;

        if (gameManager == null)
        {
            Debug.LogError("[GameFlowController] GameManager를 찾을 수 없습니다!");
            return;
        }

        SubscribeToGameEvents();
    }

    /// <summary>
    /// 게임 이벤트 구독
    /// </summary>
    private void SubscribeToGameEvents()
    {
        if (gameManager != null)
        {
            // 필요시 게임 이벤트 구독 추가
            // gameManager.OnBlockPlaced += OnBlockPlaced;
            // gameManager.OnTurnEnd += OnTurnEnd;
        }
    }

    /// <summary>
    /// 선택된 블록을 지정된 위치에 배치 시도
    /// 타일에서 호출되는 메인 메서드
    /// </summary>
    /// <param name="x">배치할 X 좌표</param>
    /// <param name="y">배치할 Y 좌표</param>
    /// <returns>배치 성공 여부</returns>
    public bool TryPlaceSelectedBlock(int x, int y)
    {
        // 1. 검증: InventoryController 확인
        if (inventoryController == null)
        {
            Debug.LogWarning("[GameFlowController] InventoryController가 초기화되지 않았습니다.");
            return false;
        }

        // 2. 검증: 선택된 블록 확인
        var selectedBlockType = inventoryController.GetSelectedCardType();
        if (selectedBlockType == null)
        {
            return false;
        }

        // 3. 게임 로직: GameManager를 통해 블록 배치
        bool success = gameManager.PlaceBlock(x, y, selectedBlockType.Value);

        // 4. 후처리: 배치 성공 시 처리
        if (success)
        {
            OnBlockPlacedSuccessfully(x, y, selectedBlockType.Value);
        }

        return success;
    }

    /// <summary>
    /// 블록 배치 성공 시 호출
    /// </summary>
    private void OnBlockPlacedSuccessfully(int x, int y, CardType blockType)
    {
        // 선택 상태 유지 (연속 배치 가능)
        inventoryController?.OnBlockPlaced(keepSelection: true);

        // UI 업데이트는 GameManager의 이벤트를 통해 자동으로 처리됨
        // (OnBoardUpdated, OnScoreUpdated 등)
    }

    /// <summary>
    /// 턴 종료 처리
    /// </summary>
    public void OnTurnEnd()
    {
        // 턴 종료 시 선택 해제
        inventoryController?.DeselectBlock();

        // 턴 종료 시 필요한 처리
        // 예: 애니메이션, 사운드, 특수 효과 등
    }

    /// <summary>
    /// 게임 재시작 처리
    /// </summary>
    public void OnGameRestart()
    {
        // 재시작 시 필요한 처리
        inventoryController?.DeselectBlock();
        // 필요시 추가 초기화
    }

    /// <summary>
    /// 현재 선택된 블록 타입 반환
    /// </summary>
    public CardType? GetSelectedCardType()
    {
        return inventoryController?.GetSelectedCardType();
    }

    /// <summary>
    /// 블록 선택 해제
    /// </summary>
    public void DeselectBlock()
    {
        inventoryController?.DeselectBlock();
    }

    /// <summary>
    /// 특정 좌표에 블록 배치 가능 여부 확인
    /// </summary>
    public bool CanPlaceBlockAt(int x, int y, CardType blockType)
    {
        if (gameManager == null) return false;

        var board = gameManager.GetBoard();
        if (board == null) return false;

        var tile = gameManager.GetTile(x, y);
        if (tile == null) return false;

        // 빈 타일인지 확인
        return tile.IsEmpty;
    }

    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (gameManager != null)
        {
            // gameManager.OnBlockPlaced -= OnBlockPlaced;
            // gameManager.OnTurnEnd -= OnTurnEnd;
        }
    }

    #region Public API for External Controllers

    /// <summary>
    /// 외부에서 게임 흐름 제어가 필요할 때 사용하는 공개 API
    /// </summary>

    /// <summary>
    /// 블록을 직접 배치 (선택 없이)
    /// </summary>
    public bool PlaceBlock(int x, int y, CardType blockType)
    {
        if (gameManager == null) return false;
        return gameManager.PlaceBlock(x, y, blockType);
    }

    /// <summary>
    /// 현재 게임 상태 반환
    /// </summary>
    public GameState GetGameState()
    {
        return gameManager?.GetGameState() ?? GameState.Playing;
    }

    /// <summary>
    /// 현재 턴 정보 반환
    /// </summary>
    public TurnData GetCurrentTurn()
    {
        return gameManager?.GetCurrentTurn();
    }

    #endregion
}
