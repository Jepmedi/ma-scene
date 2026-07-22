using UnityEngine;
using UnityEngine.XR.ARFoundation;
using EscapeRoom.Core;

// Startpunkt dieser eigenständigen Szene (Rätsel 3-Inhalt: Bild & Buch,
// läuft in Position 4 der Gesamt-Sequenz). Diese Szene enthält absichtlich
// keine eigene AR-Session: sie wird additiv über der Basisszene der Kollegen
// geladen, die XR Origin/ARPlaneManager bereitstellt.
//
// Für einen eigenständigen Test (diese Szene allein im Editor geöffnet) wird
// zusätzlich eine einfache Testkamera erzeugt, falls noch keine Hauptkamera
// vorhanden ist. Sobald die Szene additiv über der Basisszene der Kollegen
// läuft, existiert bereits eine Kamera -> die Testkamera bleibt dann aus.
public class Raetsel4Setup : MonoBehaviour
{
    [Header("Assets (im Inspector zuweisen, sonst Platzhalterfarben)")]
    public Texture2D tableauTextur;        // Vorderseite des Bildes (das Gemälde), bevor es angetippt wird
    public Texture2D buchDeckelTextur;     // Deckel des Buches (zu, und als Buch-Hinweis auf dem Bild)
    public Texture2D buchSeitenTextur;     // aufgeschlagene Buchseiten
    public AudioClip fallGeraeusch;        // Geräusch, wenn das Bild von der Wand fällt

    void Start()
    {
        EnsureTestCamera();

        if (FindAnyObjectByType<CodeManager>() == null)
            new GameObject("CodeManager").AddComponent<CodeManager>();

        GameObject root = new GameObject("Raetsel3Root");
        Raetsel3Manager manager = root.AddComponent<Raetsel3Manager>();
        root.AddComponent<Raetsel3Input>();

        manager.planeManager = FindAnyObjectByType<ARPlaneManager>();
        manager.picture = BuildPicture(root.transform, tableauTextur, buchDeckelTextur);
        manager.book = BuildBook(root.transform, buchDeckelTextur, buchSeitenTextur);
        manager.puzzleId = 3;
        manager.codeDigit = 2;

        if (fallGeraeusch != null)
        {
            AudioSource src = root.AddComponent<AudioSource>();
            src.clip = fallGeraeusch;
            src.playOnAwake = false;
            manager.fallSound = src;
        }
    }

    // Nur für Solo-Tests dieser Szene: einfache Kamera, falls keine andere aktive
    // Hauptkamera existiert (z.B. aus der additiv geladenen AR-Basisszene).
    void EnsureTestCamera()
    {
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsInactive.Exclude))
        {
            if (cam.gameObject.CompareTag("MainCamera") && cam.isActiveAndEnabled)
                return; // es gibt bereits eine Hauptkamera (z.B. AR-Kamera der Kollegen)
        }

        GameObject camObj = new GameObject("Raetsel4TestCamera");
        camObj.tag = "MainCamera";
        camObj.transform.position = new Vector3(0f, 1.2f, -1.5f);
        camObj.transform.rotation = Quaternion.Euler(15f, 0f, 0f);
        camObj.AddComponent<Camera>();
        camObj.AddComponent<AudioListener>();
    }

    // Das Bild: fällt umgedreht zu Boden. Beim Aufrichten zeigt es zunächst die
    // eigentliche Vorderseite (tableauTextur), danach wird das Kind "BuchHinweis"
    // sichtbar (Buchdeckel + Inschrift: "Schätze verstecken sich oft in Büchern").
    static FallingPicture BuildPicture(Transform parent, Texture2D tableauTextur, Texture2D buchDeckelTextur)
    {
        GameObject bild = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bild.name = "Bild";
        bild.transform.SetParent(parent);
        bild.transform.localScale = new Vector3(0.3f, 0.4f, 1f);
        Renderer bildRenderer = bild.GetComponent<Renderer>();
        bildRenderer.material = tableauTextur != null
            ? MakeMaterial(Color.white, tableauTextur)
            : MakeMaterial(new Color(0.55f, 0.42f, 0.27f)); // Platzhalter, falls keine Textur zugewiesen ist

        GameObject hint = new GameObject("BuchHinweis");
        hint.transform.SetParent(bild.transform, false);
        hint.transform.localPosition = new Vector3(0f, 0f, -0.01f);
        hint.transform.localScale = new Vector3(0.9f, 0.9f, 1f);

        GameObject deckel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        deckel.name = "BuchDeckel";
        deckel.transform.SetParent(hint.transform, false);
        Destroy(deckel.GetComponent<Collider>());
        deckel.GetComponent<Renderer>().material = buchDeckelTextur != null
            ? MakeMaterial(Color.white, buchDeckelTextur)
            : MakeMaterial(new Color(0.38f, 0.12f, 0.10f)); // Platzhalter: dunkelrot

        GameObject textObj = new GameObject("HinweisText");
        textObj.transform.SetParent(hint.transform, false);
        textObj.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        TextMesh tm = textObj.AddComponent<TextMesh>();
        tm.text = "Schätze verstecken\nsich oft\nin Büchern";
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.characterSize = 0.04f;
        tm.fontSize = 48;
        tm.color = new Color(0.95f, 0.9f, 0.8f);

        hint.SetActive(false);

        FallingPicture picture = bild.AddComponent<FallingPicture>();
        picture.frontRenderer = bildRenderer;
        picture.hintVisual = hint;
        return picture;
    }

    // Das Buch: erscheint geschlossen auf dem Boden. Antippen öffnet es und zeigt
    // die Ziffer zwischen den Seiten.
    static PuzzleBook BuildBook(Transform parent, Texture2D buchDeckelTextur, Texture2D buchSeitenTextur)
    {
        GameObject buch = new GameObject("Buch");
        buch.transform.SetParent(parent);
        BoxCollider col = buch.AddComponent<BoxCollider>();
        col.size = new Vector3(0.18f, 0.08f, 0.24f);

        GameObject geschlossen = GameObject.CreatePrimitive(PrimitiveType.Cube);
        geschlossen.name = "Geschlossen";
        geschlossen.transform.SetParent(buch.transform, false);
        geschlossen.transform.localScale = new Vector3(0.16f, 0.04f, 0.22f);
        Destroy(geschlossen.GetComponent<Collider>());
        geschlossen.GetComponent<Renderer>().material = buchDeckelTextur != null
            ? MakeMaterial(Color.white, buchDeckelTextur)
            : MakeMaterial(new Color(0.38f, 0.12f, 0.10f)); // Platzhalter: dunkelrot

        GameObject offen = new GameObject("Offen");
        offen.transform.SetParent(buch.transform, false);
        offen.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        offen.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject seiten = GameObject.CreatePrimitive(PrimitiveType.Quad);
        seiten.name = "Seiten";
        seiten.transform.SetParent(offen.transform, false);
        seiten.transform.localScale = new Vector3(0.3f, 0.2f, 1f);
        Destroy(seiten.GetComponent<Collider>());
        seiten.GetComponent<Renderer>().material = buchSeitenTextur != null
            ? MakeMaterial(Color.white, buchSeitenTextur)
            : MakeMaterial(new Color(0.93f, 0.88f, 0.74f)); // Platzhalter: creme

        GameObject zifferObj = new GameObject("ZifferText");
        zifferObj.transform.SetParent(offen.transform, false);
        zifferObj.transform.localPosition = new Vector3(0f, 0f, -0.001f);
        TextMesh ziffer = zifferObj.AddComponent<TextMesh>();
        ziffer.text = "2";
        ziffer.anchor = TextAnchor.MiddleCenter;
        ziffer.alignment = TextAlignment.Center;
        ziffer.characterSize = 0.08f;
        ziffer.fontSize = 72;
        ziffer.color = new Color(0.15f, 0.1f, 0.05f);

        offen.SetActive(false);

        PuzzleBook book = buch.AddComponent<PuzzleBook>();
        book.closedVisual = geschlossen;
        book.openVisual = offen;
        return book;
    }

    static Material MakeMaterial(Color c, Texture2D tex = null)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material m = new Material(shader);
        m.color = c;
        if (tex != null) m.mainTexture = tex;
        return m;
    }
}
