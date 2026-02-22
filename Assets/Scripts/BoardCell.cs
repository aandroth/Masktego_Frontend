using UnityEngine;

public class BoardCell : MonoBehaviour
{
    public delegate Color OnCellHoverColor(bool b);
    public OnCellHoverColor m_onCellHoverEvent;
    public delegate Color OffCellHoverColor(bool b);
    public OffCellHoverColor m_offCellHoverEvent;

    [SerializeField] Vector2Int m_cellPosition = new Vector2Int(-1, -1);
    [SerializeField] SpriteRenderer m_spriteRenderer;
    public enum CELL_STATE { NORMAL, ATTACK }
    [SerializeField] public CELL_STATE m_cellState = CELL_STATE.NORMAL;

    private void Start()
    {
        m_spriteRenderer = GetComponent<SpriteRenderer>();  
    }

    public Vector2Int GetCellPosition()
    {
        return m_cellPosition;
    }

    public CELL_STATE GetCellState()
    {
        return m_cellState;
    }

    public void SetCellStateToAttack()
    {
        m_cellState = CELL_STATE.ATTACK;
    }

    private void OnDisable()
    {
        m_cellState = CELL_STATE.NORMAL;

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(m_spriteRenderer != null && collision.GetComponent<MousePointerCollider>() != null)
            m_spriteRenderer.color = m_onCellHoverEvent.Invoke(m_cellState == CELL_STATE.NORMAL);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (m_spriteRenderer != null && collision.GetComponent<MousePointerCollider>() != null)
            m_spriteRenderer.color = m_offCellHoverEvent.Invoke(m_cellState == CELL_STATE.NORMAL);
    }
}
