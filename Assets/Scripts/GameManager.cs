using System;
using UnityEngine;
using UnityEngine.UI;
using EscapeRoom.Core;

// Rätsel 1 des Escape Games: liefert die Ziffer 7 an den zentralen CodeManager.
public class GameManager : MonoBehaviour, IPuzzle
{
    public static GameManager Instance;

    [Header("Szenen-Objekte")]
    public GameObject phoneObject;
    public GameObject puzzleObject;

    [Header("Puzzle")]
    public ImageReveal imageReveal;
    public GameObject puzzlePiecesRoot;

    [Header("Puzzle Manager")]
    public PuzzleManager puzzleManager;

    [Header("Escape-Game-Integration")]
    [Tooltip("ID dieses Rätsels im CodeManager (dieses Rätsel = 1).")]
    public int puzzleId = 1;
    [Tooltip("Ziffer, die dieses Rätsel zum Gesamtcode beiträgt.")]
    public int puzzleCode = 7;

    private bool _puzzleStarted = false;
    private bool _winShown = false;
    private bool _solved = false;

    // --- IPuzzle ---
    public int PuzzleId => puzzleId;
    public bool IsSolved => _solved;
    public event Action<int, int> Solved;

    // Wird vom Framework aufgerufen, um das Rätsel spielbereit zu schalten.
    // Hier optional: der Ablauf startet ohnehin über das klingelnde Telefon.
    public void Activate()
    {
        if (phoneObject != null) phoneObject.SetActive(true);
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        puzzleObject.SetActive(false);
    }

    public void OnPhoneAnswered()
    {
        Debug.Log("OnPhoneAnswered aufgerufen!");

        Transform cam = Camera.main.transform;
        Vector3 spawnPos = cam.position + cam.forward * 0.8f;

        puzzleObject.transform.position = spawnPos;
        puzzleObject.transform.LookAt(cam.position);
        puzzleObject.transform.Rotate(0, 180f, 0);

        // ImageQuad nach links
        imageReveal.transform.localPosition = new Vector3(-0.2f, 0f, 0f);
        imageReveal.transform.rotation = puzzleObject.transform.rotation;

        puzzleObject.SetActive(true);
    }

    public void OnPuzzleTapped()
    {
        if (_puzzleStarted) return;
        _puzzleStarted = true;
        Debug.Log("OnPuzzleTapped aufgerufen!");
        imageReveal.StartReveal();
    }

    public void OnImageDestroyed()
    {
        Debug.Log("OnImageDestroyed aufgerufen!");
        Debug.Log("puzzlePiecesRoot: " + puzzlePiecesRoot);
        Debug.Log("puzzleManager: " + puzzleManager);

        puzzlePiecesRoot.SetActive(true);
        puzzleManager.InitPuzzle(puzzleObject.transform.position);

        Debug.Log("InitPuzzle aufgerufen!");
    }

    public void OnPuzzleSolved()
    {
        if (_solved) return;
        _solved = true;

        Debug.Log($"Puzzle gelöst! Code: {puzzleCode}");

        // Meldet die Ziffer an das zentrale Code-System des Escape Games (Rätsel 1 -> 7).
        Solved?.Invoke(puzzleId, puzzleCode);
        if (CodeManager.Instance != null)
            CodeManager.Instance.SubmitDigit(puzzleId, puzzleCode);
        else
            Debug.LogWarning("[GameManager] Kein CodeManager in der Szene – Ziffer wird lokal angezeigt.");

        ShowWinMessage();
    }

    // Erstellt und zeigt eine Glückwunsch-Nachricht im Vollbild (per Code erzeugt, keine UI vorzubereiten).
    void ShowWinMessage()
    {
        if (_winShown) return;
        _winShown = true;

        // Vollbild-Canvas über allem.
        GameObject canvasObj = new GameObject("WinCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Hochformat Handy
        canvasObj.AddComponent<GraphicRaycaster>();

        // Halbtransparentes, zentriertes Panel.
        GameObject panelObj = new GameObject("Panel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        Image panel = panelObj.AddComponent<Image>();
        panel.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform pr = panel.rectTransform;
        pr.anchorMin = new Vector2(0.5f, 0.5f);
        pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.pivot = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(900, 600);

        // Glückwunsch-Text.
        GameObject textObj = new GameObject("WinText");
        textObj.transform.SetParent(panelObj.transform, false);
        Text txt = textObj.AddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // ältere Versionen
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 60;
        txt.text = $"Glückwunsch!\nPuzzle gelöst!\n\nEine Ziffer des Codes: {puzzleCode}";
        RectTransform tr = txt.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(40, 40);
        tr.offsetMax = new Vector2(-40, -40);
    }

    public void ResetPuzzle()
    {
        _puzzleStarted = false;
        _winShown = false;
    }
}
