using System.Collections.Generic;
using UnityEngine;

public class MousePointerCollider : MonoBehaviour
{

    [SerializeField] Unit m_unitInHover = null;
    [SerializeField] BoardCell m_cellInHover = null;
    [SerializeField] bool m_mouseIsControllable = true;
    [SerializeField] Vector3 screenPointOfMouse;

    private void Update()
    {
        if (m_mouseIsControllable)
        {
            screenPointOfMouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            screenPointOfMouse.z = 0;

            transform.position = screenPointOfMouse;
        }
    }

    public Unit GetUnitInHover()
    {
        return m_unitInHover;
    }

    public BoardCell GetBoardCellInHover()
    {
        return m_cellInHover;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Unit unit = collision.gameObject.GetComponent<Unit>();
        BoardCell cell = collision.gameObject.GetComponent<BoardCell>();

        m_unitInHover = m_unitInHover == null ? unit : m_unitInHover;
        m_cellInHover = m_cellInHover == null ? cell : m_cellInHover;
        if (m_unitInHover != null && m_unitInHover.GetUnitValue() > 0)
        {
            m_unitInHover.UnitOnHover();
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        Unit unit = collision.gameObject.GetComponent<Unit>();
        if (unit != null && unit.GetUnitValue() > 0)
        {
            unit.UnitOffHover();
            m_unitInHover = null;
        }
        if (m_cellInHover != null && m_cellInHover == collision.gameObject.GetComponent<BoardCell>())
            m_cellInHover = null;
    }
}
