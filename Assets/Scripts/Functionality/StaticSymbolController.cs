using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaticSymbolController : MonoBehaviour
{
    [SerializeField] private SocketIOManager socketManager;
    [SerializeField] private SlotBehaviour slotManager;
    [SerializeField] public List<SlotImage> Slot;
    [SerializeField] private Sprite[] images;
    
    [SerializeField] internal List<Column> freezedLocations = new(); 

    private List<List<int>> GenerateFreezeMatrix(List<List<int>> loc)
    {
        // Initialize matrix with 0s
        List<List<int>> freezeMatrix = new List<List<int>>();

        for (int i = 0; i < Slot.Count; i++)
        {
            List<int> row = new List<int>(new int[Slot[i].slotImages.Count]);
            freezeMatrix.Add(row);
        }

        // Set 1s for frozen slots based on loc
        foreach (List<int> indexPair in loc)
        {
            if (indexPair.Count == 2)
            {
                int row = indexPair[0];
                int column = indexPair[1];

                // Check bounds
                if (row >= 0 && row < freezeMatrix.Count &&
                    column >= 0 && column < freezeMatrix[row].Count)
                {
                    freezeMatrix[row][column] = 1;
                }
            }
        }

        // Update freezedLocations with data from freezeMatrix
        freezedLocations.Clear();
        foreach (var row in freezeMatrix)
        {
            Column column = new Column { index = new List<int>(row) };
            freezedLocations.Add(column);
        }

        return freezeMatrix;
    }

    // Method to apply the freeze effect to SlotImages based on the freeze matrix
    internal void TurnOnIndices(List<List<int>> loc)
    {
        // Generate the freeze matrix
        List<List<int>> freezeMatrix = GenerateFreezeMatrix(loc);

        // Apply the freeze effect based on the freeze matrix
        for (int i = 0; i < Slot.Count; i++)
        {
            for (int j = 0; j < Slot[i].slotImages.Count; j++)
            {
                if (freezeMatrix[i][j] == 1)
                {
                    // Apply effect to frozen slots
                    //Slot[i].slotImages[j].sprite = images[int.Parse(socketManager.resultData.ResultReel[i][j])];
                    Slot[i].slotImages[j].sprite = slotManager.ResultMatrix[i].slotImages[j].sprite;
                    Slot[i].slotImages[j].gameObject.SetActive(true);
                }
                else
                {
                    Slot[i].slotImages[j].gameObject.SetActive(false);
                }
            }
        }
    }
}

[Serializable]
public class Column
{
    public List<int> index = new();
}