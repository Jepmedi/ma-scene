using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EscapeRoom.Core;

// Rätsel 3: Ein Bild fällt auf den Boden -> Antippen richtet es auf und zeigt ein Buch
// -> ein Buch erscheint auf dem Boden -> Antippen öffnet es -> Ziffer 2.
// Meldet die Ziffer an das zentrale Code-System (Rätsel 3 -> 2).
public class Raetsel3Manager : MonoBehaviour, IPuzzle
{
    public static Raetsel3Manager Instance;

    [Header("AR")]
    public ARPlaneManager planeManager;
    public float minPlaneSize = 0.2f;

    [Header("Objekte")]
    public FallingPicture picture;     // das Bild (startet umgedreht am Boden)
    public PuzzleBook book;            // das Buch (startet inaktiv)

    [Header("Escape-Game-Integration")]
    public int puzzleId = 3;
    public int codeDigit = 2;

    [Header("Audio (optional)")]
    public AudioSource fallSound;      // Geräusch, wenn das Bild fällt

    // --- IPuzzle ---
    public int PuzzleId => puzzleId;
    public bool IsSolved => _solved;
    public event Action<int, int> Solved;
    public void Activate() { } // Ablauf startet automatisch in Start()

    private bool _solved = false;
    private Text _instruction;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        CreateInstructionUI();
        if (book != null) book.gameObject.SetActive(false);
        StartCoroutine(DropPicture());
    }

    // Schritt 1: wartet auf eine horizontale Fläche und legt das Bild (umgedreht) dorthin.
    IEnumerator DropPicture()
    {
        SetInstruction("Plötzlich fällt etwas von der Wand!");

        Vector3 pos;
        while (!TryGetRandomHorizontalPoint(out pos))
            yield return null;

        picture.PlaceBroken(pos);
        if (fallSound != null) fallSound.Play();
        SetInstruction("Rücke das Bild zurecht. (Tippe es an)");
    }

    // Schritt 2: wird vom FallingPicture aufgerufen, sobald es aufgerichtet ist und das Buch zeigt.
    public void OnPictureRevealed()
    {
        SetInstruction("Ein Buch erscheint... suche es in der Umgebung.");
        StartCoroutine(SpawnBook());
    }

    IEnumerator SpawnBook()
    {
        Vector3 pos;
        while (!TryGetRandomHorizontalPoint(out pos))
            yield return null;

        book.PlaceAt(pos);
        book.gameObject.SetActive(true);
        SetInstruction("Öffne das Buch. (Tippe es an)");
    }

    // Schritt 3: wird vom PuzzleBook aufgerufen, sobald es geöffnet ist.
    public void OnBookOpened()
    {
        if (_solved) return;
        _solved = true;

        SetInstruction($"Du findest eine Zahl zwischen den Seiten: {codeDigit}");
        Debug.Log($"[Raetsel3] gelöst -> Ziffer {codeDigit}");

        Solved?.Invoke(puzzleId, codeDigit);
        if (CodeManager.Instance != null)
            CodeManager.Instance.SubmitDigit(puzzleId, codeDigit);
        else
            Debug.LogWarning("[Raetsel3] Kein CodeManager in der Szene – Ziffer wird nur angezeigt.");
    }

    // --- AR-Hilfsfunktion: zufälliger Punkt auf einer horizontalen Ebene ---
    bool TryGetRandomHorizontalPoint(out Vector3 point)
    {
        point = Vector3.zero;
        if (planeManager != null)
        {
            foreach (ARPlane plane in planeManager.trackables)
            {
                if (plane.alignment != PlaneAlignment.HorizontalUp) continue;
                if (plane.size.x < minPlaneSize || plane.size.y < minPlaneSize) continue;

                Vector2 ext = plane.size * 0.5f;
                Vector3 local = new Vector3(UnityEngine.Random.Range(-ext.x, ext.x), 0f, UnityEngine.Random.Range(-ext.y, ext.y));
                point = plane.transform.TransformPoint(local);
                return true;
            }
        }

#if UNITY_EDITOR
        // Im Editor gibt es keine echten Ebenen -> vor der Kamera platzieren (zum Testen).
        Transform cam = Camera.main.transform;
        point = cam.position + cam.forward * 1.2f + Vector3.down * 0.3f;
        return true;
#else
        return false;
#endif
    }

    // --- einfache Anweisungs-Anzeige (per Code erzeugt, keine UI vorzubereiten) ---
    void CreateInstructionUI()
    {
        GameObject canvasObj = new GameObject("Raetsel3Canvas");
        Canvas c = canvasObj.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        c.sortingOrder = 900;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject t = new GameObject("Instruction");
        t.transform.SetParent(canvasObj.transform, false);
        _instruction = t.AddComponent<Text>();
        _instruction.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_instruction.font == null) _instruction.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        _instruction.alignment = TextAnchor.UpperCenter;
        _instruction.color = Color.white;
        _instruction.fontSize = 40;
        RectTransform rt = _instruction.rectTransform;
        rt.anchorMin = new Vector2(0.05f, 0.80f);
        rt.anchorMax = new Vector2(0.95f, 0.98f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void SetInstruction(string s)
    {
        if (_instruction != null) _instruction.text = s;
        Debug.Log("[Raetsel3] " + s);
    }
}
