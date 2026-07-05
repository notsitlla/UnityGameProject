using UnityEngine;

public class BoardGenerator : MonoBehaviour
{
    [Header("Prefab Configuration Links")]
    public GameObject miniBoardPrefab; // Attach the empty MiniBoard prefab template
    public GameObject cellPrefab;      // Attach your primary Cell template

    private const float CellSpacing = 0.7f;
    private const float MiniBoardGap = 0.35f;

    private void Start()
    {
        GenerateStructuredBoard();
    }

    private void GenerateStructuredBoard()
    {
        for (int boardRow = 0; boardRow < 3; boardRow++)
        {
            for (int boardCol = 0; boardCol < 3; boardCol++)
            {
                int miniBoardIndex = (boardRow * 3) + boardCol;

                // 1. Calculate the exact central position anchor origin point for this sub-board
                Vector3 boardCenter = new Vector3(
                    boardCol * (3 * CellSpacing + MiniBoardGap) + CellSpacing,
                    -(boardRow * (3 * CellSpacing + MiniBoardGap) + CellSpacing),
                    0
                );

                // 2. Spawn the distinct container node parent and rename it cleanly
                GameObject miniBoardObj = Instantiate(miniBoardPrefab, boardCenter, Quaternion.identity, transform);
                miniBoardObj.name = $"MiniBoard_{miniBoardIndex}";

                // Fire your active line boundary highlight panel registration rule
                BoardManager.Instance.SpawnHighlightObject(miniBoardIndex, boardCenter);

                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        int localIndex = (row * 3) + col;

                        // 3. Keep cell instantiation tracking matching your offset requirements exactly
                        Vector3 position = new Vector3(
                            boardCol * (3 * CellSpacing + MiniBoardGap) + col * CellSpacing,
                            -(boardRow * (3 * CellSpacing + MiniBoardGap) + row * CellSpacing),
                            0
                        );

                        // Nest this cell object inside the specific MiniBoard parent container instance!
                        GameObject cellObj = Instantiate(cellPrefab, position, Quaternion.identity, miniBoardObj.transform);
                        cellObj.name = $"Cell_{localIndex}";

                        Cell cellScript = cellObj.GetComponent<Cell>();
                        if (cellScript != null)
                        {
                            cellScript.miniBoardIndex = miniBoardIndex;
                            cellScript.localIndex = localIndex;
                            
                            BoardManager.Instance.RegisterCell(miniBoardIndex, localIndex, cellScript);
                        }
                    }
                }
            }
        }

        CenterEntireBoardStructure();
    }

    private void CenterEntireBoardStructure()
    {
        float totalSize = (3 * (3 * CellSpacing + MiniBoardGap)) - MiniBoardGap;
        transform.position = new Vector3(-totalSize / 2f + (CellSpacing / 2f), totalSize / 2f - (CellSpacing / 2f), 0);
    }
}