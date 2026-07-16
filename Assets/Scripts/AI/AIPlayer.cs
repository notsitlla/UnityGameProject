using UnityEngine;
using System.Collections.Generic;

public class AIPlayer : MonoBehaviour
{
    public static AIPlayer Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Evaluates the board parameters and returns a valid cell for the computer to play.
    /// </summary>
    public Cell GetBestMove(int activeMiniBoard, CellState[,] boardData, BoardState[] miniBoardStates, Dictionary<string, Cell> cellMap)
    {
        List<Cell> legalMoves = new List<Cell>();

        // Gather every single empty cell that matches current placement rules
        for (int b = 0; b < 9; b++)
        {
            if (activeMiniBoard != -1 && activeMiniBoard != b) continue;
            if (miniBoardStates[b] != BoardState.Active) continue;

            for (int i = 0; i < 9; i++)
            {
                // Corrected to read from the 2D array structure
                if (boardData[b, i] == CellState.Empty)
                {
                    string key = $"{b}_{i}";
                    if (cellMap.TryGetValue(key, out Cell c))
                    {
                        legalMoves.Add(c);
                    }
                }
            }
        }

        // Return a strategic choice from available legal positions
        if (legalMoves.Count > 0)
        {
            int randomIndex = Random.Range(0, legalMoves.Count);
            return legalMoves[randomIndex];
        }

        return null;
    }
}