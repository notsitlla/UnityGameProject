using UnityEngine;

public class Cell : MonoBehaviour
{
    [Header("Grid Coordinates")]
    public int miniBoardIndex;
    public int localIndex;

    [Header("Highlight Reference")]
    public GameObject cellHighlightPrefab; // Reference slot for our new overlay asset
    private GameObject spawnHighlightInstance;

    private SpriteRenderer baseSpriteRenderer;
    private SpriteRenderer highlightSpriteRenderer;
    private bool isOccupied = false;

    // Smooth hover animation scaling vector values
    private Vector3 targetHighlightScale = Vector3.zero;

    private void Awake()
    {
        baseSpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 1. Spawn our hover highlight as a child layer asset instantly on initialization
        if (cellHighlightPrefab != null)
        {
            spawnHighlightInstance = Instantiate(cellHighlightPrefab, transform.position, Quaternion.identity, transform);
            spawnHighlightInstance.transform.localPosition = Vector3.zero;
            spawnHighlightInstance.transform.localScale = Vector3.zero; // Hide it initially
            
            highlightSpriteRenderer = spawnHighlightInstance.GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        // 2. Interpolate the scale of the independent highlight asset smoothly every single frame!
        if (spawnHighlightInstance != null)
        {
            spawnHighlightInstance.transform.localScale = Vector3.Lerp(
                spawnHighlightInstance.transform.localScale, 
                targetHighlightScale, 
                Time.deltaTime * 18f
            );
        }
    }

    private void OnMouseEnter()
    {
        if (isOccupied) return;

        // Is this cell currently sitting inside a legally valid play zone?
        int activeBoard = BoardManager.Instance.activeMiniBoard;
        if (activeBoard != -1 && activeBoard != miniBoardIndex) return;

        // Animate the highlight frame outward to full coverage safely
        targetHighlightScale = Vector3.one;
    }

    private void OnMouseExit()
    {
        if (isOccupied) return;

        // Collapse the independent highlight scale completely down to hide it
        targetHighlightScale = Vector3.zero;
    }

    private void OnMouseDown()
    {
        if (isOccupied) return;

        int activeBoard = BoardManager.Instance.activeMiniBoard;
        if (activeBoard != -1 && activeBoard != miniBoardIndex) return;

        // Collapse overlay visual instantly when confirmed click drops
        targetHighlightScale = Vector3.zero;
        BoardManager.Instance.OnCellClicked(this);
    }

    public void ClaimCell(Color playerColor)
    {
        isOccupied = true;
        targetHighlightScale = Vector3.zero;

        if (spawnHighlightInstance != null)
        {
            Destroy(spawnHighlightInstance); // Wipe out the hover asset layer to clear memory space
        }

        // Hide base tile background completely to let the player token pieces pop cleanly
        if (playerColor == Color.clear)
        {
            baseSpriteRenderer.enabled = false;
        }
    }

    public void ResetVisual()
    {
        isOccupied = false;
        baseSpriteRenderer.enabled = true;
        baseSpriteRenderer.color = new Color32(45, 52, 73, 255); // Reset back to default base blue-gray slate

        // Re-initialize overlay child layers safely if reset is called down the road
        if (spawnHighlightInstance == null && cellHighlightPrefab != null)
        {
            spawnHighlightInstance = Instantiate(cellHighlightPrefab, transform.position, Quaternion.identity, transform);
            spawnHighlightInstance.transform.localPosition = Vector3.zero;
            spawnHighlightInstance.transform.localScale = Vector3.zero;
            highlightSpriteRenderer = spawnHighlightInstance.GetComponent<SpriteRenderer>();
        }
        targetHighlightScale = Vector3.zero;
    }
}