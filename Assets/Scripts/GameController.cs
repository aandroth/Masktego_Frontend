using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Player;
using static PlayerData;

public class GameController : MonoBehaviour
{
    [SerializeField] Board m_board;
    [SerializeField] MousePointerCollider m_mousePointerCollider;
    [SerializeField] List<Unit> m_allUnits = null;
    [SerializeField] Unit m_selectedUnit = null;
    [SerializeField] StartPanel m_startPanel = null;
    [SerializeField] GameObject m_menuPanel = null;
    [SerializeField] Backend m_backend = null;
    [SerializeField] float m_timeOutro = 5f;

    [SerializeField] GameObject m_winnerPanel;
    [SerializeField] GameObject m_bannerOrangeWin;
    [SerializeField] GameObject m_bannerPurpleWin;
    [SerializeField] GameObject m_bannerDraw;
    [SerializeField] GameObject m_winnerFlagWithPole;
    [SerializeField] Image m_winnerFlagSpriteRenderer;

    [SerializeField] bool m_controlsFrozen = false;
    [SerializeField] bool m_player1FlagSwapped = false, m_player2FlagSwapped = false;
    [SerializeField] bool m_player1BombSwapped = false, m_player2BombSwapped = false;

    [SerializeField] GameObject m_phasePanel;
    [SerializeField] GameObject m_player1MoveBanner;
    [SerializeField] GameObject m_player2MoveBanner;
    [SerializeField] GameObject m_swapPhaseBanner;
    [SerializeField] Image m_bombSwapBanner;
    [SerializeField] Image m_flagSwapBanner;


    public enum PLAY_MODE { SOLO, VS_AI, ONLINE}
    public PLAY_MODE m_playMode = PLAY_MODE.SOLO;


    public enum GAME_STATE { PLAYER_1_TURN, PLAYER_2_TURN, SWAP_PHASE }
    public GAME_STATE m_gameState = GAME_STATE.PLAYER_1_TURN;

    public enum WIN_STATE { ORANGE_WIN, PURPLE_WIN, DRAW }
    public WIN_STATE m_winState;

    

    void Start()
    {
        UnitData.InitializeUnitData();
        m_startPanel.m_changeGameMode = SetPlayMode;
        m_backend.ReceivedMessageForGameController = ReceivedMessage;
    }


    void Update()
    {
        if (!m_controlsFrozen && Input.GetMouseButtonDown(0))
        {
            BoardCell highlightedCell = m_mousePointerCollider.GetBoardCellInHover();
            Unit unit = m_mousePointerCollider.GetUnitInHover();

            if (unit != null && m_selectedUnit == null)
            {
                SelectUnit(unit);
                return;
            }

            if((Player.m_playerType == PlayerData.PLAYER_TYPE.PLAYER_1 && m_gameState == GAME_STATE.PLAYER_1_TURN) || 
               (Player.m_playerType == PlayerData.PLAYER_TYPE.PLAYER_2 && m_gameState == GAME_STATE.PLAYER_2_TURN))
                PlayerMovesOrAttacksOrDeselects(highlightedCell, unit);
            else if(m_gameState == GAME_STATE.SWAP_PHASE)
            {
                if (unit == null ||
                   (m_playerId == 1 && m_player1FlagSwapped && unit.GetUnitValue() == 7) ||
                   (m_playerId == 2 && m_player2FlagSwapped && unit.GetUnitValue() == 7) ||
                   (m_playerId == 1 && m_player1BombSwapped && unit.GetUnitValue() == 9) ||
                   (m_playerId == 2 && m_player2BombSwapped && unit.GetUnitValue() == 9))
                    return;

                PlayerSwapsOrDeselects(unit);

            }
        }
        if (Input.GetKeyUp(KeyCode.Escape) && !m_startPanel.gameObject.activeSelf)
        {
            m_menuPanel.SetActive(!m_menuPanel.activeSelf);
            m_phasePanel.SetActive(!m_menuPanel.activeSelf);
        }
    }

    public void SetPlayMode(int playMode)
    {
        // { 0: SOLO, 1: VS_AI, 2: ONLINE}
        m_playMode = (PLAY_MODE)playMode;
    }

    public void SetPlayMode(PLAY_MODE playMode)
    {
        // { 0: SOLO, 1: VS_AI, 2: ONLINE}
        m_playMode = playMode;
    }

    private void PlayerMovesOrAttacksOrDeselects(BoardCell highlightedCell, Unit unit)
    {
        if (highlightedCell != null)
        {
            if (unit == null && GetUnitAtCellPosition(highlightedCell.GetCellPosition()) != null)
                unit = GetUnitAtCellPosition(highlightedCell.GetCellPosition());

            if (unit == null)
            {
                Vector2Int pos0 = m_selectedUnit.GetCellPosition();
                Vector2Int pos1 = highlightedCell.GetCellPosition();
                m_board.MoveValueFromCellToCell(pos0, pos1);
                m_selectedUnit.SetUnitToCellPosition(pos1);
                DeselectSelectedUnit();
                if(m_playMode == PLAY_MODE.ONLINE)
                    SendMoveToServer(pos0, pos1);
                PlayerMoveFinished();
            }
            else if (m_board.GetCellValue(highlightedCell.GetCellPosition()) > 0)
            {
                if (m_selectedUnit != unit)
                {
                    DeselectSelectedUnit();
                    SelectUnit(unit);
                }
            }
            else if (m_board.GetCellValue(highlightedCell.GetCellPosition()) < 0)
            {
                if(m_playMode == PLAY_MODE.ONLINE)
                    SendMoveToServer(m_selectedUnit.GetCellPosition(), unit.GetCellPosition());
                StartCoroutine(BeginAttack(m_selectedUnit, unit));
            }
        }
        else
        {
            DeselectSelectedUnit();
            if (unit != null && m_board.GetCellValue(unit.GetCellPosition()) > 0)
                SelectUnit(unit);
        }
    }

    private void PlayerSwapsOrDeselects(Unit unit)
    {
        if (unit != null && m_board.GetCellValue(unit.GetCellPosition()) > 0)
        {
            if (m_selectedUnit.GetUnitValue() == 7 || unit.GetUnitValue() == 7)
            {
                if (m_playerId == 1)
                    m_player1FlagSwapped = true;
                else
                    m_player2FlagSwapped = true;
                Color color = m_flagSwapBanner.color;
                color.a = 0.2f;
                m_flagSwapBanner.color = color;
            }

            if (m_selectedUnit.GetUnitValue() == 9 || unit.GetUnitValue() == 9)
            {
                if (m_playerId == 1)
                    m_player1BombSwapped = true;
                else
                    m_player2BombSwapped = true;
                Color color = m_bombSwapBanner.color;
                color.a = 0.2f;
                m_bombSwapBanner.color = color;
            }
            Vector2Int unitPos0 = m_selectedUnit.GetCellPosition();
            Vector2Int unitPos1 = unit.GetCellPosition();
            m_board.SwapBoardValues(unitPos0, unitPos1);
            m_selectedUnit.SetUnitToCellPosition(unitPos1);
            unit.SetUnitToCellPosition(unitPos0);
            DeselectSelectedUnit();

            if(m_playMode == PLAY_MODE.ONLINE)
                SendSwapToServer(unitPos0, unitPos1);
            PlayerMoveFinished();
        }
        else
        {
            DeselectSelectedUnit();
        }
    }

    public void SendSwapToServer(Vector2Int pos0, Vector2Int pos1)
    {
        m_backend.SendSwapToServer(m_playerId, pos0, pos1);
    }

    public void SendMoveToServer(Vector2Int pos0, Vector2Int pos1)
    {
        m_backend.SendBoardChangeToServer(m_playerId, pos0, pos1);
    }

    public void PlayerMoveFinished()
    {
        if (m_gameState == GAME_STATE.PLAYER_1_TURN)
        {
            m_gameState = GAME_STATE.PLAYER_2_TURN;
            m_player1MoveBanner.SetActive(false);
            m_player2MoveBanner.SetActive(true);
            if (m_playMode == PLAY_MODE.SOLO)
                SwitchToPlayer(2);
            else
            {
                m_controlsFrozen = m_playerId == 1;
            }
        }
        else if (m_gameState == GAME_STATE.PLAYER_2_TURN)
        {
            m_gameState = GAME_STATE.SWAP_PHASE;
            m_player2MoveBanner.SetActive(false);
            m_swapPhaseBanner.SetActive(true);
            if (m_controlsFrozen) 
                m_controlsFrozen = false;
            if (m_playMode == PLAY_MODE.SOLO)
                SwitchToPlayer(1);
        }
        else if (m_gameState == GAME_STATE.SWAP_PHASE)
        {
            if(m_playMode == PLAY_MODE.SOLO)
            {
                if (m_playerId == 1)
                {
                    SwitchToPlayer(2);
                }
                else
                {
                    m_swapPhaseBanner.SetActive(false);
                    m_player1MoveBanner.SetActive(true);
                    m_gameState = GAME_STATE.PLAYER_1_TURN;
                    SwitchToPlayer(1);
                }
            }
        }
    }

    public void SwitchToPlayer(int id)
    {
        if (id == 1)
        {
            m_playerId = 1;
            m_playerType = PLAYER_TYPE.PLAYER_1;
            m_board.ReverseBoardValues();
        }
        else // (id == 2)
        {
            m_playerId = 2;
            m_playerType = PLAYER_TYPE.PLAYER_2;
            m_board.ReverseBoardValues();
        }

        foreach (Unit unit in m_allUnits)
        {
            if (m_board.GetCellValue(unit.GetCellPosition()) < 0)
                unit.StartFlipToEnemySprites(Mathf.Max(2f, 0));
            else
                unit.StartFlipToNormalSprites(Mathf.Max(2f, 0));
        }
    }

    public void GetBoardDataFromServer(string[] boardChangeDataArray)
    {
        //"Action, playerId, pos0.x | pos0.y, pos1.x | pos1.y
        //      0,        1,               2,               3
        string[] pos0AsString = boardChangeDataArray[2].Split('|');
        string[] pos1AsString = boardChangeDataArray[3].Split('|');
        Vector2Int pos0 = new Vector2Int(int.Parse(pos0AsString[0]), int.Parse(pos0AsString[1]));
        Vector2Int pos1 = new Vector2Int(int.Parse(pos1AsString[0]), int.Parse(pos1AsString[1]));
        int value0 = m_board.GetCellValue(pos0);
        int value1 = m_board.GetCellValue(pos1);

        Unit unit0 = GetUnitAtCellPosition(pos0);
        Unit unit1 = GetUnitAtCellPosition(pos1);

        if(value0 < 0 && (value1 < 0 || value1 == 0)) // Swap/Move from other Player
        {
            m_board.SwapBoardValues(pos0, pos1);
            unit0.SetUnitToCellPosition(pos1);
            if (unit1 != null) unit1.SetUnitToCellPosition(pos0);
            PlayerMoveFinished();
        }
        else if(value0 < 0 && value1 > 0) // Attack from other Player
        {
            StartCoroutine(BeginAttack(unit0, unit1));
        }
    }

    private Unit GetUnitAtCellPosition(Vector2Int v)
    {
        foreach (Unit unit in m_allUnits)
        {
            if (unit.GetCellPosition() == v)
            {
                return unit;
            }
        }
        return null;
    }

    private void SelectUnit(Unit unit)
    {
        if (unit == null ||
            (m_playerId == 1 && m_player1FlagSwapped && unit.GetUnitValue() == 7) ||
            (m_playerId == 1 && m_player1BombSwapped && unit.GetUnitValue() == 9) ||
            (m_playerId == 2 && m_player2FlagSwapped && unit.GetUnitValue() == 7) ||
            (m_playerId == 2 && m_player2BombSwapped && unit.GetUnitValue() == 9))
            return;
        if (m_board.GetCellValue(unit.GetCellPosition()) > 0)
        {
            m_selectedUnit = unit;
            m_selectedUnit.UnitSelected();
            if (m_gameState != GAME_STATE.SWAP_PHASE)
                m_board.ActivateHighlightSquaresFromCellWithinPace(m_selectedUnit.GetCellPosition(), m_selectedUnit.GetUnitMoveValue());
        }
    }

    private void DeselectSelectedUnit()
    {
        if (m_selectedUnit == null) return;
        m_selectedUnit.UnitUnselected();
        m_board.DeactivateAllHighlightSquares();
        m_selectedUnit = null;
    }

    private IEnumerator BeginAttack(Unit unitAttacking, Unit unitDefending)
    {
        m_controlsFrozen = true;
        float attackingDelay = 2.0f;
        Unit enemyUnit = (m_board.GetCellValue(unitAttacking.GetCellPosition()) < 0) ? unitAttacking : unitDefending;
        enemyUnit.StartFlipToNormalSprites(Mathf.Max(attackingDelay-0.5f, 0));
        while (attackingDelay > 0)
        {
            attackingDelay -= Time.deltaTime;
            yield return null;
        }
        ResolveAttack(unitAttacking, unitDefending);
        if(enemyUnit != null)
        {
            attackingDelay = 2.0f;
            enemyUnit.StartFlipToEnemySprites(Mathf.Max(attackingDelay - 0.5f, 0));
            while (attackingDelay > 0)
            {
                attackingDelay -= Time.deltaTime;
                yield return null;
            }
        }
        m_controlsFrozen = false;
        PlayerMoveFinished();
        DetermineIfPlayerWon();
    }

    private void ResolveAttack(Unit unitAttacking, Unit unitDefending)
    {
        if (unitAttacking.GetUnitValue() == 9 || unitDefending.GetUnitValue() == 9 ||
           unitAttacking.GetUnitValue() == unitDefending.GetUnitValue())
        {
            Debug.Log("Both Units Destroyed");
            m_board.SetCellValue(unitAttacking.GetCellPosition(), 0);
            m_board.SetCellValue(unitDefending.GetCellPosition(), 0);
            DestroyUnit(unitAttacking);
            DestroyUnit(unitDefending);
        }
        else if((unitAttacking.GetUnitValue() == 1 && unitDefending.GetUnitValue() == 2) ||
            (unitAttacking.GetUnitValue() == 2 && unitDefending.GetUnitValue() == 3) ||
            (unitAttacking.GetUnitValue() == 3 && unitDefending.GetUnitValue() == 1) ||
            (unitDefending.GetUnitValue() == 7))
        {
            Debug.Log("Unit 0 Wins");
            m_board.MoveValueFromCellToCell(unitAttacking.GetCellPosition(), unitDefending.GetCellPosition());
            unitAttacking.SetUnitToCellPosition(unitDefending.GetCellPosition());
            DestroyUnit(unitDefending);
        }
        else if((unitDefending.GetUnitValue() == 1 && unitAttacking.GetUnitValue() == 2) ||
            (unitDefending.GetUnitValue() == 2 && unitAttacking.GetUnitValue() == 3) ||
            (unitDefending.GetUnitValue() == 3 && unitAttacking.GetUnitValue() == 1))
        {
            Debug.Log("Unit 1 Wins");
            m_board.SetCellValue(unitAttacking.GetCellPosition(), 0);
            DestroyUnit(unitAttacking);
        }
        else
        {
            Debug.Log("No valid attack scenario");
        }
        DeselectSelectedUnit();
    }

    public void DetermineIfPlayerWon()
    {
        bool orangeHasMovableUnits = false;
        bool purpleHasMovableUnits = false;
        bool orangeHasFlag = false;
        bool purpleHasFlag = false;

        foreach (Unit unit in m_allUnits)
        {
            if (m_playerId == 1)
            {
                if (!orangeHasMovableUnits)
                {
                    if (m_board.GetCellValue(unit.GetCellPosition()) == 1 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == 2 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == 3)
                        orangeHasMovableUnits = true;
                }
                if (!purpleHasMovableUnits)
                {
                    if (m_board.GetCellValue(unit.GetCellPosition()) == -1 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == -2 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == -3)
                        purpleHasMovableUnits = true;
                }
                if (m_board.GetCellValue(unit.GetCellPosition()) == 7)
                    orangeHasFlag = true;
                if (m_board.GetCellValue(unit.GetCellPosition()) == -7)
                    purpleHasFlag = true;
            }
            else // m_playerId == 2
            {
                if (!purpleHasMovableUnits)
                {
                    if (m_board.GetCellValue(unit.GetCellPosition()) == 1 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == 2 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == 3)
                        purpleHasMovableUnits = true;
                }
                if (!orangeHasMovableUnits)
                {
                    if (m_board.GetCellValue(unit.GetCellPosition()) == -1 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == -2 ||
                        m_board.GetCellValue(unit.GetCellPosition()) == -3)
                        orangeHasMovableUnits = true;
                }
                if (m_board.GetCellValue(unit.GetCellPosition()) == 7)
                    purpleHasFlag = true;
                if (m_board.GetCellValue(unit.GetCellPosition()) == -7)
                    orangeHasFlag = true;
            }

            if (orangeHasMovableUnits && orangeHasFlag &&
               purpleHasMovableUnits && purpleHasFlag)
                return;
        }

        if((!orangeHasFlag) ||
           (!orangeHasMovableUnits && purpleHasMovableUnits))
        {
            m_winState = WIN_STATE.PURPLE_WIN;
            ActivateWinnerBanner();
        }
        else if(!purpleHasFlag ||
           (!purpleHasMovableUnits && orangeHasMovableUnits))
        {
            m_winState = WIN_STATE.ORANGE_WIN;
            ActivateWinnerBanner();
        }
        else if (!orangeHasMovableUnits && !purpleHasMovableUnits)
        {
            m_winState = WIN_STATE.DRAW;
            ActivateWinnerBanner();
        }
        StartOutro();
    }

    public void ChangeStateToPlayer1Turn()
    {
        if (m_playMode == PLAY_MODE.SOLO)
            SwitchToPlayer(1);
        else
            m_controlsFrozen = m_playerId == 2;
        m_gameState = GAME_STATE.PLAYER_1_TURN;
        m_swapPhaseBanner.SetActive(false);
        m_player1MoveBanner.SetActive(true);
    }

    public void ChangeStateToSwap()
    {
        m_gameState = GAME_STATE.SWAP_PHASE;
    }

    public void CreateUnits()
    {
        DestroyAllUnits();
        m_allUnits = new List<Unit>(m_board.FillBoardWithUnitsAndDelegates());
    }

    private void DestroyUnit(Unit unit)
    {
        m_allUnits.Remove(unit);
        unit.Destroyed();
    }

    private void DestroyAllUnits()
    {
        if (m_allUnits != null && m_allUnits.Count != 0)
        {
            while (m_allUnits.Count > 0)
                DestroyUnit(m_allUnits[0]);
        }
    }

    public void StartOutro()
    {
        StartCoroutine(OutroCoroutine());
    }

    public IEnumerator OutroCoroutine()
    {
        float timeOutro = m_timeOutro;
        while (timeOutro > 0)
        {
            timeOutro -= Time.deltaTime;
            yield return null;
        }
        QuitToStart();
    }

    public void ActivateWinnerBanner()
    {
        m_winnerPanel.SetActive(true);

        if (m_winState == WIN_STATE.ORANGE_WIN)
        {
            m_bannerOrangeWin.SetActive(true);
            m_winnerFlagSpriteRenderer.color = PlayerData.PlayerToColorDict[PLAYER_TYPE.PLAYER_1];
        }
        else if (m_winState == WIN_STATE.PURPLE_WIN)
        {
            m_bannerPurpleWin.SetActive(true);
            m_winnerFlagSpriteRenderer.color = PlayerData.PlayerToColorDict[PLAYER_TYPE.PLAYER_2];
        }
        else
            m_bannerDraw.SetActive(true);
    }


    public void ReceivedMessage(string data, string action, string[] serverData)
    {
        int id = -1;
        if (serverData.Length >= 2)
        {
            try
            {
                id = serverData.Length >= 2 ? int.Parse(serverData[1]) : -1;
                Debug.Log($"Received: {serverData} with action {action} and id {id}, playerData length: {serverData.Length}");
            }
            catch
            {
                Debug.LogWarning($"Failed to parse player id from data: {data}");
            }
        }

        switch (action)
        {
            case "Init":
                m_playerId = id;
                m_playerType = id == 1 ? PLAYER_TYPE.PLAYER_1 : PLAYER_TYPE.PLAYER_2;
                m_controlsFrozen = m_playerType == PLAYER_TYPE.PLAYER_1 ? false : true;
                m_startPanel.gameObject.SetActive(false);
                m_phasePanel.SetActive(true);
                CreateUnits();
                if(m_playerId == 2) m_board.ReverseBoardValues();
                FlipAllEnemyUnits();
                break;
            case "Board_Update":
                if (m_playMode != PLAY_MODE.SOLO)
                    GetBoardDataFromServer(serverData);
                break;
            case "Player_1_Turn":
                ChangeStateToPlayer1Turn();
                break;
            case "Start_Outro":
                StartOutro();
                break;
            case "Disconnect":
                m_startPanel?.gameObject?.SetActive(true);
                m_phasePanel.SetActive(false);
                DestroyAllUnits();
                break;
        }
    }

    public void StartSoloGame()
    {
        m_playMode = PLAY_MODE.SOLO;
        ReceivedMessage("Init,1", "Init", new string[] { "Init", "1" });
    }

    public void FlipAllEnemyUnits()
    {
        foreach (Unit unit in m_allUnits)
        {
            if (m_board.GetCellValue(unit.GetCellPosition()) < 0)
                unit.StartFlipToEnemySprites(Mathf.Max(2f, 0));
        }
    }

    public void QuitToStart()
    {
        if (m_backend.m_connected) m_backend.CancelConnection();
        SceneManager.LoadScene(0);
    }

    public void CloseGame()
    {
        Application.Quit();
    }
}