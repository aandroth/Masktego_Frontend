using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static BoardCell;
using static UnitData;

public class Unit : MonoBehaviour
{
    [SerializeField] UNIT_TYPE m_unitType = UNIT_TYPE.ROCK;
    [SerializeField] SpriteRenderer m_unitColoredSpriteRenderer, m_unitUncoloredSpriteRenderer, m_highlightRenderer, m_hoverRenderer;
    [SerializeField] Vector2Int m_cellPosition = Vector2Int.zero;
    [SerializeField] int m_unitValue = 1;
    [SerializeField] bool m_unitIsSelected = false;

    public void HighlightUnit()
    {
        m_highlightRenderer.gameObject.SetActive(true);
    }

    public void UnhighlightUnit()
    {
        m_highlightRenderer.gameObject.SetActive(false);
    }

    public void SetUnitType(UNIT_TYPE type)
    {
        m_unitType = type;
        m_unitValue = (int)type;
        m_unitColoredSpriteRenderer.sprite = UnitData.UnitToColoredSpriteDict[type];
        m_unitUncoloredSpriteRenderer.sprite = UnitData.UnitToUncoloredSpriteDict[type];
    }

    public void SetColor(Color color)
    {
        m_hoverRenderer.color = color;
        m_unitColoredSpriteRenderer.color = color;
    }

    public int GetUnitValue()
    {
        return m_unitValue;
    }

    public void SetUnitValue(int i)
    {
        m_unitValue = i;
    }

    public int GetUnitPace()
    {
        return UnitData.GetPaceOfUnitType(m_unitType);
    }

    public void SetUnitToCellPosition(Vector2Int pos)
    {
        m_cellPosition = pos;
        transform.position = new Vector3((pos.x*2)+1, (pos.y * 2) + 1, 0);
    }

    public Vector2Int GetCellPosition()
    {
        return m_cellPosition;
    }

    public int GetUnitMoveValue()
    {
        return UnitData.m_pieceTypeToPace[m_unitType];
    }

    public bool UnitIsSelected()
    {
        return m_unitIsSelected;
    }

    public Unit UnitSelected()
    {
        HighlightUnit();
        UnitOffHover();
        m_unitIsSelected = true;
        return this;
    }

    public void UnitUnselected()
    {
        UnhighlightUnit();
        m_unitIsSelected = false;
    }

    public void UnitOnHover()
    {
        if (!m_unitIsSelected && !m_hoverRenderer.gameObject.activeSelf && m_unitValue > 0)
            m_hoverRenderer.gameObject.SetActive(true);
    }

    public void UnitOffHover()
    {
        if(m_hoverRenderer.gameObject.activeSelf && m_unitValue > 0)
            m_hoverRenderer.gameObject.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        UnitOnHover();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        UnitOffHover();
    }

    public void SetSelfToEnemySprites()
    {
        m_unitColoredSpriteRenderer.sprite = UnitData.UnitToColoredSpriteDict[UNIT_TYPE.ENEMY];
        m_unitUncoloredSpriteRenderer.sprite = UnitData.UnitToUncoloredSpriteDict[UNIT_TYPE.ENEMY];
    }

    public void SetSelfToNormalSprites()
    {
        m_unitColoredSpriteRenderer.sprite = UnitData.UnitToColoredSpriteDict[m_unitType];
        m_unitUncoloredSpriteRenderer.sprite = UnitData.UnitToUncoloredSpriteDict[m_unitType];
    }

    public void StartFlipToNormalSprites(float timeToFlip)
    {
        StartCoroutine(FlipToNormalSpritesCoroutine(timeToFlip));
    }

    public IEnumerator FlipToNormalSpritesCoroutine(float timeToFlip)
    {
        float flipCountdown = timeToFlip * 0.5f;
        float originalScale = transform.localScale.x;
        while (flipCountdown > 0)
        {
            flipCountdown -= Time.deltaTime;
            float scaleX = (flipCountdown / timeToFlip) * originalScale;
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            yield return null;
        }
        transform.localScale = new Vector3(0, transform.localScale.y, transform.localScale.z);
        SetSelfToNormalSprites();
        flipCountdown = 0;
        while (flipCountdown < timeToFlip)
        {
            flipCountdown += Time.deltaTime;
            float scaleX = (flipCountdown / timeToFlip) * originalScale;
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            yield return null;
        }
        transform.localScale = new Vector3(originalScale, transform.localScale.y, transform.localScale.z);
    }

    public void StartFlipToEnemySprites(float timeToFlip)
    {
        StartCoroutine(FlipToEnemySpritesCoroutine(timeToFlip));
    }

    public IEnumerator FlipToEnemySpritesCoroutine(float timeToFlip)
    {
        timeToFlip *= 0.5f;
        float flipCountdown = timeToFlip;
        float originalScale = transform.localScale.x;
        while (flipCountdown > 0)
        {
            flipCountdown -= Time.deltaTime;
            float scaleX = (flipCountdown / timeToFlip) * originalScale;
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            yield return null;
        }
        transform.localScale = new Vector3(0, transform.localScale.y, transform.localScale.z);
        SetSelfToEnemySprites();
        flipCountdown = 0;
        while (flipCountdown < timeToFlip)
        {
            flipCountdown += Time.deltaTime;
            float scaleX = (flipCountdown / timeToFlip) * originalScale;
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
            yield return null;
        }
        transform.localScale = new Vector3(originalScale, transform.localScale.y, transform.localScale.z);
    }

    public void Destroyed()
    {
        Destroy(this.gameObject);
    }
}
