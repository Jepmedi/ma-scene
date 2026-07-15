using UnityEngine;
using System.Collections.Generic;

public class PuzzleManager : MonoBehaviour
{
    public static PuzzleManager Instance;

    [Header("Puzzle-Einstellungen")]
    public int gridSize = 3;
    public float pieceSize = 0.07f;
    public float spacing = 0.003f;
    public Texture2D puzzleTexture;

    [Header("Prefabs")]
    public GameObject piecePrefab;
    public GameObject slotPrefab;   // Quad mit Rand

    [Header("Zone")]
    // Transform, das als Anker + Ausrichtung für das GESAMTE Puzzle dient.
    // Der Rahmen (Slots) ist darauf zentriert, die Streuzone liegt daneben.
    public Transform puzzleZone;

    [Header("Streuzone (gemischte Teile)")]
    // Seite, auf der die Teile gemischt erscheinen: true = links vom Rahmen, false = rechts.
    public bool scatterOnLeft = true;
    // Abstand (in Metern) zwischen Rahmen und Streuzone.
    public float scatterGap = 0.03f;

    private List<PuzzlePiece> _pieces = new List<PuzzlePiece>();
    private List<PuzzleSlot> _slots  = new List<PuzzleSlot>();
    private List<Vector3> _cellCenters = new List<Vector3>(); // Mittelpunkte der 9 Felder (korrekte Positionen)
    private bool _solved = false;

    // Effektiver Anker für alle Positions-/Ausrichtungsberechnungen.
    private Transform _anchor;

    void Awake()
    {
        Instance = this;
    }

    // centerPosition: Weltposition, auf die das Puzzle zentriert wird, falls puzzleZone nicht gesetzt ist.
    public void InitPuzzle(Vector3 centerPosition)
    {
        Debug.Log("InitPuzzle gestartet!");

        // --- Prüfungen: eine klare Meldung ist besser als eine NullReferenceException ---
        if (piecePrefab == null)   { Debug.LogError("[PuzzleManager] piecePrefab ist im Inspector NICHT zugewiesen!"); return; }
        if (slotPrefab  == null)   { Debug.LogError("[PuzzleManager] slotPrefab ist im Inspector NICHT zugewiesen!"); return; }
        if (puzzleTexture == null) { Debug.LogError("[PuzzleManager] puzzleTexture ist im Inspector NICHT zugewiesen!"); return; }

        _anchor = puzzleZone;
        if (_anchor == null)
        {
            // Fallback: temporärer Anker, zur Kamera ausgerichtet.
            Debug.LogWarning("[PuzzleManager] puzzleZone nicht zugewiesen -> temporärer Anker an centerPosition erstellt.");
            GameObject temp = new GameObject("PuzzleAnchor(auto)");
            temp.transform.position = centerPosition;
            if (Camera.main != null)
            {
                temp.transform.LookAt(Camera.main.transform.position);
                temp.transform.Rotate(0f, 180f, 0f);
            }
            _anchor = temp.transform;
        }

        // Alte Teile/Slots aufräumen
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        _solved = false;
        _pieces.Clear();
        _slots.Clear();
        _cellCenters.Clear();

        for (int row = 0; row < gridSize; row++)
        {
            for (int col = 0; col < gridSize; col++)
            {
                CreateSlot(row, col);
                CreatePiece(row, col);
            }
        }

        Debug.Log($"[PuzzleManager] {_slots.Count} Slots und {_pieces.Count} Teile erstellt.");
    }

    // Wandelt einen Versatz in der lokalen Ebene des Ankers (x=rechts, y=oben, z=Tiefe)
    // in eine Weltposition um. Unabhängig von der Skalierung -> robust in AR.
    Vector3 ZonePoint(float x, float y, float z)
    {
        return _anchor.position
             + _anchor.right   * x
             + _anchor.up      * y
             + _anchor.forward * z;
    }

    // Lokale (zentrierte) Koordinate eines Gitterfeldes auf einer Achse.
    float GridCoord(int index)
    {
        float totalSize = pieceSize + spacing;
        return (index - (gridSize - 1) / 2f) * totalSize;
    }

    void CreateSlot(int row, int col)
    {
        float localX = GridCoord(col);
        float localY = GridCoord(row);

        // Slot leicht HINTER der Ebene (die Teile liegen davor).
        Vector3 slotPos = ZonePoint(localX, localY, 0.001f);

        GameObject slotObj = Instantiate(slotPrefab, transform);
        slotObj.name = $"Slot_{row}_{col}";
        slotObj.transform.position = slotPos;
        slotObj.transform.rotation = _anchor.rotation;
        slotObj.transform.localScale = Vector3.one * pieceSize;

        PuzzleSlot slot = slotObj.GetComponent<PuzzleSlot>();
        slot.slotIndex = row * gridSize + col;
        _slots.Add(slot);
    }

    void CreatePiece(int row, int col)
    {
        int index = row * gridSize + col;

        float localX = GridCoord(col);
        float localY = GridCoord(row);

        // KORREKTE Position: auf dem Feld des Rahmens, direkt vor dem Slot.
        Vector3 correctPos = ZonePoint(localX, localY, -0.002f);
        _cellCenters.Add(correctPos);

        // --- Startposition: in der Streuzone, NEBEN dem Rahmen ---
        float totalSize   = pieceSize + spacing;
        float frameHalf   = gridSize * totalSize * 0.5f;         // halbe Breite des Rahmens
        float scatterHalf = gridSize * totalSize * 0.5f;         // halbe Breite der Streuzone
        float side        = scatterOnLeft ? -1f : 1f;
        float scatterCenterX = side * (frameHalf + scatterGap + scatterHalf);

        // Rand, damit das Teil vollständig innerhalb des Streuquadrats bleibt.
        float margin = pieceSize * 0.5f;
        float randX = Random.Range(-scatterHalf + margin, scatterHalf - margin);
        float randY = Random.Range(-scatterHalf + margin, scatterHalf - margin);

        Vector3 startPos = ZonePoint(scatterCenterX + randX, randY, -0.01f);

        GameObject obj = Instantiate(piecePrefab, transform);
        obj.name = $"Piece_{row}_{col}";
        obj.transform.position = startPos;
        obj.transform.rotation = _anchor.rotation;
        obj.transform.localScale = Vector3.one * pieceSize;

        // Zugeschnittene Textur
        Texture2D pieceTexture = GetCroppedTexture(row, col);
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.mainTexture = pieceTexture;
        obj.GetComponent<Renderer>().material = mat;

        // Teil konfigurieren
        PuzzlePiece piece = obj.GetComponent<PuzzlePiece>();
        piece.Init(index, correctPos, _anchor, pieceSize * 0.6f);
        _pieces.Add(piece);
    }

    Texture2D GetCroppedTexture(int row, int col)
    {
        int pw = puzzleTexture.width / gridSize;
        int ph = puzzleTexture.height / gridSize;

        Color[] pixels = puzzleTexture.GetPixels(col * pw, row * ph, pw, ph);
        Texture2D result = new Texture2D(pw, ph);
        result.SetPixels(pixels);
        result.Apply();
        return result;
    }

    // Gibt den Mittelpunkt des nächstgelegenen Feldes zu einer Weltposition zurück,
    // sowie die Distanz zu diesem Feld (out).
    public Vector3 GetNearestCell(Vector3 worldPos, out float dist)
    {
        Vector3 best = worldPos;
        dist = Mathf.Infinity;
        foreach (Vector3 c in _cellCenters)
        {
            float d = Vector3.Distance(worldPos, c);
            if (d < dist) { dist = d; best = c; }
        }
        return best;
    }

    // Prüft die gesamte Anordnung: liegt jedes Teil auf SEINEM richtigen Feld?
    // Wird nach jedem Ablegen eines Teils aufgerufen.
    public void CheckCompletion()
    {
        if (_solved) return;

        float tolerance = pieceSize * 0.3f; // Toleranz um den Feldmittelpunkt
        int correct = 0;

        foreach (PuzzlePiece pc in _pieces)
        {
            bool onGoodCell = Vector3.Distance(pc.transform.position, pc.correctPosition) <= tolerance;

            // Visuelles Feedback: färbt den zugehörigen Slot grün / weiß.
            foreach (PuzzleSlot s in _slots)
            {
                if (s.slotIndex == pc.pieceIndex)
                {
                    if (onGoodCell) s.SetCorrect(); else s.SetEmpty();
                    break;
                }
            }

            if (onGoodCell) correct++;
        }

        Debug.Log($"Richtig platzierte Teile: {correct}/{_pieces.Count}");

        if (correct >= _pieces.Count)
        {
            _solved = true;
            Debug.Log("PUZZLE GELÖST! Das Bild ist wiederhergestellt.");
            if (GameManager.Instance != null)
                GameManager.Instance.OnPuzzleSolved();
        }
    }
}
