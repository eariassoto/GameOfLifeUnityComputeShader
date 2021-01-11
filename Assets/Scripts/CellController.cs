using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CellController : MonoBehaviour
{
    [SerializeField] private Renderer materialRenderer = null;
    [SerializeField] private BoxCollider collider = null;

    [SerializeField] private Color aliveColor = new Color();
    [SerializeField] private Color deadColor = new Color();

    [SerializeField] private int rowIndex = -1;
    [SerializeField] private int colIndex = -1;
    [SerializeField] private int isAlive = 0;

    public static event Action<int, int, int> CellOnFlipValue;

    public void SetCellPosition(int row, int col)
    {
        rowIndex = row;
        colIndex = col;
    }

    public void SetCellIsAlive(int isAlive)
    {
        this.isAlive = isAlive;
        UpdateCellLook();
    }

    private void UpdateCellLook()
    {
        Vector3 newPosition = gameObject.transform.position;
        Vector3 newSize = new Vector3(1, 1, 1);
        if (isAlive == 1)
        {
            materialRenderer.material.color = aliveColor;
            newPosition.y = .5f;
            newSize.y = 1;
        }
        else
        {
            materialRenderer.material.color = deadColor;
            newPosition.y = .1f;
            newSize.y = 0.2f;
        }
        gameObject.transform.position = newPosition;
        gameObject.transform.localScale = newSize;
        collider.size = newSize;
    }

    private void OnMouseDown()
    {
        isAlive = (isAlive == 0) ? 1 : 0;
        UpdateCellLook();
        CellOnFlipValue?.Invoke(rowIndex, colIndex, isAlive);
    }
}
