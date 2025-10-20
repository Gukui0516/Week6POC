using System.Collections.Generic;
using UnityEngine;

namespace GameCore.Data
{
    [System.Serializable]
    public enum TileMode { NoNumbers, WithNumbers }

    [System.Serializable]
    public enum GameState
    {
        Playing,
        GameOver,
        Victory,
        Shop  
    }


    [System.Serializable]
    public class Card
    {
        public CardType type;
        public int baseScore;

        public Card(CardType type)
        {
            this.type = type;
            baseScore = CardDataLoader.GetData(type).baseScore;
        }
    }

    [System.Serializable]
    public class Tile
    {
        public int x, y;
        public Card block;
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
        public List<Card> availableBlocks;
        public int currentTurnScore;

        public TurnData(int turnNumber, int targetScore)
        {
            this.turnNumber = turnNumber;
            this.targetScore = targetScore;
            this.availableBlocks = new List<Card>();
            this.currentTurnScore = 0;
        }
    }


    public class GlobalScoreData
    {
        public int emptyTileCount;
        public Dictionary<CardType, int> blockCounts;
        public int uniqueTypesCount;
        public int uniqueTypesExcludingF;

        public GlobalScoreData()
        {
            blockCounts = new Dictionary<CardType, int>();
        }
    }

    // 점수 계산 과정의 각 수정자를 추적하기 위한 클래스
    [System.Serializable]
    public class ScoreModifier
    {
        public string description;  // 수정자 설명 (예: "인접한 Orc 1개")
        public int value;           // 점수 변화량 (예: -1)
        public string reason;       // 공식/이유 (예: "인접 Orc 있을 때 -1")

        public ScoreModifier(string description, int value, string reason)
        {
            this.description = description;
            this.value = value;
            this.reason = reason;
        }
    }
}