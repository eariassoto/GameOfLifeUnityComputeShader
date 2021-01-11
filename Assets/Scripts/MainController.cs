using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class MainController : MonoBehaviour
{
    [SerializeField] private GameObject cellPrefab = null;
    [SerializeField] private int mapWidth = 20;
    [SerializeField] private int mapHeight = 5;
    [SerializeField] private float cellOffset = .05f;
    [SerializeField] private TMP_InputField numGenerationsInput = null;

    public ComputeShader computeShader;

    ComputeBuffer currentGenBuffer = null;
    ComputeBuffer nextGenBuffer = null;

    private struct CellData
    {
        public int IsAlive;
    }

    private CellData[] cellData;
    private CellController[] cellControllers;

    public static readonly int[][] Neighbors = {
        new[] { 0,  1 },
        new[] { 0, -1 },

        new[] {  1, 0 },
        new[] { -1, 0 },

        new[] {  1,  1 },
        new[] { -1, -1 },

        new[] {  1, -1 },
        new[] { -1,  1 }
    };

    private void Start()
    {
        CellController.CellOnFlipValue += HandleCellOnFlipValue;

        cellData = new CellData[mapWidth * mapHeight];
        cellControllers = new CellController[mapWidth * mapHeight];
        int aux = 0;
        for (int i = 0; i < mapHeight; ++i)
        {
            for (int j = 0; j < mapWidth; ++j)
            {
                GameObject gameObject = Instantiate(
                    cellPrefab,
                    new Vector3(j * (1 + cellOffset), 0, i * (-1 - cellOffset)),
                    Quaternion.identity);
                CellController cell = gameObject.GetComponent<CellController>();
                cell.SetCellPosition(i, j);
                cell.SetCellIsAlive(0);
                cellControllers[aux] = cell;
                cellData[aux++].IsAlive = 0;
            }
        }

        currentGenBuffer = new ComputeBuffer(cellData.Length, sizeof(int));
        nextGenBuffer = new ComputeBuffer(cellData.Length, sizeof(int));
    }

    private void OnDestroy()
    {
        currentGenBuffer.Dispose();
        nextGenBuffer.Dispose();
        CellController.CellOnFlipValue -= HandleCellOnFlipValue;
    }

    public void CalculateCPU()
    {
        uint numCalculations = 1;
        string numCalculationsText = numGenerationsInput.text;
        if (UInt32.TryParse(numCalculationsText, out uint num) && num > 0)
        {
            numCalculations = num;
        }
        for (int i = 0; i < numCalculations; ++i)
        {
            CalculateNextGenerationCPU();
        }

        for (int i = 0; i < mapWidth * mapHeight; ++i)
        {
            cellControllers[i].SetCellIsAlive(cellData[i].IsAlive);
        }
    }

    public void CalculateGPU()
    {
        uint numCalculations = 1;
        string numCalculationsText = numGenerationsInput.text;
        if (UInt32.TryParse(numCalculationsText, out uint num) && num > 0)
        {
            numCalculations = num;
        }
        for (int i = 0; i < numCalculations; ++i)
        {
            CalculateNextGenerationGPU();
        }

        for (int i = 0; i < mapWidth * mapHeight; ++i)
        {
            cellControllers[i].SetCellIsAlive(cellData[i].IsAlive);
        }
    }

    private void CalculateNextGenerationGPU()
    {
        currentGenBuffer.SetData(cellData);

        computeShader.SetBuffer(0, "currentGeneration", currentGenBuffer);
        computeShader.SetBuffer(0, "nextGeneration", nextGenBuffer);
        computeShader.SetInt("mapWidth", mapWidth);
        computeShader.SetInt("mapHeight", mapHeight);
        computeShader.SetInt("mapSize", mapWidth * mapHeight);

        computeShader.Dispatch(0,
            Mathf.CeilToInt((float)mapWidth / 8),
            Mathf.CeilToInt((float)mapHeight / 8),
            1);

        nextGenBuffer.GetData(cellData);
    }

    private void CalculateNextGenerationCPU()
    {
        List<int> cellsToKill = new List<int>();
        List<int> cellsToResurrect = new List<int>();

        int currentRow = 0;
        int currentCol = 0;
        for (int i = 0; i < cellData.Length; ++i)
        {
            int numAliveNeighbors = 0;
            foreach (int[] n in Neighbors)
            {
                int neighborIndex = GetFlatIndex(currentRow + n[0], currentCol + n[1]);
                if (neighborIndex < 0 || neighborIndex >= cellData.Length)
                {
                    continue;
                }
                if (cellData[neighborIndex].IsAlive == 1)
                {
                    numAliveNeighbors++;
                }

                if (numAliveNeighbors == 4)
                {
                    break;
                }
            }

            int cellIsAlive = cellData[i].IsAlive;
            if (cellIsAlive == 1)
            {
                if (numAliveNeighbors < 2 || numAliveNeighbors == 4)
                {
                    // die
                    cellsToKill.Add(i);
                }
            }
            else
            {
                if (numAliveNeighbors == 3)
                {
                    // alive
                    cellsToResurrect.Add(i);
                }
            }
            // move indexes
            currentCol++;
            if (currentCol == mapWidth)
            {
                currentCol = 0;
                currentRow++;
            }
        }

        foreach (int i in cellsToKill)
        {
            cellData[i].IsAlive = 0;
        }

        foreach (int i in cellsToResurrect)
        {
            cellData[i].IsAlive = 1;
        }
    }

    private int GetFlatIndex(int row, int col)
    {
        if (row < 0 || row >= mapHeight || col < 0 || col >= mapWidth)
        {
            return -1;
        }
        return (row * mapWidth) + col;
    }

    private void HandleCellOnFlipValue(int row, int col, int newValue)
    {
        cellData[GetFlatIndex(row, col)].IsAlive = newValue;
    }
}
