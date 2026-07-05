using UnityEngine;

public class Cell : MonoBehaviour
{
    [Header("Grid Coordinates")]
    public int miniBoardIndex;
    public int localIndex;

    private SpriteRenderer spriteRenderer;
    private bool isOccupied = false;

    // Vector targets for handling smooth scaling animations
    private Vector3 targetScale = new Vector3(0.9f, 0.9f, 1f);
    private Color32 baseColor = new Color32(45, 52, 73, 255);
    private Color32 targetColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        targetColor = baseColor;
        spriteRenderer.color = baseColor;
    }

    private void Update()
    {
        // Smoothly interpolate scale and color values every single frame!
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * 15f);
        spriteRenderer.color = Color.Lerp(spriteRenderer.color, targetColor, Time.deltaTime * 15f);
    }

    private void OnMouseEnter()
    {
        if (isOccupied) return;
        
        // Elastic pop up to 102% scale on hover!
        targetScale = new Vector3(1.02f, 1.02f, 1f);
        targetColor = new Color32(50, 216, 255, 255); // Neon Cyan glow hover tint
    }

    private void OnMouseExit()
    {
        if (isOccupied) return;

        // Return gracefully back down to base tile metrics
        targetScale = new Vector3(0.9f, 0.9f, 1f);
        targetColor = baseColor;
    }

    private void OnMouseDown()
    {
        if (isOccupied) return;

        // Punch downward slightly on press for tactical sensory response feedback
        transform.localScale = new Vector3(0.8f, 0.8f, 1f);
        BoardManager.Instance.OnCellClicked(this);
    }

    public void ClaimCell(Color playerColor)
    {
        isOccupied = true;
        
        // If clear color is passed, hide tile background and let token piece show through
        if (playerColor == Color.clear)
        {
            spriteRenderer.enabled = false;
        }
        else
        {
            targetColor = playerColor;
        }
    }
}