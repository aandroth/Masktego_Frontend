using System.Collections.Generic;
using UnityEngine;

public static class UnitData
{
    public enum UNIT_TYPE
    {
        ENEMY = -1,
        ROCK = 1,
        SCISSORS = 2,
        PAPER = 3,
        BOMB = 9,
        FLAG = 7
    }
    public static Dictionary<UNIT_TYPE, int> m_unitTypeToValue = new Dictionary<UNIT_TYPE, int>() {
        { UNIT_TYPE.ENEMY, -1},
        { UNIT_TYPE.ROCK, 1},
        { UNIT_TYPE.SCISSORS, 2},
        { UNIT_TYPE.PAPER, 3 },
        { UNIT_TYPE.BOMB, 9 },
        { UNIT_TYPE.FLAG, 7 }};
    public static Dictionary<UNIT_TYPE, int> m_pieceTypeToPace = new Dictionary<UNIT_TYPE, int>() {
        { UNIT_TYPE.ENEMY, 0},
        { UNIT_TYPE.ROCK, 1},
        { UNIT_TYPE.SCISSORS, 2},
        { UNIT_TYPE.PAPER, 3},
        { UNIT_TYPE.BOMB, 0},
        { UNIT_TYPE.FLAG, 0}};
    public static readonly Dictionary<UNIT_TYPE, Sprite> UnitToColoredSpriteDict = new Dictionary<UNIT_TYPE, Sprite>();
    public static readonly Dictionary<UNIT_TYPE, Sprite> UnitToUncoloredSpriteDict = new Dictionary<UNIT_TYPE, Sprite>();

    public static void InitializeUnitData()
    {
        UnitToColoredSpriteDict[UNIT_TYPE.ENEMY] = Resources.Load<Sprite>("Sprites/Units/CloakedFigureFaceless256");
        UnitToColoredSpriteDict[UNIT_TYPE.ROCK] = Resources.Load<Sprite>("Sprites/Units/RockUncolored256");
        UnitToColoredSpriteDict[UNIT_TYPE.PAPER] = Resources.Load<Sprite>("Sprites/Units/PaperUncolored256");
        UnitToColoredSpriteDict[UNIT_TYPE.SCISSORS] = Resources.Load<Sprite>("Sprites/Units/ScissorsUncolored256");
        UnitToColoredSpriteDict[UNIT_TYPE.BOMB] = Resources.Load<Sprite>("Sprites/Units/BombUncolored256");
        UnitToColoredSpriteDict[UNIT_TYPE.FLAG] = Resources.Load<Sprite>("Sprites/Units/FlagAlone256");

        UnitToUncoloredSpriteDict[UNIT_TYPE.ENEMY] = Resources.Load<Sprite>("Sprites/Units/CloakedFigure256Mask");
        UnitToUncoloredSpriteDict[UNIT_TYPE.ROCK] = Resources.Load<Sprite>("Sprites/Units/Rock256");
        UnitToUncoloredSpriteDict[UNIT_TYPE.PAPER] = Resources.Load<Sprite>("Sprites/Units/Paper256");
        UnitToUncoloredSpriteDict[UNIT_TYPE.SCISSORS] = Resources.Load<Sprite>("Sprites/Units/Scissors256");
        UnitToUncoloredSpriteDict[UNIT_TYPE.BOMB] = Resources.Load<Sprite>("Sprites/Units/Bomb256");
        UnitToUncoloredSpriteDict[UNIT_TYPE.FLAG] = Resources.Load<Sprite>("Sprites/Units/FlagWithPole256");
    }

    public static int GetValueOfUnitType(UNIT_TYPE unitType)
    {
        return m_unitTypeToValue[unitType];
    }

    public static int GetPaceOfUnitType(UNIT_TYPE unitType)
    {
        return m_pieceTypeToPace[unitType];
    }
}
