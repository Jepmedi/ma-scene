using UnityEngine;

public class PuzzleSlot : MonoBehaviour
{
    public int slotIndex;
    public bool isOccupied = false;
    private Renderer _renderer;

    // Farben der Slots
    private Color _emptyColor  = new Color(1f, 1f, 1f, 0.3f);   // transparentes Weiß
    private Color _correctColor = new Color(0f, 1f, 0f, 0.5f);  // Grün

    void Start()
    {
        _renderer = GetComponent<Renderer>();
        SetEmpty();
    }

    public void SetEmpty()
    {
        isOccupied = false;
        if (_renderer != null)
            _renderer.material.color = _emptyColor;
    }

    public void SetCorrect()
    {
        isOccupied = true;
        if (_renderer != null)
            _renderer.material.color = _correctColor;
    }
}
