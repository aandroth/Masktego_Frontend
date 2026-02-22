using System.Collections.Generic;
using UnityEngine;
using static UnitData;

public static class PlayerData
{
    public enum PLAYER_TYPE { PLAYER_1 = 1, PLAYER_2 = 2 }
    public static readonly Dictionary<PLAYER_TYPE, Color> PlayerToColorDict = new Dictionary<PLAYER_TYPE, Color>()
    { { PLAYER_TYPE.PLAYER_1, new Color(1, 0.5f, 0) }, 
       {PLAYER_TYPE.PLAYER_2, new Color(1, 0, 1)}  };
}
