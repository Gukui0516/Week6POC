using UnityEngine;

public enum CardType
{
    Orc,
    Werewolf,
    Goblin,
    Elf,
    Dwarf,
    Angel,
    Dragon,
    Troll,
    Vampire,
    Naga,
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
    public string effectDescription;
    public int baseScore;
    public string formula;
    public Sprite iconSprite;

    public CardData(string description, int baseScore, string formula)
    {
        this.description = description;
        this.baseScore = baseScore;
        this.formula = formula;
    }
}