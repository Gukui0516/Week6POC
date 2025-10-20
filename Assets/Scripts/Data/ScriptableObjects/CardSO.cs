using UnityEngine;

public enum CardType
{
    Orc, //1
    Werewolf, //2
    Goblin, // 3
    Elf, //4 
    Dwarf, // 5
    Angel, // 6
    Dragon, // 7
    Devil, // 8
    Vampire, // 9
    Naga, // 10
    Robot, // 11
    Slime // 12
}

[CreateAssetMenu(fileName = "CardSO_", menuName = "Game/Card Data")]
[System.Serializable]
public class CardData : ScriptableObject
{
    public int id;
    public CardType cardType;
    public string cardName;
    public string description;
    public string tooltipDescription;
    //public string effectDescription;
    public string synergyDescription;
    public string penaltyDescription;

    public int baseScore;
    public string formula;
    public Sprite iconSprite;
    public int count;
    public Color backGroundColor;


    public CardData(string description, int baseScore, string formula)
    {
        this.description = description;
        this.baseScore = baseScore;
        this.formula = formula;
    }
}