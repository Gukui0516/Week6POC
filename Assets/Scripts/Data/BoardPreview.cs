using GameCore.Data;

[System.Serializable]
public class BoardPreview
{
    public int previewX, previewY;
    public BlockType previewBlockType;
    public int[,] originalScores;    // 3x3
    public int[,] previewScores;     // 3x3
    public int totalScoreChange;

    public BoardPreview()
    {
        originalScores = new int[3, 3];
        previewScores = new int[3, 3];
    }

    public int GetScoreChange(int x, int y)
    {
        return previewScores[x, y] - originalScores[x, y];
    }
}
