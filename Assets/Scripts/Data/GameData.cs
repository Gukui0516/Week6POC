using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Data
{
    [System.Serializable]
    public enum BlockType { A, B, C, D, E, F, G }

    [System.Serializable]
    public enum TileMode { NoNumbers, WithNumbers }

    [System.Serializable]
    public enum GameState { Playing, GameOver, Victory }

    [System.Serializable]
    public class Block
    {
        public BlockType type;
        public int baseScore;

        public Block(BlockType type)
        {
            this.type = type;
            this.baseScore = GetBaseScore(type);
        }

        private int GetBaseScore(BlockType type)
        {
            switch (type)
            {
                case BlockType.A: return 2;
                case BlockType.B: return 1;
                case BlockType.C: return 0;
                case BlockType.D: return 1;
                case BlockType.E: return 4;
                case BlockType.F: return 0;
                case BlockType.G: return 5;
                default: return 0;
            }
        }
    }

    [System.Serializable]
    public class Tile
    {
        public int x, y;
        public Block block;
        public int tileNumber;
        public int calculatedScore;
        public int placedTurn;

        public Tile(int x, int y)
        {
            this.x = x;
            this.y = y;
            this.block = null;
            this.tileNumber = 0;
            this.calculatedScore = 0;
            this.placedTurn = 0;
        }

        public bool IsEmpty => block == null;
        public bool HasBlock => block != null;
        public bool IsRemovable(int currentTurn) => HasBlock && placedTurn == currentTurn;
    }

    [System.Serializable]
    public class TurnData
    {
        public int turnNumber;
        public int targetScore;
        public List<Block> availableBlocks;
        public int currentTurnScore;

        public TurnData(int turnNumber, int targetScore)
        {
            this.turnNumber = turnNumber;
            this.targetScore = targetScore;
            this.availableBlocks = new List<Block>();
            this.currentTurnScore = 0;
        }
    }


    public class GlobalScoreData
    {
        public int emptyTileCount;
        public Dictionary<BlockType, int> blockCounts;
        public int uniqueTypesCount;
        public int uniqueTypesExcludingF;

        public GlobalScoreData()
        {
            blockCounts = new Dictionary<BlockType, int>();
        }
    }
}