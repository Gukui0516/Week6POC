using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타일 그리드 전체를 관리하는 매니저
/// 모든 BlockPuzzleTile의 초기화, 상태 업데이트, 활성화/비활성화를 담당
/// </summary>
public class TileGridManager : MonoBehaviour
{
    private BlockPuzzleTile[] tiles;
    private GameManager gameManager;

    /// <summary>
    /// TileGridManager 초기화
    /// </summary>
    /// <param name="gm">GameManager 인스턴스</param>
    public void Initialize(GameManager gm)
    {
        gameManager = gm;

        if (gameManager == null)
        {
            Debug.LogError("[TileGridManager] GameManager를 찾을 수 없습니다!");
            return;
        }

        FindAndInitializeTiles();
    }

    /// <summary>
    /// 씬에서 모든 타일을 찾아 초기화
    /// </summary>
    private void FindAndInitializeTiles()
    {
        tiles = FindObjectsByType<BlockPuzzleTile>(FindObjectsSortMode.None);

        if (tiles == null || tiles.Length == 0)
        {
            Debug.LogWarning("[TileGridManager] 타일을 찾을 수 없습니다!");
            return;
        }
    }

    /// <summary>
    /// 모든 타일의 시각적 상태 업데이트
    /// </summary>
    public void UpdateAllTiles()
    {
        if (tiles == null || gameManager == null) return;
        if (gameManager.GetBoard() == null) return;

        foreach (var tile in tiles)
        {
            tile.UpdateVisual();
        }
    }

    /// <summary>
    /// 모든 타일 버튼 활성화
    /// </summary>
    public void EnableAllTiles()
    {
        if (tiles == null) return;

        foreach (var tile in tiles)
        {
            var button = tile.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true;
            }
        }
    }

    /// <summary>
    /// 모든 타일 버튼 비활성화
    /// </summary>
    public void DisableAllTiles()
    {
        if (tiles == null) return;

        foreach (var tile in tiles)
        {
            var button = tile.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    /// <summary>
    /// 특정 좌표의 타일 찾기
    /// </summary>
    /// <param name="x">X 좌표</param>
    /// <param name="y">Y 좌표</param>
    /// <returns>해당 좌표의 타일, 없으면 null</returns>
    public BlockPuzzleTile GetTileAt(int x, int y)
    {
        if (tiles == null) return null;

        foreach (var tile in tiles)
        {
            if (tile.x == x && tile.y == y)
            {
                return tile;
            }
        }

        return null;
    }

    /// <summary>
    /// 모든 타일 배열 반환
    /// </summary>
    public BlockPuzzleTile[] GetAllTiles() => tiles;

    /// <summary>
    /// 타일 개수 반환
    /// </summary>
    public int GetTileCount() => tiles?.Length ?? 0;
}
