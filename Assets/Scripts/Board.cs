using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static GameController;

public class Board : MonoBehaviour
{
    [System.Serializable]
    public struct HighlightRow
    {
        [SerializeField] public List<GameObject> rowSquares;
    }


    // 0 = empty, 8 = imappassable
    // (player 1)  1 = Piece_Rock,  2 = Piece_Paper,  3 = Piece_Scissors,  9 = Piece_Bomb,  7 = Piece_Flag
    // (player 2) -1 = Piece_Rock, -2 = Piece_Paper, -3 = Piece_Scissors, -9 = Piece_Bomb, -7 = Piece_Flag
    [SerializeField] int[][] m_boardCells = new int[5][];

    [SerializeField] List<HighlightRow> m_highlightSquares = new List<HighlightRow>();
    [SerializeField] List<Vector2Int> m_squaresAvailableForUnitMove = new List<Vector2Int>();
    [SerializeField] GameObject m_unitPrefab = null;

    [SerializeField] Color m_boardHighlightColorNormal, 
                           m_boardHighlightColorTinted, 
                           m_boardHighlightColorAttack,
                           m_boardHighlightColorAttackTinted;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        // Draw about to happen
        //m_boardCells = new int[5][] {
        //    new int[] {  3, -3,  7,   0,   0 },
        //    new int[] {  0,  0,  8,   0,   0 },
        //    new int[] {  0,  0,  0,   9,   0 },
        //    new int[] {  0,  0,  8,   0,   0 },
        //    new int[] {  0,  0, -7,   0,   0 }};

        // Purple about to win
        //m_boardCells = new int[5][] {
        //    new int[] {  1, -1,  7,   2,   0 },
        //    new int[] {  0,  0,  8,   0,   3 },
        //    new int[] {  0, -9,  0,   9,   0 },
        //    new int[] {  0,  0,  8,   0,   0 },
        //    new int[] {  1,  0, -7,   0,   0 }};

        // Orange about to win
        //m_boardCells = new int[5][] {
        //    new int[] {  1,  2,  0,    0,   0 },
        //    new int[] {  3,  1,  8,    0,   0 },
        //    new int[] {  7,  9,  0,   -7,   0 },
        //    new int[] {  3,  1,  8,    0,   0 },
        //    new int[] {  1,  2,  0,   -1,   0 }};


        m_boardCells = new int[5][] {
            new int[] {  1,  2,  0,  -2,  -1 },
            new int[] {  3,  1,  8,  -1,  -3 },
            new int[] {  7,  9,  0,  -9,  -7 },
            new int[] {  3,  1,  8,  -1,  -3 },
            new int[] {  1,  2,  0,  -2,  -1 }};

        Debug.Log(BoardToString(m_boardCells));
    }

    public bool IsCellUnoccupied(int x, int y)
    {
        Debug.Log($"IsCellUnoccupied? at {x},{y}: {m_boardCells[x][y] == 0}");
        return m_boardCells[x][y] == 0;
    }

    public int GetCellValue(int x, int y)
    {
        //Debug.Log($"Cell value at {x},{y}: {m_boardCells[x][y]}");
        return m_boardCells[x][y];
    }

    public int GetCellValue(Vector2Int v)
    {
        //Debug.Log($"Cell value at {v.x},{v.y}: {m_boardCells[v.x][v.y]}");
        return m_boardCells[v.x][v.y];
    }

    public void SetCellValue(Vector2Int v, int i)
    {
        //Debug.Log($"Cell value at {v.x},{v.y}: {m_boardCells[v.x][v.y]}");
        m_boardCells[v.x][v.y] = i;
    }

    public string BoardToString(int[][] board)
    {
        string boardString = "";
        for (int i = 0; i < m_boardCells.Length; i++)
            for (int j = 0; j < m_boardCells[i].Length; j++)
                boardString += $"{m_boardCells[i][j]},";
        return boardString;
    }

    public int[][] StringToBoard(string board)
    {
        string[] boardCells = board.Split(',');
        int[][] boardFromString = new int[10][];
        for (int i = 0; i < m_boardCells.Length; i++) {
            boardFromString[i] = new int[m_boardCells[i].Length];
            for (int j = 0; j < m_boardCells[i].Length; j++)
                boardFromString[i][j] = int.Parse(boardCells[(i*10 + j)]);
        }
        return boardFromString;
    }

    public void SwapBoardValuesForPlayer()
    {
        for (int i = 0; i < m_boardCells.Length; i++)
            for (int j = 0; j < m_boardCells[i].Length; j++)
                if (m_boardCells[i][j] != 8)
                    m_boardCells[i][j] = -m_boardCells[i][j];
    }

    public void SwapBoardValues(Vector2Int pos0, Vector2Int pos1)
    {
        var temp = m_boardCells[pos0.x][pos0.y];
        m_boardCells[pos0.x][pos0.y] = m_boardCells[pos1.x][pos1.y];
        m_boardCells[pos1.x][pos1.y] = temp;
    }

    public void MoveValueFromCellToCell(Vector2Int p0, Vector2Int p1)
    {
        m_boardCells[p1.x][p1.y] = m_boardCells[p0.x][p0.y];
        m_boardCells[p0.x][p0.y] = 0;
        //OutputCellValues();
    }

    public void OutputCellValues()
    {
        for (int i = 0; i < m_boardCells.Length; i++)
        {
            string line = "";
            for (int j = 0; j < m_boardCells[i].Length; j++)
            {
                line += m_boardCells[i][j] + ", ";
            }
            Debug.Log(line);
        }
    }

    public void UpdateBoardWithData(int[][] newBoardCells)
    {
        m_boardCells = newBoardCells;
    }

    public void UpdateBoardWithReversedData(int[][] newBoardCells)
    {
        for (int i = 0; i < m_boardCells.Length; ++i)
        {
            for (int j = 0; j < m_boardCells[i].Length; ++j)
            {
                m_boardCells[i][j] = newBoardCells[i][j] != 8 && newBoardCells[i][j] != 0 ? newBoardCells[i][j] * -1 : newBoardCells[i][j];
            }
        }
    }

    public void ReverseBoardValues()
    {
        for (int i = 0; i < m_boardCells.Length; ++i)
        {
            for (int j = 0; j < m_boardCells[i].Length; ++j)
            {
                if(m_boardCells[i][j] != 8 && m_boardCells[i][j] != 0)
                    m_boardCells[i][j] = m_boardCells[i][j] * -1;
            }
        }
    }

    public void UpdateBoardCellWithData(Vector2Int pos, int value)
    {
        m_boardCells[pos.x][pos.y] = value;
    }

    // Update is called once per frame
    public int[][] GetDataFromBoard()
    {
        return m_boardCells;
    }

    // Update is called once per frame
    public int[][] GetReverseDataFromBoard()
    {
        int[][] reverseBoard = new int[m_boardCells.Length][];
        for(int i = 0; i < m_boardCells.Length; ++i)
        {
            reverseBoard[i] = new int[m_boardCells[i].Length];
            for (int j = 0; j < m_boardCells[i].Length; ++j)
            {
                reverseBoard[i][j] = m_boardCells[i][j] != 8 && m_boardCells[i][j] != 0 ? m_boardCells[i][j] * -1 : m_boardCells[i][j];
            }
        }

        return reverseBoard;
    }

    public void ActivateHighlightSquaresFromCellWithinPace(Vector2Int pos, int pace)
    {
        List<Vector2Int> visitedPositions = new List<Vector2Int>();

        FindAllPathSquares(new Vector2Int(pos.x, pos.y + 1), pace); // up
        visitedPositions = new List<Vector2Int>();
        FindAllPathSquares(new Vector2Int(pos.x, pos.y - 1), pace); // down
        visitedPositions = new List<Vector2Int>();
        FindAllPathSquares(new Vector2Int(pos.x + 1, pos.y), pace); // left
        visitedPositions = new List<Vector2Int>();
        FindAllPathSquares(new Vector2Int(pos.x - 1, pos.y), pace); // right

        void FindAllPathSquares(Vector2Int currPos, int currPace)
        {
            //Debug.Log($"pace of: {currPace}, at {currPos.x},{currPos.y}");
            if (currPos.x < 0 || currPos.x >= m_boardCells.Length ||
                currPos.y < 0 || currPos.y >= m_boardCells[0].Length)
            {
                //Debug.Log($"Out of bounds: {currPos.x},{currPos.y}");
                return;
            }
            if (currPace <= 0)
            {
                //Debug.Log($"Out of pace: {currPace}");
                return;
            }
            if(GetCellValue(currPos) < 0)
            {
                Debug.Log($"Cell value at {currPos.x},{currPos.y}: {m_boardCells[currPos.x][currPos.y]}");
                Debug.Log($"Cell value at {currPos.x},{currPos.y}: {GetCellValue(currPos)}"); OutputCellValues();
                m_highlightSquares[currPos.x].rowSquares[currPos.y].SetActive(true);
                m_highlightSquares[currPos.x].rowSquares[currPos.y].GetComponent<SpriteRenderer>().color = m_boardHighlightColorAttack;
                m_highlightSquares[currPos.x].rowSquares[currPos.y].GetComponent<BoardCell>().m_cellState = BoardCell.CELL_STATE.ATTACK;
                m_squaresAvailableForUnitMove.Add(currPos);
                return;
            }
            if(GetCellValue(currPos) == 8)
            {
                //Debug.Log($"GetCellValue(currPos): {GetCellValue(currPos)}");
                return;
            }
            if(visitedPositions.Contains(currPos))
            {
                //Debug.Log($"Already visited: {currPos}");
                return;
            }

            //Debug.Log($"Highlighting square at {currPos} with pace {currPace}");
            visitedPositions.Add(currPos);
            if (GetCellValue(currPos) == 0)
            {
                m_highlightSquares[currPos.x].rowSquares[currPos.y].SetActive(true);
                m_squaresAvailableForUnitMove.Add(currPos);
            }

            FindAllPathSquares(new Vector2Int(currPos.x, currPos.y+1), currPace - 1); // up
            FindAllPathSquares(new Vector2Int(currPos.x, currPos.y-1), currPace - 1); // down
            FindAllPathSquares(new Vector2Int(currPos.x+1, currPos.y), currPace - 1); // left
            FindAllPathSquares(new Vector2Int(currPos.x-1, currPos.y), currPace - 1); // right
        }
    }

    public void DeactivateAllHighlightSquares()
    {
        foreach (HighlightRow row in m_highlightSquares)
            foreach (GameObject square in row.rowSquares)
                if (square != null)
                {
                    square?.SetActive(false);
                    square.gameObject.GetComponent<SpriteRenderer>().color = m_boardHighlightColorNormal;
                }
        m_squaresAvailableForUnitMove.Clear();
    }

    public bool CellIsActive(Vector2Int cellPos)
    {
        return m_squaresAvailableForUnitMove.Contains(cellPos);
    }

    public bool CellIsActive(BoardCell cell)
    {
        return cell != null && m_squaresAvailableForUnitMove.Contains(cell.GetCellPosition());
    }

    public Color GetHighlightColorForSquareOnHoverEnter(bool cellIsNormal)
    {
        return cellIsNormal ? m_boardHighlightColorTinted : m_boardHighlightColorAttackTinted;
    }

    public Color GetHighlightColorForSquareOnHoverExit(bool cellIsNormal)
    {
        return cellIsNormal ? m_boardHighlightColorNormal : m_boardHighlightColorAttack;
    }

    public List<Unit> FillBoardWithUnitsAndDelegates()
    {
        List<Unit> unitsCreated = new List<Unit>();

        for (int i = 0; i < m_boardCells.Length; ++i)
            for (int j = 0; j < m_boardCells[i].Length; ++j)
            {
                int cellValue = m_boardCells[i][j];
                if (cellValue != 0 && cellValue != 8)
                {
                    Unit unit = GameObject.Instantiate(m_unitPrefab).GetComponent<Unit>();
                    unit.SetUnitType((UnitData.UNIT_TYPE)Mathf.Abs(GetCellValue(i, j)));
                    if (GetCellValue(i, j) < 0)
                    {
                        //Debug.Log($"Setting unit value for enemy unit at {i},{j}: {Mathf.Abs(m_boardCells[i][j])}");
                        unit.SetUnitValue(Mathf.Abs(m_boardCells[i][j]));
                    }
                    unit.SetColor(cellValue > 0 ? PlayerData.PlayerToColorDict[PlayerData.PLAYER_TYPE.PLAYER_1] :
                                                  PlayerData.PlayerToColorDict[PlayerData.PLAYER_TYPE.PLAYER_2]);
                    unit.SetUnitToCellPosition(new Vector2Int(i, j));
                    unitsCreated.Add(unit);
                }

                if (cellValue != 8)
                {
                    m_highlightSquares[i].rowSquares[j].GetComponent<BoardCell>().m_onCellHoverEvent = GetHighlightColorForSquareOnHoverEnter;
                    m_highlightSquares[i].rowSquares[j].GetComponent<BoardCell>().m_offCellHoverEvent = GetHighlightColorForSquareOnHoverExit;
                }
            }
        return unitsCreated;
    }
}
