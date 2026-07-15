using UnityEngine;

public class PuzzlePiece : MonoBehaviour
{
    [HideInInspector] public Vector3 correctPosition;
    [HideInInspector] public int pieceIndex;

    public float snapDistance = 0.05f;
    public bool isPlaced = false;   // true, wenn das Teil auf einem Feld eingerastet ist

    private bool _isDragging = false;
    private Camera _cam;

    // Ebene des Bildes: die Teile bewegen sich IN dieser Ebene (korrekt in AR).
    private Transform _anchor;
    private Vector3 _grabOffset;

    void Start()
    {
        _cam = Camera.main;
    }

    // Wird vom PuzzleManager bei der Erstellung aufgerufen.
    public void Init(int index, Vector3 correctPos, Transform anchor, float snap)
    {
        pieceIndex      = index;
        correctPosition = correctPos;
        _anchor         = anchor;
        snapDistance    = snap;
    }

    Plane GetPiecePlane()
    {
        Vector3 normal = (_anchor != null) ? _anchor.forward : -_cam.transform.forward;
        return new Plane(normal, transform.position);
    }

    public void OnStartDrag(Vector2 screenPos)
    {
        if (_cam == null) _cam = Camera.main;
        _isDragging = true;

        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (GetPiecePlane().Raycast(ray, out float enter))
            _grabOffset = transform.position - ray.GetPoint(enter);
        else
            _grabOffset = Vector3.zero;
    }

    public void OnDrag(Vector2 screenPos)
    {
        if (!_isDragging) return;

        Ray ray = _cam.ScreenPointToRay(screenPos);
        if (GetPiecePlane().Raycast(ray, out float enter))
            transform.position = ray.GetPoint(enter) + _grabOffset;
    }

    public void OnEndDrag()
    {
        if (!_isDragging) return;
        _isDragging = false;

        if (PuzzleManager.Instance == null) return;

        // Richtet das Teil am nächstgelegenen Feld aus (egal welches).
        Vector3 cell = PuzzleManager.Instance.GetNearestCell(transform.position, out float dist);
        if (dist <= snapDistance)
        {
            transform.position = cell;
            isPlaced = true;
        }
        else
        {
            isPlaced = false; // in der Streuzone belassen
        }

        // Prüft, ob das gesamte Puzzle korrekt wiederhergestellt ist.
        PuzzleManager.Instance.CheckCompletion();
    }
}
